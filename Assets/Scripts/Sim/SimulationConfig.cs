using System;

namespace Paw.Sim
{
    /// <summary>Durations are integer microseconds; visual-dependent timings are supplied explicitly.</summary>
    public sealed class SimulationConfig
    {
        public long PopLanding;
        public long DrillLanding;
        public long BoxLanding;
        public long GravityTravel;
        public long SweepLanding = 320000;
        public long ImpactDelay = 100000;
        public long GravityDelay = 120000;
        public long CanDelay = 200000;
        public long Flight = 580000;
        public long BoxClose = 300000;
        public long BoxDeparture = 710000;
        public long BoxArrival = 580000;
        public long RackHop = 340000;
        public long ExpressMinimum = 900000;
        public int PopWeight = 3;
        public int SweepWeight = 2;
        public int DrillWeight = 2;
        public int BoxWeight = 1;
        public int ShuffleAttempts = 10;

        public SimulationConfig(long popLanding, long drillLanding, long boxLanding, long gravityTravel)
        {
            PopLanding = popLanding; DrillLanding = drillLanding; BoxLanding = boxLanding; GravityTravel = gravityTravel;
        }

        public SimulationConfig Clone() => (SimulationConfig)MemberwiseClone();

        public void Validate()
        {
            foreach (long duration in new[] { PopLanding, DrillLanding, BoxLanding, GravityTravel, SweepLanding,
                ImpactDelay, GravityDelay, CanDelay, Flight, BoxClose, BoxDeparture, BoxArrival, RackHop, ExpressMinimum })
                if (duration < 0) throw new ArgumentException("Simulation durations must be nonnegative.");
            long weight = (long)PopWeight + SweepWeight + DrillWeight + BoxWeight;
            if (PopWeight < 0 || SweepWeight < 0 || DrillWeight < 0 || BoxWeight < 0 || weight <= 0 ||
                weight > int.MaxValue || ShuffleAttempts <= 0)
                throw new ArgumentException("Shuffle weights and retry limit are invalid.");
        }
    }
}
