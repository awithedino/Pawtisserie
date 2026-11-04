using System;

namespace Paw.Sim
{
    public static class CallBoxPicker
    {
        public static int FindQueueIndex(BoardState board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            int chosen = -1, most = 0;
            for (int type = 0; type < board.PastryTypes; type++)
            {
                bool open = false, queued = false;
                foreach (var box in board.Lanes)
                    if (box != null && box.Type == type && box.Phase == BoxPhase.Ready) open = true;
                foreach (var box in board.Queue) if (box.Type == type) queued = true;
                if (open || !queued) continue;
                int waiting = 0;
                foreach (var pastry in board.Pastries)
                    if (pastry.Type == type && (pastry.Destination == Destination.Stash ||
                        pastry.Destination == Destination.Plate || pastry.Destination == Destination.Rack)) waiting++;
                if (waiting > most) { most = waiting; chosen = type; }
            }
            return board.Queue.FindIndex(box => box.Type == chosen);
        }
    }
}
