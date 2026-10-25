using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class LootStackRulesTests
    {
        [Test]
        public void AmmoStacksBetweenTwoAndSix()
        {
            var catalog = DefaultItemLibrary.CreateCatalog();
            ItemDefinition ammo = catalog.Get("ammo");
            int min = int.MaxValue;
            int max = int.MinValue;
            for (int seed = 0; seed < 64; seed++)
            {
                int count = LootStackRules.Roll(ammo, seed);
                Assert.GreaterOrEqual(count, 2);
                Assert.LessOrEqual(count, 6);
                min = count < min ? count : min;
                max = count > max ? count : max;
            }

            Assert.Less(min, max, "堆叠数量应随种子变化");
        }

        [Test]
        public void NonStackableItemIsOne()
        {
            var catalog = DefaultItemLibrary.CreateCatalog();
            Assert.AreEqual(1, LootStackRules.Roll(catalog.Get("blade"), 3));
            Assert.AreEqual(1, LootStackRules.Roll(catalog.Get("scrap"), 3));
        }

        [Test]
        public void OtherStackableCapsAtThree()
        {
            var catalog = DefaultItemLibrary.CreateCatalog();
            ItemDefinition coin = catalog.Get("bit");
            for (int seed = 0; seed < 16; seed++)
            {
                int count = LootStackRules.Roll(coin, seed);
                Assert.GreaterOrEqual(count, 1);
                Assert.LessOrEqual(count, 3);
            }
        }
    }
}
