using System;
using System.Collections.Generic;

namespace Paw.Sim
{
    /// <summary>In-memory authored input; -1 in Layout denotes a cream can.</summary>
    public sealed class LevelDefinition
    {
        public int Id;
        public int Columns;
        public int Rows;
        public int PastryTypes;
        public bool IsTutorial;
        public int[] Layout;
        public bool[] Cloches;
        public int[] Boxes;
        public CardKind[] Deck;
        public Dictionary<int, CardKind> Gifts = new Dictionary<int, CardKind>();

        public void Validate()
        {
            if (Id < 0 || Columns <= 0 || Rows <= 0 || (long)Columns * Rows > int.MaxValue)
                throw new ArgumentException("Invalid level id or tray dimensions.");
            if (PastryTypes < 2 || PastryTypes > 4) throw new ArgumentException("Only pastry types 0–3 are defined.");
            if (Layout == null || Layout.Length != Columns * Rows) throw new ArgumentException("Layout must fill the tray.");
            if (Cloches != null && Cloches.Length != Layout.Length) throw new ArgumentException("Cloche layout size differs.");
            if (Boxes == null || Deck == null || Gifts == null) throw new ArgumentException("Boxes, deck and gifts are required.");
            var counts = new int[PastryTypes];
            int cans = 0;
            for (int i = 0; i < Layout.Length; i++)
            {
                int type = Layout[i];
                if (type < -1 || type >= PastryTypes) throw new ArgumentException("Unknown tray content.");
                if (type == -1)
                {
                    cans++;
                    if (Cloches != null && Cloches[i]) throw new ArgumentException("A can cannot have a cloche.");
                }
                else counts[type]++;
            }
            var boxCounts = new int[PastryTypes];
            foreach (int type in Boxes)
            {
                if (type < 0 || type >= PastryTypes) throw new ArgumentException("Unknown box type.");
                boxCounts[type]++;
            }
            for (int type = 0; type < PastryTypes; type++)
                if (counts[type] % Rules.BoxCapacity != 0 || counts[type] / Rules.BoxCapacity != boxCounts[type])
                    throw new ArgumentException("Pastry counts must be multiples of three with exactly matching boxes.");
            foreach (CardKind card in Deck)
                if (CardIds.ToId(card) == null) throw new ArgumentException("Unknown card in deck.");
            foreach (var gift in Gifts)
                if (gift.Key < 1 || gift.Key > Boxes.Length || CardIds.ToId(gift.Value) == null)
                    throw new ArgumentException("Gift arrival numbers must be in the one-based box range.");
            if (IsTutorial)
            {
                if (Id != 0 || Columns != 3 || Rows != 3 || PastryTypes != 2 || cans != 0 ||
                    counts[0] != 6 || counts[1] != 3 || Deck.Length != 3 || Gifts.Count != 0 ||
                    Deck[0] != CardKind.Pop || Deck[1] != CardKind.Sweep || Deck[2] != CardKind.Sweep ||
                    Boxes.Length != 3 || Boxes[0] != 0 || Boxes[1] != 1 || Boxes[2] != 0)
                    throw new ArgumentException("Tutorial requires the Level 0 card, pastry and box counts.");
                if (Cloches != null && Array.Exists(Cloches, value => value))
                    throw new ArgumentException("Tutorial has no cloches.");
            }
            else if (Deck.Length != Rules.StartingCards || Boxes.Length < Rules.BoxLanes ||
                Boxes[0] == Boxes[1] || Boxes[0] == Boxes[2] || Boxes[1] == Boxes[2])
                throw new ArgumentException("Standard levels require eight cards and three different starting boxes.");
        }
    }
}
