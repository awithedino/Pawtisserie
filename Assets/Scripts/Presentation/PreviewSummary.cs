using System;
using System.Collections.Generic;
using Paw.Sim;

namespace Paw.Presentation
{
    public sealed class PreviewSummary
    {
        private readonly List<PreviewBadge> badges = new List<PreviewBadge>();
        public IReadOnlyList<PreviewBadge> Badges { get; }
        public PreviewBadgeKind Risk { get; private set; } = PreviewBadgeKind.Box;

        public PreviewSummary(Prediction prediction)
        {
            if (prediction == null) throw new ArgumentNullException(nameof(prediction));
            Badges = badges.AsReadOnly();
            var origins = new Dictionary<int, Cell>();
            var routed = new HashSet<int>();
            var byCell = new Dictionary<Cell, int>();
            foreach (var item in prediction.Events)
            {
                if (item.Kind == SimEventKind.PastryLaunched) origins[item.EntityId] = item.Cell;
                if (item.Kind == SimEventKind.ClocheBroken) Add(item.Cell, item.EntityId, PreviewBadgeKind.Cloche, byCell);
                if (item.Kind == SimEventKind.CanPrimed) Add(item.Cell, item.EntityId, PreviewBadgeKind.Can, byCell);
                if (item.Kind != SimEventKind.Routed || !origins.TryGetValue(item.EntityId, out Cell cell) || !routed.Add(item.EntityId)) continue;
                var destination = (Destination)item.Value;
                var kind = destination == Destination.Box ? PreviewBadgeKind.Box : destination == Destination.Plate ? PreviewBadgeKind.Plate :
                    destination == Destination.Rack ? PreviewBadgeKind.Rack : PreviewBadgeKind.Overflow;
                Add(cell, item.EntityId, kind, byCell);
                if (kind > Risk) Risk = kind;
            }
        }

        private void Add(Cell cell, int id, PreviewBadgeKind kind, Dictionary<Cell, int> byCell)
        {
            var badge = new PreviewBadge(cell, id, kind);
            // A later launch at the same cell replaces its earlier cloche-only cue.
            if (byCell.TryGetValue(cell, out int index)) badges[index] = badge;
            else { byCell.Add(cell, badges.Count); badges.Add(badge); }
        }
    }
}
