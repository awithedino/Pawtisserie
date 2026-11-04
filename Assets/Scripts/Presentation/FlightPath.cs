using System;

namespace Paw.Presentation
{
    public readonly struct FlightPath
    {
        public Point2 Start { get; }
        public Point2 End { get; }
        public long StartTime { get; }
        public long EndTime { get; }
        public double ArcHeight { get; }
        public FlightPath(Point2 start, Point2 end, long startTime, long endTime, double arcHeight)
        {
            if (endTime < startTime || double.IsNaN(arcHeight) || double.IsInfinity(arcHeight))
                throw new ArgumentException("Invalid flight interval or arc.");
            Start = start; End = end; StartTime = startTime; EndTime = endTime; ArcHeight = arcHeight;
        }
        public Point2 At(long time)
        {
            double t = EndTime == StartTime ? (time >= EndTime ? 1 : 0) :
                Math.Max(0, Math.Min(1, (double)(time - StartTime) / (EndTime - StartTime)));
            return new Point2(Start.X + (End.X - Start.X) * t,
                Start.Y + (End.Y - Start.Y) * t + 4 * ArcHeight * t * (1 - t));
        }
        public FlightPath Retarget(Point2 end, long time, long endTime, double height)
            => new FlightPath(At(time), end, time, endTime, height);
    }
}
