using System.Collections.Generic;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class MerchantTests
    {
        private static ItemCatalog CreateCatalog()
        {
            return CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##", 40),
                CombatTestFactory.Weapon("rod", 8, 2, 1, ItemTag.Blade, "#\n#\n#", 65),
                CombatTestFactory.Passive("plate", 4, 0, ItemTag.Shield | ItemTag.Armor, "##\n##", 50));
        }

        [Test]
        public void Restock_IsDeterministicAndPriced()
        {
            ItemCatalog catalog = CreateCatalog();
            var merchant = new Merchant(catalog);

            merchant.Restock(7, 2);
            var firstIds = new List<string>();
            for (int i = 0; i < merchant.Offers.Count; i++)
            {
                firstIds.Add(merchant.Offers[i].ItemId);
                Assert.AreEqual(catalog.Get(merchant.Offers[i].ItemId).Value * 3, merchant.Offers[i].Price);
                Assert.AreEqual(1, merchant.Offers[i].Remaining);
            }

            merchant.Restock(7, 2);
            var secondIds = new List<string>();
            for (int i = 0; i < merchant.Offers.Count; i++)
            {
                secondIds.Add(merchant.Offers[i].ItemId);
            }

            CollectionAssert.AreEqual(firstIds, secondIds);
            Assert.AreEqual(2, merchant.Offers.Count);
        }

        [Test]
        public void LootIsNotSoldByMerchant()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##", 40),
                CombatTestFactory.Loot("coin", 40, "#"));

            var merchant = new Merchant(catalog);

            CollectionAssert.DoesNotContain(merchant.EligibleItemIds, "coin");
            CollectionAssert.Contains(merchant.EligibleItemIds, "blade");
        }

        [Test]
        public void Restock_BuildsShelfWithOfferInstances()
        {
            ItemCatalog catalog = CreateCatalog();
            var merchant = new Merchant(catalog);
            merchant.Restock(7, 2);

            Assert.AreEqual(2, merchant.Stock.Count);

            for (int i = 0; i < merchant.Offers.Count; i++)
            {
                MerchantOffer offer = merchant.Offers[i];
                Assert.AreNotEqual(0, offer.InstanceId);

                PlacedItem stockItem;
                Assert.IsTrue(merchant.Stock.TryGet(offer.InstanceId, out stockItem));
                Assert.AreEqual(offer.ItemId, stockItem.DefinitionId);
            }
        }

        [Test]
        public void Restock_DefaultCatalogFillsSixItemShelf()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();
            var merchant = new Merchant(catalog);

            for (int seed = 1; seed <= 20; seed++)
            {
                merchant.Restock(seed, Merchant.DefaultStockSize);
                Assert.AreEqual(Merchant.DefaultStockSize, merchant.Offers.Count, "seed " + seed);
                Assert.AreEqual(Merchant.DefaultStockSize, merchant.Stock.Count, "seed " + seed);
            }
        }

        [Test]
        public void RestockForDay_TracksDayAndRefreshesNextDay()
        {
            ItemCatalog catalog = CreateCatalog();
            var merchant = new Merchant(catalog);
            merchant.RestockForDay(1, 1, 7, 2);

            Assert.IsTrue(merchant.HasStockFor(1, 1));
            Assert.AreEqual(2, merchant.Offers.Count);

            var campaign = new CampaignState(new[] { 600 }, 500);
            var stash = new BagInventory(6, 5);
            PlacedItem placed;
            string error;
            Assert.IsTrue(merchant.TryBuy(0, campaign, stash, out placed, out error), error);
            Assert.AreEqual(1, merchant.Stock.Count);
            Assert.AreEqual(0, merchant.Offers[0].Remaining);

            merchant.RestockForDay(1, 2, 8, 2);

            Assert.IsTrue(merchant.HasStockFor(1, 2));
            Assert.AreEqual(2, merchant.Offers.Count);
            Assert.AreEqual(2, merchant.Stock.Count);
            for (int i = 0; i < merchant.Offers.Count; i++)
            {
                Assert.AreEqual(1, merchant.Offers[i].Remaining);
            }
        }

        [Test]
        public void TryBuyAt_PlacesAtRequestedCellAndRemovesShelfItem()
        {
            ItemCatalog catalog = CreateCatalog();
            var merchant = new Merchant(catalog);
            merchant.Restock(7, 3);

            int offerIndex = -1;
            for (int i = 0; i < merchant.Offers.Count; i++)
            {
                if (merchant.Offers[i].ItemId == "blade")
                {
                    offerIndex = i;
                    break;
                }
            }

            Assert.GreaterOrEqual(offerIndex, 0);
            MerchantOffer offer = merchant.Offers[offerIndex];

            var campaign = new CampaignState(new[] { 600 }, 500);
            var stash = new BagInventory(6, 5);
            var target = new Cell(3, 2);

            PlacedItem placed;
            string error;
            Assert.IsTrue(merchant.TryBuyAt(offerIndex, campaign, stash, target, 0, out placed, out error), error);
            Assert.IsNotNull(placed);
            Assert.AreEqual(target, placed.Origin);
            Assert.AreEqual(500 - offer.Price, campaign.Bit);
            Assert.AreEqual(0, offer.Remaining);
            Assert.AreEqual(0, offer.InstanceId);
            Assert.AreEqual(2, merchant.Stock.Count);
        }

        [Test]
        public void SellToStock_ListsBuybackAndAllowsRepurchase()
        {
            ItemCatalog catalog = CreateCatalog();
            var merchant = new Merchant(catalog);
            var campaign = new CampaignState(new[] { 600 }, 0);
            var source = new BagInventory(2, 2);

            PlacedItem item;
            source.TryAdd("blade", catalog.Get("blade").Shape, out item);

            int value;
            string error;
            Assert.IsTrue(
                merchant.TrySellToStock(source, item.InstanceId, campaign, Cell.Zero, 0, out value, out error),
                error);

            Assert.AreEqual(40, value);
            Assert.AreEqual(40, campaign.Bit);
            Assert.AreEqual(0, source.Count);
            Assert.AreEqual(1, merchant.Stock.Count);
            Assert.AreEqual(1, merchant.Offers.Count);
            Assert.IsTrue(merchant.Offers[0].IsBuyback);
            Assert.AreEqual(40, merchant.Offers[0].Price);

            var stash = new BagInventory(6, 5);
            PlacedItem bought;
            Assert.IsTrue(merchant.TryBuyAt(0, campaign, stash, Cell.Zero, 0, out bought, out error), error);
            Assert.AreEqual(0, campaign.Bit);
            Assert.AreEqual(0, merchant.Stock.Count);
            Assert.AreEqual(1, stash.Count);
        }

        [Test]
        public void StockItemCanBeRearrangedAndStillBoughtBack()
        {
            ItemCatalog catalog = CreateCatalog();
            var merchant = new Merchant(catalog);
            merchant.Restock(7, 1);

            PlacedItem stockItem = merchant.Stock.Items[0];
            MerchantOffer offer = merchant.Offers[0];
            var target = new Cell(4, 4);
            string error;
            Assert.IsTrue(merchant.Stock.TryTransform(stockItem.InstanceId, target, 0, out error), error);
            Assert.AreEqual(target, stockItem.Origin);
            Assert.AreEqual(offer.InstanceId, stockItem.InstanceId);
            Assert.AreEqual(0, merchant.FindOfferIndexByInstance(stockItem.InstanceId));

            var campaign = new CampaignState(new[] { 600 }, 500);
            var stash = new BagInventory(6, 5);
            PlacedItem bought;
            Assert.IsTrue(merchant.TryBuyAt(0, campaign, stash, new Cell(0, 0), 0, out bought, out error), error);
            Assert.AreEqual(stockItem.DefinitionId, bought.DefinitionId);
        }

        [Test]
        public void SellToStock_RefusesOccupiedShelfCell()
        {
            ItemCatalog catalog = CreateCatalog();
            var merchant = new Merchant(catalog);
            var campaign = new CampaignState(new[] { 600 }, 0);
            var source = new BagInventory(2, 2);

            PlacedItem blocker;
            merchant.Stock.TryAdd("plate", catalog.Get("plate").Shape, out blocker);

            PlacedItem item;
            source.TryAdd("blade", catalog.Get("blade").Shape, out item);

            int value;
            string error;
            Assert.IsFalse(
                merchant.TrySellToStock(source, item.InstanceId, campaign, Cell.Zero, 0, out value, out error));
            Assert.AreEqual(0, campaign.Bit);
            Assert.AreEqual(1, source.Count);
            Assert.AreEqual(1, merchant.Stock.Count);
            Assert.AreEqual(0, merchant.Offers.Count);
        }

        [Test]
        public void Buy_SpendsBitAndAddsToStash()
        {
            ItemCatalog catalog = CreateCatalog();
            var merchant = new Merchant(catalog);
            merchant.Restock(3, 1);

            var campaign = new CampaignState(new[] { 600 }, 500);
            var stash = new BagInventory(6, 5);

            MerchantOffer offer = merchant.Offers[0];
            PlacedItem placed;
            string error;
            Assert.IsTrue(merchant.TryBuy(0, campaign, stash, out placed, out error), error);

            Assert.IsNotNull(placed);
            Assert.AreEqual(offer.ItemId, placed.DefinitionId);
            Assert.AreEqual(500 - offer.Price, campaign.Bit);
            Assert.AreEqual(0, offer.Remaining);
            Assert.AreEqual(1, stash.Count);
        }

        [Test]
        public void Buy_FailsWithoutBit()
        {
            ItemCatalog catalog = CreateCatalog();
            var merchant = new Merchant(catalog);
            merchant.Restock(3, 1);

            var campaign = new CampaignState(new[] { 600 }, 0);
            var stash = new BagInventory(6, 5);

            PlacedItem placed;
            string error;
            Assert.IsFalse(merchant.TryBuy(0, campaign, stash, out placed, out error));
            Assert.IsNull(placed);
            Assert.IsNotNull(error);
            Assert.AreEqual(0, campaign.Bit);
            Assert.AreEqual(0, stash.Count);
            Assert.AreEqual(1, merchant.Offers[0].Remaining);
        }

        [Test]
        public void Buy_FailsWhenStashIsFullWithoutCharging()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Passive("plate", 4, 0, ItemTag.Shield | ItemTag.Armor, "##\n##", 50));

            var merchant = new Merchant(catalog);
            merchant.Restock(1, 1);

            var campaign = new CampaignState(new[] { 600 }, 500);
            var stash = new BagInventory(1, 1);

            PlacedItem placed;
            string error;
            Assert.IsFalse(merchant.TryBuy(0, campaign, stash, out placed, out error));
            Assert.IsNull(placed);
            Assert.AreEqual(500, campaign.Bit);
            Assert.AreEqual(0, stash.Count);
            Assert.AreEqual(1, merchant.Offers[0].Remaining);
        }

        [Test]
        public void Sell_RemovesItemAndAddsValue()
        {
            ItemCatalog catalog = CreateCatalog();
            var merchant = new Merchant(catalog);
            var campaign = new CampaignState(new[] { 600 }, 0);
            var source = new BagInventory(2, 2);

            PlacedItem item;
            source.TryAdd("blade", catalog.Get("blade").Shape, out item);

            int value;
            string error;
            Assert.IsTrue(merchant.TrySell(source, item.InstanceId, campaign, out value, out error), error);

            Assert.AreEqual(40, value);
            Assert.AreEqual(40, campaign.Bit);
            Assert.AreEqual(0, source.Count);
        }

        [Test]
        public void Restock_ResetsSoldOutOffers()
        {
            ItemCatalog catalog = CreateCatalog();
            var merchant = new Merchant(catalog);
            merchant.Restock(7, 2);

            var campaign = new CampaignState(new[] { 600 }, 500);
            var stash = new BagInventory(6, 5);
            PlacedItem placed;
            string error;
            Assert.IsTrue(merchant.TryBuy(0, campaign, stash, out placed, out error), error);
            Assert.AreEqual(0, merchant.Offers[0].Remaining);

            merchant.Restock(7, 2);
            Assert.AreEqual(1, merchant.Offers[0].Remaining);
        }
    }
}
