using System;
using Paw.Sim;

namespace Paw.Presentation
{
    /// <summary>Shared logical-space mapping for sprite centers and mouse hit tests.</summary>
    public readonly struct BoardMapping
    {
        public Point2 TopLeft { get; }
        public double CellSize { get; }
        public int Columns { get; }
        public int Rows { get; }
        public BoardMapping(Point2 topLeft, double cellSize, int columns, int rows)
        {
            if (cellSize <= 0 || double.IsInfinity(cellSize) || double.IsNaN(cellSize) || columns <= 0 || rows <= 0)
                throw new ArgumentOutOfRangeException(nameof(cellSize));
            TopLeft = topLeft; CellSize = cellSize; Columns = columns; Rows = rows;
        }
        public Point2 Center(Cell cell) => new Point2(TopLeft.X + (cell.Column + .5) * CellSize, TopLeft.Y - (cell.Row + .5) * CellSize);
        public bool TryCell(Point2 point, out Cell cell)
        {
            double x = (point.X - TopLeft.X) / CellSize, y = (TopLeft.Y - point.Y) / CellSize;
            if (x < 0 || x >= Columns || y < 0 || y >= Rows) { cell = default; return false; }
            cell = new Cell((int)x, (int)y); return true;
        }
    }
}
