using System.Linq;
using NUnit.Framework;
using Paw.Sim;

namespace Paw.Tests
{
    public class PrototypeLevelTests
    {
        [TestCase(1, "1,1,1,1,0,2,0,2,2,1,2,2,0,2,2,2,2,2,0,0,0,0,0,1,0,2,1,1,0,1,1,1,1,2,0,0", "000000000000000000000000000000000000", "2,0,1,0,0,1,1,2,0,2,2,1")]
        [TestCase(2, "0,3,0,0,3,0,1,3,1,1,3,2,2,0,0,2,0,2,0,3,2,0,2,3,-1,1,3,-1,1,1,0,2,3,-1,1,2,0,3,2,1,1,0", "000000000000000100010000010000000000000100", "2,3,0,0,1,2,2,1,0,0,3,3,1")]
        [TestCase(3, "1,3,3,3,3,2,3,2,2,0,1,1,2,-1,0,0,3,2,1,0,3,0,-1,2,1,0,1,1,0,2,3,-1,1,0,0,2,2,0,1,2,1,0,1,0,2,3,-1,1,2", "0000000110001000000000001000000001100010000000001", "0,1,2,1,1,3,1,0,3,2,2,0,3,2,0")]
        public void GeneratorMatchesTheHtmlRunInNode(int index, string layout, string covers, string boxes)
        {
            var level = PrototypeLevels.Create(index); level.Validate();
            Assert.AreEqual(layout, string.Join(",", level.Layout));
            Assert.AreEqual(covers, string.Concat(level.Cloches.Select(value => value ? "1" : "0")));
            Assert.AreEqual(boxes, string.Join(",", level.Boxes));
            Assert.AreEqual(3, level.Boxes.Take(3).Distinct().Count());
            for (int i = 0; i < level.Layout.Length; i++)
            {
                if (level.Layout[i] != -1) continue;
                Assert.IsTrue(i >= level.Columns);
                if (i % level.Columns + 1 < level.Columns) Assert.IsTrue(level.Layout[i + 1] != -1);
                if (i + level.Columns < level.Layout.Length) Assert.IsTrue(level.Layout[i + level.Columns] != -1);
            }
            var fresh = PrototypeLevels.Create(index); level.Layout[0] = -1;
            Assert.AreEqual(layout, string.Join(",", fresh.Layout));
        }

        [Test]
        public void CatalogTutorialWinsAndAllGiftNumbersMatchTheOneBasedPort()
        {
            var sim = SimulationTests.Create(PrototypeLevels.Create(0));
            SimulationTests.Play(sim, 1, 1); SimulationTests.Play(sim, 1, 1); SimulationTests.Play(sim, 1, 2);
            Assert.AreEqual(Outcome.Won, sim.CurrentOutcome);
            Assert.AreEqual("3,6,9,11", string.Join(",", PrototypeLevels.Create(1).Gifts.Keys));
            Assert.AreEqual("4,7,10,12", string.Join(",", PrototypeLevels.Create(2).Gifts.Keys));
            Assert.AreEqual("3,6,9,12,14", string.Join(",", PrototypeLevels.Create(3).Gifts.Keys));
        }
    }
}
