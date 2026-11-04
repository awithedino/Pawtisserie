using System;
using NUnit.Framework;
using Paw.Sim;

namespace Paw.Tests
{
    public class RulesTests
    {
        [Test]
        public void FixedV1NumbersMatchTheGdd()
        {
            Assert.AreEqual(5, Rules.HandSize);
            Assert.AreEqual(8, Rules.StartingCards);
            Assert.AreEqual(3, Rules.BoxLanes);
            Assert.AreEqual(3, Rules.BoxCapacity);
            Assert.AreEqual(5, Rules.Plates);
            Assert.AreEqual(2, Rules.RackSlots);
        }

        [Test]
        public void StartingDeckIsWhatIsLeftAfterTheHand()
        {
            Assert.AreEqual(3, Rules.StartingCards - Rules.HandSize);
        }

        [Test]
        public void CardKindValuesAreStable()
        {
            Assert.AreEqual(0, (int)CardKind.Pop);
            Assert.AreEqual(1, (int)CardKind.Sweep);
            Assert.AreEqual(2, (int)CardKind.Drill);
            Assert.AreEqual(3, (int)CardKind.Box);
        }

        [Test]
        public void EveryCardKindRoundTripsThroughItsId()
        {
            foreach (CardKind kind in Enum.GetValues(typeof(CardKind)))
            {
                string id = CardIds.ToId(kind);
                Assert.IsNotNull(id, kind.ToString());
                Assert.IsTrue(CardIds.TryParse(id, out CardKind parsed), id);
                Assert.AreEqual(kind, parsed);
            }
        }

        [Test]
        public void UnknownCardIdIsRejected()
        {
            Assert.IsFalse(CardIds.TryParse("tap", out _));
            Assert.IsFalse(CardIds.TryParse(null, out _));
        }
    }
}
