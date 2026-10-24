using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class StackingTests
    {
        [Test]
        public void AddAmount_FillsExistingStackBeforeUsingAnotherCell()
        {
            var bag = new BagInventory(4, 2);
            ItemShape shape = ItemShape.Parse("#");

            int added = bag.AddAmount("ammo", shape, 10, 6);

            Assert.AreEqual(10, added);
            Assert.AreEqual(2, bag.Count);
            Assert.AreEqual(6, bag.Items[0].Count);
            Assert.AreEqual(4, bag.Items[1].Count);
            Assert.AreEqual(2, bag.OccupiedCellCount);
        }

        [Test]
        public void RemoveAmount_ConsumesAcrossStacksAndDeletesEmptyStacks()
        {
            var bag = new BagInventory(4, 2);
            ItemShape shape = ItemShape.Parse("#");
            bag.AddAmount("ammo", shape, 10, 6);

            Assert.IsTrue(bag.TryRemoveAmount("ammo", 8));
            Assert.AreEqual(2, bag.CountOf("ammo"));
            Assert.AreEqual(1, bag.Count);
            Assert.AreEqual(2, bag.Items[0].Count);

            Assert.IsTrue(bag.TryRemoveAmount("ammo", 2));
            Assert.AreEqual(0, bag.CountOf("ammo"));
            Assert.AreEqual(0, bag.Count);
            Assert.AreEqual(0, bag.OccupiedCellCount);
        }

        [Test]
        public void AddAmount_StopsWhenGridIsFull()
        {
            var bag = new BagInventory(2, 1);
            ItemShape shape = ItemShape.Parse("#");

            int added = bag.AddAmount("ammo", shape, 20, 6);

            Assert.AreEqual(12, added);
            Assert.AreEqual(12, bag.CountOf("ammo"));
            Assert.AreEqual(2, bag.Count);
        }

        [Test]
        public void ItemBitWallet_StoresAndSpendsABitStack()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();
            var stash = new BagInventory(6, 5);
            var wallet = new ItemBitWallet(stash, catalog);

            Assert.IsTrue(wallet.Add(300));
            Assert.AreEqual(300, wallet.Amount);
            Assert.AreEqual(1, stash.Count);
            Assert.AreEqual("bit", stash.Items[0].DefinitionId);
            Assert.AreEqual(300, stash.Items[0].Count);

            Assert.IsTrue(wallet.TrySpend(120));
            Assert.AreEqual(180, wallet.Amount);
            Assert.AreEqual(180, stash.Items[0].Count);
        }

        [Test]
        public void TryStackAt_MergesIntoAnExistingStack()
        {
            var bag = new BagInventory(3, 3);
            ItemShape shape = ItemShape.Parse("#");

            PlacedItem first;
            bag.TryAddAt("ammo", shape, new Cell(1, 1), 0, true, 4, out first, out _);

            PlacedItem merged;
            string error;
            Assert.IsTrue(bag.TryStackAt("ammo", shape, new Cell(1, 1), 0, 3, 60, out merged, out error), error);
            Assert.AreEqual(first.InstanceId, merged.InstanceId);
            Assert.AreEqual(7, bag.CountOf("ammo"));
            Assert.AreEqual(1, bag.Count);
        }

        [Test]
        public void TryAddOrStack_QuickMoveMergesBeforeUsingAnotherCell()
        {
            var bag = new BagInventory(3, 3);
            ItemShape shape = ItemShape.Parse("#");

            PlacedItem first;
            Assert.IsTrue(bag.TryAddAt("ammo", shape, new Cell(1, 1), 0, true, 4, out first, out _));

            PlacedItem merged;
            string error;
            Assert.IsTrue(bag.TryAddOrStack("ammo", shape, true, 3, 60, out merged, out error), error);
            Assert.AreEqual(first.InstanceId, merged.InstanceId);
            Assert.AreEqual(7, bag.CountOf("ammo"));
            Assert.AreEqual(1, bag.Count);
        }

        [Test]
        public void CampaignPaymentConsumesTheBitItemFromTheStash()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();
            var stash = new BagInventory(6, 5);
            var wallet = new ItemBitWallet(stash, catalog);
            wallet.Add(600);
            var campaign = new CampaignState(new[] { 600 }, wallet);

            for (int i = 0; i < 7; i++)
            {
                Assert.IsTrue(campaign.BeginExpedition());
                campaign.SettleExpedition();
            }

            Assert.AreEqual(CampaignStatus.Victory, campaign.Status);
            Assert.AreEqual(0, campaign.Bit);
            Assert.AreEqual(0, stash.CountOf("bit"));
        }

        [Test]
        public void ItemBitWallet_RejectsOverspendingWithoutChangingTheStack()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();
            var stash = new BagInventory(6, 5);
            var wallet = new ItemBitWallet(stash, catalog);
            wallet.Add(40);

            Assert.IsFalse(wallet.TrySpend(41));
            Assert.AreEqual(40, wallet.Amount);
            Assert.AreEqual(40, stash.CountOf("bit"));
        }
    }
}
