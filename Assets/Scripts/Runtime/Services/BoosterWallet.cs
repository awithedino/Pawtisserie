using System;
using Paw.Sim;

namespace Paw.Runtime
{
    /// <summary>Owns booster counts independently of any level.</summary>
    public sealed class BoosterWallet
    {
        private readonly int[] counts;
        public BoosterWallet(int[] initial)
        {
            if (initial == null || initial.Length != 3) throw new ArgumentException("Three booster counts are required.", nameof(initial));
            counts = (int[])initial.Clone();
            foreach (int count in counts) if (count < 0) throw new ArgumentOutOfRangeException(nameof(initial));
        }
        public int[] Snapshot() => (int[])counts.Clone();
        public void CopyFrom(Simulator sim)
        {
            if (sim == null) throw new ArgumentNullException(nameof(sim));
            for (int i = 0; i < counts.Length; i++) counts[i] = sim.GetInventory((BoosterKind)i);
        }
    }
}
