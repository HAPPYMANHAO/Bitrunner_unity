using System;
using System.Collections.Generic;
using System.Text;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class EnemyLibraryTests
    {
        private static ItemCatalog Catalog()
        {
            return DefaultItemLibrary.CreateCatalog();
        }

        [Test]
        public void LibraryContainsAtLeastSevenArchetypes()
        {
            Assert.GreaterOrEqual(EnemyLibrary.All.Count, 7);
        }

        [Test]
        public void EveryArchetypeFitsItsBagAndUsesKnownItems()
        {
            ItemCatalog catalog = Catalog();
            var names = new HashSet<string>();

            for (int i = 0; i < EnemyLibrary.All.Count; i++)
            {
                EnemyArchetype archetype = EnemyLibrary.All[i];
                Assert.IsTrue(names.Add(archetype.DisplayName), "Duplicate name: " + archetype.DisplayName);
                Assert.Greater(archetype.MaxHp, 0);
                Assert.Greater(archetype.BaseEnergy, 0);
                Assert.IsNotNull(EnemyLibrary.Get(archetype.Id));

                CombatActor actor = EnemyLibrary.Create(archetype.Id, catalog, 0);
                Assert.AreEqual(archetype.MaxHp, actor.MaxHp);
                Assert.AreEqual(archetype.BaseEnergy, actor.BaseEnergy);
                Assert.AreEqual(archetype.DisplayName, actor.DisplayName);
                Assert.Greater(actor.Bag.Count, 0);
                Assert.LessOrEqual(actor.Bag.OccupiedCellCount, actor.Bag.Capacity);
                Assert.IsNotNull(actor.Brain);

                for (int itemIndex = 0; itemIndex < archetype.Items.Length; itemIndex++)
                {
                    EnemyItemPlacement placement = archetype.Items[itemIndex];
                    Assert.IsNotNull(catalog.Get(placement.ItemId), placement.ItemId);
                    Assert.GreaterOrEqual(placement.X, 0);
                    Assert.GreaterOrEqual(placement.Y, 0);
                    Assert.Less(placement.X, EnemyLibrary.BagWidth);
                    Assert.Less(placement.Y, EnemyLibrary.BagHeight);
                }
            }
        }

        [Test]
        public void CreateAppliesBonusHp()
        {
            CombatActor actor = EnemyLibrary.Create("bulwark", Catalog(), 9);

            Assert.AreEqual(34 + 9, actor.MaxHp);
            Assert.AreEqual(34 + 9, actor.Hp);
        }

        [Test]
        public void PickTypesReturnsDistinctEnemies()
        {
            string[] types = EnemyLibrary.PickTypes(new Random(7), 5);
            var distinct = new HashSet<string>(types);

            Assert.AreEqual(5, types.Length);
            Assert.AreEqual(5, distinct.Count);
        }

        [Test]
        public void PickMinionNeverReturnsPrimary()
        {
            for (int i = 0; i < 30; i++)
            {
                string minion = EnemyLibrary.PickMinion(1234, "shocker", i);
                Assert.AreNotEqual("shocker", minion);
                Assert.IsNotNull(EnemyLibrary.Get(minion));
            }
        }

        [Test]
        public void ScenarioCombatOutcomesUseEnemyIdsFromPool()
        {
            var pool = new[] { "scavenger", "drone", "hacker" };
            var plan = new DivePlan
            {
                KeyItemId = "dive_basic",
                Name = "测试潜网",
                EnemyCount = 5,
                LootCount = 9,
                HazardCount = 6,
                LootPool = new[] { "coin" },
                EnemyPool = pool
            };
            ExpeditionScenario scenario = ExpeditionScenario.Generate(99, plan);
            var seen = new HashSet<string>();

            for (int i = 0; i < scenario.CurrentOptions.Count; i++)
            {
                ExpeditionActionOption action = scenario.CurrentOptions[i];
                for (int j = 0; j < action.Outcomes.Count; j++)
                {
                    ExpeditionOutcomeEntry outcome = action.Outcomes[j];
                    if (outcome.Kind == ExpeditionOutcomeKind.Combat)
                    {
                        CollectionAssert.Contains(pool, outcome.EnemyId);
                        seen.Add(outcome.EnemyId);
                    }
                }
            }

            Assert.GreaterOrEqual(seen.Count, 1);
        }

        [Test]
        public void RandomizedLoadoutsStayInsideBagAndKeepRoleIdentity()
        {
            ItemCatalog catalog = Catalog();

            for (int i = 0; i < EnemyLibrary.All.Count; i++)
            {
                EnemyArchetype archetype = EnemyLibrary.All[i];
                var signatures = new HashSet<string>();

                for (int seed = 0; seed < 200; seed++)
                {
                    CombatActor actor = EnemyLibrary.Create(archetype.Id, catalog, 0, seed);
                    Assert.LessOrEqual(actor.Bag.OccupiedCellCount, actor.Bag.Capacity, archetype.Id);
                    Assert.Greater(actor.Bag.Count, 0, archetype.Id);
                    AssertRoleIdentity(archetype.Id, actor, catalog);
                    signatures.Add(Signature(actor));
                }

                Assert.GreaterOrEqual(signatures.Count, 1, archetype.Id);
            }
        }

        [Test]
        public void CreateIsDeterministicForTheSameSeed()
        {
            ItemCatalog catalog = Catalog();

            for (int i = 0; i < EnemyLibrary.All.Count; i++)
            {
                EnemyArchetype archetype = EnemyLibrary.All[i];
                CombatActor first = EnemyLibrary.Create(archetype.Id, catalog, 0, 424242);
                CombatActor second = EnemyLibrary.Create(archetype.Id, catalog, 0, 424242);
                Assert.AreEqual(Signature(first), Signature(second), archetype.Id);
            }
        }

        [Test]
        public void SeedVariesTheBulwarkLoadout()
        {
            ItemCatalog catalog = Catalog();
            var signatures = new HashSet<string>();

            for (int seed = 0; seed < 80; seed++)
            {
                signatures.Add(Signature(EnemyLibrary.Create("bulwark", catalog, 0, seed)));
            }

            Assert.Greater(signatures.Count, 1);
        }

        private static void AssertRoleIdentity(string archetypeId, CombatActor actor, ItemCatalog catalog)
        {
            switch (archetypeId)
            {
                case "scavenger":
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Weapon), archetypeId);
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Loot), archetypeId);
                    break;
                case "enforcer":
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Armor), archetypeId);
                    Assert.GreaterOrEqual(CountCategory(actor, catalog, ItemCategory.Weapon), 2, archetypeId);
                    break;
                case "drone":
                    Assert.IsTrue(HasWeaponType(actor, catalog, DamageType.Projectile), archetypeId);
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Ammo), archetypeId);
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Tool), archetypeId);
                    break;
                case "bulwark":
                    Assert.IsTrue(HasArmor(actor, catalog, 4, int.MaxValue), archetypeId);
                    Assert.IsTrue(HasArmor(actor, catalog, 1, 2), archetypeId);
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Weapon), archetypeId);
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Consumable), archetypeId);
                    break;
                case "shocker":
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Weapon), archetypeId);
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Tool), archetypeId);
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Consumable), archetypeId);
                    break;
                case "hacker":
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Tool), archetypeId);
                    Assert.IsTrue(HasWeaponType(actor, catalog, DamageType.Projectile), archetypeId);
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Ammo), archetypeId);
                    break;
                case "poisoner":
                    Assert.IsTrue(HasWeaponType(actor, catalog, DamageType.Toxin), archetypeId);
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Consumable), archetypeId);
                    break;
                case "suffocator":
                    Assert.IsTrue(HasWeaponType(actor, catalog, DamageType.Asphyxiation), archetypeId);
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Armor), archetypeId);
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Consumable), archetypeId);
                    break;
                case "wild_dog":
                case "squirrel":
                    Assert.IsTrue(HasItem(actor, "wild_bite"), archetypeId);
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Loot), archetypeId);
                    break;
                case "raider":
                    Assert.GreaterOrEqual(CountCategory(actor, catalog, ItemCategory.Weapon), 2, archetypeId);
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Armor), archetypeId);
                    Assert.IsTrue(HasCategory(actor, catalog, ItemCategory.Loot), archetypeId);
                    break;
            }
        }

        private static bool HasItem(CombatActor actor, string itemId)
        {
            IReadOnlyList<PlacedItem> items = actor.Bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].DefinitionId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasCategory(CombatActor actor, ItemCatalog catalog, ItemCategory category)
        {
            return CountCategory(actor, catalog, category) > 0;
        }

        private static int CountCategory(CombatActor actor, ItemCatalog catalog, ItemCategory category)
        {
            int count = 0;
            IReadOnlyList<PlacedItem> items = actor.Bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition;
                if (catalog.TryGet(items[i].DefinitionId, out definition) && definition.Category == category)
                {
                    count++;
                }
            }

            return count;
        }

        private static bool HasArmor(CombatActor actor, ItemCatalog catalog, int minCells, int maxCells)
        {
            IReadOnlyList<PlacedItem> items = actor.Bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition;
                if (!catalog.TryGet(items[i].DefinitionId, out definition)
                    || definition.Category != ItemCategory.Armor)
                {
                    continue;
                }

                int cells = definition.Shape.CellCount;
                if (cells >= minCells && cells <= maxCells)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasWeaponType(CombatActor actor, ItemCatalog catalog, DamageType type)
        {
            IReadOnlyList<PlacedItem> items = actor.Bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition;
                if (catalog.TryGet(items[i].DefinitionId, out definition)
                    && definition.Category == ItemCategory.Weapon
                    && definition.Type == type)
                {
                    return true;
                }
            }

            return false;
        }

        private static string Signature(CombatActor actor)
        {
            var builder = new StringBuilder();
            IReadOnlyList<PlacedItem> items = actor.Bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                PlacedItem item = items[i];
                builder.Append(item.DefinitionId).Append(':')
                    .Append(item.Origin.X).Append(',')
                    .Append(item.Origin.Y).Append(':')
                    .Append(item.Rotation).Append(':')
                    .Append(item.Count).Append(';');
            }

            return builder.ToString();
        }
    }
}
