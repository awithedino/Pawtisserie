using System;

namespace Paw.Runtime
{
    public sealed class SimClock
    {
        private double fraction;
        private bool focused = true;
        private bool skip;
        public long Time { get; private set; }
        public void Reset() { Time = 0; fraction = 0; }
        public void Focus(bool value) { focused = value; skip = value; }
        public long Advance(double seconds)
        {
            if (seconds < 0 || double.IsInfinity(seconds) || double.IsNaN(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (!focused) return Time;
            if (skip) { skip = false; return Time; }
            fraction += seconds * 1000000;
            long whole = (long)Math.Floor(fraction); fraction -= whole; Time = checked(Time + whole); return Time;
        }
    }
}
