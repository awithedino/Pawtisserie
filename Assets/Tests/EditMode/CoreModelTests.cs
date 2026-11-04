using System;
using System.Linq;
using NUnit.Framework;
using Paw.Sim;

namespace Paw.Tests
{
    public class CoreModelTests
    {
        internal static LevelDefinition Standard()
        {
            return new LevelDefinition { Id = 1, Columns = 3, Rows = 3, PastryTypes = 3,
                Layout = new[] { 0, 1, 2, 0, 1, 2, 0, 1, 2 }, Boxes = new[] { 0, 1, 2 },
                Deck = Enumerable.Repeat(CardKind.Drill, Rules.StartingCards).ToArray() };
        }

        internal static LevelDefinition Tutorial()
        {
            return new LevelDefinition { Id = 0, Columns = 3, Rows = 3, PastryTypes = 2, IsTutorial = true,
                Layout = new[] { 1, 0, 1, 0, 0, 0, 1, 0, 0 }, Boxes = new[] { 0, 1, 0 },
                Deck = new[] { CardKind.Pop, CardKind.Sweep, CardKind.Sweep } };
        }

        [TestCase(CardKind.Pop, 3)]
        [TestCase(CardKind.Sweep, 3)]
        [TestCase(CardKind.Drill, 3)]
        [TestCase(CardKind.Box, 4)]
        public void CornerShapesAreClippedAndUnique(CardKind card, int count)
        {
            var cells = CardShapes.GetCells(card, new Cell(0, 0), 3, 3);
            Assert.AreEqual(count, cells.Count);
            Assert.AreEqual(count, cells.Distinct().Count());
            Assert.IsTrue(cells.All(cell => cell.Column >= 0 && cell.Row >= 0 && cell.Column < 3 && cell.Row < 3));
        }

        [Test]
        public void ShapeProcessingOrderIsExplicit()
        {
            Assert.AreEqual("1,1;1,0;2,1;1,2;0,1", string.Join(";", CardShapes.GetCells(CardKind.Pop, new Cell(1, 1), 3, 3)));
            Assert.AreEqual("0,1;1,1;2,1", string.Join(";", CardShapes.GetCells(CardKind.Sweep, new Cell(1, 1), 3, 3)));
            Assert.AreEqual("1,0;1,1;1,2", string.Join(";", CardShapes.GetCells(CardKind.Drill, new Cell(1, 1), 3, 3)));
            Assert.AreEqual("0,0;1,0;2,0;0,1;1,1;2,1;0,2;1,2;2,2", string.Join(";", CardShapes.GetCells(CardKind.Box, new Cell(1, 1), 3, 3)));
            Assert.Throws<ArgumentOutOfRangeException>(() => CardShapes.GetCells(CardKind.Pop, new Cell(-1, 0), 3, 3));
            Assert.Throws<ArgumentOutOfRangeException>(() => CardShapes.GetCells((CardKind)99, new Cell(0, 0), 3, 3));
        }

        [Test]
        public void StandardAndExplicitTutorialValidate()
        {
            Standard().Validate(); Tutorial().Validate();
            var tutorial = Tutorial(); tutorial.IsTutorial = false;
            Assert.Throws<ArgumentException>(() => tutorial.Validate());
        }

        [Test]
        public void MalformedCountsCardsAndGiftsAreRejected()
        {
            var level = Standard(); level.Layout[0] = 1;
            Assert.Throws<ArgumentException>(() => level.Validate());
            level = Standard(); level.Deck[0] = (CardKind)99;
            Assert.Throws<ArgumentException>(() => level.Validate());
            level = Standard(); level.Gifts.Add(4, CardKind.Box);
            Assert.Throws<ArgumentException>(() => level.Validate());
            level = Tutorial(); level.Cloches = new bool[9]; level.Cloches[0] = true;
            Assert.Throws<ArgumentException>(() => level.Validate());
        }

        [Test]
        public void BoardCopiesAuthoredInputAndClonesAllMutableContainers()
        {
            var level = Standard();
            var inventory = new[] { 2, 2, 1 };
            var board = BoardFactory.Create(level, 42, inventory);
            level.Layout[0] = 2; level.Deck[0] = CardKind.Box; inventory[0] = 0;
            Assert.AreEqual(0, board.Tray[0].Type); Assert.AreEqual(CardKind.Drill, board.Hand[0]);
            var clone = board.Clone(); clone.Tray[0].Cloche = true; clone.Lanes[0].Need = 0;
            clone.Hand.Clear(); clone.Deck.Clear(); clone.Inventory[0] = 0; clone.RandomState = 99;
            Assert.IsFalse(board.Tray[0].Cloche); Assert.AreEqual(3, board.Lanes[0].Need);
            Assert.AreEqual(5, board.Hand.Count); Assert.AreEqual(3, board.Deck.Count);
            Assert.AreEqual(2, board.Inventory[0]); Assert.AreEqual(42u, board.RandomState);
            Assert.AreEqual(9, board.Tray.Select(item => item.Id).Distinct().Count());
        }

        [Test]
        public void InvalidConfigAndInventoryAreRejected()
        {
            var config = new SimulationConfig(0, 0, 0, 0); config.Flight = -1;
            Assert.Throws<ArgumentException>(() => config.Validate());
            config.Flight = 0; config.PopWeight = -1;
            Assert.Throws<ArgumentException>(() => config.Validate());
            Assert.Throws<ArgumentException>(() => BoardFactory.Create(Standard(), 1, new[] { -1, 0, 0 }));
        }
    }
}
