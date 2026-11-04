using System;

namespace Paw.Sim
{
    public readonly struct Cell : IEquatable<Cell>
    {
        public int Column { get; }
        public int Row { get; }
        public Cell(int column, int row) { Column = column; Row = row; }
        public bool Equals(Cell other) => Column == other.Column && Row == other.Row;
        public override bool Equals(object obj) => obj is Cell other && Equals(other);
        public override int GetHashCode() => unchecked(Column * 397 ^ Row);
        public override string ToString() => Column + "," + Row;
    }
}
