using System;

namespace BitRunner.Core
{
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int X;
        public readonly int Y;

        public Cell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Cell Zero
        {
            get { return new Cell(0, 0); }
        }

        public Cell Offset(int dx, int dy)
        {
            return new Cell(X + dx, Y + dy);
        }

        public bool Equals(Cell other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is Cell other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override string ToString()
        {
            return "(" + X + "," + Y + ")";
        }

        public static bool operator ==(Cell left, Cell right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Cell left, Cell right)
        {
            return !left.Equals(right);
        }
    }
}
