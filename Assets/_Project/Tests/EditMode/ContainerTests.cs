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

            ItemTransfer.PreserveContainer(crate, moved);

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
    }
}
