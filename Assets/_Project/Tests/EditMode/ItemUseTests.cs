using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public class ItemUseTests
    {
        private static ItemCatalog Catalog()
        {
            return DefaultItemLibrary.CreateCatalog();
        }

        [Test]
        public void PatchHealsAndIsConsumedOutsideCombat()
        {
            ItemCatalog catalog = Catalog();
            var bag = new BagInventory(3, 3);
            PlacedItem placed;
            Assert.IsTrue(bag.TryAdd("patch", catalog.Get("patch").Shape, out placed));

            int newHp;
            string message;
            bool used = ItemUse.TryUseOutsideCombat(
                catalog.Get("patch"),
                bag,
                placed.InstanceId,
                20,
                40,
                out newHp,
                out message);

            Assert.IsTrue(used);
            Assert.AreEqual(28, newHp);
            Assert.AreEqual(0, bag.Count);
            StringAssert.Contains("恢复 8", message);
        }

        [Test]
        public void PatchAtFullHealthIsNotConsumed()
        {
            ItemCatalog catalog = Catalog();
            var bag = new BagInventory(3, 3);
            PlacedItem placed;
            bag.TryAdd("patch", catalog.Get("patch").Shape, out placed);

            int newHp;
            string message;
            bool used = ItemUse.TryUseOutsideCombat(catalog.Get("patch"), bag, placed.InstanceId, 40, 40, out newHp, out message);

            Assert.IsFalse(used);
            Assert.AreEqual(1, bag.Count);
            Assert.AreEqual(40, newHp);
        }

        [Test]
        public void WeaponCannotBeUsedOutsideCombat()
        {
            ItemCatalog catalog = Catalog();
            var bag = new BagInventory(3, 3);
            PlacedItem placed;
            bag.TryAdd("blade", catalog.Get("blade").Shape, out placed);

            int newHp;
            string message;
            bool used = ItemUse.TryUseOutsideCombat(catalog.Get("blade"), bag, placed.InstanceId, 30, 40, out newHp, out message);

            Assert.IsFalse(used);
            Assert.AreEqual(1, bag.Count);
            StringAssert.Contains("战斗", message);
        }

        [Test]
        public void PassiveArmorCannotBeUsedOutsideCombat()
        {
            ItemCatalog catalog = Catalog();
            var bag = new BagInventory(3, 3);
            PlacedItem placed;
            bag.TryAdd("plate", catalog.Get("plate").Shape, out placed);

            int newHp;
            string message;
            bool used = ItemUse.TryUseOutsideCombat(catalog.Get("plate"), bag, placed.InstanceId, 30, 40, out newHp, out message);

            Assert.IsFalse(used);
            Assert.AreEqual(1, bag.Count);
        }
    }
}
