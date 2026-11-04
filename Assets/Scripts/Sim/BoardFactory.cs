using System;

namespace Paw.Sim
{
    public static class BoardFactory
    {
        public static BoardState Create(LevelDefinition level, uint seed, int[] inventory)
        {
            if (level == null) throw new ArgumentNullException(nameof(level));
            level.Validate();
            if (inventory == null || inventory.Length != 3 || Array.Exists(inventory, value => value < 0))
                throw new ArgumentException("Supply three nonnegative booster counts.", nameof(inventory));
            var state = new BoardState
            {
                LevelId = level.Id, Columns = level.Columns, Rows = level.Rows, PastryTypes = level.PastryTypes,
                IsTutorial = level.IsTutorial, Tray = new TrayItem[level.Layout.Length],
                Inventory = (int[])inventory.Clone(), RandomState = seed
            };
            for (int i = 0; i < level.Layout.Length; i++)
                state.Tray[i] = new TrayItem { Id = i + 1, Type = level.Layout[i],
                    Cell = new Cell(i % level.Columns, i / level.Columns), Cloche = level.Cloches != null && level.Cloches[i] };
            for (int i = 0; i < level.Boxes.Length; i++)
            {
                var box = new BoxState { Id = i + 1, Type = level.Boxes[i], Phase = BoxPhase.Ready };
                if (level.Gifts.TryGetValue(i + 1, out CardKind gift)) box.Gift = gift;
                if (i < Rules.BoxLanes) state.Lanes[i] = box;
                else state.Queue.Add(box);
            }
            foreach (CardKind card in level.Deck)
                if (state.Hand.Count < Rules.HandSize) state.Hand.Add(card); else state.Deck.Add(card);
            return state;
        }
    }
}
