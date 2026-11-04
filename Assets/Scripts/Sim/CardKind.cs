namespace Paw.Sim
{
    /// <summary>The cat's move cards. Values are stored in level data: append only, never reorder.</summary>
    public enum CardKind
    {
        Pop = 0,
        Sweep = 1,
        Drill = 2,
        Box = 3,
    }

    /// <summary>String ids used in level data (GDD section 6.1).</summary>
    public static class CardIds
    {
        public static string ToId(CardKind kind)
        {
            switch (kind)
            {
                case CardKind.Pop: return "pop";
                case CardKind.Sweep: return "sweep";
                case CardKind.Drill: return "drill";
                case CardKind.Box: return "box";
                default: return null;
            }
        }

        public static bool TryParse(string id, out CardKind kind)
        {
            switch (id)
            {
                case "pop": kind = CardKind.Pop; return true;
                case "sweep": kind = CardKind.Sweep; return true;
                case "drill": kind = CardKind.Drill; return true;
                case "box": kind = CardKind.Box; return true;
                default: kind = CardKind.Pop; return false;
            }
        }
    }
}
