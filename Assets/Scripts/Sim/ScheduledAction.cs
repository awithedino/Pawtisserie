namespace Paw.Sim
{
    internal sealed class ScheduledAction
    {
        internal long Time;
        internal long Sequence;
        internal string Kind;
        internal int Id;
        internal int Value;
        internal Cell Cell;
        internal ScheduledAction Clone() => (ScheduledAction)MemberwiseClone();
    }
}
