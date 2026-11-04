namespace Paw.Sim
{
    internal static class StoragePump
    {
        internal static void Run(Simulator sim)
        {
            var board = sim.Model;
            foreach (var storage in new[] { board.Stash, board.Plates, board.Rack })
                foreach (int id in storage.ToArray())
                {
                    var pastry = sim.FindPastry(id);
                    if (pastry == null || !pastry.Landed) continue;
                    var box = DestinationPicker.MatchingBox(board, pastry.Type);
                    if (box != null) DestinationPicker.Send(sim, pastry, Destination.Box, box.Id, sim.Config.Flight);
                }
            while (board.Plates.Count < Rules.Plates && board.Rack.Count > 0)
            {
                var pastry = sim.FindPastry(board.Rack[0]);
                long duration = pastry.Landed ? sim.Config.RackHop : System.Math.Max(0, pastry.Arrival - sim.Time);
                DestinationPicker.Send(sim, pastry, Destination.Plate, 0, duration);
            }
        }
    }
}
