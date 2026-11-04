using System;

namespace Paw.Presentation
{
    /// <summary>Calculates the slot positions for a hand of cards.</summary>
    public static class FanLayout
    {
        public static void Slot(int index, int count, double preferredStep, double availableWidth, double dip, double tilt,
            out Point2 position, out double angle)
        {
            if (count < 1 || index < 0 || index >= count || preferredStep < 0 || availableWidth < 0 || dip < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            double offset = index - (count - 1) * .5;
            double step = count == 1 ? 0 : Math.Min(preferredStep, availableWidth / (count - 1));
            position = new Point2(offset * step, -dip * offset * offset);
            angle = -tilt * offset;
        }
    }
}
