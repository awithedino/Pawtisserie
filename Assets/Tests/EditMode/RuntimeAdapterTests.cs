using System;
using NUnit.Framework;
using Paw.Runtime;
using UnityEngine;

namespace Paw.Tests
{
    public class RuntimeAdapterTests
    {
        [Test]
        public void ClockRetainsFractionsAndSkipsTheFocusReturnInterval()
        {
            var clock = new SimClock();
            Assert.AreEqual(0, clock.Advance(.0000004)); Assert.AreEqual(0, clock.Advance(.0000004));
            Assert.AreEqual(1, clock.Advance(.0000004));
            clock.Focus(false); Assert.AreEqual(1, clock.Advance(100));
            clock.Focus(true); Assert.AreEqual(1, clock.Advance(100));
            Assert.AreEqual(10001, clock.Advance(.01));
            clock.Reset(); Assert.AreEqual(0, clock.Time);
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Advance(-1));
        }

        [Test]
        public void ViewConfigCopiesApprovedTimingsAndInventoryIntoFreshRunData()
        {
            var config = ScriptableObject.CreateInstance<ViewConfig>();
            try
            {
                Assert.IsTrue(config.IsValid);
                var first = config.CreateSimulationConfig(); first.Validate();
                Assert.AreEqual(320000, first.PopLanding); Assert.AreEqual(450000, first.DrillLanding);
                Assert.AreEqual(360000, first.BoxLanding); Assert.AreEqual(250000, first.GravityTravel);
                first.Flight = 1; Assert.AreEqual(580000, config.CreateSimulationConfig().Flight);
                var inventory = config.CreateInventory(); inventory[0] = 0;
                Assert.AreEqual(2, config.CreateInventory()[0]); Assert.AreEqual(60, config.PoolSize);
                Assert.AreEqual(700000, config.PanelDelay); Assert.AreEqual(800, config.BoardHeight);
                Assert.AreNotEqual(config.PastryColor(1), config.CardColor(Paw.Sim.CardKind.Sweep));
            }
            finally { UnityEngine.Object.DestroyImmediate(config); }
        }
    }
}
