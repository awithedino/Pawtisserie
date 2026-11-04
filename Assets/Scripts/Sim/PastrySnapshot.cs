namespace Paw.Sim
{
    public readonly struct PastrySnapshot
    {
        public int Id { get; }
        public int Type { get; }
        public Destination Destination { get; }
        public int BoxId { get; }
        public bool Landed { get; }
        public long Arrival { get; }
        public int FlightVersion { get; }
        internal PastrySnapshot(StoredPastry pastry)
        {
            Id = pastry.Id; Type = pastry.Type; Destination = pastry.Destination; BoxId = pastry.BoxId;
            Landed = pastry.Landed; Arrival = pastry.Arrival; FlightVersion = pastry.FlightVersion;
        }
    }
}
