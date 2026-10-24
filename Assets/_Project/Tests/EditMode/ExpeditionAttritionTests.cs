using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    /// <summary>
    /// 阵亡只丢「本次探索中新捡到的」，自带装备连同已消耗的耐久原样带回。
    /// </summary>
    public class ExpeditionAttritionTests
    {
        private ItemCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = DefaultItemLibrary.CreateCatalog();
        }

        [Test]
        public void MarkOwned_CoversContainerContents()
        {
            var bag = new BagInventory(6, 5);
            PlacedItem blade = Add(bag, "blade");
            PlacedItem crate = Add(bag, "weapon_crate");
            PutInside(crate, "pulse");

            Assert.AreEqual(3, ExpeditionAttrition.MarkOwned(bag));
            Assert.IsTrue(blade.ExpeditionBound);
            Assert.IsTrue(crate.ExpeditionBound);
            Assert.IsTrue(crate.Container.Bag.Items[0].ExpeditionBound);
        }

        [Test]
        public void MarkOwned_OnNullBagIsZero()
        {
            Assert.AreEqual(0, ExpeditionAttrition.MarkOwned(null));
        }

        [Test]
        public void ClearMarks_ResetsEverything()
        {
            var bag = new BagInventory(6, 5);
            PlacedItem crate = Add(bag, "weapon_crate");
            PutInside(crate, "pulse");
            ExpeditionAttrition.MarkOwned(bag);

            ExpeditionAttrition.ClearMarks(bag);

            Assert.IsFalse(crate.ExpeditionBound);
            Assert.IsFalse(crate.Container.Bag.Items[0].ExpeditionBound);
        }

        [Test]
        public void Apply_KeepsOwnedItemsAndDropsNewLoot()
        {
            var bag = new BagInventory(6, 5);
            PlacedItem blade = Add(bag, "blade");
            ExpeditionAttrition.MarkOwned(bag);

            Add(bag, "scrap");

            ExpeditionAttrition.Report report = ExpeditionAttrition.Apply(bag);

            Assert.AreEqual(1, bag.Count);
            Assert.AreSame(blade, bag.Items[0]);
            Assert.AreEqual(1, report.Lost);
            Assert.AreEqual(1, report.Kept);
        }

        [Test]
        public void Apply_DropsLootStoredInsideOwnedContainer()
        {
            var bag = new BagInventory(6, 5);
            PlacedItem crate = Add(bag, "weapon_crate");
            ExpeditionAttrition.MarkOwned(bag);

            PutInside(crate, "pulse");

            ExpeditionAttrition.Report report = ExpeditionAttrition.Apply(bag);

            Assert.AreEqual(1, bag.Count);
            Assert.AreSame(crate, bag.Items[0]);
            Assert.AreEqual(0, crate.Container.Bag.Count);
            Assert.AreEqual(1, report.Lost);
        }

        [Test]
        public void Apply_KeepsOwnedItemsInsideOwnedContainer()
        {
            var bag = new BagInventory(6, 5);
            PlacedItem crate = Add(bag, "weapon_crate");
            PutInside(crate, "blade");
            ExpeditionAttrition.MarkOwned(bag);

            ExpeditionAttrition.Report report = ExpeditionAttrition.Apply(bag);

            Assert.AreEqual(1, bag.Count);
            Assert.AreEqual(1, crate.Container.Bag.Count);
            Assert.AreEqual(0, report.Lost);
        }

        [Test]
        public void Apply_RescuesOwnedItemFromLootedContainer()
        {
            var bag = new BagInventory(6, 5);
            PlacedItem blade = Add(bag, "blade");
            ExpeditionAttrition.MarkOwned(bag);

            PlacedItem crate = Add(bag, "weapon_crate");
            string error;
            Assert.IsTrue(
                ContainerOps.TryPut(crate.Container, bag, blade.InstanceId, _catalog, out error),
                error);

            ExpeditionAttrition.Report report = ExpeditionAttrition.Apply(bag);

            Assert.AreEqual(1, bag.Count);
            Assert.AreEqual("blade", bag.Items[0].DefinitionId);
            Assert.AreEqual(1, report.Lost);
            Assert.AreEqual(1, report.Rescued);
            Assert.AreEqual(0, report.Orphaned);
        }

        [Test]
        public void Apply_WithoutMarkingDropsEverything()
        {
            var bag = new BagInventory(6, 5);
            Add(bag, "blade");

            ExpeditionAttrition.Report report = ExpeditionAttrition.Apply(bag);

            Assert.AreEqual(0, bag.Count);
            Assert.AreEqual(0, report.Kept);
            Assert.AreEqual(1, report.Lost);
        }

        [Test]
        public void Apply_ReturnsConsumedDurabilityUnchanged()
        {
            var bag = new BagInventory(6, 5);
            PlacedItem blade = Add(bag, "blade");
            blade.Durability = 2;
            ExpeditionAttrition.MarkOwned(bag);

            Add(bag, "scrap");
            ExpeditionAttrition.Apply(bag);

            Assert.AreSame(blade, bag.Items[0]);
            Assert.AreEqual(2, blade.Durability);
        }

        [Test]
        public void Apply_RescuedItemKeepsConsumedDurability()
        {
            var bag = new BagInventory(6, 5);
            PlacedItem blade = Add(bag, "blade");
            blade.Durability = 2;
            ExpeditionAttrition.MarkOwned(bag);

            PlacedItem crate = Add(bag, "weapon_crate");
            string error;
            Assert.IsTrue(
                ContainerOps.TryPut(crate.Container, bag, blade.InstanceId, _catalog, out error),
                error);

            ExpeditionAttrition.Apply(bag);

            Assert.AreEqual(1, bag.Count);
            Assert.AreEqual(2, bag.Items[0].Durability);
        }

        [Test]
        public void ContainerRoundTrip_KeepsOwnedMark()
        {
            var bag = new BagInventory(6, 5);
            PlacedItem crate = Add(bag, "weapon_crate");
            PlacedItem blade = Add(bag, "blade");
            ExpeditionAttrition.MarkOwned(bag);

            string error;
            Assert.IsTrue(
                ContainerOps.TryPut(crate.Container, bag, blade.InstanceId, _catalog, out error),
                error);

            PlacedItem inner = crate.Container.Bag.Items[0];
            Assert.IsTrue(inner.ExpeditionBound, "挪进容器不该把自己带上船的装备判成战利品");

            ExpeditionAttrition.Report report = ExpeditionAttrition.Apply(bag);
            Assert.AreEqual(0, report.Lost);
            Assert.AreEqual(1, crate.Container.Bag.Count);
        }

        [Test]
        public void ContainerRoundTrip_DoesNotResetDurability()
        {
            var bag = new BagInventory(6, 5);
            PlacedItem crate = Add(bag, "weapon_crate");
            var source = new BagInventory(6, 5);
            PlacedItem blade = Add(source, "blade");
            blade.Durability = 3;

            string error;
            Assert.IsTrue(
                ContainerOps.TryPut(crate.Container, source, blade.InstanceId, _catalog, out error),
                error);

            PlacedItem inner = crate.Container.Bag.Items[0];
            Assert.AreEqual(3, inner.Durability);

            Assert.IsTrue(
                ContainerOps.TryTake(crate.Container, bag, inner.InstanceId, _catalog, out error),
                error);

            Assert.AreEqual(3, FindDurability(bag, "blade"));
        }

        private static int FindDurability(BagInventory bag, string definitionId)
        {
            for (int i = 0; i < bag.Items.Count; i++)
            {
                if (bag.Items[i].DefinitionId == definitionId)
                {
                    return bag.Items[i].Durability;
                }
            }

            return -1;
        }

        private PlacedItem Add(BagInventory bag, string itemId)
        {
            ItemDefinition definition = _catalog.Get(itemId);
            PlacedItem item;
            Assert.IsTrue(bag.TryAdd(itemId, definition.Shape, out item), "无法放入 " + itemId);
            ContainerOps.Ensure(item, definition);
            return item;
        }

        private void PutInside(PlacedItem container, string itemId)
        {
            var source = new BagInventory(6, 5);
            PlacedItem item = Add(source, itemId);
            string error;
            Assert.IsTrue(
                ContainerOps.TryPut(container.Container, source, item.InstanceId, _catalog, out error),
                error);
        }
    }
}
