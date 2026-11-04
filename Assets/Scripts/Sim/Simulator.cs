using System;
using System.Collections.Generic;

namespace Paw.Sim
{
    /// <summary>Deterministic event queue shared by execution and prediction.</summary>
    public sealed class Simulator
    {
        private BoardState board;
        private SimulationConfig config;
        private List<ScheduledAction> pending = new List<ScheduledAction>();
        private List<SimEvent> events = new List<SimEvent>();
        private IReadOnlyList<SimEvent> eventView;
        private long sequence;
        private bool express;
        public long Time { get; private set; }
        public bool IsSettled => pending.Count == 0 && !express;
        public BoardState State => board.Clone();
        public IReadOnlyList<SimEvent> Events => eventView ?? (eventView = events.AsReadOnly());
        public int EventCount => events.Count;
        public SimEvent GetEvent(int index) => events[index];
        public int Columns => board.Columns;
        public int Rows => board.Rows;
        public int LevelId => board.LevelId;
        public Outcome CurrentOutcome => board.Outcome;
        public int QueueCount => board.Queue.Count;
        public int PastryCount => board.Pastries.Count;
        public int HandCount => board.Hand.Count;
        public int DeckCount => board.Deck.Count;
        public TrayCellState GetTrayCell(int index) => new TrayCellState(board.Tray[index]);
        public BoxSnapshot GetLane(int index) => new BoxSnapshot(board.Lanes[index]);
        public BoxSnapshot GetQueuedBox(int index) => new BoxSnapshot(board.Queue[index]);
        public PastrySnapshot GetPastry(int index) => new PastrySnapshot(board.Pastries[index]);
        public CardKind GetHandCard(int index) => board.Hand[index];
        public int GetInventory(BoosterKind kind) => board.Inventory[(int)kind];
        public int GetStorageCount(Destination storage) => Storage(storage).Count;
        public int GetStorageId(Destination storage, int index) => Storage(storage)[index];

        private List<int> Storage(Destination destination)
        {
            switch (destination)
            {
                case Destination.Plate: return board.Plates;
                case Destination.Rack: return board.Rack;
                case Destination.Stash: return board.Stash;
                default: throw new ArgumentOutOfRangeException(nameof(destination));
            }
        }
        internal BoardState Model => board;
        internal SimulationConfig Config => config;
        internal bool ExpressActive => express;

        public Simulator(LevelDefinition level, SimulationConfig config, uint seed, int[] inventory)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.Validate(); this.config = config.Clone();
            board = BoardFactory.Create(level, seed, inventory);
        }

        private Simulator() { }

        public Simulator Clone(bool includeHistory = true)
        {
            return new Simulator { board = board.Clone(), config = config.Clone(), Time = Time,
                sequence = sequence, express = express, pending = pending.ConvertAll(action => action.Clone()),
                events = includeHistory ? new List<SimEvent>(events) : new List<SimEvent>() };
        }

        public bool TryPlay(int handIndex, Cell target, out string reason)
        {
            if (!CanCommand(out reason)) return false;
            if (handIndex < 0 || handIndex >= board.Hand.Count) { reason = "Card is not in hand."; return false; }
            if (target.Column < 0 || target.Column >= board.Columns || target.Row < 0 || target.Row >= board.Rows)
            { reason = "Target is outside the tray."; return false; }
            CardKind card = board.Hand[handIndex];
            long landing = card == CardKind.Pop ? config.PopLanding : card == CardKind.Sweep ? config.SweepLanding :
                card == CardKind.Drill ? config.DrillLanding : config.BoxLanding;
            long delay = checked(landing + config.ImpactDelay);
            EnsureCanSchedule(delay);
            board.Hand.RemoveAt(handIndex);
            Emit(SimEventKind.CardPlayed, (int)card, handIndex, target, endTime: Time + delay);
            Deal();
            Schedule("impact", delay, value: (int)card, cell: target);
            return true;
        }

        internal bool CanCommand(out string reason)
        {
            reason = board.Outcome != Outcome.Playing ? "Level is not playing." : !IsSettled ? "Combo is running." : null;
            return reason == null;
        }

        public void AdvanceTo(long time)
        {
            if (time < Time) throw new ArgumentOutOfRangeException(nameof(time), "Cannot rewind simulation time.");
            while (pending.Count > 0 && pending[0].Time <= time)
            {
                var action = pending[0]; pending.RemoveAt(0); Time = action.Time;
                Dispatch(action); StoragePump.Run(this);
                CheckSettled();
            }
            Time = time;
            CheckSettled();
        }

        public void RunUntilSettled(int eventLimit = 100000)
        {
            if (eventLimit <= 0) throw new ArgumentOutOfRangeException(nameof(eventLimit));
            int handled = 0;
            CheckSettled();
            while (pending.Count > 0)
            {
                if (++handled > eventLimit) throw new InvalidOperationException("Simulation event limit exceeded.");
                var action = pending[0]; pending.RemoveAt(0); Time = action.Time;
                Dispatch(action); StoragePump.Run(this); CheckSettled();
            }
        }

        internal void Emit(SimEventKind kind, int id = 0, int value = 0, Cell cell = default, int targetId = 0, long endTime = -1)
            => events.Add(new SimEvent(Time, kind, id, value, cell, targetId, endTime));

        private void EnsureCanSchedule(long delay)
        {
            if (delay < 0 || Time > long.MaxValue - delay) throw new ArgumentOutOfRangeException(nameof(delay));
        }

        internal void Schedule(string kind, long delay, int id = 0, int value = 0, Cell cell = default)
        {
            EnsureCanSchedule(delay);
            var action = new ScheduledAction { Kind = kind, Time = Time + delay, Sequence = sequence++, Id = id, Value = value, Cell = cell };
            int index = pending.Count;
            while (index > 0 && pending[index - 1].Time > action.Time) index--;
            pending.Insert(index, action);
        }

        internal void CancelArrival(int id) => pending.RemoveAll(action => action.Kind == "land" && action.Id == id);

        internal void Deal()
        {
            while (board.Hand.Count < Rules.HandSize && board.Deck.Count > 0)
            {
                var card = board.Deck[0]; board.Deck.RemoveAt(0); board.Hand.Add(card);
                Emit(SimEventKind.CardDealt, (int)card);
            }
        }

        internal void PayGift(BoxState box)
        {
            if (!box.Gift.HasValue) return;
            var gift = box.Gift.Value; box.Gift = null;
            if (board.Outcome != Outcome.Playing) return;
            board.Deck.Add(gift); Emit(SimEventKind.GiftEarned, box.Id, (int)gift); Deal();
        }

        internal BoxState FindBox(int id)
        {
            foreach (var box in board.Lanes) if (box != null && box.Id == id) return box;
            return null;
        }

        internal StoredPastry FindPastry(int id) => board.Pastries.Find(pastry => pastry.Id == id);

        internal void Close(BoxState box)
        {
            if (box.Phase == BoxPhase.Closing || box.Phase == BoxPhase.Leaving) return;
            box.Phase = BoxPhase.Closing; Emit(SimEventKind.BoxClosing, box.Id, box.Need, endTime: Time + config.BoxClose);
            Schedule("leave", config.BoxClose, box.Id);
        }

        internal void LeaveEmpty(BoxState box)
        {
            box.Phase = BoxPhase.Leaving; Emit(SimEventKind.BoxLeaving, box.Id, endTime: Time + config.BoxDeparture);
            Schedule("depart", config.BoxDeparture, box.Id);
        }

        internal void Fail(Outcome outcome)
        {
            if (board.Outcome != Outcome.Playing) return;
            board.Outcome = outcome; Emit(SimEventKind.Outcome, value: (int)outcome);
        }

        private void CheckSettled()
        {
            if (!IsSettled || board.Outcome != Outcome.Playing) return;
            StoragePump.Run(this);
            if (!IsSettled) return;
            BoxReconciler.Run(this);
            if (!IsSettled) return;
            OutcomeChecker.Run(this);
        }

        private void Dispatch(ScheduledAction action)
        {
            switch (action.Kind)
            {
                case "impact": MoveResolver.Impact(this, (CardKind)action.Value, action.Cell); break;
                case "gravity": MoveResolver.Gravity(this); break;
                case "fall-end": Emit(SimEventKind.GravitySettled); break;
                case "can": MoveResolver.Explode(this, action.Id); break;
                case "land": DestinationPicker.Land(this, action.Id, action.Value); break;
                case "ready":
                    var ready = FindBox(action.Id);
                    if (ready != null && ready.Phase == BoxPhase.Arriving)
                    { ready.Phase = BoxPhase.Ready; Emit(SimEventKind.BoxReady, ready.Id, ready.Type); }
                    break;
                case "leave":
                    var leaving = FindBox(action.Id);
                    if (leaving != null) { leaving.Phase = BoxPhase.Leaving; Emit(SimEventKind.BoxLeaving, leaving.Id, endTime: Time + config.BoxDeparture); Schedule("depart", config.BoxDeparture, leaving.Id); }
                    break;
                case "depart": Depart(action.Id); break;
                case "express-end": FinishExpress(); break;
                default: throw new InvalidOperationException("Unknown scheduled action: " + action.Kind);
            }
        }

        private void Depart(int id)
        {
            for (int lane = 0; lane < board.Lanes.Length; lane++)
            {
                var box = board.Lanes[lane];
                if (box == null || box.Id != id) continue;
                Emit(SimEventKind.BoxDeparted, box.Id); PayGift(box); board.Lanes[lane] = null;
                if (board.Queue.Count > 0)
                {
                    var next = board.Queue[0]; board.Queue.RemoveAt(0); board.Lanes[lane] = next;
                    next.Phase = BoxPhase.Arriving; Emit(SimEventKind.BoxArriving, next.Id, lane, endTime: Time + config.BoxArrival);
                    Schedule("ready", config.BoxArrival, next.Id);
                }
                return;
            }
        }

        public bool TryUseBooster(BoosterKind kind, out string reason) => BoosterEffects.TryUse(this, kind, out reason);

        public bool TryRevive(out string reason)
        {
            reason = null;
            if (board.Outcome == Outcome.OutOfMoves && IsSettled)
            {
                board.Hand.Add(CardKind.Box); board.Hand.Add(CardKind.Box);
                board.Outcome = Outcome.Playing; Emit(SimEventKind.Revived, value: (int)Outcome.OutOfMoves); return true;
            }
            if (board.Outcome != Outcome.Overflow && board.Outcome != Outcome.RackNotCleared)
            { reason = "There is no matching revive offer."; return false; }
            board.Outcome = Outcome.Playing; express = true; Emit(SimEventKind.ExpressStarted, endTime: Time + config.ExpressMinimum);
            foreach (var pastry in new List<StoredPastry>(board.Pastries))
                if (pastry.Destination == Destination.Overflow || (pastry.Landed &&
                    (pastry.Destination == Destination.Plate || pastry.Destination == Destination.Rack)))
                    DestinationPicker.Send(this, pastry, Destination.Express, 0, pastry.Landed ? config.RackHop : Math.Max(0, pastry.Arrival - Time));
            Schedule("express-end", config.ExpressMinimum);
            return true;
        }

        private void FinishExpress()
        {
            // Accepted chains and flights must finish before the collector can leave.
            if (pending.Count > 0)
            {
                Schedule("express-end", pending[pending.Count - 1].Time - Time); return;
            }
            express = false; Emit(SimEventKind.ExpressDeparted); BoxReconciler.Run(this);
        }

        internal void SwapIn(int lane, BoxState next)
        {
            var previous = board.Lanes[lane];
            if (previous != null) board.Queue.Insert(0, previous);
            board.Lanes[lane] = next; next.Phase = BoxPhase.Arriving;
            Emit(SimEventKind.BoxArriving, next.Id, lane, endTime: Time + config.BoxArrival); Schedule("ready", config.BoxArrival, next.Id);
        }
    }
}
