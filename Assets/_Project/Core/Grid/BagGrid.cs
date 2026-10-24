using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class BagGrid
    {
        private readonly int[] _cells;

        public BagGrid(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            Width = width;
            Height = height;
            _cells = new int[width * height];
        }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public int Capacity
        {
            get { return Width * Height; }
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public int GetOccupant(int x, int y)
        {
            if (!InBounds(x, y))
            {
                throw new ArgumentOutOfRangeException(nameof(x), "Cell is outside the grid.");
            }

            return _cells[y * Width + x];
        }

        public bool IsFree(int x, int y)
        {
            return GetOccupant(x, y) == 0;
        }

        public bool CanPlace(ItemShape shape, Cell origin, int ignoreId)
        {
            if (shape == null)
            {
                throw new ArgumentNullException(nameof(shape));
            }

            IReadOnlyList<Cell> cells = shape.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                int x = origin.X + cells[i].X;
                int y = origin.Y + cells[i].Y;
                if (!InBounds(x, y))
                {
                    return false;
                }

                int occupant = _cells[y * Width + x];
                if (occupant != 0 && occupant != ignoreId)
                {
                    return false;
                }
            }

            return true;
        }

        public bool CanPlace(ItemShape shape, Cell origin)
        {
            return CanPlace(shape, origin, 0);
        }

        public void Place(int id, ItemShape shape, Cell origin)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (!CanPlace(shape, origin))
            {
                throw new InvalidOperationException("Shape cannot be placed at " + origin + ".");
            }

            IReadOnlyList<Cell> cells = shape.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                int x = origin.X + cells[i].X;
                int y = origin.Y + cells[i].Y;
                _cells[y * Width + x] = id;
            }
        }

        public void Clear(int id)
        {
            if (id == 0)
            {
                return;
            }

            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] == id)
                {
                    _cells[i] = 0;
                }
            }
        }

        public int GetOccupiedCount()
        {
            int count = 0;
            for (int i = 0; i < _cells.Length; i++)
            {
                if (_cells[i] != 0)
                {
                    count++;
                }
            }

            return count;
        }

        public List<Cell> GetCells(int id)
        {
            var result = new List<Cell>();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (_cells[y * Width + x] == id)
                    {
                        result.Add(new Cell(x, y));
                    }
                }
            }

            return result;
        }
    }
}
