using NUnit.Framework;
using Paw.Presentation;
using Paw.Sim;

namespace Paw.Tests.EditMode
{
    public sealed class PointerGestureTests
    {
        private sealed class RestrictedGesture : IGestureFilter
        {
            public bool AllowsCard(int handIndex) => handIndex == 0;
            public bool AllowsTarget(int handIndex, Cell target) => handIndex == 0 && target.Column == 1 && target.Row == 1;
        }
        [Test]
        public void DragPreviewsTargetAndDropsOnceOnRelease()
        {
            var gesture = new PointerGesture(); var first = new Cell(0, 0); var second = new Cell(1, 2);
            Assert.IsTrue(gesture.CardDown(1, 2, 0, 0));
            gesture.CardMove(1, 11, 0, 10, first, true);
            Assert.IsTrue(gesture.IsDragging); Assert.AreEqual(first, gesture.Target.Value);
            gesture.CardMove(1, 15, 0, 10, second, true);
            Assert.AreEqual(second, gesture.Target.Value);
            Assert.IsTrue(gesture.CardUp(1, second, true, out int index, out Cell cell));
            Assert.AreEqual(2, index); Assert.AreEqual(second, cell);
            Assert.IsFalse(gesture.CardUp(1, second, true, out _, out _));
        }

        [Test]
        public void ReleaseOutsideCancelsAndAnotherPointerCannotTakeOver()
        {
            var gesture = new PointerGesture();
            Assert.IsTrue(gesture.CardDown(1, 0, 0, 0));
            Assert.IsFalse(gesture.CardDown(2, 1, 0, 0));
            gesture.CardMove(1, 11, 0, 10, new Cell(0, 0), true);
            gesture.CardMove(1, 20, 0, 10, null, true);
            Assert.IsFalse(gesture.Target.HasValue);
            Assert.IsFalse(gesture.CardUp(2, new Cell(0, 0), true, out _, out _));
            Assert.IsFalse(gesture.CardUp(1, null, true, out _, out _));
            Assert.AreEqual(-1, gesture.SelectedIndex);
        }

        [Test]
        public void BusyHandAllowsSelectionButBlocksDragAndBoardPlay()
        {
            var gesture = new PointerGesture();
            gesture.CardDown(1, 0, 0, 0); gesture.CardMove(1, 50, 0, 10, new Cell(0, 0), false);
            Assert.IsFalse(gesture.IsDragging);
            Assert.IsFalse(gesture.CardUp(1, new Cell(0, 0), false, out _, out _));
            Assert.AreEqual(0, gesture.SelectedIndex);
            Assert.IsFalse(gesture.BoardDown(1, new Cell(0, 0), false));
        }

        [Test]
        public void SelectedCardTouchSlidesAndReleasesOnFinalTile()
        {
            var gesture = new PointerGesture(); var target = new Cell(2, 1);
            gesture.CardDown(1, 1, 0, 0); gesture.CardUp(1, null, true, out _, out _);
            Assert.IsTrue(gesture.BoardDown(2, new Cell(0, 0), true));
            gesture.BoardMove(2, target); Assert.AreEqual(target, gesture.Target.Value);
            Assert.IsTrue(gesture.BoardUp(2, target, true, out int card, out Cell cell));
            Assert.AreEqual(1, card); Assert.AreEqual(target, cell);
        }

        [Test]
        public void ThresholdBoundaryAndCancellation()
        {
            var gesture = new PointerGesture();
            gesture.CardDown(1, 0, 0, 0); gesture.CardMove(1, 10, 0, 10, new Cell(0, 0), true);
            Assert.IsFalse(gesture.IsDragging); gesture.Cancel();
            Assert.IsFalse(gesture.CardUp(1, new Cell(0, 0), true, out _, out _));
        }

        [Test]
        public void ReleaseKeepsSelectionUntilThePlayIsAcceptedOrCancelled()
        {
            var gesture = new PointerGesture(); var target = new Cell(1, 1);
            gesture.CardDown(1, 0, 0, 0); gesture.CardUp(1, null, true, out _, out _);
            Assert.IsTrue(gesture.BoardDown(-1, target, true));
            Assert.AreEqual(0, gesture.SelectedIndex);
            Assert.IsTrue(gesture.BoardUp(-1, target, true, out int card, out Cell cell));
            Assert.AreEqual(0, card); Assert.AreEqual(target, cell);
            Assert.AreEqual(0, gesture.SelectedIndex);
            Assert.IsTrue(gesture.BoardDown(-1, target, true));
            gesture.Cancel(); Assert.AreEqual(-1, gesture.SelectedIndex);
        }

        [Test]
        public void OptionalFilterBlocksRestrictedCardsAndTargets()
        {
            var gesture = new PointerGesture { Filter = new RestrictedGesture() };
            Assert.IsFalse(gesture.CardDown(1, 1, 0, 0));
            Assert.IsTrue(gesture.CardDown(1, 0, 0, 0));
            gesture.CardUp(1, null, true, out _, out _);
            Assert.IsFalse(gesture.BoardDown(1, new Cell(0, 0), true));
            Assert.IsTrue(gesture.BoardDown(1, new Cell(1, 1), true));
            gesture.BoardMove(1, new Cell(0, 0)); Assert.IsFalse(gesture.Target.HasValue);
            Assert.IsFalse(gesture.BoardUp(1, new Cell(0, 0), true, out _, out _));
            Assert.AreEqual(0, gesture.SelectedIndex);
        }
    }
}
