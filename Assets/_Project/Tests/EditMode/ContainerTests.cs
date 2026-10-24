using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class ContainerTests
    {
        private static ItemCatalog Catalog()
        {
            return DefaultItemLibrary.CreateCatalog();
        }

        private static PlacedItem Add(BagInventory bag, ItemCatalog catalog, string itemId)
        {
            ItemDefinition definition = catalog.Get(itemId);
            PlacedItem item;
            Assert.IsTrue(bag.TryAdd(itemId, definition.Shape, out item));
            ContainerOps.Ensure(item, definition);
            return item;
        }

        [Test]
        public void WeaponCrate_HasIndependentInnerGrid()
        {
            ItemCatalog catalog = Catalog();
            var bag = new BagInventory(6, 5);
            PlacedItem crate = Add(bag, catalog, "weapon_crate");
            ItemContainer container = crate.Container;

            Assert.IsNotNull(container);
            Assert.AreEqual(8, container.Bag.Grid.Width);
            Assert.AreEqual(10, container.Bag.Grid.Height);
            Assert.AreEqual(80, container.Bag.Grid.Capacity);
            Assert.AreEqual(0, container.Bag.Count);
        }

        [Test]
        public void WeaponCrate_AcceptsWeaponAndRejectsArmor()
        {
            ItemCatalog catalog = Catalog();
            var parent = new BagInventory(6, 5);
            PlacedItem crate = Add(parent, catalog, "weapon_crate");
            var source = new BagInventory(6, 5);

            PlacedItem blade = Add(source, catalog, "blade");
            string error;
            Assert.IsTrue(ContainerOps.TryPut(crate.Container, source, blade.InstanceId, catalog, out error), error);
            Assert.AreEqual(1, crate.Container.Bag.Count);
            Assert.AreEqual(0, source.Count);

            PlacedItem plate = Add(source, catalog, "plate");
            Assert.IsFalse(ContainerOps.TryPut(crate.Container, source, plate.InstanceId, catalog, out error));
            Assert.AreEqual(1, source.Count);
            Assert.AreEqual(1, crate.Container.Bag.Count);
        }

        [Test]
        public void Container_RejectsNestedContainer()
        {
            ItemCatalog catalog = Catalog();
            var parent = new BagInventory(6, 5);
            PlacedItem crate = Add(parent, catalog, "weapon_crate");
            var source = new BagInventory(6, 5);
            PlacedItem parts = Add(source, catalog, "parts_case");

            string error;
            Assert.IsFalse(ContainerOps.TryPut(crate.Container, source, parts.InstanceId, catalog, out error));
            Assert.AreEqual(1, source.Count);
        }

        [Test]
        public void Container_TakesContentsBackToBag()
        {
            ItemCatalog catalog = Catalog();
            var parent = new BagInventory(6, 5);
            PlacedItem crate = Add(parent, catalog, "weapon_crate");
            var source = new BagInventory(6, 5);
            PlacedItem blade = Add(source, catalog, "blade");
            string error;
            Assert.IsTrue(ContainerOps.TryPut(crate.Container, source, blade.InstanceId, catalog, out error), error);

            var target = new BagInventory(6, 5);
            PlacedItem content = crate.Container.Bag.Items[0];
            Assert.IsTrue(ContainerOps.TryTake(crate.Container, target, content.InstanceId, catalog, out error), error);
            Assert.AreEqual(0, crate.Container.Bag.Count);
            Assert.AreEqual(1, target.Count);
        }

        [Test]
        public void TransferHelper_PreservesContainerReference()
        {
            ItemCatalog catalog = Catalog();
            var source = new BagInventory(6, 5);
            PlacedItem crate = Add(source, catalog, "weapon_crate");
            var target = new BagInventory(6, 5);
            PlacedItem moved;
            Assert.IsTrue(target.TryAdd("weapon_crate", catalog.Get("weapon_crate").Shape, out moved));

            ItemTransfer.PreserveAll(crate, moved);

            Assert.AreSame(crate.Container, moved.Container);
        }

        [Test]
        public void NonEmptyContainer_CannotBeSold()
        {
            ItemCatalog catalog = Catalog();
            var source = new BagInventory(6, 5);
            PlacedItem crate = Add(source, catalog, "weapon_crate");
            var contents = new BagInventory(6, 5);
            PlacedItem blade = Add(contents, catalog, "blade");
            string error;
            Assert.IsTrue(ContainerOps.TryPut(crate.Container, contents, blade.InstanceId, catalog, out error), error);

            var merchant = new Merchant(catalog);
            var campaign = new CampaignState(new[] { 600 }, 0);
            int value;
            Assert.IsFalse(merchant.TrySell(source, crate.InstanceId, campaign, out value, out error));
            Assert.IsNotNull(error);
            Assert.AreEqual(1, source.Count);
        }

        [Test]
        public void ContainerDescription_ShowsInnerSpaceAndTags()
        {
            ItemCatalog catalog = Catalog();
            ItemDefinition crate = catalog.Get("weapon_crate");
            string text = ItemText.Describe(crate);

            StringAssert.Contains("容器：内部 8×10", text);
            StringAssert.Contains("武器", text);
            StringAssert.Contains("标签：", text);
        }

        [Test]
        public void ItemSearch_CountsContentsInsideContainer()
        {
            ItemCatalog catalog = Catalog();
            var stash = new BagInventory(6, 5);
            PlacedItem parts = Add(stash, catalog, "parts_case");
            var source = new BagInventory(6, 5);
            PlacedItem scrap = Add(source, catalog, "scrap");
            PlacedItem wire = Add(source, catalog, "wire");
            string error;
            Assert.IsTrue(ContainerOps.TryPut(parts.Container, source, scrap.InstanceId, catalog, out error), error);
            Assert.IsTrue(ContainerOps.TryPut(parts.Container, source, wire.InstanceId, catalog, out error), error);

            Assert.AreEqual(0, stash.CountOf("scrap"), "顶层统计不应看到容器内容物");
            Assert.AreEqual(1, ItemSearch.CountOf(stash, "scrap"));
            Assert.AreEqual(1, ItemSearch.CountOf(stash, "wire"));
            Assert.IsTrue(ItemSearch.Has(stash, "scrap", 1));
            Assert.IsFalse(ItemSearch.Has(stash, "scrap", 2));
        }

        [Test]
        public void ItemSearch_ConsumesTopLevelBeforeContainer()
        {
            ItemCatalog catalog = Catalog();
            var stash = new BagInventory(6, 5);
            PlacedItem parts = Add(stash, catalog, "parts_case");
            var source = new BagInventory(6, 5);
            PlacedItem inner = Add(source, catalog, "scrap");
            string error;
            Assert.IsTrue(ContainerOps.TryPut(parts.Container, source, inner.InstanceId, catalog, out error), error);
            PlacedItem top = Add(stash, catalog, "scrap");

            Assert.IsTrue(ItemSearch.TryConsume(stash, "scrap", 1));
            PlacedItem stillThere;
            Assert.IsFalse(stash.TryGet(top.InstanceId, out stillThere), "顶层废料应优先被消耗");
            Assert.AreEqual(1, parts.Container.Bag.CountOf("scrap"));

            Assert.IsTrue(ItemSearch.TryConsume(stash, "scrap", 1));
            Assert.AreEqual(0, parts.Container.Bag.Count);
            Assert.AreEqual(0, ItemSearch.CountOf(stash, "scrap"));
        }

        [Test]
        public void ItemSearch_ConsumeLeavesBagUntouchedWhenShort()
        {
            ItemCatalog catalog = Catalog();
            var stash = new BagInventory(6, 5);
            Add(stash, catalog, "scrap");

            Assert.IsFalse(ItemSearch.TryConsume(stash, "scrap", 2));
            Assert.AreEqual(1, stash.CountOf("scrap"));
            Assert.AreEqual(1, ItemSearch.CountOf(stash, "scrap"));
        }

        [Test]
        public void ItemSearch_ConsumeAllIsAtomicAcrossIds()
        {
            ItemCatalog catalog = Catalog();
            var stash = new BagInventory(6, 5);
            Add(stash, catalog, "scrap");

            var amounts = new System.Collections.Generic.Dictionary<string, int>
            {
                { "scrap", 1 },
                { "wire", 1 }
            };

            Assert.IsFalse(ItemSearch.TryConsumeAll(stash, amounts));
            Assert.AreEqual(1, stash.CountOf("scrap"), "整批校验失败时不应扣掉任何材料");
        }
    }
}
