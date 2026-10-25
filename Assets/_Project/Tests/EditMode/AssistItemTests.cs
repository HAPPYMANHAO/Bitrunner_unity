using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class AssistItemTests
    {
        [Test]
        public void AllTiersExistWithAssistEffects()
        {
            var catalog = DefaultItemLibrary.CreateCatalog();
            string[] ids =
            {
                "flag_light_1", "flag_light_2", "flag_light_3",
                "flag_muffle_1", "flag_muffle_2", "flag_muffle_3",
                "flag_ward_1", "flag_ward_2", "flag_ward_3"
            };

            for (int i = 0; i < ids.Length; i++)
            {
                ItemDefinition definition = catalog.Get(ids[i]);
                Assert.IsNotNull(definition, ids[i]);
                Assert.AreEqual(1, definition.Shape.CellCount, ids[i]);
                Assert.Greater(definition.DurabilityMax, 0, ids[i]);
                Assert.IsNotNull(definition.Assist, ids[i]);
                Assert.IsTrue(definition.Assist.HasEffect, ids[i]);
            }
        }

        [Test]
        public void ConsumeClampsAtZero()
        {
            var catalog = DefaultItemLibrary.CreateCatalog();
            ItemDefinition definition = catalog.Get("flag_light_1");
            var bag = new BagInventory(2, 1);
            PlacedItem item;
            Assert.IsTrue(bag.TryAdd("flag_light_1", definition.Shape, out item));

            Assert.AreEqual(definition.DurabilityMax, ItemDurability.Current(item, definition));
            Assert.AreEqual(definition.DurabilityMax - 1, ItemDurability.Consume(item, definition, 1));
            Assert.AreEqual(0, ItemDurability.Consume(item, definition, 99));
            Assert.AreEqual(0, item.Durability);
        }

        [Test]
        public void TooltipShowsExpeditionEffectAndDurability()
        {
            var catalog = DefaultItemLibrary.CreateCatalog();
            ItemDefinition definition = catalog.Get("flag_light_2");
            var bag = new BagInventory(2, 1);
            PlacedItem item;
            Assert.IsTrue(bag.TryAdd("flag_light_2", definition.Shape, out item));

            string text = ItemText.Describe(definition, item);
            StringAssert.Contains("探索效果", text);
            StringAssert.Contains("警戒", text);
            StringAssert.Contains("耐久", text);
        }
    }
}
