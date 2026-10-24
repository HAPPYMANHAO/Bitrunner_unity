using System.Collections.Generic;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    /// <summary>
    /// C-6 内容全套的完整性测试：T2–T5 装备 / 敌人 / 材料 / 地点 / 密钥 / 密钥盒。
    ///
    /// 这一批内容的价值不在单件数值，而在**档位能串起来**：
    /// 密钥体积按档递增 → 潜网舱要跟着升级 → 地点档位要匹配 → 掉落池每一档都得真有东西。
    /// 所以测试重点放在这些跨表关系上，而不是逐个物品断言伤害数字。
    /// </summary>
    public class TierContentTests
    {
        private static ItemCatalog Catalog()
        {
            return DefaultItemLibrary.CreateCatalog();
        }

        private static Dictionary<int, List<ItemDefinition>> GroupByTier(ItemCatalog catalog)
        {
            var tiers = new Dictionary<int, List<ItemDefinition>>();
            for (int tier = 1; tier <= ItemCatalog.MaxLevel; tier++)
            {
                tiers[tier] = new List<ItemDefinition>();
            }

            foreach (ItemDefinition definition in catalog.Definitions)
            {
                tiers[definition.Level].Add(definition);
            }

            return tiers;
        }

        private static int CountOfCategory(ItemCatalog catalog, int tier, ItemCategory category)
        {
            int count = 0;
            foreach (ItemDefinition definition in catalog.Definitions)
            {
                if (definition.Level == tier && definition.Category == category)
                {
                    count++;
                }
            }

            return count;
        }

        [Test]
        public void CatalogHasContentInEveryTier()
        {
            Dictionary<int, List<ItemDefinition>> tiers = GroupByTier(Catalog());

            for (int tier = 1; tier <= ItemCatalog.MaxLevel; tier++)
            {
                Assert.GreaterOrEqual(tiers[tier].Count, 6, "Tier " + tier + " 内容太少。");
            }
        }

        [Test]
        public void EveryTierFromTwoUpHasWeaponsAndArmor()
        {
            ItemCatalog catalog = Catalog();

            // T1 是教学区，废铁背心 + 短刺已经够用；T2 起每一档都必须能用「换装」表达成长。
            for (int tier = 2; tier <= ItemCatalog.MaxLevel; tier++)
            {
                Assert.GreaterOrEqual(CountOfCategory(catalog, tier, ItemCategory.Weapon), 3, "T" + tier + " 武器");
                Assert.GreaterOrEqual(CountOfCategory(catalog, tier, ItemCategory.Armor), 2, "T" + tier + " 护甲");
            }
        }

        [Test]
        public void HighTierGearStaysInsideTheDamageBudget()
        {
            ItemCatalog catalog = Catalog();

            // docs/ITEM_TIER_PLAN.md §4.4：1 能量的伤害上限 5/8/11/17/25，2 能量的上限 9/14/20/30/44。
            int[] oneEnergy = { 0, 5, 8, 11, 17, 25 };
            int[] twoEnergy = { 0, 9, 14, 20, 30, 44 };

            foreach (ItemDefinition definition in catalog.Definitions)
            {
                if (definition.Category != ItemCategory.Weapon || definition.Damage <= 0)
                {
                    continue;
                }

                int budget = definition.EnergyCost >= 2 ? twoEnergy[definition.Level] : oneEnergy[definition.Level];
                Assert.LessOrEqual(definition.Damage, budget, definition.Id + " 伤害超出 T" + definition.Level + " 预算");
            }

            // T4/T5 的价值由内容侧严格按曲线写，不靠事后调。
            foreach (ItemDefinition definition in catalog.Definitions)
            {
                if (definition.Level < 4)
                {
                    continue;
                }

                if (definition.Category == ItemCategory.Weapon || definition.Category == ItemCategory.Armor)
                {
                    int expected = definition.Level == 4 ? 290 : 700;
                    Assert.AreEqual(expected, definition.Value, definition.Id + " 价值偏离曲线");
                }
            }
        }

        [Test]
        public void HighTierGearIsTougherButNotImmortal()
        {
            ItemCatalog catalog = Catalog();

            // 耐久刻意只做 ×3.5（8→28）而不是 ×5：死亡已是足够惩罚，但不能变成永久物。
            foreach (ItemDefinition definition in catalog.Definitions)
            {
                if (!definition.HasDurability)
                {
                    continue;
                }

                Assert.LessOrEqual(definition.DurabilityMax, 28, definition.Id + " 耐久过高，装备消耗 sink 会归零");
            }

            Assert.GreaterOrEqual(catalog.Get("polymer_dagger").DurabilityMax, 21);
            Assert.GreaterOrEqual(catalog.Get("corona_aegis").ArmorMelee, 24);
        }

        [Test]
        public void KeyVolumeGrowsWithTier()
        {
            ItemCatalog catalog = Catalog();
            string[] keys = { "dive_wild", "dive_military", "dive_signal", "dive_deep", "dive_core" };
            int[] expectedCells = { 1, 2, 4, 6, 9 };
            int[] expectedTier = { 1, 2, 3, 4, 5 };

            for (int i = 0; i < keys.Length; i++)
            {
                ItemDefinition definition = catalog.Get(keys[i]);
                Assert.AreEqual(ItemCategory.Key, definition.Category, keys[i]);
                Assert.AreEqual(expectedTier[i], definition.Level, keys[i] + " 档位");
                Assert.AreEqual(expectedCells[i], definition.Shape.CellCount, keys[i] + " 格数必须随档递增");
            }
        }

        [Test]
        public void DeepestKeyNeedsAWidePod()
        {
            ItemCatalog catalog = Catalog();

            // 核心密钥 3×3：潜网舱 Lv.1（1×2）和 Lv.2（2×2）连放都放不下。
            ItemDefinition coreKey = catalog.Get("dive_core");
            Assert.AreEqual(3, coreKey.Shape.Width);
            Assert.AreEqual(3, coreKey.Shape.Height);

            var small = new BagInventory(DivePod.WidthForLevel(2), DivePod.HeightForLevel(2));
            PlacedItem placed;
            Assert.IsFalse(small.TryAdd("dive_core", coreKey.Shape, out placed), "Lv.2 舱不该放得下 3×3 密钥");

            var large = new BagInventory(DivePod.WidthForLevel(5), DivePod.HeightForLevel(5));
            Assert.IsTrue(large.TryAdd("dive_core", coreKey.Shape, out placed), "Lv.5 舱必须放得下");
        }

        [Test]
        public void KeyBoxTakesKeysAndNothingElse()
        {
            ItemCatalog catalog = Catalog();
            ContainerSpec spec = catalog.Get("key_box").ContainerSpec;

            Assert.IsTrue(spec.Accepts(catalog.Get("dive_wild")));
            Assert.IsTrue(spec.Accepts(catalog.Get("dive_core")));
            Assert.IsFalse(spec.Accepts(catalog.Get("blade")), "密钥盒不该收武器");
            Assert.IsFalse(spec.Accepts(catalog.Get("scrap")), "密钥盒不该收材料");
            Assert.IsFalse(spec.Accepts(catalog.Get("key_box")), "密钥盒不该自嵌套");
        }

        [Test]
        public void KeyBoxHoldsEveryKeyAtOnce()
        {
            ItemCatalog catalog = Catalog();
            ContainerSpec spec = catalog.Get("key_box").ContainerSpec;

            // 显式摆位而不是靠自动寻空：自动寻空是首次适配，一把 3×3 的密钥可能被挤到放不下。
            // 这里要验证的是「盒子够大」这个结论，不是装箱算法。
            string[] keys = { "dive_core", "dive_deep", "dive_signal", "dive_military", "dive_wild" };
            var slots = new[]
            {
                new Cell(0, 0), new Cell(3, 0), new Cell(3, 2), new Cell(0, 3), new Cell(2, 3)
            };

            var inner = new BagInventory(spec.Width, spec.Height);
            int used = 0;
            for (int i = 0; i < keys.Length; i++)
            {
                ItemDefinition definition = catalog.Get(keys[i]);
                Assert.IsTrue(spec.Accepts(definition), keys[i]);

                PlacedItem placed;
                string error;
                Assert.IsTrue(
                    inner.TryAddAt(definition.Id, definition.Shape, slots[i], 0, true, out placed, out error),
                    keys[i] + " 放不进密钥盒：" + error);
                used += definition.Shape.CellCount;
            }

            Assert.AreEqual(22, used);
            Assert.GreaterOrEqual(spec.Width * spec.Height, used);
        }

        [Test]
        public void KeyCanBeStoredInAKeyBoxOnTheStash()
        {
            ItemCatalog catalog = Catalog();
            var stash = new BagInventory(StorageRack.Width, StorageRack.HeightForLevel(1));

            ItemDefinition boxDefinition = catalog.Get("key_box");
            PlacedItem box;
            Assert.IsTrue(stash.TryAdd("key_box", boxDefinition.Shape, out box));
            ContainerOps.Ensure(box, boxDefinition);
            Assert.IsNotNull(box.Container);

            ItemDefinition keyDefinition = catalog.Get("dive_deep");
            PlacedItem key;
            Assert.IsTrue(stash.TryAdd("dive_deep", keyDefinition.Shape, out key));

            string error;
            Assert.IsTrue(ContainerOps.TryPut(box.Container, stash, key.InstanceId, catalog, out error), error);
            Assert.AreEqual(0, stash.CountOf("dive_deep"), "密钥应该已经进了盒子");
            Assert.AreEqual(1, box.Container.Bag.CountOf("dive_deep"));
        }

        [Test]
        public void PodCoversFiveLevelsWithMonotoneCapacity()
        {
            int previous = 0;
            for (int level = 1; level <= DivePod.MaxLevel; level++)
            {
                int capacity = DivePod.WidthForLevel(level) * DivePod.HeightForLevel(level);
                Assert.Greater(capacity, previous, "Lv." + level + " 容量必须比上一级大");
                previous = capacity;

                // 高度封顶 3 行：基地右侧面板只有 138px，再高会压到说明文字。
                Assert.LessOrEqual(DivePod.HeightForLevel(level), 3);
            }

            Assert.AreEqual(5, DivePod.MaxLevel);
        }

        [Test]
        public void PodUpgradeToLevelFiveKeepsTheKey()
        {
            ItemCatalog catalog = Catalog();
            var pod = new DivePod();
            BagInventory stash = StashWithUpgradeMaterials(catalog, 1);
            var campaign = new CampaignState(new[] { 600 }, 9000);

            PlacedItem placed;
            Assert.IsTrue(pod.Grid.TryAdd("dive_basic", catalog.Get("dive_basic").Shape, out placed));

            string error;
            for (int level = 2; level <= DivePod.MaxLevel; level++)
            {
                Assert.IsTrue(pod.TryUpgrade(campaign, stash, out error), "升到 Lv." + level + "：" + error);
                Assert.AreEqual(level, pod.Level);
                Assert.AreEqual(DivePod.WidthForLevel(level), pod.Grid.Grid.Width);
                Assert.AreEqual(DivePod.HeightForLevel(level), pod.Grid.Grid.Height);
                Assert.AreEqual(1, pod.Grid.CountOf("dive_basic"), "升级不该丢掉密钥");
            }

            Assert.IsFalse(pod.CanUpgrade);
            Assert.IsFalse(pod.TryUpgrade(campaign, stash, out error));
        }

        [Test]
        public void AdvancedLocationsRequireAMatchingPodLevel()
        {
            ItemCatalog catalog = Catalog();

            AssertBlockedUntil(catalog, "dive_deep", 4);
            AssertBlockedUntil(catalog, "dive_core", 5);
        }

        [Test]
        public void EveryLocationPoolHasContentInEveryTierItClaims()
        {
            ItemCatalog catalog = Catalog();

            for (int i = 0; i < DiveKeyLibrary.All.Count; i++)
            {
                DiveKeyDefinition definition = DiveKeyLibrary.All[i];
                AssertTieredPool(catalog, definition.DungeonName, definition.LootPool, definition.TierMin, definition.TierWeights);
            }

            for (int i = 0; i < DiveKeyLibrary.AllRecipes.Count; i++)
            {
                DiveRecipe recipe = DiveKeyLibrary.AllRecipes[i];
                AssertTieredPool(catalog, recipe.Name, recipe.LootPool, recipe.TierMin, recipe.TierWeights);
            }
        }

        [Test]
        public void EveryLootPoolAndEnemyPoolIdExists()
        {
            ItemCatalog catalog = Catalog();

            for (int i = 0; i < DiveKeyLibrary.All.Count; i++)
            {
                DiveKeyDefinition definition = DiveKeyLibrary.All[i];
                AssertPoolIds(catalog, definition.DungeonName, definition.LootPool, definition.EnemyPool);
            }

            for (int i = 0; i < DiveKeyLibrary.AllRecipes.Count; i++)
            {
                DiveRecipe recipe = DiveKeyLibrary.AllRecipes[i];
                AssertPoolIds(catalog, recipe.Name, recipe.LootPool, recipe.EnemyPool);
            }
        }

        [Test]
        public void EnemyRosterCoversTierTwoThroughFive()
        {
            var byTier = new Dictionary<int, int>();
            for (int tier = 1; tier <= 5; tier++)
            {
                byTier[tier] = 0;
            }

            for (int i = 0; i < EnemyLibrary.All.Count; i++)
            {
                byTier[EnemyLibrary.All[i].MaxItemLevel]++;
            }

            for (int tier = 2; tier <= 5; tier++)
            {
                Assert.GreaterOrEqual(byTier[tier], 4, "T" + tier + " 敌人不足 4 种");
            }

            Assert.GreaterOrEqual(EnemyLibrary.All.Count, 27);
        }

        [Test]
        public void BossIsTheToughestUnitAndSitsInTheCorePool()
        {
            ItemCatalog catalog = Catalog();
            EnemyArchetype boss = EnemyLibrary.Get("corona_mech");

            Assert.IsNotNull(boss);
            Assert.AreEqual(140, boss.MaxHp);
            Assert.AreEqual(5, boss.MaxItemLevel);

            for (int i = 0; i < EnemyLibrary.All.Count; i++)
            {
                EnemyArchetype other = EnemyLibrary.All[i];
                if (other.Id == boss.Id)
                {
                    continue;
                }

                Assert.Less(other.MaxHp, boss.MaxHp, other.Id + " 不该比 Boss 更厚");
            }

            CombatActor actor = EnemyLibrary.Create("corona_mech", catalog, 0);
            Assert.AreEqual(140, actor.MaxHp);
            Assert.LessOrEqual(actor.Bag.OccupiedCellCount, actor.Bag.Capacity);
            Assert.Greater(actor.Bag.Count, 0);
        }

        [Test]
        public void CraftStationGatesTierFourAndFiveRecipes()
        {
            ItemCatalog catalog = Catalog();
            var station = new CraftingStation();
            BagInventory stash = StashWithUpgradeMaterials(catalog, 2);
            var campaign = new CampaignState(new[] { 600 }, 9000);

            string error;
            Assert.IsTrue(station.TryUpgrade(campaign, stash, out error), error);
            Assert.IsTrue(station.TryUpgrade(campaign, stash, out error), error);
            Assert.AreEqual(3, station.Level);

            // 纳米缝合器需要的材料（光纤×2 + 修复包）= 5 格，Lv.3 的 3×3 工作台摆得下，但等级不够。
            PlacedItem placed;
            Assert.IsTrue(station.Grid.TryAdd("optic_fiber", catalog.Get("optic_fiber").Shape, out placed));
            Assert.IsTrue(station.Grid.TryAdd("optic_fiber", catalog.Get("optic_fiber").Shape, out placed));
            Assert.IsTrue(station.Grid.TryAdd("patch", catalog.Get("patch").Shape, out placed));

            Assert.IsNull(station.MatchingRecipe, "Lv.3 不该能做出 T4 配方");
            Assert.IsNotNull(station.LockedRecipe);
            Assert.AreEqual("nano_stitcher", station.LockedRecipe.Id);

            Assert.IsTrue(station.TryUpgrade(campaign, stash, out error), error);
            Assert.AreEqual(4, station.Level);
            Assert.IsNotNull(station.MatchingRecipe);
            Assert.AreEqual("nano_stitcher", station.MatchingRecipe.Id);

            PlacedItem output;
            Assert.IsTrue(station.TryCraft(catalog, stash, out output, out error), error);
            Assert.AreEqual("nano_stitcher", output.DefinitionId);
            Assert.AreEqual(0, station.Grid.Count, "材料应该被消耗掉");
        }

        [Test]
        public void FullProgressionChainReachesTheCoreDive()
        {
            ItemCatalog catalog = Catalog();
            var pod = new DivePod();
            BagInventory stash = StashWithUpgradeMaterials(catalog, 1);
            var campaign = new CampaignState(new[] { 600 }, 9000);

            string error;
            for (int level = 2; level <= DivePod.MaxLevel; level++)
            {
                Assert.IsTrue(pod.TryUpgrade(campaign, stash, out error), "升到 Lv." + level + "：" + error);
            }

            PlacedItem placed;
            Assert.IsTrue(pod.Grid.TryAdd("dive_core", catalog.Get("dive_core").Shape, true, out placed));

            DivePlan plan;
            Assert.IsTrue(pod.TryResolveDive(catalog, out plan, out error), error);
            Assert.AreEqual("核心潜网", plan.Name);
            Assert.AreEqual(5, plan.TierMin);
            Assert.AreEqual(100, plan.TierWeights[0]);
            CollectionAssert.Contains(plan.EnemyPool, "corona_mech");

            // 掉落池展开后只能剩 T5，不能漏出低档物品。
            string[] expanded = LootTierTable.Expand(plan.LootPool, catalog, plan.TierMin, plan.TierWeights);
            Assert.Greater(expanded.Length, 0);
            for (int i = 0; i < expanded.Length; i++)
            {
                Assert.AreEqual(5, catalog.Get(expanded[i]).Level, expanded[i]);
            }
        }

        [Test]
        public void DeepDiveDropsBothTierFourAndTierFive()
        {
            ItemCatalog catalog = Catalog();
            DiveKeyDefinition deep = null;
            for (int i = 0; i < DiveKeyLibrary.All.Count; i++)
            {
                if (DiveKeyLibrary.All[i].ItemId == "dive_deep")
                {
                    deep = DiveKeyLibrary.All[i];
                }
            }

            Assert.IsNotNull(deep);
            string[] expanded = LootTierTable.Expand(deep.LootPool, catalog, deep.TierMin, deep.TierWeights);

            bool hasFour = false;
            bool hasFive = false;
            for (int i = 0; i < expanded.Length; i++)
            {
                int tier = catalog.Get(expanded[i]).Level;
                hasFour |= tier == 4;
                hasFive |= tier == 5;
            }

            Assert.IsTrue(hasFour, "深空潜网必须有 T4 内容");
            Assert.IsTrue(hasFive, "深空潜网必须有 T5 内容（否则那 40% 权重是空的）");
        }

        // ---------- 辅助 ----------

        private static void AssertBlockedUntil(ItemCatalog catalog, string keyItemId, int requiredLevel)
        {
            for (int level = 1; level < requiredLevel; level++)
            {
                var pod = new DivePod();
                ForceLevel(pod, level);

                PlacedItem placed;
                if (!pod.Grid.TryAdd(keyItemId, catalog.Get(keyItemId).Shape, true, out placed))
                {
                    // 连摆都摆不进去 —— 这是比等级更硬的一道门，不算失败。
                    continue;
                }

                DivePlan plan;
                string error;
                Assert.IsFalse(pod.TryResolveDive(catalog, out plan, out error), "Lv." + level + " 不该能进 " + keyItemId);
                Assert.IsTrue(pod.LastResolveBlockedByTier, keyItemId + " 应被等级拦住");
            }

            var allowed = new DivePod();
            ForceLevel(allowed, requiredLevel);
            PlacedItem key;
            Assert.IsTrue(
                allowed.Grid.TryAdd(keyItemId, catalog.Get(keyItemId).Shape, true, out key),
                keyItemId + " 在 Lv." + requiredLevel + " 必须放得进舱里");

            DivePlan resolved;
            string resolveError;
            Assert.IsTrue(allowed.TryResolveDive(catalog, out resolved, out resolveError), resolveError);
        }

        /// <summary>
        /// 直接造一个指定等级的潜网舱。升级路径本身由 <see cref="PodUpgradeToLevelFiveKeepsTheKey"/> 覆盖，
        /// 这里只关心「等级 → 能否进入」这一条映射。
        /// </summary>
        private static void ForceLevel(DivePod pod, int level)
        {
            var campaign = new CampaignState(new[] { 600 }, 9000);
            BagInventory stash = StashWithUpgradeMaterials(Catalog(), 1);

            string error;
            while (pod.Level < level)
            {
                Assert.IsTrue(pod.TryUpgrade(campaign, stash, out error), error);
            }
        }

        private static void AssertTieredPool(
            ItemCatalog catalog,
            string name,
            string[] pool,
            int tierMin,
            int[] tierWeights)
        {
            if (pool == null || tierWeights == null || tierWeights.Length == 0)
            {
                return;
            }

            string[] expanded = LootTierTable.Expand(pool, catalog, tierMin, tierWeights);
            Assert.Greater(expanded.Length, 0, name + " 掉落池展开后为空");

            for (int tier = 0; tier < tierWeights.Length; tier++)
            {
                if (tierWeights[tier] <= 0)
                {
                    continue;
                }

                int expected = tierMin + tier;
                bool found = false;
                for (int i = 0; i < pool.Length; i++)
                {
                    ItemDefinition definition;
                    if (catalog.TryGet(pool[i], out definition) && definition.Level == expected)
                    {
                        found = true;
                        break;
                    }
                }

                Assert.IsTrue(found, name + " 声明了 T" + expected + " 权重，但池子里没有一件 T" + expected + " 物品");
            }
        }

        private static void AssertPoolIds(
            ItemCatalog catalog,
            string name,
            string[] lootPool,
            string[] enemyPool)
        {
            if (lootPool != null)
            {
                for (int i = 0; i < lootPool.Length; i++)
                {
                    ItemDefinition definition;
                    Assert.IsTrue(catalog.TryGet(lootPool[i], out definition), name + " 掉落池里有未知物品 '" + lootPool[i] + "'");
                }
            }

            if (enemyPool != null)
            {
                for (int i = 0; i < enemyPool.Length; i++)
                {
                    Assert.IsNotNull(EnemyLibrary.Get(enemyPool[i]), name + " 敌人池里有未知单位 '" + enemyPool[i] + "'");
                }
            }
        }

        /// <summary>
        /// 造一个够把所有设施升到满级的仓库。kind = 1 给潜网舱，kind = 2 给制作站。
        /// 两种材料表不同，混在一起会互相干扰，所以分开造。
        /// </summary>
        private static BagInventory StashWithUpgradeMaterials(ItemCatalog catalog, int kind)
        {
            var stash = new BagInventory(StorageRack.Width, StorageRack.HeightForLevel(3));
            string[] ids = kind == 1
                ? new[]
                {
                    "scrap", "scrap", "wire", "circuit", "alloy", "alloy",
                    "alloy", "alloy", "circuit", "servo", "superalloy", "superalloy", "plasma_cell"
                }
                : new[]
                {
                    "scrap", "wire", "alloy", "circuit", "circuit", "circuit", "servo",
                    "capacitor", "superalloy", "plasma_cell", "optic_fiber"
                };

            for (int i = 0; i < ids.Length; i++)
            {
                PlacedItem placed;
                Assert.IsTrue(stash.TryAdd(ids[i], catalog.Get(ids[i]).Shape, true, out placed), ids[i]);
            }

            return stash;
        }
    }
}
