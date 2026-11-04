namespace Paw.Sim
{
    internal static class MoveResolver
    {
        internal static void Impact(Simulator sim, CardKind kind, Cell target)
        {
            var board = sim.Model;
            sim.Emit(SimEventKind.Impact, value: (int)kind, cell: target);
            foreach (var cell in CardShapes.GetCells(kind, target, board.Columns, board.Rows))
            {
                int index = cell.Row * board.Columns + cell.Column;
                var item = board.Tray[index];
                if (item == null) continue;
                if (item.Cloche) { item.Cloche = false; sim.Emit(SimEventKind.ClocheBroken, item.Id, cell: cell); }
                else if (item.IsCan)
                {
                    if (item.Primed) continue;
                    item.Primed = true; sim.Emit(SimEventKind.CanPrimed, item.Id, cell: cell, endTime: sim.Time + sim.Config.CanDelay);
                    sim.Schedule("can", sim.Config.CanDelay, item.Id);
                }
                else
                {
                    board.Tray[index] = null;
                    var pastry = new StoredPastry { Id = item.Id, Type = item.Type };
                    board.Pastries.Add(pastry); sim.Emit(SimEventKind.PastryLaunched, item.Id, item.Type, cell);
                    DestinationPicker.Launch(sim, pastry);
                }
            }
            sim.Schedule("gravity", sim.Config.GravityDelay);
        }

        internal static void Explode(Simulator sim, int id)
        {
            var board = sim.Model;
            for (int i = 0; i < board.Tray.Length; i++)
            {
                var item = board.Tray[i];
                if (item == null || item.Id != id || !item.IsCan) continue;
                board.Tray[i] = null; sim.Emit(SimEventKind.CanExploded, id, cell: item.Cell);
                Impact(sim, CardKind.Pop, item.Cell); return;
            }
        }

        internal static void Gravity(Simulator sim)
        {
            var board = sim.Model;
            bool moved = false;
            for (int x = 0; x < board.Columns; x++)
            {
                int destination = board.Rows - 1;
                for (int y = board.Rows - 1; y >= 0; y--)
                {
                    int index = y * board.Columns + x;
                    var item = board.Tray[index];
                    if (item == null) continue;
                    if (y != destination)
                    {
                        board.Tray[index] = null; item.Cell = new Cell(x, destination);
                        board.Tray[destination * board.Columns + x] = item;
                        sim.Emit(SimEventKind.TrayFell, item.Id, y, item.Cell, endTime: sim.Time + sim.Config.GravityTravel); moved = true;
                    }
                    destination--;
                }
            }
            if (moved) sim.Schedule("fall-end", sim.Config.GravityTravel);
        }
    }
}
