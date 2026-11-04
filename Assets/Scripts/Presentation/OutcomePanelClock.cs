using System;
using Paw.Sim;

namespace Paw.Presentation
{
    /// <summary>Determines when the current outcome can be presented.</summary>
    public sealed class OutcomePanelClock
    {
        private Outcome outcome = Outcome.Playing;
        private long outcomeTime = -1;
        private long overflowLanding = -1;
        private long settledTime = -1;

        public void Reset() { outcome = Outcome.Playing; outcomeTime = overflowLanding = settledTime = -1; }

        public void Observe(SimEvent item)
        {
            if (item.Kind == SimEventKind.Revived || item.Kind == SimEventKind.ExpressStarted) { Reset(); return; }
            if (item.Kind == SimEventKind.Outcome)
            {
                outcome = (Outcome)item.Value; outcomeTime = item.Time; settledTime = -1;
                if (outcome != Outcome.Overflow) overflowLanding = -1;
            }
            if (outcome == Outcome.Overflow && item.Kind == SimEventKind.Landed &&
                (Destination)item.Value == Destination.Overflow && overflowLanding < 0)
                overflowLanding = item.Time;
        }

        public bool IsDue(long now, bool settled, long winDelay, long overflowDelay, long failDelay)
        {
            if (now < 0 || winDelay < 0 || overflowDelay < 0 || failDelay < 0) throw new ArgumentOutOfRangeException();
            if (outcome == Outcome.Playing || outcomeTime < 0) return false;
            if (outcome == Outcome.Overflow)
                return overflowLanding >= 0 && now >= overflowLanding && now - overflowLanding >= overflowDelay;
            if (!settled) return false;
            if (settledTime < 0) settledTime = now;
            long delay = outcome == Outcome.Won ? winDelay : failDelay;
            return now >= settledTime && now - settledTime >= delay;
        }
    }
}
