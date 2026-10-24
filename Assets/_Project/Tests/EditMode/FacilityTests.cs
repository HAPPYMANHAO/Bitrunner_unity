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

        private static PlacedItem AddContainer(BagInventory bag, ItemCatalog catalog, string containerId)
        {
            ItemDefinition definition = catalog.Get(containerId);
            PlacedItem placed;
            Assert.IsTrue(bag.TryAdd(containerId, definition.Shape, out placed), containerId);
            ContainerOps.Ensure(placed, definition);
            return placed;
        }

        private static void PutInside(PlacedItem container, BagInventory bag, ItemCatalog catalog, string itemId)
        {
            PlacedItem placed;
            Assert.IsTrue(bag.TryAdd(itemId, catalog.Get(itemId).Shape, out placed), itemId);
            string error;
            Assert.IsTrue(
                ContainerOps.TryPut(container.Container, bag, placed.InstanceId, catalog, out error),
                error);
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

            // 超频潜网的最低档是 T2，L1 潜网舱进不去——这是「设施门控」的第一道验证。
            Assert.IsFalse(pod.TryResolveDive(catalog, out plan, out error));
            Assert.IsTrue(pod.LastResolveBlockedByTier);
            StringAssert.Contains("Lv.2", error);

            var campaign = new CampaignState(new[] { 600 }, 500);
            BagInventory stash = StashWith(catalog, "scrap", "scrap", "wire");
            Assert.IsTrue(pod.TryUpgrade(campaign, stash, out error), error);

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

        [Test]
        public void DivePod_UpgradeUsesMaterialsStoredInsideStashContainer()
        {
            ItemCatalog catalog = Catalog();
            var pod = new DivePod();
            Add(pod.Grid, catalog, "dive_basic", 0, 0);
            var campaign = new CampaignState(new[] { 600 }, 500);
            var stash = new BagInventory(6, 5);
            PlacedItem parts = AddContainer(stash, catalog, "parts_case");
            PutInside(parts, stash, catalog, "scrap");
            PutInside(parts, stash, catalog, "scrap");
            PutInside(parts, stash, catalog, "wire");

            int bitCost = pod.UpgradeCost;
            Assert.AreEqual(0, stash.CountOf("scrap"), "顶层不应有材料，材料全在零件盒里");
            Assert.AreEqual(2, ItemSearch.CountOf(stash, "scrap"));
            Assert.AreEqual(1, ItemSearch.CountOf(stash, "wire"));

            string error;
            Assert.IsTrue(pod.TryUpgrade(campaign, stash, out error), error);

            Assert.AreEqual(2, pod.Level);
            Assert.AreEqual(500 - bitCost, campaign.Bit);
            Assert.AreEqual(0, ItemSearch.CountOf(stash, "scrap"));
            Assert.AreEqual(0, ItemSearch.CountOf(stash, "wire"));

            // 零件盒本身仍是玩家资产，只是被清空
            PlacedItem kept;
            Assert.IsTrue(stash.TryGet(parts.InstanceId, out kept), "容器物品不应被一起消耗掉");
            Assert.IsFalse(ContainerOps.HasContents(kept));
        }

        [Test]
        public void DivePod_UpgradePrefersTopLevelMaterialsBeforeContainer()
        {
            ItemCatalog catalog = Catalog();
            var pod = new DivePod();
            Add(pod.Grid, catalog, "dive_basic", 0, 0);
            var campaign = new CampaignState(new[] { 600 }, 500);
            var stash = new BagInventory(6, 5);
            PlacedItem parts = AddContainer(stash, catalog, "parts_case");
            PutInside(parts, stash, catalog, "scrap");
            PutInside(parts, stash, catalog, "scrap");
            Add(stash, catalog, "scrap", 4, 4);
            Add(stash, catalog, "wire", 0, 4);

            string error;
            Assert.IsTrue(pod.TryUpgrade(campaign, stash, out error), error);

            Assert.AreEqual(0, stash.CountOf("scrap"), "顶层废料应优先被吃完");
            Assert.AreEqual(0, stash.CountOf("wire"));
            Assert.AreEqual(1, parts.Container.Bag.CountOf("scrap"), "顶层够用时不应动容器里的存量");
        }

        [Test]
        public void BitMine_UpgradeKeepsBitAndContainerContentsWhenShort()
        {
            ItemCatalog catalog = Catalog();
            var mine = new BitMine();
            var campaign = new CampaignState(new[] { 600 }, 500);
            var stash = new BagInventory(6, 5);
            PlacedItem parts = AddContainer(stash, catalog, "parts_case");
            PutInside(parts, stash, catalog, "scrap");

            string error;
            Assert.IsFalse(mine.TryUpgrade(campaign, stash, out error), "缺导线时不应升级成功");
            Assert.AreEqual(500, campaign.Bit, "材料不足时 bit 必须原样保留");
            Assert.AreEqual(1, mine.Level);
            Assert.AreEqual(1, parts.Container.Bag.CountOf("scrap"), "失败时不应扣掉容器里的材料");
        }

        [Test]
        public void RequirementText_ShowsAvailableCountsIncludingContainerContents()
        {
            ItemCatalog catalog = Catalog();
            var stash = new BagInventory(6, 5);
            PlacedItem parts = AddContainer(stash, catalog, "parts_case");
            PutInside(parts, stash, catalog, "scrap");

            string text = FacilityUpgrade.RequirementText(catalog, stash, 150, new[] { "scrap", "wire" });

            StringAssert.Contains("废料×1", text);
            StringAssert.Contains("导线×1(有 0)", text);
            Assert.IsFalse(text.Contains("废料×1(有"), "零件盒里的废料应被算作已有");
        }
    }
}
