using System;
using System.Linq;
using NUnit.Framework;
using Paw.Sim;

namespace Paw.Tests
{
    public class SimulationTests
    {
        internal static SimulationConfig Config() => new SimulationConfig(100000, 100000, 100000, 150000);
        internal static Simulator Create(LevelDefinition level, uint seed = 42)
            => new Simulator(level, Config(), seed, new[] { 5, 5, 5 });

        internal static LevelDefinition Level(int columns, int[] layout, int[] boxes, CardKind card = CardKind.Drill)
        {
            return new LevelDefinition { Id = 1, Columns = columns, Rows = layout.Length / columns,
                PastryTypes = layout.Max() + 1, Layout = layout, Boxes = boxes,
                Deck = Enumerable.Repeat(card, Rules.StartingCards).ToArray() };
        }

        internal static LevelDefinition Waiting(bool doubleType = false)
        {
            return doubleType ? Level(5, new[] { 3, 3, 0, 1, 2, 3, 3, 0, 1, 2, 3, 3, 0, 1, 2 }, new[] { 0, 1, 2, 3, 3 }) :
                Level(4, new[] { 3, 0, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2 }, new[] { 0, 1, 2, 3 });
        }

        internal static LevelDefinition OverflowLevel(CardKind card)
        {
            return Level(6, new[] { 3, 3, 3, 0, 1, 2, 3, 3, 3, 0, 1, 2,
                3, 3, 3, 0, 1, 2, 3, 3, 3, 3, 3, 3 }, new[] { 0, 1, 2, 3, 3, 3, 3, 3 }, card);
        }

        internal static void Play(Simulator sim, int column, int row, int hand = 0)
        {
            Assert.IsTrue(sim.TryPlay(hand, new Cell(column, row), out string reason), reason);
            sim.RunUntilSettled();
        }

        internal static string Transcript(Simulator sim) => string.Join("\n", sim.Events.Select(item => item.ToString()));

        internal static string Snapshot(BoardState b)
        {
            return string.Join("|", b.LevelId, b.Columns, b.Rows, b.RandomState, b.Outcome, b.Delivered, b.RemovedByExpress,
                string.Join(";", b.Tray.Select(p => p == null ? "_" : p.Id + "," + p.Type + "," + p.Cell + "," + p.Cloche + "," + p.Primed)),
                string.Join(";", b.Lanes.Select(p => p == null ? "_" : p.Id + "," + p.Type + "," + p.Need + "," + p.Reserved + "," + p.Filled + "," + p.Phase + "," + p.Gift)),
                string.Join(";", b.Queue.Select(p => p.Id + "," + p.Type + "," + p.Gift)),
                string.Join(";", b.Pastries.Select(p => p.Id + "," + p.Type + "," + p.Destination + "," + p.BoxId + "," + p.Landed + "," + p.FlightVersion + "," + p.Arrival)),
                string.Join(",", b.Plates), string.Join(",", b.Rack), string.Join(",", b.Stash),
                string.Join(",", b.Hand), string.Join(",", b.Deck), string.Join(",", b.Inventory));
        }

        internal static void Conserved(Simulator sim, int initial)
        {
            var b = sim.State;
            Assert.AreEqual(initial, b.Tray.Count(p => p != null && !p.IsCan) + b.Pastries.Count + b.Delivered + b.RemovedByExpress);
            Assert.AreEqual(b.Pastries.Count, b.Pastries.Select(p => p.Id).Distinct().Count());
            Assert.IsTrue(b.Plates.Count <= Rules.Plates && b.Rack.Count <= Rules.RackSlots);
            Assert.IsTrue(b.Lanes.All(box => box == null || (box.Filled <= box.Reserved && box.Reserved <= box.Need)));
        }

        [Test]
        public void ThreeColumnsWinAndFreshRunsHaveIndependentState()
        {
            var level = CoreModelTests.Standard(); var sim = Create(level);
            for (int x = 0; x < 3; x++) { Play(sim, x, 1); Conserved(sim, 9); }
            Assert.AreEqual(Outcome.Won, sim.State.Outcome); Assert.AreEqual(9, sim.State.Delivered);
            Assert.IsTrue(sim.IsSettled);
            Assert.IsFalse(sim.TryPlay(0, new Cell(0, 0), out _));
            Assert.AreEqual(Snapshot(Create(level).State), Snapshot(Create(level).State));
            Assert.AreEqual(0, Create(level).Events.Count);
        }

        [Test]
        public void TutorialScriptWinsUsingItsThreeCards()
        {
            var sim = Create(CoreModelTests.Tutorial());
            Play(sim, 1, 1); Assert.AreEqual(5, sim.State.Delivered);
            Play(sim, 1, 1); Assert.AreEqual(7, sim.State.Delivered);
            Play(sim, 1, 2); Assert.AreEqual(Outcome.Won, sim.State.Outcome);
            Conserved(sim, 9);
        }

        [Test]
        public void RejectedCommandsDoNotConsumeCardsInventoryOrRandomness()
        {
            var sim = Create(CoreModelTests.Standard()); string before = Snapshot(sim.State);
            Assert.IsFalse(sim.TryPlay(-1, new Cell(0, 0), out _));
            Assert.IsFalse(sim.TryPlay(0, new Cell(3, 0), out _));
            Assert.IsFalse(sim.TryUseBooster(BoosterKind.Stash, out _));
            Assert.IsFalse(sim.TryUseBooster(BoosterKind.CallBox, out _));
            Assert.IsFalse(sim.TryRevive(out _));
            Assert.AreEqual(before, Snapshot(sim.State)); Assert.AreEqual(0, sim.Events.Count);
            Assert.IsTrue(sim.TryPlay(0, new Cell(0, 0), out _)); before = Snapshot(sim.State);
            Assert.IsFalse(sim.TryPlay(0, new Cell(1, 0), out _));
            Assert.IsFalse(sim.TryUseBooster(BoosterKind.Shuffle, out _));
            Assert.AreEqual(before, Snapshot(sim.State));
        }

        [Test]
        public void ClocheRequiresASecondHit()
        {
            var level = CoreModelTests.Standard(); level.Cloches = Enumerable.Repeat(true, 9).ToArray();
            var sim = Create(level); Play(sim, 0, 1);
            Assert.AreEqual(3, sim.Events.Count(e => e.Kind == SimEventKind.ClocheBroken));
            Assert.AreEqual(0, sim.State.Delivered); Assert.IsTrue(sim.State.Tray.All(item => item != null));
            Play(sim, 0, 1); Assert.AreEqual(3, sim.State.Delivered); Conserved(sim, 9);
        }

        [Test]
        public void FallingPrimedCanExplodesFromItsCurrentCellOnlyOnce()
        {
            var level = Level(3, new[] { -1, -1, 0, 0, 1, 2, 0, 1, 2, -1, 1, 2 }, new[] { 0, 1, 2 }, CardKind.Pop);
            var sim = Create(level); Play(sim, 0, 1);
            var explosion = sim.Events.Single(e => e.Kind == SimEventKind.CanExploded && e.EntityId == 1);
            Assert.AreEqual(new Cell(0, 2), explosion.Cell);
            Assert.AreEqual(1, sim.Events.Count(e => e.Kind == SimEventKind.CanPrimed && e.EntityId == 1));
            Assert.IsTrue(sim.Events.Count(e => e.Kind == SimEventKind.CanExploded) >= 2);
            Conserved(sim, 9);
        }

        [Test]
        public void FrameStepSizeAndMidFlightCloneDoNotChangeOutcomes()
        {
            var level = Waiting(); level.Deck[0] = CardKind.Box;
            var fast = Create(level); var stepped = Create(level);
            Assert.IsTrue(fast.TryPlay(0, new Cell(1, 1), out _));
            Assert.IsTrue(stepped.TryPlay(0, new Cell(1, 1), out _));
            fast.AdvanceTo(300000); var clone = fast.Clone();
            string original = Snapshot(fast.State); clone.RunUntilSettled();
            Assert.AreEqual(original, Snapshot(fast.State));
            fast.RunUntilSettled();
            while (!stepped.IsSettled) stepped.AdvanceTo(stepped.Time + 17000);
            Assert.AreEqual(Snapshot(fast.State), Snapshot(stepped.State));
            Assert.AreEqual(Transcript(fast), Transcript(stepped));
            Assert.AreEqual(Transcript(fast), Transcript(clone));
            Conserved(fast, 12);
        }

        [Test]
        public void BoxReservationsIncludeAirbornePastriesAndWaitForArrival()
        {
            var sim = Create(CoreModelTests.Standard());
            Assert.IsTrue(sim.TryPlay(0, new Cell(0, 1), out _)); sim.AdvanceTo(200000);
            var box = sim.State.Lanes[0];
            Assert.AreEqual(3, box.Reserved); Assert.AreEqual(0, box.Filled);
            Assert.AreEqual(BoxPhase.Ready, box.Phase); Assert.IsFalse(sim.IsSettled);
            sim.RunUntilSettled(); Assert.AreEqual(3, sim.State.Delivered); Conserved(sim, 9);
        }

        [Test]
        public void NewBoxPumpsLandedPlatePastriesAndGiftsDealIntoDeck()
        {
            var level = Waiting(); level.Deck[0] = CardKind.Box; level.Gifts.Add(1, CardKind.Sweep);
            var sim = Create(level); Play(sim, 1, 1);
            Assert.AreEqual(9, sim.State.Delivered); Assert.AreEqual(0, sim.State.Plates.Count);
            Assert.AreEqual(1, sim.Events.Count(e => e.Kind == SimEventKind.GiftEarned));
            Assert.AreEqual(3, sim.Events.Count(e => e.Kind == SimEventKind.Routed && e.Value == (int)Destination.Plate));
            Assert.AreEqual(9, sim.Events.Count(e => e.Kind == SimEventKind.Routed && e.Value == (int)Destination.Box));
            Conserved(sim, 12);
        }

        [Test]
        public void RackFailureWaitsForTheComboAndExpressRemovesItsContents()
        {
            var sim = Create(OverflowLevel(CardKind.Sweep));
            Assert.IsTrue(sim.TryPlay(0, new Cell(0, 3), out _));
            sim.AdvanceTo(420000); Assert.AreEqual(Outcome.Playing, sim.State.Outcome);
            Assert.AreEqual(5, sim.State.Plates.Count); Assert.AreEqual(1, sim.State.Rack.Count);
            sim.RunUntilSettled(); Assert.AreEqual(Outcome.RackNotCleared, sim.State.Outcome);
            long reviveTime = sim.Time;
            Assert.IsTrue(sim.TryRevive(out _)); sim.RunUntilSettled();
            Assert.AreEqual(6, sim.State.RemovedByExpress); Assert.AreEqual(Outcome.Playing, sim.State.Outcome);
            Assert.IsTrue(sim.Time >= reviveTime + Config().ExpressMinimum); Conserved(sim, 24);
        }

        [Test]
        public void OverflowReviveRedirectsOnlyOverflowFlightsAndLeavesAirborneStorageAlone()
        {
            var level = OverflowLevel(CardKind.Box); level.Gifts.Add(6, CardKind.Box);
            var sim = Create(level);
            Assert.IsTrue(sim.TryPlay(0, new Cell(1, 1), out _)); sim.AdvanceTo(200000);
            Assert.AreEqual(Outcome.Overflow, sim.State.Outcome);
            Assert.AreEqual(0, sim.Events.Count(e => e.Kind == SimEventKind.Landed));
            Assert.AreEqual(5, sim.State.Plates.Count); Assert.AreEqual(2, sim.State.Rack.Count);
            Assert.IsTrue(sim.TryRevive(out _)); sim.RunUntilSettled();
            Assert.AreEqual(2, sim.State.RemovedByExpress);
            Assert.AreEqual(2, sim.Events.Count(e => e.Kind == SimEventKind.Landed && e.Value == (int)Destination.Express));
            Assert.AreEqual(5, sim.State.Plates.Count); Assert.AreEqual(2, sim.State.Rack.Count);
            Assert.AreEqual(5, sim.State.Queue.Count);
            Assert.AreEqual(13, sim.State.Queue.Sum(box => box.Need));
            Assert.AreEqual(Outcome.RackNotCleared, sim.State.Outcome); Conserved(sim, 24);
            Assert.IsTrue(sim.TryRevive(out _)); sim.RunUntilSettled();
            Assert.AreEqual(9, sim.State.RemovedByExpress);
            Assert.AreEqual(Outcome.Playing, sim.State.Outcome); Conserved(sim, 24);
        }

        [Test]
        public void OutOfMovesReviveAddsTwoFlopsAndDoesNotResetTheBoard()
        {
            var sim = Create(CoreModelTests.Standard());
            for (int i = 0; i < 8; i++) Play(sim, 0, 1);
            Assert.AreEqual(Outcome.OutOfMoves, sim.State.Outcome);
            Assert.IsTrue(sim.TryRevive(out _));
            Assert.AreEqual("Box,Box", string.Join(",", sim.State.Hand));
            Assert.AreEqual(3, sim.State.Delivered); Assert.AreEqual(Outcome.Playing, sim.State.Outcome);
            Conserved(sim, 9);
        }

        [Test]
        public void PredictionMatchesExecutionAndNeverTouchesTheOriginal()
        {
            var level = Waiting(); level.Deck[0] = CardKind.Box;
            var sim = Create(level); var before = Snapshot(sim.State);
            var prediction = Predictor.Predict(sim, 0, new Cell(1, 1));
            Assert.IsTrue(prediction.Accepted); Assert.AreEqual(before, Snapshot(sim.State));
            Assert.AreEqual(0, sim.Time); Assert.AreEqual(0, sim.Events.Count);
            Play(sim, 1, 1);
            Assert.AreEqual(Snapshot(sim.State), Snapshot(prediction.FinalState));
            Assert.AreEqual(Transcript(sim), string.Join("\n", prediction.Events.Select(e => e.ToString())));
            Assert.AreEqual(sim.Time, prediction.SettledTime);
        }

        [Test]
        public void TutorialAndInvalidBoosterCommandsPreserveInventory()
        {
            var sim = Create(CoreModelTests.Tutorial()); var before = Snapshot(sim.State);
            Assert.IsFalse(sim.TryUseBooster(BoosterKind.Shuffle, out _));
            Assert.AreEqual(before, Snapshot(sim.State));
            sim = Create(CoreModelTests.Standard()); before = Snapshot(sim.State);
            Assert.IsFalse(sim.TryUseBooster((BoosterKind)99, out _));
            Assert.AreEqual(before, Snapshot(sim.State));
        }

        [Test]
        public void ShuffleIsSeededAndDoesNotTouchDeckOrCallerConfig()
        {
            var config = Config(); var sim = new Simulator(CoreModelTests.Standard(), config, 0, new[] { 1, 0, 0 });
            var other = new Simulator(CoreModelTests.Standard(), config, 0, new[] { 1, 0, 0 });
            config.PopWeight = -100;
            var deck = sim.State.Deck.ToArray();
            Assert.IsTrue(sim.TryUseBooster(BoosterKind.Shuffle, out _));
            Assert.IsTrue(other.TryUseBooster(BoosterKind.Shuffle, out _));
            Assert.AreEqual(Snapshot(sim.State), Snapshot(other.State));
            Assert.AreEqual(string.Join(",", deck), string.Join(",", sim.State.Deck));
            Assert.AreEqual(0, sim.State.Inventory[0]);
            string before = Snapshot(sim.State); Assert.IsFalse(sim.TryUseBooster(BoosterKind.Shuffle, out _));
            Assert.AreEqual(before, Snapshot(sim.State));
        }

        [Test]
        public void StashGetsFirstClaimWhenCallBoxBringsItsType()
        {
            var sim = Create(Waiting(true)); Play(sim, 0, 1);
            var stashed = sim.State.Plates.ToArray();
            Assert.IsTrue(sim.TryUseBooster(BoosterKind.Stash, out _));
            Assert.AreEqual(3, sim.State.Stash.Count); Assert.AreEqual(0, sim.State.Plates.Count);
            Play(sim, 1, 1); var plates = sim.State.Plates.ToArray();
            int start = sim.Events.Count;
            Assert.IsTrue(sim.TryUseBooster(BoosterKind.CallBox, out _)); sim.RunUntilSettled();
            var routed = sim.Events.Skip(start).Where(e => e.Kind == SimEventKind.Routed && e.Value == (int)Destination.Box).ToArray();
            Assert.AreEqual(string.Join(",", stashed), string.Join(",", routed.Select(e => e.EntityId)));
            Assert.AreEqual(string.Join(",", plates), string.Join(",", sim.State.Plates));
            Assert.AreEqual(0, sim.State.Stash.Count); Assert.AreEqual(3, sim.State.Delivered);
            Assert.AreEqual(0, sim.State.Lanes[0].Type); Conserved(sim, 15);
        }

        [Test]
        public void ReplayIncludingBoosterMatchesEveryEvent()
        {
            var a = Create(Waiting()); var b = Create(Waiting());
            foreach (var sim in new[] { a, b })
            {
                Play(sim, 0, 1); Assert.IsTrue(sim.TryUseBooster(BoosterKind.Stash, out _));
                Assert.IsTrue(sim.TryUseBooster(BoosterKind.CallBox, out _)); sim.RunUntilSettled();
                Assert.IsTrue(sim.TryUseBooster(BoosterKind.Shuffle, out _));
            }
            Assert.AreEqual(Transcript(a), Transcript(b)); Assert.AreEqual(Snapshot(a.State), Snapshot(b.State));
        }

        [Test]
        public void ExpressShrinksQueuedBoxWhenSevenRemovedLeavesPartialCapacity()
        {
            var level = Level(7, new[] { 3, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 3, 0, 1, 0, 0, 1, 1, 2, 2, 2 }, new[] { 0, 1, 2, 3, 3, 3, 3 }, CardKind.Sweep);
            var sim = Create(level); Play(sim, 0, 0);
            Assert.AreEqual(Outcome.RackNotCleared, sim.State.Outcome);
            Assert.IsTrue(sim.TryRevive(out _)); sim.RunUntilSettled();
            Assert.AreEqual(7, sim.State.RemovedByExpress);
            Assert.AreEqual("2,3", string.Join(",", sim.State.Queue.Select(box => box.Need)));
            Assert.AreEqual(5, sim.State.Tray.Count(p => p != null && p.Type == 3));
            // A second reconciliation must count the reduced capacities instead of restoring phantom room.
            sim.RunUntilSettled();
            Assert.AreEqual(5, sim.State.Queue.Sum(box => box.Need)); Conserved(sim, 21);
            Play(sim, 0, 1);
            Assert.IsTrue(sim.TryUseBooster(BoosterKind.CallBox, out _)); sim.RunUntilSettled();
            Assert.AreEqual(2, sim.Events.Last(e => e.Kind == SimEventKind.BoxClosing && e.EntityId == 4).Value);
            Conserved(sim, 21);
        }

        [Test]
        public void RemovedGiftGoesToDeckWhenAllSurvivingBoxesHaveGifts()
        {
            var level = OverflowLevel(CardKind.Box);
            for (int i = 1; i <= 6; i++) level.Gifts.Add(i, CardKind.Pop);
            var sim = Create(level); Play(sim, 1, 1);
            Assert.AreEqual(Outcome.Overflow, sim.State.Outcome);
            Assert.IsTrue(sim.TryRevive(out _)); sim.RunUntilSettled();
            Assert.AreEqual(1, sim.Events.Count(e => e.Kind == SimEventKind.GiftEarned && e.EntityId == 6));
            Assert.AreEqual(5, sim.State.Lanes.Count(box => box != null && box.Gift.HasValue) + sim.State.Queue.Count(box => box.Gift.HasValue));
            Conserved(sim, 24);
        }

        [Test]
        public void AllZeroDurationsStillTerminateAndRespectInsertionOrder()
        {
            var config = new SimulationConfig(0, 0, 0, 0) { ImpactDelay = 0, GravityDelay = 0, CanDelay = 0,
                SweepLanding = 0, Flight = 0, BoxClose = 0, BoxDeparture = 0, BoxArrival = 0, RackHop = 0, ExpressMinimum = 0 };
            var sim = new Simulator(CoreModelTests.Tutorial(), config, 1, new[] { 0, 0, 0 });
            Play(sim, 1, 1); Play(sim, 1, 1); Play(sim, 1, 2);
            Assert.AreEqual(Outcome.Won, sim.State.Outcome); Assert.AreEqual(0, sim.Time);
            Assert.Throws<ArgumentOutOfRangeException>(() => sim.AdvanceTo(-1));
        }

        [Test]
        public void SeededScenarioMatrixConservesPastriesAndPredictionMatchesEachMove()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var random = new Random(seed);
                var level = Waiting(true);
                for (int i = level.Layout.Length - 1; i > 0; i--)
                {
                    int other = random.Next(i + 1);
                    int value = level.Layout[i]; level.Layout[i] = level.Layout[other]; level.Layout[other] = value;
                }
                for (int i = 0; i < level.Deck.Length; i++) level.Deck[i] = (CardKind)random.Next(4);
                level.Cloches = level.Layout.Select(_ => random.Next(4) == 0).ToArray();
                level.Gifts.Add(1, CardKind.Box);
                var sim = Create(level, (uint)seed);
                bool revived = false;
                for (int turn = 0; turn < 16 && sim.State.Outcome != Outcome.Won; turn++)
                {
                    if (sim.State.Outcome != Outcome.Playing)
                    {
                        if (revived) break;
                        Assert.IsTrue(sim.TryRevive(out _)); sim.RunUntilSettled(); revived = true;
                    }
                    if (sim.State.Outcome != Outcome.Playing) break;
                    int x = random.Next(level.Columns), y = random.Next(level.Rows);
                    int hand = random.Next(sim.State.Hand.Count);
                    var prediction = Predictor.Predict(sim, hand, new Cell(x, y));
                    Assert.IsTrue(prediction.Accepted);
                    Assert.IsTrue(sim.TryPlay(hand, new Cell(x, y), out _));
                    while (!sim.IsSettled)
                    { sim.AdvanceTo(sim.Time + 37000); Conserved(sim, 15); }
                    Assert.AreEqual(Snapshot(prediction.FinalState), Snapshot(sim.State), "Seed " + seed + " turn " + turn);
                    foreach (BoosterKind kind in new[] { BoosterKind.Stash, BoosterKind.CallBox, BoosterKind.Shuffle })
                    {
                        sim.TryUseBooster(kind, out _); sim.RunUntilSettled(); Conserved(sim, 15);
                    }
                }
            }
        }

        [Test]
        public void AirborneRackPastryRetargetsToFreedPlateWithoutDuplicateLanding()
        {
            var level = Level(5, new[] { 0, 0, 0, 1, 2, 3, 3, 3, 1, 2,
                3, 3, -1, 1, 2, 3, 3, 3, 3, -1 }, new[] { 0, 1, 2, 3, 3, 3 }, CardKind.Box);
            var config = Config(); config.BoxClose = 0; config.BoxDeparture = 0; config.BoxArrival = 0;
            var sim = new Simulator(level, config, 42, new[] { 0, 0, 0 });
            Play(sim, 1, 1);
            var rackRoute = sim.Events.Single(e => e.Kind == SimEventKind.Routed && e.Value == (int)Destination.Rack);
            var plateRoute = sim.Events.Single(e => e.Kind == SimEventKind.Routed && e.EntityId == rackRoute.EntityId && e.Value == (int)Destination.Plate);
            var plateLanding = sim.Events.Single(e => e.Kind == SimEventKind.Landed && e.EntityId == rackRoute.EntityId && e.Value == (int)Destination.Plate);
            Assert.IsTrue(plateRoute.Time < rackRoute.Time + config.Flight);
            Assert.AreEqual(rackRoute.Time + config.Flight, plateLanding.Time);
            Assert.AreEqual(0, sim.Events.Count(e => e.Kind == SimEventKind.Landed && e.EntityId == rackRoute.EntityId && e.Value == (int)Destination.Rack));
            Conserved(sim, 18);
        }
    }
}
