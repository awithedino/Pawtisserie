using Paw.Sim;

namespace Paw.Presentation
{
    /// <summary>Describes the current booster button without deciding game rules.</summary>
    public readonly struct BoosterButtonState
    {
        public int Count { get; }
        public bool ShowsAd => Count == 0;
        public bool Interactable { get; }
        public bool CanUse { get; }
        public BoosterButtonState(int count, Outcome outcome, bool settled, bool tutorial, bool panelVisible, bool adsEnabled)
        {
            Count = count;
            Interactable = outcome == Outcome.Playing && !tutorial && !panelVisible && (count > 0 || adsEnabled);
            CanUse = Interactable && (count == 0 || settled);
        }
    }
}
