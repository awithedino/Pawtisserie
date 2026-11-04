namespace Paw.Sim
{
    public readonly struct TrayCellState
    {
        public bool Exists { get; }
        public int Id { get; }
        public int Type { get; }
        public Cell Cell { get; }
        public bool Cloche { get; }
        public bool Primed { get; }
        public bool IsCan => Exists && Type == -1;
        internal TrayCellState(TrayItem item)
        {
            Exists = item != null; Id = item == null ? 0 : item.Id; Type = item == null ? 0 : item.Type;
            Cell = item == null ? default : item.Cell; Cloche = item != null && item.Cloche; Primed = item != null && item.Primed;
        }
    }
}
