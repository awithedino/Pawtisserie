using System;

namespace Paw.Sim
{
    internal static class DestinationPicker
    {
        internal static BoxState MatchingBox(BoardState board, int type)
        {
            foreach (var box in board.Lanes)
                if (box != null && box.Phase == BoxPhase.Ready && box.Type == type && box.Reserved < box.Need) return box;
            return null;
        }

        internal static void Launch(Simulator sim, StoredPastry pastry)
        {
            var board = sim.Model;
            var box = MatchingBox(board, pastry.Type);
            if (box != null) Send(sim, pastry, Destination.Box, box.Id, sim.Config.Flight);
            else if (board.Plates.Count < Rules.Plates) Send(sim, pastry, Destination.Plate, 0, sim.Config.Flight);
            else if (board.Rack.Count < Rules.RackSlots) Send(sim, pastry, Destination.Rack, 0, sim.Config.Flight);
            else
            {
                if (sim.ExpressActive) { Send(sim, pastry, Destination.Express, 0, sim.Config.Flight); return; }
                Send(sim, pastry, Destination.Overflow, 0, sim.Config.Flight);
                sim.Fail(Outcome.Overflow);
            }
        }

        internal static void Send(Simulator sim, StoredPastry pastry, Destination destination, int boxId, long duration)
        {
            var board = sim.Model;
            board.Plates.Remove(pastry.Id); board.Rack.Remove(pastry.Id); board.Stash.Remove(pastry.Id);
            sim.CancelArrival(pastry.Id);
            pastry.Destination = destination; pastry.BoxId = boxId; pastry.Landed = false;
            pastry.FlightVersion++;
            pastry.Arrival = checked(sim.Time + duration);
            if (destination == Destination.Box)
            {
                var box = sim.FindBox(boxId);
                if (box == null || box.Phase != BoxPhase.Ready || box.Reserved >= box.Need)
                    throw new InvalidOperationException("Cannot reserve this box.");
                box.Reserved++;
            }
            else if (destination == Destination.Plate) board.Plates.Add(pastry.Id);
            else if (destination == Destination.Rack) board.Rack.Add(pastry.Id);
            sim.Emit(SimEventKind.Routed, pastry.Id, (int)destination, targetId: boxId, endTime: pastry.Arrival);
            sim.Schedule("land", duration, pastry.Id, pastry.FlightVersion);
        }

        internal static void Land(Simulator sim, int id, int version)
        {
            var pastry = sim.FindPastry(id);
            if (pastry == null || pastry.FlightVersion != version) return;
            pastry.Landed = true; sim.Emit(SimEventKind.Landed, id, (int)pastry.Destination);
            if (pastry.Destination == Destination.Box)
            {
                var box = sim.FindBox(pastry.BoxId);
                if (box == null) throw new InvalidOperationException("Flight lost its reserved box.");
                box.Filled++; sim.Model.Delivered++; sim.Model.Pastries.Remove(pastry);
                if (box.Filled == box.Need) sim.Close(box);
            }
            else if (pastry.Destination == Destination.Express)
            { sim.Model.RemovedByExpress++; sim.Model.Pastries.Remove(pastry); }
        }
    }
}
