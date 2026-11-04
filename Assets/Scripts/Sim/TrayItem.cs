namespace Paw.Sim
{
    public sealed class TrayItem
    {
        public int Id;
        public int Type;
        public bool Cloche;
        public bool Primed;
        public Cell Cell;
        public bool IsCan => Type == -1;
        public TrayItem Clone() => (TrayItem)MemberwiseClone();
    }
}
