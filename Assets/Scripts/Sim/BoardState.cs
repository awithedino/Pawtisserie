using System.Collections.Generic;

namespace Paw.Sim
{
    /// <summary>Owned mutable simulation data; public snapshots are deep copies.</summary>
    public sealed class BoardState
    {
        public int LevelId;
        public int Columns;
        public int Rows;
        public int PastryTypes;
        public bool IsTutorial;
        public TrayItem[] Tray;
        public BoxState[] Lanes = new BoxState[Rules.BoxLanes];
        public List<BoxState> Queue = new List<BoxState>();
        public List<StoredPastry> Pastries = new List<StoredPastry>();
        public List<int> Plates = new List<int>();
        public List<int> Rack = new List<int>();
        public List<int> Stash = new List<int>();
        public List<CardKind> Hand = new List<CardKind>();
        public List<CardKind> Deck = new List<CardKind>();
        public int[] Inventory = new int[3];
        public Outcome Outcome;
        public uint RandomState;
        public int RemovedByExpress;
        public int Delivered;

        public BoardState Clone()
        {
            var clone = (BoardState)MemberwiseClone();
            clone.Tray = new TrayItem[Tray.Length];
            for (int i = 0; i < Tray.Length; i++) clone.Tray[i] = Tray[i]?.Clone();
            clone.Lanes = new BoxState[Lanes.Length];
            for (int i = 0; i < Lanes.Length; i++) clone.Lanes[i] = Lanes[i]?.Clone();
            clone.Queue = Queue.ConvertAll(box => box.Clone());
            clone.Pastries = Pastries.ConvertAll(pastry => pastry.Clone());
            clone.Plates = new List<int>(Plates); clone.Rack = new List<int>(Rack); clone.Stash = new List<int>(Stash);
            clone.Hand = new List<CardKind>(Hand); clone.Deck = new List<CardKind>(Deck);
            clone.Inventory = (int[])Inventory.Clone();
            return clone;
        }
    }
}
