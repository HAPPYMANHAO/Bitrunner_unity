using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class ItemShapeTests
    {
        [Test]
        public void Parse_SingleCell_HasExpectedBounds()
        {
            ItemShape shape = ItemShape.Parse("#");

            Assert.AreEqual(1, shape.Width);
            Assert.AreEqual(1, shape.Height);
            Assert.AreEqual(1, shape.CellCount);
            Assert.IsTrue(shape.Contains(new Cell(0, 0)));
        }

        [Test]
        public void Parse_DoesNotNormalizeAwayPositiveOffset()
        {
            ItemShape shape = ItemShape.Parse(".#", "##");

            Assert.AreEqual(".#\n##", shape.ToPattern());
        }

        [Test]
        public void Parse_RejectsRaggedRows()
        {
            Assert.Throws<System.ArgumentException>(() => ItemShape.Parse("##", "#"));
        }

        [Test]
        public void Parse_RejectsDisconnectedCells()
        {
            Assert.Throws<System.ArgumentException>(() => ItemShape.Parse(".#", "#."));
        }

        [Test]
        public void Parse_RejectsEmptyPattern()
        {
            Assert.Throws<System.ArgumentException>(() => ItemShape.Parse());
        }

        [Test]
        public void RotateClockwise_TurnsVerticalLLeftToTopRow()
        {
            ItemShape shape = ItemShape.Parse("##", "#.", "#.");

            Assert.AreEqual("###\n..#", shape.RotateClockwise().ToPattern());
        }

        [Test]
        public void Rotate_FourTimes_ReturnsOriginal()
        {
            ItemShape shape = ItemShape.Parse("##", "#.", "#.");

            Assert.AreEqual(shape.ToPattern(), shape.Rotate(4).ToPattern());
        }

        [Test]
        public void Rotate_Twice_SwapsBounds()
        {
            ItemShape shape = ItemShape.Parse("###");

            ItemShape twice = shape.Rotate(2);

            Assert.AreEqual(3, twice.Width);
            Assert.AreEqual(1, twice.Height);
            Assert.AreEqual("###", twice.ToPattern());
        }

        [Test]
        public void Rotate_OneByThree_BecomesThreeByOne()
        {
            ItemShape shape = ItemShape.Parse("#", "#", "#");

            ItemShape rotated = shape.RotateClockwise();

            Assert.AreEqual(3, rotated.Width);
            Assert.AreEqual(1, rotated.Height);
            Assert.AreEqual("###", rotated.ToPattern());
        }

        [Test]
        public void Parse_NormalizesNegativeOffsets()
        {
            var shape = new ItemShape(new[]
            {
                new Cell(5, 3),
                new Cell(6, 3),
                new Cell(5, 4)
            });

            Assert.AreEqual("##\n#.", shape.ToPattern());
        }

        [Test]
        public void ToPattern_RoundTrips()
        {
            ItemShape shape = ItemShape.Parse("##.", ".#.", ".##");

            Assert.AreEqual(shape.ToPattern(), ItemShape.Parse(shape.ToPattern().Split('\n')).ToPattern());
        }
    }
}
