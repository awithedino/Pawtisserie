using System.Collections.Generic;

namespace Paw.Sim
{
    public sealed class Prediction
    {
        public bool Accepted { get; }
        public string Reason { get; }
        public BoardState FinalState { get; }
        public long SettledTime { get; }
        public IReadOnlyList<SimEvent> Events { get; }

        internal Prediction(bool accepted, string reason, BoardState state, long time, List<SimEvent> events)
        {
            Accepted = accepted; Reason = reason; FinalState = state; SettledTime = time; Events = events.AsReadOnly();
        }
    }
}
