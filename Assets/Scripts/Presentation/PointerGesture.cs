using System;
using Paw.Sim;

namespace Paw.Presentation
{
    /// <summary>Turns pointer gestures into card selection, previews and drop intents.</summary>
    public sealed class PointerGesture
    {
        private enum Capture { None, Card, Board }
        private readonly AimState aim = new AimState();
        private Capture capture;
        private int pointer;
        private int card;
        private double downX, downY;
        private bool dragging;
        public IGestureFilter Filter { get; set; }
        public int SelectedIndex => aim.SelectedIndex;
        public Cell? Target => aim.Target;
        public bool IsDragging => capture == Capture.Card && dragging;
        public int DragCard => IsDragging ? card : -1;
        public bool HasCapture => capture != Capture.None;
        public bool OwnsCard(int id, int index) => capture == Capture.Card && pointer == id && card == index;
        public bool OwnsBoard(int id) => capture == Capture.Board && pointer == id;

        public bool CardDown(int id, int index, double x, double y)
        {
            if (capture != Capture.None || index < 0 || (Filter != null && !Filter.AllowsCard(index))) return false;
            capture = Capture.Card; pointer = id; card = index; downX = x; downY = y; dragging = false;
            return true;
        }

        public void CardMove(int id, double x, double y, double threshold, Cell? target, bool canPlay)
        {
            if (capture != Capture.Card || pointer != id) return;
            if (threshold < 0 || double.IsNaN(threshold)) throw new ArgumentOutOfRangeException(nameof(threshold));
            double dx = x - downX, dy = y - downY;
            if (!dragging && canPlay && dx * dx + dy * dy > threshold * threshold)
            {
                dragging = true;
                aim.Cancel();
                aim.Select(card);
            }
            if (dragging) aim.Hover(canPlay ? AllowedTarget(target) : null);
        }

        public bool CardUp(int id, Cell? target, bool canPlay, out int dropCard, out Cell dropCell)
        {
            dropCard = -1; dropCell = default;
            if (capture != Capture.Card || pointer != id) return false;
            if (dragging) target = AllowedTarget(target);
            bool drop = dragging && canPlay && target.HasValue;
            if (drop) { dropCard = card; dropCell = target.Value; aim.Hover(target); }
            else if (!dragging) aim.Select(card);
            else aim.Cancel();
            capture = Capture.None; dragging = false;
            return drop;
        }

        public bool BoardDown(int id, Cell target, bool canPlay)
        {
            if (capture != Capture.None || !canPlay || aim.SelectedIndex < 0 || !AllowedTarget(target).HasValue) return false;
            capture = Capture.Board; pointer = id; aim.Hover(target); return true;
        }

        public void BoardMove(int id, Cell? target)
        {
            if (capture == Capture.Board && pointer == id) aim.Hover(AllowedTarget(target));
        }

        public bool BoardUp(int id, Cell? target, bool canPlay, out int dropCard, out Cell dropCell)
        {
            dropCard = -1; dropCell = default;
            if (capture != Capture.Board || pointer != id) return false;
            target = AllowedTarget(target);
            bool drop = canPlay && target.HasValue && aim.SelectedIndex >= 0;
            if (drop) { dropCard = aim.SelectedIndex; dropCell = target.Value; aim.Hover(target); }
            else aim.Hover(null);
            capture = Capture.None;
            return drop;
        }

        public void Hover(Cell? target, bool canPlay)
        {
            if (capture == Capture.None) aim.Hover(canPlay ? AllowedTarget(target) : null);
        }

        private Cell? AllowedTarget(Cell? target)
        {
            return target.HasValue && (Filter == null || Filter.AllowsTarget(aim.SelectedIndex, target.Value)) ? target : null;
        }
        public void Cancel() { capture = Capture.None; dragging = false; aim.Cancel(); }
    }
}
