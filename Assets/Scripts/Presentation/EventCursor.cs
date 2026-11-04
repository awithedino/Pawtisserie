using System;
using Paw.Sim;

namespace Paw.Presentation
{
    public sealed class EventCursor
    {
        private Simulator source;
        public int Position { get; private set; }
        public void Reset(Simulator simulation)
        {
            source = simulation ?? throw new ArgumentNullException(nameof(simulation)); Position = 0;
        }
        public bool TryRead(out SimEvent item)
        {
            if (source == null || Position >= source.EventCount) { item = default; return false; }
            item = source.GetEvent(Position++); return true;
        }
    }
}
