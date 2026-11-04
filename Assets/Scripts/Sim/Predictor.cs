using System;
using System.Collections.Generic;

namespace Paw.Sim
{
    public static class Predictor
    {
        public static Prediction Predict(Simulator simulation, int handIndex, Cell target)
        {
            if (simulation == null) throw new ArgumentNullException(nameof(simulation));
            var clone = simulation.Clone(includeHistory: false);
            int start = clone.Events.Count;
            bool accepted = clone.TryPlay(handIndex, target, out string reason);
            if (accepted) clone.RunUntilSettled();
            var events = new List<SimEvent>();
            for (int i = start; i < clone.Events.Count; i++) events.Add(clone.Events[i]);
            return new Prediction(accepted, reason, clone.State, clone.Time, events);
        }
    }
}
