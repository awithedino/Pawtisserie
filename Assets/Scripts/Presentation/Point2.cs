using System;

namespace Paw.Presentation
{
    public readonly struct Point2 : IEquatable<Point2>
    {
        public double X { get; }
        public double Y { get; }
        public Point2(double x, double y) { X = x; Y = y; }
        public bool Equals(Point2 other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Point2 other && Equals(other);
        public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();
    }
}
