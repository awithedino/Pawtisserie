namespace Paw.Runtime
{
    public enum HapticCue { Select, DragStart, AimMove, Drop, Explosion }
    public interface IHapticCues { void Play(HapticCue cue); }
}
