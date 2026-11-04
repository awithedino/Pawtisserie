using NUnit.Framework;
using Paw.Presentation;

namespace Paw.Tests.EditMode
{
    public sealed class FanLayoutTests
    {
        [Test]
        public void FiveCardsAreCenteredSymmetricAndFitAvailableWidth()
        {
            FanLayout.Slot(0, 5, 58, 160, 2.6, 4.5, out Point2 left, out double leftAngle);
            FanLayout.Slot(2, 5, 58, 160, 2.6, 4.5, out Point2 center, out double centerAngle);
            FanLayout.Slot(4, 5, 58, 160, 2.6, 4.5, out Point2 right, out double rightAngle);
            Assert.AreEqual(-80d, left.X); Assert.AreEqual(80d, right.X);
            Assert.AreEqual(left.Y, right.Y); Assert.AreEqual(-leftAngle, rightAngle);
            Assert.AreEqual(0d, center.X); Assert.AreEqual(0d, center.Y); Assert.AreEqual(0d, centerAngle);
        }

        [Test]
        public void SingleCardHasCenteredFlatSlot()
        {
            FanLayout.Slot(0, 1, 58, 200, 2.6, 4.5, out Point2 position, out double angle);
            Assert.AreEqual(0d, position.X); Assert.AreEqual(0d, position.Y); Assert.AreEqual(0d, angle);
        }
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void FewerCardsRemainCenteredAndSymmetric(int count)
        {
            FanLayout.Slot(0, count, 58, 232, 2.6, 4.5, out Point2 left, out double leftAngle);
            FanLayout.Slot(count - 1, count, 58, 232, 2.6, 4.5, out Point2 right, out double rightAngle);
            Assert.AreEqual(-left.X, right.X); Assert.AreEqual(left.Y, right.Y); Assert.AreEqual(-leftAngle, rightAngle);
        }
    }
}
