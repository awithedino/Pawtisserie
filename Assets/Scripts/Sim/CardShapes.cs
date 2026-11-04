using System;
using System.Collections.Generic;

namespace Paw.Sim
{
    public static class CardShapes
    {
        public static List<Cell> GetCells(CardKind kind, Cell target, int columns, int rows)
        {
            if (columns <= 0 || rows <= 0) throw new ArgumentOutOfRangeException(nameof(columns));
            if (target.Column < 0 || target.Column >= columns || target.Row < 0 || target.Row >= rows)
                throw new ArgumentOutOfRangeException(nameof(target));
            var result = new List<Cell>();
            void Add(int x, int y)
            {
                if (x >= 0 && x < columns && y >= 0 && y < rows) result.Add(new Cell(x, y));
            }
            switch (kind)
            {
                case CardKind.Pop:
                    Add(target.Column, target.Row); Add(target.Column, target.Row - 1);
                    Add(target.Column + 1, target.Row); Add(target.Column, target.Row + 1);
                    Add(target.Column - 1, target.Row); break;
                case CardKind.Sweep:
                    for (int x = 0; x < columns; x++) Add(x, target.Row);
                    break;
                case CardKind.Drill:
                    for (int y = 0; y < rows; y++) Add(target.Column, y);
                    break;
                case CardKind.Box:
                    for (int y = target.Row - 1; y <= target.Row + 1; y++)
                        for (int x = target.Column - 1; x <= target.Column + 1; x++) Add(x, y);
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
            return result;
        }
    }
}
