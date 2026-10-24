using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    /// <summary>
    /// C-4 / C-7：中央处理器、保险网络中枢、仓储设备三件新设施，
    /// 以及扩容时必须保住的物品状态（耐久 / 容器内容物 / 实例身份）。
    /// </summary>
    public class FacilityExpansionTests
    {
        private static ItemDefinition Chip(string id, int level)
        {
            return new ItemDefinition
            {
                Id = id,
                DisplayName = id,
                Level = level,
                Category = ItemCategory.Tool,
                Activation = ItemActivation.Passive,
                Shape = ItemShape.Parse("#"),
                Tags = ItemTag.CpuChip,
                Traits = new[] { new TraitRef(TraitCatalog.HpChip, level) },
                EnergyCost = 0,
                Value = 20
            };
        }

        private static ItemDefinition NotAChip(string id)
        {
            return new ItemDefinition
            {
                Id = id,
                DisplayName = id,
                Level = 1,
                Category = ItemCategory.Tool,
                Activation = ItemActivation.Passive,
                Shape = ItemShape.Parse("#"),
                Tags = ItemTag.Tool,
                EnergyCost = 0,
                Value = 20
            };
        }

        // ---------- 中央处理器 ----------

        [Test]
        public void Cpu_StartsAtTwoByTwoAndOnlyTakesChips()
        {
            var cpu = new CpuCore();

            Assert.AreEqual(1, cpu.Level);
            Assert.AreEqual(2, cpu.Width);
            Assert.AreEqual(2, cpu.Height);
            Assert.AreEqual(1, cpu.MaxChipTier);
            Assert.IsTrue(CpuCore.CanAccept(Chip("c", 1)));
            Assert.IsFalse(CpuCore.CanAccept(NotAChip("wrench")));
            Assert.IsFalse(CpuCore.CanAccept(null));
        }

        [Test]
        public void Cpu_RejectsChipAboveItsTier()
        {
            var cpu = new CpuCore();

            Assert.IsTrue(cpu.Accepts(Chip("t1", 1)));
            Assert.IsFalse(cpu.Accepts(Chip("t3", 3)));
        }

        [Test]
        public void Cpu_LevelShapeTableMatchesDesign()
        {
            // 见 docs/ITEM_TIER_PLAN.md §6.2：2×2 / 3×3 / 3×4 / 4×4 / 5×4
            Assert.AreEqual(2, CpuCore.WidthForLevel(1));
            Assert.AreEqual(2, CpuCore.HeightForLevel(1));
            Assert.AreEqual(3, CpuCore.WidthForLevel(2));
            Assert.AreEqual(3, CpuCore.HeightForLevel(2));
            Assert.AreEqual(3, CpuCore.WidthForLevel(3));
            Assert.AreEqual(4, CpuCore.HeightForLevel(3));
            Assert.AreEqual(4, CpuCore.WidthForLevel(4));
            Assert.AreEqual(4, CpuCore.HeightForLevel(4));
            Assert.AreEqual(5, CpuCore.WidthForLevel(5));
            Assert.AreEqual(4, CpuCore.HeightForLevel(5));
        }

        [Test]
        public void Cpu_UpgradeExpandsGridAndKeepsChips()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(Chip("hp1", 1));
            var cpu = new CpuCore();
            PlacedItem placed;
            cpu.Grid.TryAdd("hp1", catalog.Get("hp1").Shape, out placed);
            placed.Durability = 3;

            var campaign = new CampaignState(new[] { 600 }, 2000);
            var stash = new BagInventory(4, 4);
            stash.TryAdd("scrap", ItemShape.Parse("#"), out _);
            stash.TryAdd("scrap", ItemShape.Parse("#"), out _);
            stash.TryAdd("wire", ItemShape.Parse("#"), out _);

            string error;
            Assert.IsTrue(cpu.TryUpgrade(campaign, stash, out error), error);

            Assert.AreEqual(2, cpu.Level);
            Assert.AreEqual(3, cpu.Width);
            Assert.AreEqual(3, cpu.Height);
            Assert.AreEqual(1600, campaign.Bit);
            Assert.AreEqual(1, cpu.Grid.Count);
            Assert.AreEqual(3, cpu.Grid.Items[0].Durability);
        }

        [Test]
        public void Cpu_UpgradeFailsWithoutMaterialsAndKeepsLevel()
        {
            var cpu = new CpuCore();
            var campaign = new CampaignState(new[] { 600 }, 2000);
            var stash = new BagInventory(4, 4);

            string error;
            Assert.IsFalse(cpu.TryUpgrade(campaign, stash, out error));
            Assert.AreEqual(1, cpu.Level);
            Assert.AreEqual(2000, campaign.Bit);
        }

        [Test]
        public void Cpu_FullLevelIsTerminal()
        {
            var cpu = new CpuCore();
            var campaign = new CampaignState(new[] { 600 }, 100000);
            var stash = new BagInventory(9, 9);
            for (int i = 0; i < 10; i++)
            {
                stash.TryAdd("scrap", ItemShape.Parse("#"), out _);
                stash.TryAdd("wire", ItemShape.Parse("#"), out _);
                stash.TryAdd("circuit", ItemShape.Parse("#"), out _);
                stash.TryAdd("alloy", ItemShape.Parse("#"), out _);
            }

            string error;
            for (int i = 0; i < 4; i++)
            {
                Assert.IsTrue(cpu.TryUpgrade(campaign, stash, out error), error);
            }

            Assert.AreEqual(5, cpu.Level);
            Assert.IsFalse(cpu.CanUpgrade);
            Assert.IsFalse(cpu.TryUpgrade(campaign, stash, out error));
        }

        // ---------- 仓储设备 / 保险中枢 ----------

        [Test]
        public void Storage_GrowsRowsOnlyAndKeepsItems()
        {
            var rack = new StorageRack();
            Assert.AreEqual(1, rack.Level);
            Assert.AreEqual(10, StorageRack.HeightForLevel(1));
            Assert.AreEqual(13, StorageRack.HeightForLevel(2));
            Assert.AreEqual(16, StorageRack.HeightForLevel(3));
            Assert.AreEqual(StorageRack.Width, 6);

            var campaign = new CampaignState(new[] { 600 }, 2000);
            var stash = new BagInventory(StorageRack.Width, StorageRack.HeightForLevel(1));
            PlacedItem plate;
            stash.TryAdd("plate", ItemShape.Parse("##", "##"), out plate);
            plate.Durability = 5;
            stash.TryAdd("scrap", ItemShape.Parse("#"), out _);
            stash.TryAdd("scrap", ItemShape.Parse("#"), out _);
            stash.TryAdd("scrap", ItemShape.Parse("#"), out _);
            stash.TryAdd("wire", ItemShape.Parse("#"), out _);

            string error;
            Assert.IsTrue(rack.TryUpgrade(campaign, stash, out error), error);

            Assert.IsTrue(stash.ResizeTo(StorageRack.Width, StorageRack.HeightForLevel(rack.Level)));
            Assert.AreEqual(13, stash.Grid.Height);
            Assert.AreEqual(0, stash.CountOf("scrap"));
            Assert.AreEqual(0, stash.CountOf("wire"));
            Assert.AreEqual(1, stash.Count);
            Assert.AreEqual(5, stash.Items[0].Durability);
        }

        [Test]
        public void Insurance_GrowsToThreeByThree()
        {
            var hub = new InsuranceHub();
            Assert.AreEqual(2, InsuranceHub.WidthForLevel(1));
            Assert.AreEqual(2, InsuranceHub.HeightForLevel(1));
            Assert.AreEqual(4, hub.CapacityFor(1));
            Assert.AreEqual(6, hub.CapacityFor(2));
            Assert.AreEqual(9, hub.CapacityFor(3));

            var campaign = new CampaignState(new[] { 600 }, 2000);
            var stash = new BagInventory(6, 10);
            stash.TryAdd("wire", ItemShape.Parse("#"), out _);
            stash.TryAdd("wire", ItemShape.Parse("#"), out _);
            stash.TryAdd("circuit", ItemShape.Parse("#"), out _);

            string error;
            Assert.IsTrue(hub.TryUpgrade(campaign, stash, out error), error);
            Assert.AreEqual(2, hub.Level);
            Assert.AreEqual(1700, campaign.Bit);
        }

        // ---------- 原地扩容 ----------

        [Test]
        public void ResizeTo_KeepsInstanceIdentityAndContainerContents()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();
            var bag = new BagInventory(4, 4);

            PlacedItem crate;
            bag.TryAdd("parts_case", catalog.Get("parts_case").Shape, out crate);
            ContainerOps.Ensure(crate, catalog.Get("parts_case"));
            PlacedItem inner;
            crate.Container.Bag.TryAdd("scrap", catalog.Get("scrap").Shape, out inner);

            int instanceId = crate.InstanceId;
            Assert.IsTrue(bag.ResizeTo(6, 10));

            PlacedItem after;
            Assert.IsTrue(bag.TryGet(instanceId, out after));
            Assert.AreSame(crate, after);
            Assert.IsNotNull(after.Container);
            Assert.AreEqual(1, after.Container.Bag.Count);
        }

        [Test]
        public void ResizeTo_RefusesWhenShrinkingBelowContent()
        {
            var bag = new BagInventory(3, 3);
            for (int i = 0; i < 9; i++)
            {
                bag.TryAdd("x", ItemShape.Parse("#"), out _);
            }

            Assert.AreEqual(9, bag.Count);
            Assert.IsFalse(bag.ResizeTo(2, 2));
            Assert.AreEqual(9, bag.Count);
            Assert.AreEqual(3, bag.Grid.Width);
            Assert.AreEqual(3, bag.Grid.Height);
        }

        // ---------- 芯片内容 ----------

        [Test]
        public void DefaultCatalogShipsAllFifteenChips()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();

            int chips = 0;
            int energyChips = 0;
            foreach (ItemDefinition definition in catalog.Definitions)
            {
                if (!CpuCore.CanAccept(definition))
                {
                    continue;
                }

                chips++;
                Assert.LessOrEqual(definition.Level, 5);
                if (definition.HasTrait(TraitCatalog.BaseEnergyChip))
                {
                    energyChips++;
                    // 增加能量必定有副作用。
                    Assert.Less(definition.GetTraitRank(TraitCatalog.EnergyDrawback), 6);
                    Assert.IsTrue(definition.HasTrait(TraitCatalog.EnergyDrawback));
                }
            }

            Assert.AreEqual(15, chips);
            Assert.AreEqual(5, energyChips);
        }

        [Test]
        public void ChipsAreNotEquippedByEnemies()
        {
            // 敌人配装的科技件槽按 ItemTag.Chip 过滤，芯片用的是 CpuChip，所以筛不进来。
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();
            foreach (ItemDefinition definition in catalog.Definitions)
            {
                if (CpuCore.CanAccept(definition))
                {
                    Assert.AreEqual(ItemTag.None, definition.Tags & ItemTag.Chip, definition.Id);
                }
            }
        }
    }
}
