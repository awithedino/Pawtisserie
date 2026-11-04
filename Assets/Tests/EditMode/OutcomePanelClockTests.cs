using NUnit.Framework;
using Paw.Presentation;
using Paw.Sim;

namespace Paw.Tests
{
    public class OutcomePanelClockTests
    {
        private static SimEvent Event(long time, SimEventKind kind, int value = 0) => new SimEvent(time, kind, 0, value, default);

        [Test]
        public void WinDelayStartsWhenSettledAndResetClearsTheTimer()
        {
            var clock = new OutcomePanelClock();
            clock.Observe(Event(100, SimEventKind.Outcome, (int)Outcome.Won));
            Assert.IsFalse(clock.IsDue(200, false, 70, 55, 0));
            Assert.IsFalse(clock.IsDue(300, true, 70, 55, 0));
            Assert.IsFalse(clock.IsDue(369, true, 70, 55, 0));
            Assert.IsTrue(clock.IsDue(370, true, 70, 55, 0));
            clock.Reset(); Assert.IsFalse(clock.IsDue(500, true, 70, 55, 0));
        }

        [Test]
        public void OverflowWaitsForLandingButNotForSettlement()
        {
            var clock = new OutcomePanelClock();
            clock.Observe(Event(100, SimEventKind.Outcome, (int)Outcome.Overflow));
            Assert.IsFalse(clock.IsDue(1000, false, 70, 55, 0));
            clock.Observe(Event(200, SimEventKind.Landed, (int)Destination.Overflow));
            Assert.IsFalse(clock.IsDue(254, false, 70, 55, 0));
            Assert.IsTrue(clock.IsDue(255, false, 70, 55, 0));
            clock.Observe(Event(256, SimEventKind.ExpressStarted));
            Assert.IsFalse(clock.IsDue(1000, true, 70, 55, 0));
        }

        [Test]
        public void SettledFailureAndButtonPolicyFollowApprovedRules()
        {
            var clock = new OutcomePanelClock();
            clock.Observe(Event(10, SimEventKind.Outcome, (int)Outcome.OutOfMoves));
            Assert.IsFalse(clock.IsDue(10, false, 70, 55, 0));
            Assert.IsTrue(clock.IsDue(20, true, 70, 55, 0));
            var busy = new BoosterButtonState(1, Outcome.Playing, false, false, false, true);
            Assert.IsTrue(busy.Interactable); Assert.IsFalse(busy.CanUse);
            var grant = new BoosterButtonState(0, Outcome.Playing, false, false, false, true);
            Assert.IsTrue(grant.ShowsAd); Assert.IsTrue(grant.CanUse);
            Assert.IsFalse(new BoosterButtonState(1, Outcome.Playing, true, true, false, true).Interactable);
            Assert.IsFalse(new BoosterButtonState(1, Outcome.Playing, true, false, true, true).Interactable);
        }
    }
}
