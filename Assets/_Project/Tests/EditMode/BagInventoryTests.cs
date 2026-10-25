using System;
using System.Collections.Generic;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class BagInventoryTests
    {
        [Test]
        public void TryAdd_PlacesFirstItemAtOrigin()
        {
            var inventory = new BagInventory(3, 2);

            PlacedItem item;
            Assert.IsTrue(inventory.TryAdd("chip", ItemShape.Parse("#"), out item));

            Assert.AreEqual(new Cell(0, 0), item.Origin);
            Assert.AreEqual(item.InstanceId, inventory.Grid.GetOccupant(0, 0));
            Assert.AreEqual(1, inventory.OccupiedCellCount);
            Assert.AreEqual(5, inventory.FreeCellCount);
        }

        [Test]
        public void TryAdd_FillsLeftToRightThenNextRow()
        {
            var inventory = new BagInventory(3, 2);
            ItemShape single = ItemShape.Parse("#");

            PlacedItem first;
            PlacedItem second;
            PlacedItem third;
            PlacedItem fourth;

            inventory.TryAdd("a", single, out first);
            inventory.TryAdd("a", single, out second);
            inventory.TryAdd("a", single, out third);
            inventory.TryAdd("a", single, out fourth);

            Assert.AreEqual(new Cell(0, 0), first.Origin);
            Assert.AreEqual(new Cell(1, 0), second.Origin);
            Assert.AreEqual(new Cell(2, 0), third.Origin);
            Assert.AreEqual(new Cell(0, 1), fourth.Origin);
        }

        [Test]
        public void TryAdd_FailsWhenBagIsFull()
        {
            var inventory = new BagInventory(2, 2);
            ItemShape single = ItemShape.Parse("#");

            Assert.IsTrue(inventory.TryAdd("a", single, out _));
            Assert.IsTrue(inventory.TryAdd("a", single, out _));
            Assert.IsTrue(inventory.TryAdd("a", single, out _));
            Assert.IsTrue(inventory.TryAdd("a", single, out _));

            PlacedItem rejected;
            Assert.IsFalse(inventory.TryAdd("a", single, out rejected));
            Assert.IsNull(rejected);
            Assert.AreEqual(4, inventory.OccupiedCellCount);
        }

        [Test]
        public void TryAdd_RotatesToFitNarrowBag()
        {
            var inventory = new BagInventory(1, 2);

            PlacedItem item;
            Assert.IsTrue(inventory.TryAdd("rod", ItemShape.Parse("##"), true, out item));

            Assert.AreEqual(1, item.Rotation);
            Assert.AreEqual(1, item.Shape.Width);
            Assert.AreEqual(2, item.Shape.Height);
        }

        [Test]
        public void TryAdd_DoesNotRotateNonRotatableItem()
        {
            var inventory = new BagInventory(1, 2);

            PlacedItem item;
            Assert.IsFalse(inventory.TryAdd("rod", ItemShape.Parse("##"), false, out item));
            Assert.IsNull(item);
        }

        [Test]
        public void TryAddAt_RejectsOccupiedCell()
        {
            var inventory = new BagInventory(3, 3);
            ItemShape single = ItemShape.Parse("#");

            PlacedItem first;
            inventory.TryAddAt("a", single, new Cell(1, 1), 0, true, out first, out _);

            PlacedItem second;
            string error;
            Assert.IsFalse(inventory.TryAddAt("a", single, new Cell(1, 1), 0, true, out second, out error));
            Assert.IsNull(second);
            Assert.IsNotNull(error);
            Assert.AreEqual(1, inventory.Count);
        }

        [Test]
        public void TryAddAt_RejectsOutOfBounds()
        {
            var inventory = new BagInventory(2, 2);
            ItemShape shape = ItemShape.Parse("##");

            PlacedItem item;
            string error;
            Assert.IsFalse(inventory.TryAddAt("a", shape, new Cell(1, 0), 0, true, out item, out error));
            Assert.IsNotNull(error);
        }

        [Test]
        public void TryMove_MovesWithinGrid()
        {
            var inventory = new BagInventory(4, 4);
            PlacedItem item;
            inventory.TryAdd("chip", ItemShape.Parse("#"), out item);

            string error;
            Assert.IsTrue(inventory.TryMove(item.InstanceId, new Cell(3, 3), out error));

            Assert.AreEqual(new Cell(3, 3), item.Origin);
            Assert.AreEqual(item.InstanceId, inventory.Grid.GetOccupant(3, 3));
            Assert.AreEqual(0, inventory.Grid.GetOccupant(0, 0));
        }

        [Test]
        public void TryMove_ToOccupiedCell_FailsWithoutChangingState()
        {
            var inventory = new BagInventory(3, 3);
            ItemShape single = ItemShape.Parse("#");

            PlacedItem first;
            PlacedItem second;
            inventory.TryAdd("a", single, out first);
            inventory.TryAdd("a", single, out second);

            List<Cell> before = first.GetOccupiedCells();
            string error;
            Assert.IsFalse(inventory.TryMove(first.InstanceId, second.Origin, out error));

            Assert.IsNotNull(error);
            Assert.AreEqual(new Cell(0, 0), first.Origin);
            CollectionAssert.AreEquivalent(before, first.GetOccupiedCells());
            Assert.AreEqual(first.InstanceId, inventory.Grid.GetOccupant(0, 0));
            Assert.AreEqual(second.InstanceId, inventory.Grid.GetOccupant(1, 0));
        }

        [Test]
        public void TryMove_OutOfBounds_FailsWithoutChangingState()
        {
            var inventory = new BagInventory(2, 2);
            PlacedItem item;
            inventory.TryAdd("chip", ItemShape.Parse("#"), out item);

            string error;
            Assert.IsFalse(inventory.TryMove(item.InstanceId, new Cell(2, 0), out error));
            Assert.AreEqual(new Cell(0, 0), item.Origin);
            Assert.AreEqual(item.InstanceId, inventory.Grid.GetOccupant(0, 0));
        }

        [Test]
        public void TryRotate_SucceedsWhenSpaceAllows()
        {
            var inventory = new BagInventory(3, 2);
            PlacedItem item;
            inventory.TryAddAt("rod", ItemShape.Parse("#", "#"), new Cell(0, 0), 0, true, out item, out _);

            string error;
            Assert.IsTrue(inventory.TryRotate(item.InstanceId, out error));

            Assert.AreEqual(1, item.Rotation);
            Assert.AreEqual(2, item.Shape.Width);
            Assert.AreEqual(1, item.Shape.Height);
            Assert.AreEqual(item.InstanceId, inventory.Grid.GetOccupant(1, 0));
            Assert.AreEqual(0, inventory.Grid.GetOccupant(0, 1));
        }

        [Test]
        public void TryRotate_FailsWhenRotatedShapeWouldLeaveGrid()
        {
            var inventory = new BagInventory(2, 2);
            PlacedItem item;
            inventory.TryAddAt("rod", ItemShape.Parse("#", "#"), new Cell(1, 0), 0, true, out item, out _);

            List<Cell> before = item.GetOccupiedCells();
            string error;
            Assert.IsFalse(inventory.TryRotate(item.InstanceId, out error));

            Assert.IsNotNull(error);
            Assert.AreEqual(0, item.Rotation);
            CollectionAssert.AreEquivalent(before, item.GetOccupiedCells());
            Assert.AreEqual(item.InstanceId, inventory.Grid.GetOccupant(1, 1));
        }

        [Test]
        public void TryRotate_FailsForNonRotatableItem()
        {
            var inventory = new BagInventory(3, 3);
            PlacedItem item;
            inventory.TryAdd("block", ItemShape.Parse("##", "##"), false, out item);

            string error;
            Assert.IsFalse(inventory.TryRotate(item.InstanceId, out error));
            Assert.AreEqual(0, item.Rotation);
        }

        [Test]
        public void GetAdjacentPairs_FindsEdgeNeighboursOnly()
        {
            var inventory = new BagInventory(4, 4);
            ItemShape single = ItemShape.Parse("#");

            PlacedItem a;
            PlacedItem b;
            PlacedItem c;
            inventory.TryAddAt("a", single, new Cell(0, 0), 0, true, out a, out _);
            inventory.TryAddAt("b", single, new Cell(1, 0), 0, true, out b, out _);
            inventory.TryAddAt("c", single, new Cell(3, 3), 0, true, out c, out _);

            IReadOnlyList<ItemPair> pairs = inventory.GetAdjacentPairs();

            Assert.AreEqual(1, pairs.Count);
            Assert.AreEqual(a.InstanceId, pairs[0].A);
            Assert.AreEqual(b.InstanceId, pairs[0].B);
            Assert.IsTrue(inventory.AreAdjacent(a.InstanceId, b.InstanceId));
            Assert.IsTrue(inventory.AreAdjacent(b.InstanceId, a.InstanceId));
            Assert.IsFalse(inventory.AreAdjacent(a.InstanceId, c.InstanceId));
        }

        [Test]
        public void GetAdjacentPairs_DoesNotDuplicateMultiCellContacts()
        {
            var inventory = new BagInventory(4, 4);
            PlacedItem wall;
            PlacedItem block;
            inventory.TryAddAt("wall", ItemShape.Parse("##", "##"), new Cell(0, 0), 0, true, out wall, out _);
            inventory.TryAddAt("block", ItemShape.Parse("##", "##"), new Cell(2, 0), 0, true, out block, out _);

            IReadOnlyList<ItemPair> pairs = inventory.GetAdjacentPairs();

            Assert.AreEqual(1, pairs.Count);
        }

        [Test]
        public void Remove_FreesCellsAndDropsItem()
        {
            var inventory = new BagInventory(3, 3);
            PlacedItem item;
            inventory.TryAdd("block", ItemShape.Parse("##", "##"), out item);

            Assert.IsTrue(inventory.Remove(item.InstanceId));

            Assert.IsFalse(inventory.TryGet(item.InstanceId, out _));
            Assert.AreEqual(0, inventory.OccupiedCellCount);
            Assert.AreEqual(0, inventory.Count);
        }

        [Test]
        public void RandomOperations_KeepGridConsistent()
        {
            var shapes = new[]
            {
                ItemShape.Parse("#"),
                ItemShape.Parse("##"),
                ItemShape.Parse("#", "#"),
                ItemShape.Parse("##", "##"),
                ItemShape.Parse("##", "#."),
                ItemShape.Parse("###"),
                ItemShape.Parse("#.", "##")
            };

            var random = new Random(20240521);
            var inventory = new BagInventory(6, 5);

            for (int step = 0; step < 3000; step++)
            {
                int operation = random.Next(100);

                if (operation < 45)
                {
                    ItemShape shape = shapes[random.Next(shapes.Length)];
                    PlacedItem added;
                    inventory.TryAdd("item", shape, true, out added);
                }
                else if (operation < 62)
                {
                    List<PlacedItem> items = Snapshot(inventory);
                    if (items.Count > 0)
                    {
                        PlacedItem item = items[random.Next(items.Count)];
                        var target = new Cell(
                            random.Next(-1, inventory.Grid.Width + 1),
                            random.Next(-1, inventory.Grid.Height + 1));
                        List<Cell> before = item.GetOccupiedCells();
                        string error;
                        bool moved = inventory.TryMove(item.InstanceId, target, out error);

                        if (moved)
                        {
                            Assert.AreEqual(target, item.Origin);
                        }
                        else
                        {
                            Assert.IsNotNull(error);
                            Assert.AreEqual(target == item.Origin ? target : item.Origin, item.Origin);
                            CollectionAssert.AreEquivalent(before, item.GetOccupiedCells());
                        }
                    }
                }
                else if (operation < 78)
                {
                    List<PlacedItem> items = Snapshot(inventory);
                    if (items.Count > 0)
                    {
                        PlacedItem item = items[random.Next(items.Count)];
                        List<Cell> before = item.GetOccupiedCells();
                        int rotationBefore = item.Rotation;
                        string error;
                        bool rotated = inventory.TryRotate(item.InstanceId, out error);

                        if (rotated)
                        {
                            Assert.AreNotEqual(rotationBefore, item.Rotation);
                        }
                        else
                        {
                            Assert.IsNotNull(error);
                            Assert.AreEqual(rotationBefore, item.Rotation);
                            CollectionAssert.AreEquivalent(before, item.GetOccupiedCells());
                        }
                    }
                }
                else
                {
                    List<PlacedItem> items = Snapshot(inventory);
                    if (items.Count > 0)
                    {
                        PlacedItem item = items[random.Next(items.Count)];
                        int cells = item.Shape.CellCount;
                        int occupiedBefore = inventory.OccupiedCellCount;

                        Assert.IsTrue(inventory.Remove(item.InstanceId));
                        Assert.IsFalse(inventory.TryGet(item.InstanceId, out _));
                        Assert.AreEqual(occupiedBefore - cells, inventory.OccupiedCellCount);
                    }
                }

                AssertConsistent(inventory);
            }
        }

        private static List<PlacedItem> Snapshot(BagInventory inventory)
        {
            var items = new List<PlacedItem>(inventory.Items);
            return items;
        }

        [Test]
        public void TryRotateRelocating_MovesToNearestFreeSpotWhenInPlaceFails()
        {
            var bag = new BagInventory(3, 2);
            PlacedItem wide;
            PlacedItem blocker;
            string error;
            Assert.IsTrue(bag.TryAddAt("wide", ItemShape.Parse("##"), new Cell(0, 0), 0, true, out wide, out error), error);
            Assert.IsTrue(bag.TryAddAt("blocker", ItemShape.Parse("#"), new Cell(0, 1), 0, true, out blocker, out error), error);

            Assert.IsFalse(bag.TryRotate(wide.InstanceId, out error), "原地旋转应被 blocker 挡住");
            Assert.IsTrue(bag.TryRotateRelocating(wide.InstanceId, out error), error);

            Assert.AreEqual(1, wide.Rotation);
            Assert.AreEqual(new Cell(1, 0), wide.Origin);
            Assert.AreEqual(wide.InstanceId, bag.Grid.GetOccupant(1, 0));
            Assert.AreEqual(wide.InstanceId, bag.Grid.GetOccupant(1, 1));
            Assert.AreEqual(blocker.InstanceId, bag.Grid.GetOccupant(0, 1));
        }

        [Test]
        public void TryRotateRelocating_FailsWhenNoSpaceAnywhere()
        {
            var bag = new BagInventory(2, 1);
            PlacedItem wide;
            string error;
            Assert.IsTrue(bag.TryAddAt("wide", ItemShape.Parse("##"), new Cell(0, 0), 0, true, out wide, out error), error);

            Assert.IsFalse(bag.TryRotateRelocating(wide.InstanceId, out error));
            Assert.AreEqual(0, wide.Rotation);
            Assert.AreEqual(new Cell(0, 0), wide.Origin);
        }

        private static void AssertConsistent(BagInventory inventory)
        {
            int occupied = 0;
            var seen = new HashSet<Cell>();

            foreach (PlacedItem item in inventory.Items)
            {
                List<Cell> cells = item.GetOccupiedCells();
                Assert.AreEqual(item.Shape.CellCount, cells.Count);

                foreach (Cell cell in cells)
                {
                    Assert.IsTrue(inventory.Grid.InBounds(cell.X, cell.Y), "Cell outside grid: " + cell);
                    Assert.AreEqual(item.InstanceId, inventory.Grid.GetOccupant(cell.X, cell.Y));
                    Assert.IsTrue(seen.Add(cell), "Two items share cell " + cell);
                }

                occupied += cells.Count;
            }

            Assert.AreEqual(occupied, inventory.OccupiedCellCount);
            Assert.AreEqual(inventory.Capacity, inventory.OccupiedCellCount + inventory.FreeCellCount);
        }
    }
}
