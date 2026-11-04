using System.Linq;
using NUnit.Framework;
using Paw.Sim;

namespace Paw.Tests
{
    public class EventContractTests
    {
        [Test]
        public void TimedEventsEndAtTheirActualTransitions()
        {
            var sim = SimulationTests.Create(SimulationTests.Waiting());
            SimulationTests.Play(sim, 1, 1);
            var close = sim.Events.Single(e => e.Kind == SimEventKind.BoxClosing && e.EntityId == 1);
            var leave = sim.Events.Single(e => e.Kind == SimEventKind.BoxLeaving && e.EntityId == 1);
            var departed = sim.Events.Single(e => e.Kind == SimEventKind.BoxDeparted && e.EntityId == 1);
            var arrive = sim.Events.Single(e => e.Kind == SimEventKind.BoxArriving && e.EntityId == 4);
            var ready = sim.Events.Single(e => e.Kind == SimEventKind.BoxReady && e.EntityId == 4);
            Assert.AreEqual(close.EndTime, leave.Time); Assert.AreEqual(leave.EndTime, departed.Time);
            Assert.AreEqual(arrive.EndTime, ready.Time);
            var played = sim.Events.Single(e => e.Kind == SimEventKind.CardPlayed);
            Assert.AreEqual(played.EndTime, sim.Events.First(e => e.Kind == SimEventKind.Impact).Time);
            foreach (var route in sim.Events.Where(e => e.Kind == SimEventKind.Routed))
                Assert.AreEqual(route.EndTime, sim.Events.First(e => e.Kind == SimEventKind.Landed && e.EntityId == route.EntityId && e.Time >= route.Time).Time);
            Assert.IsTrue(sim.Events.All(e => e.EndTime >= e.Time));
            Assert.AreEqual(sim.Events.Count, sim.EventCount);
            Assert.AreEqual(sim.Events[0], sim.GetEvent(0)); Assert.AreSame(sim.Events, sim.Events);
        }

        [Test]
        public void ReadAccessorsAreValueCopiesAndReflectLiveChanges()
        {
            var sim = SimulationTests.Create(CoreModelTests.Standard());
            var cell = sim.GetTrayCell(0); var box = sim.GetLane(0);
            Assert.AreEqual(0, cell.Type); Assert.AreEqual(3, box.Need);
            SimulationTests.Play(sim, 0, 0);
            Assert.IsFalse(sim.GetTrayCell(0).Exists); Assert.IsTrue(cell.Exists);
            Assert.AreEqual(0, box.Filled); Assert.IsFalse(sim.GetLane(0).Exists);
            Assert.AreEqual(5, sim.HandCount); Assert.AreEqual(2, sim.DeckCount);
            Assert.AreEqual(5, sim.GetInventory(BoosterKind.Stash));
        }

        [Test]
        public void PredictionCloneDropsHistoryButRetainsPendingActions()
        {
            var sim = SimulationTests.Create(CoreModelTests.Standard()); SimulationTests.Play(sim, 0, 0);
            Assert.IsTrue(sim.EventCount > 0);
            var clone = sim.Clone(includeHistory: false);
            Assert.AreEqual(0, clone.EventCount); Assert.AreEqual(sim.Time, clone.Time);
            Assert.AreEqual(SimulationTests.Snapshot(sim.State), SimulationTests.Snapshot(clone.State));
            var prediction = Predictor.Predict(sim, 0, new Cell(1, 0));
            Assert.IsTrue(prediction.Accepted);
            Assert.IsTrue(prediction.Events.All(e => e.Time >= sim.Time));
            Assert.IsTrue(sim.TryPlay(0, new Cell(1, 0), out _)); var mid = sim.Clone(false);
            sim.RunUntilSettled(); mid.RunUntilSettled();
            Assert.AreEqual(SimulationTests.Snapshot(sim.State), SimulationTests.Snapshot(mid.State));
        }
    }
}
