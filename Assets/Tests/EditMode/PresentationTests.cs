using System.Linq;
using NUnit.Framework;
using Paw.Sim;
using Paw.Presentation;

namespace Paw.Tests
{
    public class PresentationTests
    {
        [Test]
        public void CursorReadsOnceAndResetStartsTheNewRun()
        {
            var sim = SimulationTests.Create(CoreModelTests.Standard()); var cursor = new EventCursor(); cursor.Reset(sim);
            Assert.IsFalse(cursor.TryRead(out _)); SimulationTests.Play(sim, 0, 0);
            int read = 0;
            while (cursor.TryRead(out SimEvent item)) { Assert.AreEqual(sim.GetEvent(read), item); read++; }
            Assert.AreEqual(sim.EventCount, read); Assert.IsFalse(cursor.TryRead(out _));
            cursor.Reset(sim); Assert.IsTrue(cursor.TryRead(out _));
            cursor.Reset(SimulationTests.Create(CoreModelTests.Standard())); Assert.IsFalse(cursor.TryRead(out _));
        }

        [Test]
        public void FlightEndpointsArcAndRetargetAreContinuous()
        {
            var path = new FlightPath(new Point2(0, 0), new Point2(10, 0), 100, 200, 3);
            Assert.AreEqual(new Point2(0, 0), path.At(0)); Assert.AreEqual(new Point2(10, 0), path.At(300));
            Assert.AreEqual(new Point2(5, 3), path.At(150));
            var next = path.Retarget(new Point2(8, 8), 150, 200, 1);
            Assert.AreEqual(path.At(150), next.At(150)); Assert.AreEqual(new Point2(8, 8), next.At(200));
            Assert.AreEqual(new Point2(4, 5), new FlightPath(new Point2(0, 0), new Point2(4, 5), 0, 0, 1).At(0));
        }

        [Test]
        public void AimSelectionSwitchingAndMouseConfirmationFollowThePlan()
        {
            var aim = new AimState(); Assert.IsFalse(aim.MouseClick(new Cell(0, 0)));
            aim.Select(1); Assert.IsFalse(aim.Tap(new Cell(1, 1)));
            aim.Select(2); Assert.AreEqual(new Cell(1, 1), aim.Target.Value);
            Assert.IsTrue(aim.Tap(new Cell(1, 1))); aim.Select(2);
            Assert.AreEqual(-1, aim.SelectedIndex); Assert.IsFalse(aim.Target.HasValue);
            aim.Select(0); aim.Hover(new Cell(0, 0)); Assert.IsTrue(aim.MouseClick(new Cell(0, 0)));
            aim.Cancel(); Assert.AreEqual(-1, aim.SelectedIndex);
        }

        [Test]
        public void PreviewUsesLaunchDestinationEvenWhenStorageLaterClears()
        {
            var level = SimulationTests.Waiting(); level.Deck[0] = CardKind.Box;
            var prediction = Predictor.Predict(SimulationTests.Create(level), 0, new Cell(1, 1));
            var preview = new PreviewSummary(prediction);
            Assert.AreEqual(3, preview.Badges.Count(b => b.Kind == PreviewBadgeKind.Plate));
            Assert.AreEqual(6, preview.Badges.Count(b => b.Kind == PreviewBadgeKind.Box));
            Assert.AreEqual(PreviewBadgeKind.Plate, preview.Risk);
            Assert.AreEqual(0, prediction.FinalState.Plates.Count);
        }

        [Test]
        public void PreviewShowsChainHitsBeyondThePrimaryFootprintAndOverflowRisk()
        {
            var level = SimulationTests.Level(3, new[] { -1, -1, 0, 0, 1, 2, 0, 1, 2, -1, 1, 2 }, new[] { 0, 1, 2 }, CardKind.Pop);
            var prediction = Predictor.Predict(SimulationTests.Create(level), 0, new Cell(0, 1));
            var preview = new PreviewSummary(prediction);
            Assert.IsTrue(preview.Badges.Any(b => b.Cell.Row == 3));
            Assert.IsTrue(preview.Badges.Any(b => b.Kind == PreviewBadgeKind.Can));
            preview = new PreviewSummary(Predictor.Predict(SimulationTests.Create(SimulationTests.OverflowLevel(CardKind.Box)), 0, new Cell(1, 1)));
            Assert.AreEqual(PreviewBadgeKind.Overflow, preview.Risk);
        }

        [Test]
        public void BoardMappingRoundTripsEveryCenterAndRejectsOutsideEdges()
        {
            var map = new BoardMapping(new Point2(-175, 200), 50, 7, 7);
            for (int y = 0; y < 7; y++) for (int x = 0; x < 7; x++)
            { var target = new Cell(x, y); Assert.IsTrue(map.TryCell(map.Center(target), out Cell actual)); Assert.AreEqual(target, actual); }
            Assert.IsFalse(map.TryCell(new Point2(175, 200), out _));
            Assert.IsFalse(map.TryCell(new Point2(-175.01, 200), out _));
            Assert.IsFalse(map.TryCell(new Point2(-175, -150), out _));
        }
    }
}
