using System.Collections.Generic;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    /// <summary>
    /// 档位门控：掉落、商店、敌人配装三道门都必须按 ItemDefinition.Level 收口。
    /// </summary>
    public class TierGateTests
    {
        private static ItemCatalog TieredCatalog()
        {
            return new ItemCatalog(new[]
            {
                Material("t1a", 1, 10),
                Material("t1b", 1, 12),
                Material("t1c", 1, 14),
                Material("t2a", 2, 50),
                Material("t4a", 4, 290)
            });
        }

        private static ItemDefinition Material(string id, int level, int value)
        {
            return new ItemDefinition
            {
                Id = id,
                DisplayName = id,
                Category = ItemCategory.Material,
                Shape = ItemShape.Parse("#"),
                Level = level,
                Value = value
            };
        }

        [Test]
        public void Expand_ExcludesItemsAboveTheTopTier()
        {
            ItemCatalog catalog = TieredCatalog();
            string[] pool = { "t1a", "t2a", "t4a" };

            string[] expanded = LootTierTable.Expand(pool, catalog, 1, new[] { 50, 50 });

            CollectionAssert.DoesNotContain(expanded, "t4a");
            CollectionAssert.Contains(expanded, "t2a");
        }

        [Test]
        public void Expand_ExcludesItemsBelowMinTier()
        {
            ItemCatalog catalog = TieredCatalog();
            string[] pool = { "t1a", "t2a", "t4a" };

            string[] expanded = LootTierTable.Expand(pool, catalog, 2, new[] { 50, 50, 50 });

            CollectionAssert.DoesNotContain(expanded, "t1a");
            CollectionAssert.Contains(expanded, "t2a");
            CollectionAssert.Contains(expanded, "t4a");
        }

        [Test]
        public void Expand_TierShareFollowsWeightNotItemCount()
        {
            ItemCatalog catalog = TieredCatalog();
            string[] pool = { "t1a", "t1b", "t1c", "t2a" };

            string[] expanded = LootTierTable.Expand(pool, catalog, 1, new[] { 50, 50 });

            int low = Count(expanded, "t1a") + Count(expanded, "t1b") + Count(expanded, "t1c");
            int high = Count(expanded, "t2a");

            // 高档只有一件物品，也只该吃掉它那一档的份额，不能因为件数少就变成稀有大放送。
            Assert.AreEqual(low, high, 30, "T2 份额 " + high + " / T1 份额 " + low);
        }

        [Test]
        public void Expand_WeightsCanStarveATier()
        {
            ItemCatalog catalog = TieredCatalog();
            string[] pool = { "t1a", "t2a" };

            string[] expanded = LootTierTable.Expand(pool, catalog, 1, new[] { 100, 0 });

            CollectionAssert.DoesNotContain(expanded, "t2a");
            CollectionAssert.Contains(expanded, "t1a");
        }

        [Test]
        public void Expand_WithoutCatalogKeepsPoolUnchanged()
        {
            string[] pool = { "t1a", "t2a" };

            Assert.AreSame(pool, LootTierTable.Expand(pool, null, 1, new[] { 50, 50 }));
        }

        [Test]
        public void Expand_UnknownIdsFallIntoLowestTierInsteadOfVanishing()
        {
            var catalog = new ItemCatalog(new[] { Material("t1a", 1, 10) });
            string[] pool = { "t1a", "not_in_catalog" };

            string[] expanded = LootTierTable.Expand(pool, catalog, 1, new[] { 100 });

            CollectionAssert.Contains(expanded, "not_in_catalog");
        }

        [Test]
        public void TopTier_ReportsHighestNonZeroTier()
        {
            Assert.AreEqual(3, LootTierTable.TopTier(1, new[] { 40, 40, 20 }));
            Assert.AreEqual(4, LootTierTable.TopTier(3, new[] { 70, 30 }));
            Assert.AreEqual(0, LootTierTable.TopTier(1, null));
        }

        [Test]
        public void BaseLootPool_NeverDropsTheTierFourIdol()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();
            string[] basePool = new List<string>(DiveKeyLibrary.BaseLoot).ToArray();

            string[] expanded = LootTierTable.Expand(basePool, catalog, 1, new[] { 40, 40, 20 });

            // 数据雕像是 T4，基础潜网只到 T3，它不该出现在起手地点的掉落里。
            CollectionAssert.DoesNotContain(expanded, "idol");
            CollectionAssert.Contains(expanded, "scrap");
        }

        [Test]
        public void DefaultCatalog_EveryItemHasAValidTier()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();

            foreach (ItemDefinition definition in catalog.Definitions)
            {
                Assert.GreaterOrEqual(definition.Level, 1, definition.Id);
                Assert.LessOrEqual(definition.Level, 5, definition.Id);
            }

            Assert.AreEqual(1, catalog.Get("scrap").Level);
            Assert.AreEqual(4, catalog.Get("idol").Level);
        }

        [Test]
        public void Merchant_WeekOneNeverOffersTierThree()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();
            var merchant = new Merchant(catalog);

            for (int seed = 1; seed <= 12; seed++)
            {
                merchant.RestockForDay(1, 1, seed);
                for (int i = 0; i < merchant.Offers.Count; i++)
                {
                    ItemDefinition definition = catalog.Get(merchant.Offers[i].ItemId);
                    Assert.LessOrEqual(definition.Level, 2, "W1 刷出了 " + definition.Id);
                }
            }
        }

        [Test]
        public void Merchant_MaxTierLadderByWeek()
        {
            Assert.AreEqual(2, Merchant.MaxTierForWeek(1));
            Assert.AreEqual(3, Merchant.MaxTierForWeek(2));
            Assert.AreEqual(3, Merchant.MaxTierForWeek(3));
            Assert.AreEqual(4, Merchant.MaxTierForWeek(4));
            Assert.AreEqual(5, Merchant.MaxTierForWeek(9));
        }

        [Test]
        public void Merchant_LaterWeeksDoOfferHigherTiers()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();
            var merchant = new Merchant(catalog);

            bool sawTier3 = false;
            for (int seed = 1; seed <= 20 && !sawTier3; seed++)
            {
                merchant.RestockForDay(3, 1, seed);
                for (int i = 0; i < merchant.Offers.Count; i++)
                {
                    if (catalog.Get(merchant.Offers[i].ItemId).Level >= 3)
                    {
                        sawTier3 = true;
                        break;
                    }
                }
            }

            Assert.IsTrue(sawTier3, "W3 商店应该能刷出 T3 装备");
        }

        [Test]
        public void EnemyLoadout_NeverExceedsArchetypeMaxItemLevel()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();

            foreach (EnemyArchetype archetype in EnemyLibrary.All)
            {
                for (int seed = 1; seed <= 6; seed++)
                {
                    CombatActor actor = EnemyLibrary.Create(archetype.Id, catalog, 0, seed);
                    for (int i = 0; i < actor.Bag.Items.Count; i++)
                    {
                        ItemDefinition definition = catalog.Get(actor.Bag.Items[i].DefinitionId);
                        Assert.LessOrEqual(
                            definition.Level,
                            archetype.MaxItemLevel,
                            archetype.Id + " 带了超档物品 " + definition.Id);
                    }
                }
            }
        }

        [Test]
        public void EnemyLoadout_WildDogStaysAtTierOne()
        {
            ItemCatalog catalog = DefaultItemLibrary.CreateCatalog();

            for (int seed = 1; seed <= 8; seed++)
            {
                CombatActor dog = EnemyLibrary.Create("wild_dog", catalog, 0, seed);
                for (int i = 0; i < dog.Bag.Items.Count; i++)
                {
                    Assert.AreEqual(1, catalog.Get(dog.Bag.Items[i].DefinitionId).Level);
                }
            }
        }

        private static int Count(string[] pool, string id)
        {
            int total = 0;
            for (int i = 0; i < pool.Length; i++)
            {
                if (pool[i] == id)
                {
                    total++;
                }
            }

            return total;
        }
    }
}
