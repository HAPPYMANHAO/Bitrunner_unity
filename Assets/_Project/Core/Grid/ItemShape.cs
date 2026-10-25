using System;
using System.Collections.Generic;
using System.Text;

namespace BitRunner.Core
{
    public sealed class ItemShape
    {
        private readonly Cell[] _cells;
        private readonly int _width;
        private readonly int _height;

        public ItemShape(IEnumerable<Cell> cells)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            var unique = new HashSet<Cell>();
            foreach (Cell cell in cells)
            {
                if (cell.X < 0 || cell.Y < 0)
                {
                    throw new ArgumentException("Shape cells must be non-negative.", nameof(cells));
                }

                unique.Add(cell);
            }

            if (unique.Count == 0)
            {
                throw new ArgumentException("Shape must contain at least one cell.", nameof(cells));
            }

            _cells = Normalize(unique, out _width, out _height);
            EnsureConnected(_cells);
        }

        public IReadOnlyList<Cell> Cells
        {
            get { return _cells; }
        }

        public int Width
        {
            get { return _width; }
        }

        public int Height
        {
            get { return _height; }
        }

        public int CellCount
        {
            get { return _cells.Length; }
        }

        public bool Contains(Cell cell)
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] == cell)
                {
                    return true;
                }
            }

            return false;
        }

        public ItemShape Rotate(int quarterTurns)
        {
            int turns = ((quarterTurns % 4) + 4) % 4;
            ItemShape result = this;
            for (int i = 0; i < turns; i++)
            {
                result = result.RotateClockwise();
            }

            return result;
        }

        public ItemShape RotateClockwise()
        {
            var rotated = new Cell[_cells.Length];
            for (int i = 0; i < _cells.Length; i++)
            {
                Cell cell = _cells[i];
                rotated[i] = new Cell(_height - 1 - cell.Y, cell.X);
            }

            return new ItemShape(rotated);
        }

        /// <summary>
        /// 形状内的一点随形状顺时针旋转 90°，与 RotateClockwise 使用同一映射。
        /// 拖拽旋转时用它重算抓取偏移，保证抓取格始终留在光标下。
        /// </summary>
        public Cell RotateOffsetClockwise(Cell offset)
        {
            return new Cell(_height - 1 - offset.Y, offset.X);
        }

        public string ToPattern()
        {
            var builder = new StringBuilder();
            for (int y = 0; y < _height; y++)
            {
                if (y > 0)
                {
                    builder.Append('\n');
                }

                for (int x = 0; x < _width; x++)
                {
                    builder.Append(Contains(new Cell(x, y)) ? '#' : '.');
                }
            }

            return builder.ToString();
        }

        public static ItemShape Parse(params string[] rows)
        {
            if (rows == null || rows.Length == 0)
            {
                throw new ArgumentException("At least one row is required.", nameof(rows));
            }

            int height = rows.Length;
            int width = rows[0].Length;
            if (width == 0)
            {
                throw new ArgumentException("Rows must not be empty.", nameof(rows));
            }

            var cells = new List<Cell>();
            for (int y = 0; y < height; y++)
            {
                string row = rows[y];
                if (row.Length != width)
                {
                    throw new ArgumentException("All rows must have the same length.", nameof(rows));
                }

                for (int x = 0; x < width; x++)
                {
                    char value = row[x];
                    if (value == '#')
                    {
                        cells.Add(new Cell(x, y));
                    }
                    else if (value != '.')
                    {
                        throw new ArgumentException("Rows may only contain '#' and '.'.", nameof(rows));
                    }
                }
            }

            return new ItemShape(cells);
        }

        private static Cell[] Normalize(HashSet<Cell> cells, out int width, out int height)
        {
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (Cell cell in cells)
            {
                if (cell.X < minX)
                {
                    minX = cell.X;
                }

                if (cell.Y < minY)
                {
                    minY = cell.Y;
                }

                if (cell.X > maxX)
                {
                    maxX = cell.X;
                }

                if (cell.Y > maxY)
                {
                    maxY = cell.Y;
                }
            }

            width = maxX - minX + 1;
            height = maxY - minY + 1;

            var normalized = new Cell[cells.Count];
            int index = 0;
            foreach (Cell cell in cells)
            {
                normalized[index] = new Cell(cell.X - minX, cell.Y - minY);
                index++;
            }

            Array.Sort(normalized, CompareCells);
            return normalized;
        }

        private static int CompareCells(Cell a, Cell b)
        {
            int byRow = a.Y.CompareTo(b.Y);
            return byRow != 0 ? byRow : a.X.CompareTo(b.X);
        }

        private static void EnsureConnected(Cell[] cells)
        {
            var remaining = new HashSet<Cell>(cells);
            var queue = new Queue<Cell>();
            queue.Enqueue(cells[0]);
            remaining.Remove(cells[0]);

            while (queue.Count > 0)
            {
                Cell current = queue.Dequeue();
                TryVisit(remaining, queue, new Cell(current.X + 1, current.Y));
                TryVisit(remaining, queue, new Cell(current.X - 1, current.Y));
                TryVisit(remaining, queue, new Cell(current.X, current.Y + 1));
                TryVisit(remaining, queue, new Cell(current.X, current.Y - 1));
            }

            if (remaining.Count > 0)
            {
                throw new ArgumentException("Shape cells must be connected.");
            }
        }

        private static void TryVisit(HashSet<Cell> remaining, Queue<Cell> queue, Cell candidate)
        {
            if (remaining.Remove(candidate))
            {
                queue.Enqueue(candidate);
            }
        }
    }
}
