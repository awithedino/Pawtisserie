namespace Paw.Sim
{
    internal static class BoxReconciler
    {
        internal static void Run(Simulator sim)
        {
            var board = sim.Model;
            for (int type = 0; type < board.PastryTypes; type++)
            {
                int supply = 0, room = 0;
                foreach (var item in board.Tray) if (item != null && item.Type == type) supply++;
                foreach (var pastry in board.Pastries)
                    if (pastry.Type == type && (pastry.Destination == Destination.Plate || pastry.Destination == Destination.Rack || pastry.Destination == Destination.Stash)) supply++;
                foreach (var box in board.Lanes)
                    if (box != null && box.Type == type && (box.Phase == BoxPhase.Ready || box.Phase == BoxPhase.Arriving)) room += box.Need - box.Reserved;
                foreach (var box in board.Queue) if (box.Type == type) room += box.Need;
                int excess = room - supply;
                for (int i = board.Queue.Count - 1; i >= 0 && excess > 0; i--)
                {
                    var box = board.Queue[i];
                    if (box.Type != type || excess < box.Need) continue;
                    board.Queue.RemoveAt(i); excess -= box.Need;
                    sim.Emit(SimEventKind.BoxRemoved, box.Id); TransferGift(sim, box);
                }
                foreach (var box in board.Lanes)
                {
                    if (excess <= 0) break;
                    if (box == null || box.Type != type || (box.Phase != BoxPhase.Ready && box.Phase != BoxPhase.Arriving)) continue;
                    int reduction = System.Math.Min(excess, box.Need - box.Reserved);
                    if (reduction <= 0) continue;
                    box.Need -= reduction; excess -= reduction; sim.Emit(SimEventKind.BoxResized, box.Id, box.Need);
                    if (box.Need == 0) { TransferGift(sim, box); sim.LeaveEmpty(box); }
                    else if (box.Filled == box.Need) sim.Close(box);
                }
                foreach (var box in board.Queue)
                {
                    if (excess <= 0) break;
                    if (box.Type != type) continue;
                    int reduction = System.Math.Min(excess, box.Need);
                    box.Need -= reduction; excess -= reduction;
                    sim.Emit(SimEventKind.BoxResized, box.Id, box.Need);
                }
            }
        }

        private static void TransferGift(Simulator sim, BoxState removed)
        {
            if (!removed.Gift.HasValue) return;
            var board = sim.Model;
            foreach (var box in board.Lanes)
                if (box != null && box != removed && box.Need > 0 && box.Phase != BoxPhase.Leaving && !box.Gift.HasValue)
                { box.Gift = removed.Gift; removed.Gift = null; sim.Emit(SimEventKind.GiftTransferred, removed.Id, box.Id); return; }
            foreach (var box in board.Queue)
                if (!box.Gift.HasValue)
                { box.Gift = removed.Gift; removed.Gift = null; sim.Emit(SimEventKind.GiftTransferred, removed.Id, box.Id); return; }
            sim.PayGift(removed);
        }
    }
}
