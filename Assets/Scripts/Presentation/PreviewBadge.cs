using Paw.Sim;

namespace Paw.Presentation
{
    public readonly struct PreviewBadge
    {
        public Cell Cell { get; }
        public int EntityId { get; }
        public PreviewBadgeKind Kind { get; }
        public PreviewBadge(Cell cell, int id, PreviewBadgeKind kind) { Cell = cell; EntityId = id; Kind = kind; }
    }
}
