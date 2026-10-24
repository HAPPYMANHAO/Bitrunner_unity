using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    /// <summary>
    /// 通讯中枢只认通讯上行链路，且「同时能做几个任务」由它的格子数决定。
    /// 这些测试锁的就是这两件事。
    /// </summary>
    public class CommsHubTests
    {
        private static ItemCatalog Catalog()
        {
            return DefaultItemLibrary.CreateCatalog();
        }

        private static CampaignState RichCampaign()
        {
            return new CampaignState(CampaignState.CreateDefaultInstallments(), 100000);
        }

        /// <summary>升到目标等级所需的材料一整套（覆盖 L1→L5 全部消耗）。</summary>
        private static BagInventory UpgradeMaterials(ItemCatalog catalog)
        {
            var stash = new BagInventory(6, 10);
            string[] ids =
            {
                "scrap", "scrap", "wire", "alloy", "circuit", "circuit",
                "servo", "superalloy", "optic_fiber"
            };
            for (int i = 0; i < ids.Length; i++)
            {
                PlacedItem placed;
                Assert.IsTrue(stash.TryAdd(ids[i], catalog.Get(ids[i]).Shape, out placed), ids[i]);
            }

            return stash;
        }

        private static CommsHub HubAtLevel(int level, ItemCatalog catalog, CampaignState campaign)
        {
            var hub = new CommsHub();
            BagInventory stash = UpgradeMaterials(catalog);
            while (hub.Level < level)
            {
                string error;
                Assert.IsTrue(hub.TryUpgrade(campaign, stash, out error), error);
            }

            return hub;
        }

        private static PlacedItem Put(CommsHub hub, ItemCatalog catalog, string id)
        {
            PlacedItem placed;
            Assert.IsTrue(hub.Grid.TryAdd(id, catalog.Get(id).Shape, out placed), id);
            return placed;
        }

        [Test]
        public void CommsHub_StartsSmall()
        {
            var hub = new CommsHub();

            Assert.AreEqual(1, hub.Level);
            Assert.AreEqual(2, hub.Width);
            Assert.AreEqual(2, hub.Height);
            Assert.AreEqual(4, hub.Width * hub.Height);
        }

        [Test]
        public void CommsHub_LevelsGrowMonotonicallyUpToSixteenCells()
        {
            int[] expected =
            {
                CommsHub.WidthForLevel(1) * CommsHub.HeightForLevel(1),
                CommsHub.WidthForLevel(2) * CommsHub.HeightForLevel(2),
                CommsHub.WidthForLevel(3) * CommsHub.HeightForLevel(3),
                CommsHub.WidthForLevel(4) * CommsHub.HeightForLevel(4),
                CommsHub.WidthForLevel(5) * CommsHub.HeightForLevel(5)
            };

            CollectionAssert.AreEqual(new[] { 4, 6, 9, 12, 16 }, expected);
        }

        [Test]
        public void CommsHub_AcceptsOnlyUplinks()
        {
            ItemCatalog catalog = Catalog();

            Assert.IsTrue(CommsHub.CanAccept(catalog.Get("uplink_junk")));
            Assert.IsTrue(CommsHub.CanAccept(catalog.Get("uplink_seed")));

            // 关卡密钥、芯片、装备、材料一律不收。
            Assert.IsFalse(CommsHub.CanAccept(catalog.Get("dive_basic")));
            Assert.IsFalse(CommsHub.CanAccept(catalog.Get("dive_core")));
            Assert.IsFalse(CommsHub.CanAccept(catalog.Get("chip_hp_1")));
            Assert.IsFalse(CommsHub.CanAccept(catalog.Get("plate")));
            Assert.IsFalse(CommsHub.CanAccept(catalog.Get("scrap")));
            Assert.IsFalse(CommsHub.CanAccept(null));
        }

        /// <summary>
        /// 上行链路必须用独立的 Category。如果复用 Key，它会满足
        /// DivePod.EnsureSelection 的筛选条件，被当成关卡密钥选中。
        /// </summary>
        [Test]
        public void UplinkIsNotAKeySoThePodNeverSelectsIt()
        {
            ItemCatalog catalog = Catalog();
            var pod = new DivePod();
            Assert.IsNotNull(pod);

            Assert.AreNotEqual(ItemCategory.Key, catalog.Get("uplink_junk").Category);
            Assert.AreEqual(ItemCategory.Uplink, catalog.Get("uplink_junk").Category);
        }

        [Test]
        public void UplinkCannotBeStoredInTheKeyBox()
        {
            ItemCatalog catalog = Catalog();
            ContainerSpec spec = catalog.Get("key_box").ContainerSpec;

            Assert.IsTrue(spec.Accepts(catalog.Get("dive_core")));
            Assert.IsFalse(spec.Accepts(catalog.Get("uplink_junk")));
            Assert.IsFalse(spec.Accepts(catalog.Get("uplink_seed")));
        }

        [Test]
        public void UplinkVolumeFollowsTheSameRuleAsKeys()
        {
            ItemCatalog catalog = Catalog();

            Assert.AreEqual(1, catalog.Get("uplink_junk").Shape.CellCount);
            Assert.AreEqual(2, catalog.Get("uplink_arms").Shape.CellCount);
            Assert.AreEqual(4, catalog.Get("uplink_intel").Shape.CellCount);
            Assert.AreEqual(6, catalog.Get("uplink_seed").Shape.CellCount);

            // 四条合计 13 格：L4（12 格）刚好装不下，L5（16 格）才行。
            Assert.AreEqual(13, catalog.Get("uplink_junk").Shape.CellCount
                                + catalog.Get("uplink_arms").Shape.CellCount
                                + catalog.Get("uplink_intel").Shape.CellCount
                                + catalog.Get("uplink_seed").Shape.CellCount);
            Assert.Less(CommsHub.WidthForLevel(4) * CommsHub.HeightForLevel(4), 13);
            Assert.GreaterOrEqual(CommsHub.WidthForLevel(5) * CommsHub.HeightForLevel(5), 13);
        }

        [Test]
        public void AllFourUplinksFitOnlyAtLevelFive()
        {
            ItemCatalog catalog = Catalog();
            CampaignState campaign = RichCampaign();

            CommsHub levelFour = HubAtLevel(4, catalog, campaign);
            Assert.IsFalse(PutAllFour(levelFour, catalog), "L4 只有 12 格，不该装下 13 格的四条链路");

            CommsHub levelFive = HubAtLevel(5, catalog, RichCampaign());
            Assert.IsTrue(PutAllFour(levelFive, catalog), "L5 有 16 格，四条链路必须放得下");
            Assert.AreEqual(4, levelFive.ContactedNpcIds.Count);
        }

        private static bool PutAllFour(CommsHub hub, ItemCatalog catalog)
        {
            string[] ids = { "uplink_junk", "uplink_arms", "uplink_intel", "uplink_seed" };

            // 显式摆位：seed(3×2) 放 (0,0)-(2,1)，intel(2×2) 放 (0,2)-(1,3)，
            // arms(1×2) 放 (2,2)，junk 放 (3,3)。
            var origins = new[]
            {
                new Cell(3, 3), // junk 1 格
                new Cell(2, 2), // arms 2 格横放
                new Cell(0, 2), // intel 2×2
                new Cell(0, 0)  // seed 3×2
            };

            for (int i = 0; i < ids.Length; i++)
            {
                ItemDefinition definition = catalog.Get(ids[i]);
                PlacedItem placed;
                string error;
                bool ok = hub.Grid.TryAddAt(
                    ids[i], definition.Shape, origins[i], 0, true, out placed, out error);
                if (!ok)
                {
                    return false;
                }
            }

            return true;
        }

        [Test]
        public void ContactedNpcsFollowTheUplinksInTheHub()
        {
            ItemCatalog catalog = Catalog();
            CampaignState campaign = RichCampaign();
            CommsHub hub = HubAtLevel(3, catalog, campaign);

            Assert.AreEqual(0, hub.ContactedNpcIds.Count);
            Assert.IsFalse(hub.HasUplinkFor(NpcLibrary.Junk));

            Put(hub, catalog, "uplink_junk");
            Put(hub, catalog, "uplink_arms");

            Assert.AreEqual(2, hub.ContactedNpcIds.Count);
            Assert.AreEqual(NpcLibrary.Junk, hub.ContactedNpcIds[0], "按档位排序，老陈在前");
            Assert.AreEqual(NpcLibrary.Arms, hub.ContactedNpcIds[1]);
            Assert.IsTrue(hub.HasUplinkFor(NpcLibrary.Junk));
            Assert.IsFalse(hub.HasUplinkFor(NpcLibrary.Intel));
        }

        [Test]
        public void LockedUplinkCannotBeModified()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubAtLevel(2, catalog, RichCampaign());
            PlacedItem uplink = Put(hub, catalog, "uplink_junk");

            string error;
            Assert.IsTrue(hub.CanModify(uplink.InstanceId, out error), error);
            Assert.IsTrue(hub.TryLockForQuest(NpcLibrary.Junk, "q_junk_1", out error), error);

            Assert.IsTrue(hub.IsLocked(uplink.InstanceId));
            Assert.AreEqual("q_junk_1", hub.LockedQuestOf(uplink.InstanceId));
            Assert.IsFalse(hub.CanModify(uplink.InstanceId, out error));
            StringAssert.Contains("清一清场", error);

            Assert.IsTrue(hub.UnlockForQuest("q_junk_1"));
            Assert.IsFalse(hub.IsLocked(uplink.InstanceId));
            Assert.IsTrue(hub.CanModify(uplink.InstanceId, out error), error);
        }

        [Test]
        public void LockRequiresTheUplinkToBeInTheHub()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubAtLevel(2, catalog, RichCampaign());

            string error;
            Assert.IsFalse(hub.TryLockForQuest(NpcLibrary.Junk, "q_junk_1", out error));
            StringAssert.Contains("没有这位联系人", error);
        }

        [Test]
        public void TryGetUplinkPrefersTheUnlockedCopy()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubAtLevel(3, catalog, RichCampaign());
            PlacedItem first = Put(hub, catalog, "uplink_junk");
            PlacedItem second = Put(hub, catalog, "uplink_junk");

            string error;
            Assert.IsTrue(hub.TryLockForQuest(NpcLibrary.Junk, "q_junk_1", out error), error);

            PlacedItem found;
            Assert.IsTrue(hub.TryGetUplink(NpcLibrary.Junk, out found));
            Assert.AreNotEqual(first.InstanceId, found.InstanceId);
            Assert.AreEqual(second.InstanceId, found.InstanceId);
        }

        /// <summary>
        /// 升级必须保留 PlacedItem 实例。用 FacilityGrid.Resize 会重建实例，
        /// 锁表里的 instanceId 全部失效——任务进行中升一次级就能把链路抽走。
        /// </summary>
        [Test]
        public void UpgradeKeepsUplinksAndTheirLocks()
        {
            ItemCatalog catalog = Catalog();
            CampaignState campaign = RichCampaign();
            var hub = new CommsHub();
            PlacedItem uplink = Put(hub, catalog, "uplink_junk");

            string error;
            Assert.IsTrue(hub.TryLockForQuest(NpcLibrary.Junk, "q_junk_1", out error), error);

            BagInventory stash = UpgradeMaterials(catalog);
            Assert.IsTrue(hub.TryUpgrade(campaign, stash, out error), error);

            Assert.AreEqual(2, hub.Level);
            Assert.IsTrue(hub.HasUplinkFor(NpcLibrary.Junk));
            Assert.IsTrue(hub.IsLocked(uplink.InstanceId), "升级后锁必须还在同一个实例上");
            Assert.IsFalse(hub.CanModify(uplink.InstanceId, out error));
        }

        [Test]
        public void UpgradeCostsAndMaterialsAreFilledForEveryLevel()
        {
            int[] costs = { 250, 500, 900, 1500 };
            ItemCatalog catalog = Catalog();

            for (int level = 1; level < CommsHub.MaxLevel; level++)
            {
                CommsHub leveled = HubAtLevel(level, catalog, RichCampaign());
                Assert.AreEqual(costs[level - 1], leveled.UpgradeCost, "L" + level + " 的升级费");
                Assert.Greater(leveled.UpgradeMaterialIds.Length, 0, "L" + level + " 的升级材料");
            }

            CommsHub maxed = HubAtLevel(5, catalog, RichCampaign());
            Assert.IsFalse(maxed.CanUpgrade);
            Assert.AreEqual(0, maxed.UpgradeCost);
            Assert.AreEqual(0, maxed.UpgradeMaterialIds.Length);
        }

        [Test]
        public void UpgradingWithoutMoneyFails()
        {
            ItemCatalog catalog = Catalog();
            var poor = new CampaignState(CampaignState.CreateDefaultInstallments(), 0);
            var hub = new CommsHub();
            BagInventory stash = UpgradeMaterials(catalog);

            string error;
            Assert.IsFalse(hub.TryUpgrade(poor, stash, out error));
            Assert.AreEqual(1, hub.Level);
        }
    }
}
