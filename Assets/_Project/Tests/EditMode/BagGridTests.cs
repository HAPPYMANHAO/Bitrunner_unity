using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class BagGridTests
    {
        [Test]
        public void NewGrid_IsEmpty()
        {
            var grid = new BagGrid(4, 3);

            Assert.AreEqual(12, grid.Capacity);
            Assert.AreEqual(0, grid.GetOccupiedCount());
            Assert.IsTrue(grid.IsFree(0, 0));
            Assert.IsTrue(grid.IsFree(3, 2));
        }

        [Test]
        public void InBounds_RejectsNegativeAndOutside()
        {
            var grid = new BagGrid(4, 3);

            Assert.IsFalse(grid.InBounds(-1, 0));
            Assert.IsFalse(grid.InBounds(0, -1));
            Assert.IsFalse(grid.InBounds(4, 0));
            Assert.IsFalse(grid.InBounds(0, 3));
            Assert.IsTrue(grid.InBounds(0, 0));
            Assert.IsTrue(grid.InBounds(3, 2));
        }

        [Test]
        public void CanPlace_RejectsOutOfBounds()
        {
            var grid = new BagGrid(3, 3);
            ItemShape shape = ItemShape.Parse("##");

            Assert.IsFalse(grid.CanPlace(shape, new Cell(2, 0)));
            Assert.IsTrue(grid.CanPlace(shape, new Cell(1, 0)));
        }

        [Test]
        public void CanPlace_RejectsOverlap()
        {
            var grid = new BagGrid(3, 3);
            ItemShape single = ItemShape.Parse("#");

            grid.Place(7, single, new Cell(1, 1));

            Assert.IsFalse(grid.CanPlace(single, new Cell(1, 1)));
            Assert.IsTrue(grid.CanPlace(single, new Cell(2, 2)));
        }

        [Test]
        public void CanPlace_IgnoreIdAllowsSameItem()
        {
            var grid = new BagGrid(3, 3);
            ItemShape shape = ItemShape.Parse("##");

            grid.Place(9, shape, new Cell(0, 0));

            Assert.IsFalse(grid.CanPlace(shape, new Cell(0, 0)));
            Assert.IsTrue(grid.CanPlace(shape, new Cell(0, 0), 9));
        }

        [Test]
        public void Place_WritesOccupantToEveryCell()
        {
            var grid = new BagGrid(4, 4);
            ItemShape shape = ItemShape.Parse("##", ".#");

            grid.Place(3, shape, new Cell(1, 1));

            Assert.AreEqual(3, grid.GetOccupant(1, 1));
            Assert.AreEqual(3, grid.GetOccupant(2, 1));
            Assert.AreEqual(3, grid.GetOccupant(2, 2));
            Assert.AreEqual(0, grid.GetOccupant(1, 2));
            Assert.AreEqual(3, grid.GetOccupiedCount());
        }

        [Test]
        public void Clear_RemovesOnlyMatchingId()
        {
            var grid = new BagGrid(3, 3);
            ItemShape single = ItemShape.Parse("#");

            grid.Place(1, single, new Cell(0, 0));
            grid.Place(2, single, new Cell(1, 0));

            grid.Clear(1);

            Assert.IsTrue(grid.IsFree(0, 0));
            Assert.AreEqual(2, grid.GetOccupant(1, 0));
            Assert.AreEqual(1, grid.GetOccupiedCount());
        }

        [Test]
        public void GetCells_ReturnsEveryOccupiedCell()
        {
            var grid = new BagGrid(4, 4);
            ItemShape shape = ItemShape.Parse("##");

            grid.Place(5, shape, new Cell(2, 3));

            var cells = grid.GetCells(5);

            Assert.AreEqual(2, cells.Count);
            CollectionAssert.Contains(cells, new Cell(2, 3));
            CollectionAssert.Contains(cells, new Cell(3, 3));
        }

        [Test]
        public void Place_ThrowsWhenInvalid()
        {
            var grid = new BagGrid(2, 2);
            ItemShape shape = ItemShape.Parse("###");

            Assert.Throws<System.InvalidOperationException>(() => grid.Place(1, shape, new Cell(0, 0)));
        }
    }
}
