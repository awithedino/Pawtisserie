using System;
using System.Collections.Generic;

namespace Paw.Sim
{
    internal static class BoosterEffects
    {
        private const uint ZeroSeedState = 0x6D2B79F5u;

        internal static bool TryUse(Simulator sim, BoosterKind kind, out string reason)
        {
            if (!sim.CanCommand(out reason)) return false;
            var board = sim.Model;
            if (board.IsTutorial) { reason = "Boosters are disabled in the tutorial."; return false; }
            if ((int)kind < 0 || (int)kind >= board.Inventory.Length) { reason = "Unknown booster."; return false; }
            if (board.Inventory[(int)kind] == 0) { reason = "No booster inventory."; return false; }
            bool used;
            switch (kind)
            {
                case BoosterKind.Shuffle: used = Shuffle(sim); break;
                case BoosterKind.CallBox: used = CallBox(sim); break;
                case BoosterKind.Stash: used = Stash(sim); break;
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
            if (!used) { reason = "Booster has no eligible target."; return false; }
            board.Inventory[(int)kind]--; sim.Emit(SimEventKind.BoosterUsed, (int)kind);
            StoragePump.Run(sim); return true;
        }

        private static int Next(BoardState board, int count)
        {
            uint state = board.RandomState == 0 ? ZeroSeedState : board.RandomState;
            state ^= state << 13; state ^= state >> 17; state ^= state << 5;
            board.RandomState = state;
            return (int)(state % (uint)count);
        }

        private static bool Shuffle(Simulator sim)
        {
            var board = sim.Model; var config = sim.Config;
            if (board.Hand.Count == 0) return false;
            var original = board.Hand.ToArray();
            int weight = checked(config.PopWeight + config.SweepWeight + config.DrillWeight + config.BoxWeight);
            for (int attempt = 0; attempt < config.ShuffleAttempts; attempt++)
            {
                bool changed = false;
                for (int i = 0; i < board.Hand.Count; i++)
                {
                    int roll = Next(board, weight);
                    var card = roll < config.PopWeight ? CardKind.Pop :
                        roll < config.PopWeight + config.SweepWeight ? CardKind.Sweep :
                        roll < config.PopWeight + config.SweepWeight + config.DrillWeight ? CardKind.Drill : CardKind.Box;
                    board.Hand[i] = card; changed |= card != original[i];
                }
                if (changed) break;
            }
            return true;
        }

        private static bool CallBox(Simulator sim)
        {
            var board = sim.Model;
            int index = CallBoxPicker.FindQueueIndex(board);
            if (index < 0) return false;
            var next = board.Queue[index]; board.Queue.RemoveAt(index);
            for (int lane = 0; lane < board.Lanes.Length; lane++)
            {
                var box = board.Lanes[lane];
                if (box == null || (box.Phase == BoxPhase.Ready && box.Filled == 0 && box.Reserved == 0))
                { sim.SwapIn(lane, next); return true; }
            }
            board.Queue.Insert(0, next); sim.Emit(SimEventKind.BoxCalled, next.Id); return true;
        }

        private static bool Stash(Simulator sim)
        {
            var board = sim.Model;
            bool moved = false;
            foreach (int id in board.Plates.ToArray())
            {
                var pastry = sim.FindPastry(id);
                if (!pastry.Landed) continue;
                board.Plates.Remove(id); board.Stash.Add(id); pastry.Destination = Destination.Stash;
                sim.Emit(SimEventKind.Stashed, id); moved = true;
            }
            return moved;
        }
    }
}
