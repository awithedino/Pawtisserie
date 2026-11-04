namespace Paw.Sim
{
    public sealed class BoxState
    {
        public int Id;
        public int Type;
        public int Need = Rules.BoxCapacity;
        // Reserved includes landed pastries and all promised arrivals.
        public int Reserved;
        public int Filled;
        public BoxPhase Phase;
        public CardKind? Gift;
        public BoxState Clone() => (BoxState)MemberwiseClone();
    }
}
