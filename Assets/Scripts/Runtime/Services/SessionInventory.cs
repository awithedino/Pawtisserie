using UnityEngine;

namespace Paw.Runtime
{
    /// <summary>Holds the booster wallet across scene loads in one Play session.</summary>
    public static class SessionInventory
    {
        private static BoosterWallet wallet;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Reset() { wallet = null; }
        public static BoosterWallet GetOrCreate(int[] starting) => wallet ?? (wallet = new BoosterWallet(starting));
    }
}
