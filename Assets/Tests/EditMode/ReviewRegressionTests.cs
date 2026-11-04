using System.Linq;
using NUnit.Framework;
using Paw.Sim;

namespace Paw.Tests
{
    public class ReviewRegressionTests
    {
        [Test]
        public void UntouchedCansDoNotBlockWinAndReplayAgrees()
        {
            var level = SimulationTests.Level(3, new[] { -1, -1, -1, 0, 1, 2, 0, 1, 2, 0, 1, 2 },
                new[] { 0, 1, 2 }, CardKind.Sweep);
            var a = SimulationTests.Create(level); var b = SimulationTests.Create(level);
            foreach (var sim in new[] { a, b })
            {
                for (int i = 0; i < 3; i++) SimulationTests.Play(sim, 0, 3);
                Assert.AreEqual(Outcome.Won, sim.State.Outcome);
                Assert.AreEqual(3, sim.State.Tray.Count(item => item != null && item.IsCan));
                SimulationTests.Conserved(sim, 9);
            }
            Assert.AreEqual(SimulationTests.Transcript(a), SimulationTests.Transcript(b));
        }

        [Test]
        public void ExpressDoesNotCaptureNewChainLaunchesWhenPlatesAreAvailable()
        {
            var level = SimulationTests.OverflowLevel(CardKind.Box);
            level.Layout[14] = -1; level.Layout[22] = -1; level.Layout[23] = -1;
            level.Boxes = level.Boxes.Take(7).ToArray();
            var config = SimulationTests.Config(); config.Flight = 0;
            var a = new Simulator(level, config, 42, new[] { 0, 0, 0 }); var b = a.Clone();
            foreach (var sim in new[] { a, b })
            {
                Assert.IsTrue(sim.TryPlay(0, new Cell(1, 1), out _)); sim.AdvanceTo(200000);
                Assert.AreEqual(Outcome.Overflow, sim.State.Outcome);
                Assert.IsTrue(sim.TryRevive(out _)); sim.RunUntilSettled();
                Assert.AreEqual(8, sim.State.RemovedByExpress);
                Assert.AreEqual(1, sim.State.Plates.Count);
                Assert.IsTrue(sim.Events.Any(e => e.Kind == SimEventKind.Routed && e.EntityId == 21 && e.Value == (int)Destination.Plate));
                SimulationTests.Conserved(sim, 21);
            }
            Assert.AreEqual(SimulationTests.Transcript(a), SimulationTests.Transcript(b));
        }

        [Test]
        public void CallBoxSkipsUnqueuedMostWaitingTypeAndBreaksTiesByType()
        {
            var board = BoardFactory.Create(SimulationTests.Waiting(true), 1, new[] { 0, 1, 0 });
            board.Lanes[0].Phase = BoxPhase.Leaving;
            for (int i = 0; i < 4; i++) board.Pastries.Add(new StoredPastry { Type = 0, Destination = Destination.Stash });
            board.Pastries.Add(new StoredPastry { Type = 3, Destination = Destination.Stash });
            Assert.AreEqual(0, CallBoxPicker.FindQueueIndex(board));
            board.Queue.Add(new BoxState { Id = 6, Type = 0 });
            Assert.AreEqual(2, CallBoxPicker.FindQueueIndex(board));
            board.Pastries.RemoveRange(1, 3);
            Assert.AreEqual(2, CallBoxPicker.FindQueueIndex(board));
        }

        [Test]
        public void DefaultBoxCycleEndsAtTheApprovedTime()
        {
            var a = SimulationTests.Create(SimulationTests.Waiting()); var b = a.Clone();
            foreach (var sim in new[] { a, b })
            {
                SimulationTests.Play(sim, 1, 1);
                long close = sim.Events.Single(e => e.Kind == SimEventKind.BoxClosing && e.EntityId == 1).Time;
                long departed = sim.Events.Single(e => e.Kind == SimEventKind.BoxDeparted && e.EntityId == 1).Time;
                long ready = sim.Events.Single(e => e.Kind == SimEventKind.BoxReady && e.EntityId == 4).Time;
                Assert.AreEqual(1010000, departed - close);
                Assert.AreEqual(1590000, ready - close);
            }
            Assert.AreEqual(SimulationTests.Transcript(a), SimulationTests.Transcript(b));
        }

        [Test]
        public void GiftsMayUseTheLastTwoBoxesButMustStillBeInRange()
        {
            var level = CoreModelTests.Standard(); level.Gifts.Add(2, CardKind.Pop); level.Gifts.Add(3, CardKind.Box);
            level.Validate();
            var a = SimulationTests.Create(level); var b = a.Clone();
            foreach (var sim in new[] { a, b })
            {
                SimulationTests.Play(sim, 1, 1); SimulationTests.Play(sim, 2, 1);
                Assert.AreEqual(2, sim.Events.Count(e => e.Kind == SimEventKind.GiftEarned));
            }
            Assert.AreEqual(SimulationTests.Transcript(a), SimulationTests.Transcript(b));
            level.Gifts.Add(0, CardKind.Pop);
            Assert.Throws<System.ArgumentException>(() => level.Validate());
        }
    }
}
