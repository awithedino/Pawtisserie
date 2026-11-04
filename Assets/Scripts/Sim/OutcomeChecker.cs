namespace Paw.Sim
{
    internal static class OutcomeChecker
    {
        internal static void Run(Simulator sim)
        {
            var board = sim.Model;
            if (board.Rack.Count > 0) { sim.Fail(Outcome.RackNotCleared); return; }
            bool hasPastry = board.Pastries.Count > 0;
            foreach (var item in board.Tray) if (item != null && !item.IsCan) hasPastry = true;
            if (!hasPastry)
            {
                board.Outcome = Outcome.Won; sim.Emit(SimEventKind.Outcome, value: (int)Outcome.Won);
            }
            else if (board.Hand.Count == 0 && board.Deck.Count == 0) sim.Fail(Outcome.OutOfMoves);
        }
    }
}
