using Paw.Sim;

namespace Paw.Presentation
{
    public sealed class AimState
    {
        public int SelectedIndex { get; private set; } = -1;
        public Cell? Target { get; private set; }
        public void Cancel() { SelectedIndex = -1; Target = null; }
        public void Select(int index)
        {
            if (index < 0 || index == SelectedIndex) { Cancel(); return; }
            SelectedIndex = index;
        }
        public void Hover(Cell? target)
        {
            if (SelectedIndex >= 0) Target = target;
        }
        public bool Tap(Cell target)
        {
            if (SelectedIndex < 0) return false;
            bool drop = Target.HasValue && Target.Value.Equals(target); Target = target;
            return drop;
        }
        public bool MouseClick(Cell target)
        {
            if (SelectedIndex < 0) return false;
            Target = target; return true;
        }
    }
}
