namespace Paw.Sim
{
    /// <summary>A timestamped state transition; routes retain intermediate storage destinations.</summary>
    public readonly struct SimEvent
    {
        public long Time { get; }
        public SimEventKind Kind { get; }
        public long EndTime { get; }
        public int EntityId { get; }
        public int Value { get; }
        public int TargetId { get; }
        public Cell Cell { get; }
        public SimEvent(long time, SimEventKind kind, int entityId, int value, Cell cell, int targetId = 0, long endTime = -1)
        { Time = time; Kind = kind; EntityId = entityId; Value = value; Cell = cell; TargetId = targetId; EndTime = endTime < 0 ? time : endTime; }
        public override string ToString() => Time + ":" + Kind + ":" + EntityId + ":" + Value + ":" + Cell + ":" + TargetId + ":" + EndTime;
    }
}
