using System;
using System.Collections.Generic;

namespace Paw.Sim
{
    /// <summary>The four reference levels, preserving the HTML generator's random-call order.</summary>
    public static class PrototypeLevels
    {
        public const int Count = 4;

        public static LevelDefinition Create(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (index == 0)
                return new LevelDefinition { Id = 0, Columns = 3, Rows = 3, PastryTypes = 2, IsTutorial = true,
                    Layout = new[] { 1, 0, 1, 0, 0, 0, 1, 0, 0 }, Boxes = new[] { 0, 1, 0 },
                    Deck = new[] { CardKind.Pop, CardKind.Sweep, CardKind.Sweep } };
            int columns = index == 1 ? 6 : 7, rows = index == 3 ? 7 : 6;
            int types = index == 1 ? 3 : 4, cans = index == 1 ? 0 : index == 2 ? 3 : 4;
            int cloches = index == 1 ? 0 : index == 2 ? 4 : 8;
            uint random = index == 1 ? 7u : index == 2 ? 19u : 42u;
            int units = (columns * rows - cans) / Rules.BoxCapacity;
            var perType = new int[types];
            var colors = new List<int>();
            for (int type = 0; type < types; type++)
            {
                perType[type] = units / types + (type < units % types ? 1 : 0);
                for (int i = 0; i < perType[type] * Rules.BoxCapacity; i++) colors.Add(type);
            }
            Shuffle(colors, ref random);
            var cells = new List<int>();
            for (int row = 1; row < rows; row++) for (int column = 0; column < columns; column++) cells.Add(row * columns + column);
            Shuffle(cells, ref random);
            var canCells = new HashSet<int>();
            foreach (int cell in cells)
            {
                if (canCells.Count == cans) break;
                int x = cell % columns, y = cell / columns;
                if ((y + 1 < rows && canCells.Contains(cell + columns)) || canCells.Contains(cell - columns) ||
                    (x + 1 < columns && canCells.Contains(cell + 1)) || (x > 0 && canCells.Contains(cell - 1))) continue;
                canCells.Add(cell);
            }
            var layout = new int[columns * rows]; var pastryCells = new List<int>(); int next = 0;
            for (int cell = 0; cell < layout.Length; cell++)
            {
                if (canCells.Contains(cell)) layout[cell] = -1;
                else { layout[cell] = colors[next++]; pastryCells.Add(cell); }
            }
            Shuffle(pastryCells, ref random);
            var covers = new bool[layout.Length];
            for (int i = 0; i < cloches; i++) covers[pastryCells[i]] = true;
            var boxes = new List<int>();
            for (int type = 0; type < types; type++) for (int i = 0; i < perType[type]; i++) boxes.Add(type);
            Shuffle(boxes, ref random);
            for (int k = 1; k < Rules.BoxLanes; k++)
            {
                bool duplicate = false;
                for (int i = 0; i < k; i++) if (boxes[i] == boxes[k]) duplicate = true;
                if (!duplicate) continue;
                for (int j = k + 1; j < boxes.Count; j++)
                {
                    bool seen = false;
                    for (int i = 0; i < k; i++) if (boxes[i] == boxes[j]) seen = true;
                    if (seen) continue;
                    int swap = boxes[k]; boxes[k] = boxes[j]; boxes[j] = swap; break;
                }
            }
            var deck = index == 1 ? new[] { CardKind.Pop, CardKind.Pop, CardKind.Sweep, CardKind.Pop, CardKind.Drill, CardKind.Pop, CardKind.Sweep, CardKind.Pop } :
                index == 2 ? new[] { CardKind.Pop, CardKind.Box, CardKind.Pop, CardKind.Sweep, CardKind.Drill, CardKind.Pop, CardKind.Box, CardKind.Sweep } :
                new[] { CardKind.Pop, CardKind.Sweep, CardKind.Box, CardKind.Pop, CardKind.Drill, CardKind.Box, CardKind.Sweep, CardKind.Pop };
            var gifts = index == 1 ? new Dictionary<int, CardKind> { { 3, CardKind.Sweep }, { 6, CardKind.Pop }, { 9, CardKind.Box }, { 11, CardKind.Pop } } :
                index == 2 ? new Dictionary<int, CardKind> { { 4, CardKind.Drill }, { 7, CardKind.Box }, { 10, CardKind.Pop }, { 12, CardKind.Sweep } } :
                new Dictionary<int, CardKind> { { 3, CardKind.Pop }, { 6, CardKind.Box }, { 9, CardKind.Sweep }, { 12, CardKind.Drill }, { 14, CardKind.Pop } };
            var level = new LevelDefinition { Id = index, Columns = columns, Rows = rows, PastryTypes = types,
                Layout = layout, Cloches = covers, Boxes = boxes.ToArray(), Deck = deck, Gifts = gifts };
            level.Validate(); return level;
        }

        private static void Shuffle<T>(List<T> values, ref uint state)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = (int)(Next(ref state) * (i + 1));
                T swap = values[i]; values[i] = values[j]; values[j] = swap;
            }
        }

        private static double Next(ref uint state)
        {
            unchecked
            {
                state += 0x6D2B79F5u;
                uint t = (state ^ (state >> 15)) * (1u | state);
                t ^= t + (t ^ (t >> 7)) * (61u | t);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
    }
}
