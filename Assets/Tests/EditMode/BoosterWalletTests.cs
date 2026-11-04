using NUnit.Framework;
using Paw.Runtime;
using Paw.Sim;

namespace Paw.Tests
{
    public class BoosterWalletTests
    {
        [Test]
        public void InventoryPersistsAcrossLoadsAndResetsForANewSession()
        {
            SessionInventory.Reset();
            var wallet = SessionInventory.GetOrCreate(new[] { 2, 2, 1 });
            var first = new Simulator(CoreModelTests.Standard(), SimulationTests.Config(), 42, wallet.Snapshot());
            Assert.IsTrue(first.TryUseBooster(BoosterKind.Shuffle, out _));
            wallet.CopyFrom(first);
            var second = new Simulator(CoreModelTests.Standard(), SimulationTests.Config(), 42, SessionInventory.GetOrCreate(new[] { 2, 2, 1 }).Snapshot());
            Assert.AreEqual(1, second.GetInventory(BoosterKind.Shuffle));
            Assert.IsTrue(second.TryGrantBooster(BoosterKind.Stash, out _));
            wallet.CopyFrom(second);
            var third = new Simulator(CoreModelTests.Standard(), SimulationTests.Config(), 42, wallet.Snapshot());
            Assert.AreEqual(2, third.GetInventory(BoosterKind.Stash));
            SessionInventory.Reset();
            Assert.AreEqual(2, SessionInventory.GetOrCreate(new[] { 2, 2, 1 }).Snapshot()[0]);
            SessionInventory.Reset();
        }
    }
}
