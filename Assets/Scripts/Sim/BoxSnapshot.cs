namespace Paw.Sim
{
    public readonly struct BoxSnapshot
    {
        public bool Exists { get; }
        public int Id { get; }
        public int Type { get; }
        public int Need { get; }
        public int Reserved { get; }
        public int Filled { get; }
        public BoxPhase Phase { get; }
        public CardKind? Gift { get; }
        internal BoxSnapshot(BoxState box)
        {
            Exists = box != null; Id = box == null ? 0 : box.Id; Type = box == null ? 0 : box.Type;
            Need = box == null ? 0 : box.Need; Reserved = box == null ? 0 : box.Reserved;
            Filled = box == null ? 0 : box.Filled; Phase = box == null ? default : box.Phase; Gift = box?.Gift;
        }
    }
}
