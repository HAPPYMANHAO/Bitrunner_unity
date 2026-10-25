using System.Text;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class EnemyBrainTests
    {
        [Test]
        public void PrefersHighestLethalDamage()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"),
                CombatTestFactory.Weapon("rod", 8, 2, 1, ItemTag.Blade, "#\n#\n#"));

            CombatActor player = CombatTestFactory.Actor("player", 4, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog, "blade", "rod");
            enemy.Brain = new EnemyBrain();

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();
            engine.EndTurn();

            Assert.AreEqual(CombatPhase.Defeat, engine.Phase);
            Assert.AreEqual("rod", LastUsedItem(engine));
        }

        [Test]
        public void HealsWhenBelowThreshold()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"),
                CombatTestFactory.ActiveSelf("patch", 1, 0, 8, true, StatusType.None, 2, ItemTag.Consumable, "#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 30, 3, 4, 3, catalog, "blade", "patch");
            enemy.SetHp(5);
            enemy.Brain = new EnemyBrain();

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();
            engine.EndTurn();

            Assert.AreEqual(13, enemy.Hp);
            Assert.IsNull(CombatTestFactory.FindItem(enemy, "patch"));
            Assert.IsTrue(HasUsedItem(engine, "patch"));
        }


        [Test]
        public void UsesShieldFromOwnBagAndSpendsEnergy()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.ActiveSelf("barrier", 1, 4, 0, false, StatusType.None, 2, ItemTag.Tool, "#"),
                CombatTestFactory.Weapon("prop", 1, 99, 0, ItemTag.Blade, "##"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog, "barrier", "prop");
            enemy.Brain = new EnemyBrain();

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();
            engine.EndTurn();

            Assert.AreEqual(4, enemy.Shield);
            Assert.AreEqual(2, enemy.Energy);
            Assert.IsTrue(HasUsedItem(engine, "barrier"));
        }

        [Test]
        public void IdenticalStatesProduceIdenticalRuns()
        {
            CombatEngine first = BuildDeterministicEngine();
            CombatEngine second = BuildDeterministicEngine();

            first.Start();
            second.Start();
            first.EndTurn();
            second.EndTurn();

            Assert.AreEqual(Describe(first), Describe(second));
        }

        [Test]
        public void DoesNothingWithoutUsableItems()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Loot("coin", 40, "#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog);
            CombatActor enemy = CombatTestFactory.Actor("enemy", 20, 3, 4, 3, catalog, "coin");
            enemy.Brain = new EnemyBrain();

            var engine = new CombatEngine(player, new[] { enemy }, catalog);
            engine.Start();
            engine.EndTurn();

            Assert.AreEqual(30, player.Hp);
            Assert.AreEqual(0, CountUsedItems(engine));
        }

        private static CombatEngine BuildDeterministicEngine()
        {
            ItemCatalog catalog = CombatTestFactory.Catalog(
                CombatTestFactory.Weapon("blade", 5, 1, 0, ItemTag.Blade, "##"),
                CombatTestFactory.Weapon("rod", 8, 2, 1, ItemTag.Blade, "#\n#\n#"),
                CombatTestFactory.ActiveSelf("patch", 1, 0, 8, true, StatusType.None, 2, ItemTag.Consumable, "#"),
                CombatTestFactory.Passive("plate", 4, 0, ItemTag.Shield | ItemTag.Armor, "##\n##"),
                CombatTestFactory.Loot("coin", 40, "#"));

            CombatActor player = CombatTestFactory.Actor("player", 30, 3, 6, 5, catalog, "blade", "plate");
            CombatActor enemy = CombatTestFactory.Actor("enemy", 35, 3, 4, 3, catalog, "blade", "rod", "patch", "coin");
            enemy.Brain = new EnemyBrain();
            return new CombatEngine(player, new[] { enemy }, catalog);
        }

        private static string Describe(CombatEngine engine)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < engine.Events.Count; i++)
            {
                CombatEvent combatEvent = engine.Events[i];
                builder.Append(combatEvent.Type).Append('|')
                    .Append(combatEvent.ActorId).Append('|')
                    .Append(combatEvent.ItemId).Append('|')
                    .Append(combatEvent.Amount).Append('|')
                    .Append(combatEvent.Shielded).Append(';');
            }

            return builder.ToString();
        }

        private static string LastUsedItem(CombatEngine engine)
        {
            for (int i = engine.Events.Count - 1; i >= 0; i--)
            {
                if (engine.Events[i].Type == CombatEventType.ItemUsed)
                {
                    return engine.Events[i].ItemId;
                }
            }

            return null;
        }

        private static bool HasUsedItem(CombatEngine engine, string itemId)
        {
            for (int i = 0; i < engine.Events.Count; i++)
            {
                if (engine.Events[i].Type == CombatEventType.ItemUsed && engine.Events[i].ItemId == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountUsedItems(CombatEngine engine)
        {
            int count = 0;
            for (int i = 0; i < engine.Events.Count; i++)
            {
                if (engine.Events[i].Type == CombatEventType.ItemUsed)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
