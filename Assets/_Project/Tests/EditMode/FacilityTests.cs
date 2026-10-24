using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class FacilityTests
    {
        private static ItemCatalog Catalog()
        {
            return DefaultItemLibrary.CreateCatalog();
        }

        private static void Add(BagInventory bag, ItemCatalog catalog, string id, int x, int y)
        {
            PlacedItem placed;
            string error;
            Assert.IsTrue(
                bag.TryAddAt(id, catalog.Get(id).Shape, new Cell(x, y), 0, true, out placed, out error),
                error);
        }

        private static BagInventory StashWith(ItemCatalog catalog, params string[] itemIds)
        {
            var stash = new BagInventory(6, 5);
            for (int i = 0; i < itemIds.Length; i++)
            {
                PlacedItem placed;
                Assert.IsTrue(stash.TryAdd(itemIds[i], catalog.Get(itemIds[i]).Shape, out placed), itemIds[i]);
            }

            return stash;
        }

        [Test]
        public void DivePod_StartsSmallAndCanHoldAKeyPlusCatalyst()
        {
            ItemCatalog catalog = Catalog();
            var pod = new DivePod();

            Assert.AreEqual(1, pod.Width);
            Assert.AreEqual(2, pod.Height);
            Assert.IsNotNull(catalog.Get("dive_wild"));
            Assert.AreEqual(1, catalog.Get("dive_wild").Shape.CellCount);
            Assert.AreEqual(2, catalog.Get("dive_military").Shape.CellCount);
            Assert.AreEqual(4, catalog.Get("dive_signal").Shape.CellCount);
        }

        [Test]
        public void DivePod_WildKeyBuildsAnEasyOutdoorDive()
        {
            ItemCatalog catalog = Catalog();
            var pod = new DivePod();
            Add(pod.Grid, catalog, "dive_wild", 0, 0);
            pod.EnsureSelection(catalog);

            DivePlan plan;
            string error;
            Assert.IsTrue(pod.TryResolveDive(catalog, out plan, out error), error);
            Assert.AreEqual("旷野", plan.Name);
            Assert.AreEqual(5, plan.Width);
            Assert.AreEqual(2, plan.EnemyCount);
            Assert.AreEqual(4, plan.LootCount);
            CollectionAssert.AreEquivalent(new[] { "wild_dog", "squirrel" }, plan.EnemyPool);
        }

        [Test]
        public void DivePod_UpgradeRequiresMaterialsAndKeepsBitWhenMissing()
        {
            ItemCatalog catalog = Catalog();
            var pod = new DivePod();
            Add(pod.Grid, catalog, "dive_basic", 0, 0);
            var campaign = new CampaignState(new[] { 600 }, 500);
            var emptyStash = new BagInventory(6, 5);

            string error;
            Assert.IsFalse(pod.TryUpgrade(campaign, emptyStash, out error));
            Assert.AreEqual(500, campaign.Bit);
            Assert.AreEqual(1, pod.Level);
        }

        [Test]
        public void DivePod_ResolvesBasicKey()
        {
            ItemCatalog catalog = Catalog();
            var pod = new DivePod();
            Add(pod.Grid, catalog, "dive_basic", 0, 0);
            pod.EnsureSelection(catalog);

            DivePlan plan;
            string error;
            Assert.IsTrue(pod.TryResolveDive(catalog, out plan, out error), error);
            Assert.AreEqual("基础潜网", plan.Name);
            Assert.IsFalse(plan.ConsumeKey);
            Assert.AreEqual("dive_basic", pod.SelectedKeyItemId);
        }

        [Test]
        public void DivePod_ComboConsumesCatalystAndKeepsKey()
        {
            ItemCatalog catalog = Catalog();
            var pod = new DivePod();
            Add(pod.Grid, catalog, "dive_basic", 0, 0);
            Add(pod.Grid, catalog, "chip", 0, 1);
            pod.EnsureSelection(catalog);

            DivePlan plan;
            string error;
            Assert.IsTrue(pod.TryResolveDive(catalog, out plan, out error), error);
            Assert.AreEqual("超频潜网", plan.Name);

            pod.Consume(plan, catalog);

            Assert.AreEqual(1, pod.Grid.Count);
            Assert.AreEqual("dive_basic", pod.Grid.Items[0].DefinitionId);
            Assert.AreEqual("dive_basic", pod.SelectedKeyItemId);
        }

        [Test]
        public void DivePod_UpgradeExpandsGridAndKeepsKey()
        {
            ItemCatalog catalog = Catalog();
            var pod = new DivePod();
            Add(pod.Grid, catalog, "dive_basic", 0, 0);
            var campaign = new CampaignState(new[] { 600 }, 500);
            BagInventory stash = StashWith(catalog, "scrap", "scrap", "wire");

            string error;
            Assert.IsTrue(pod.TryUpgrade(campaign, stash, out error), error);
            Assert.AreEqual(2, pod.Level);
            Assert.AreEqual(2, pod.Width);
            Assert.AreEqual(2, pod.Height);
            Assert.AreEqual(300, campaign.Bit);
            Assert.AreEqual(1, pod.Grid.Count);
            Assert.AreEqual(0, stash.CountOf("scrap"));
            Assert.AreEqual(0, stash.CountOf("wire"));
        }

        [Test]
        public void BitMine_FillsEachDayAndCollects()
        {
            var mine = new BitMine();
            mine.EnsureFilledForDay(1, 1);

            Assert.AreEqual(1, mine.FilledCells);
            Assert.AreEqual(BitMine.YieldPerCell, mine.StoredBit);
            Assert.AreEqual(BitMine.YieldPerCell, mine.Collect());
            Assert.AreEqual(0, mine.StoredBit);

            mine.EnsureFilledForDay(1, 1);
            Assert.AreEqual(0, mine.FilledCells);

            mine.EnsureFilledForDay(1, 2);
            Assert.AreEqual(1, mine.FilledCells);
        }

        [Test]
        public void BitMine_CollectsIntoBitItemStack()
        {
            ItemCatalog catalog = Catalog();
            var stash = new BagInventory(6, 5);
            var wallet = new ItemBitWallet(stash, catalog);
            var campaign = new CampaignState(new[] { 600 }, wallet);
            var mine = new BitMine();
            mine.EnsureFilledForDay(1, 1);

            bool collected;
            int amount = mine.TryCollect(campaign, out collected);

            Assert.IsTrue(collected);
            Assert.AreEqual(BitMine.YieldPerCell, amount);
            Assert.AreEqual(BitMine.OutputItemId, stash.Items[0].DefinitionId);
            Assert.AreEqual(amount, stash.CountOf("bit"));
            Assert.AreEqual(amount, campaign.Bit);
            Assert.AreEqual(0, mine.StoredBit);
        }

        [Test]
        public void BitMine_KeepsOutputWhenStashHasNoSpace()
        {
            ItemCatalog catalog = Catalog();
            var stash = new BagInventory(1, 1);
            Add(stash, catalog, "coin", 0, 0);
            var wallet = new ItemBitWallet(stash, catalog);
            var campaign = new CampaignState(new[] { 600 }, wallet);
            var mine = new BitMine();
            mine.EnsureFilledForDay(1, 1);

            bool collected;
            int amount = mine.TryCollect(campaign, out collected);

            Assert.IsFalse(collected);
            Assert.AreEqual(BitMine.YieldPerCell, amount);
            Assert.AreEqual(0, stash.CountOf("bit"));
            Assert.AreEqual(BitMine.YieldPerCell, mine.StoredBit);
        }

        [Test]
        public void BitMine_UpgradeExpandsCapacity()
        {
            ItemCatalog catalog = Catalog();
            var mine = new BitMine();
            mine.EnsureFilledForDay(1, 1);
            var campaign = new CampaignState(new[] { 600 }, 500);
            BagInventory stash = StashWith(catalog, "scrap", "wire");

            string error;
            Assert.IsTrue(mine.TryUpgrade(campaign, stash, out error), error);
            Assert.AreEqual(2, mine.Level);
            Assert.AreEqual(2, mine.Width);
            Assert.AreEqual(1, mine.Height);
            Assert.AreEqual(400, campaign.Bit);
            Assert.AreEqual(0, stash.CountOf("scrap"));
            Assert.AreEqual(0, stash.CountOf("wire"));

            mine.EnsureFilledForDay(1, 2);
            Assert.AreEqual(2, mine.FilledCells);
            Assert.AreEqual(10, mine.Collect());
        }

        [Test]
        public void CraftingStation_ConsumesRecipeAndAddsOutput()
        {
            ItemCatalog catalog = Catalog();
            var station = new CraftingStation();
            Add(station.Grid, catalog, "scrap", 0, 0);
            Add(station.Grid, catalog, "scrap", 1, 0);

            var stash = new BagInventory(6, 5);
            PlacedItem placed;
            string error;
            Assert.IsTrue(station.TryCraft(catalog, stash, out placed, out error), error);

            Assert.AreEqual("scrap_blade", placed.DefinitionId);
            Assert.AreEqual(1, stash.Count);
            Assert.AreEqual(0, station.Grid.Count);
        }

        [Test]
        public void CraftingStation_RefusesWithoutRecipe()
        {
            ItemCatalog catalog = Catalog();
            var station = new CraftingStation();
            Add(station.Grid, catalog, "scrap", 0, 0);

            var stash = new BagInventory(6, 5);
            PlacedItem placed;
            string error;
            Assert.IsFalse(station.TryCraft(catalog, stash, out placed, out error));
            Assert.AreEqual(1, station.Grid.Count);
            Assert.AreEqual(0, stash.Count);
        }

        [Test]
        public void CraftingStation_UpgradeExpandsGrid()
        {
            ItemCatalog catalog = Catalog();
            var station = new CraftingStation();
            var campaign = new CampaignState(new[] { 600 }, 500);
            BagInventory stash = StashWith(catalog, "scrap", "wire");

            string error;
            Assert.IsTrue(station.TryUpgrade(campaign, stash, out error), error);
            Assert.AreEqual(2, station.Level);
            Assert.AreEqual(3, station.Grid.Grid.Width);
            Assert.AreEqual(2, station.Grid.Grid.Height);
            Assert.AreEqual(350, campaign.Bit);
            Assert.AreEqual(0, stash.CountOf("scrap"));
            Assert.AreEqual(0, stash.CountOf("wire"));
        }
    }
}
