namespace Paw.Sim
{
    public sealed class StoredPastry
    {
        public int Id;
        public int Type;
        public Destination Destination;
        public int BoxId;
        public bool Landed;
        public int FlightVersion;
        public long Arrival;
        public StoredPastry Clone() => (StoredPastry)MemberwiseClone();
    }
}
