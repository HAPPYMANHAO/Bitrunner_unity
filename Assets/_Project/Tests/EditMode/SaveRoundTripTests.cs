using System.Collections.Generic;
using BitRunner.Core;
using NUnit.Framework;
using UnityEngine;

namespace BitRunner.Tests
{
    public sealed class SaveRoundTripTests
    {
        private ItemCatalog _catalog;
        private GameSnapshot _snapshot;
        private string _questId;
        private int _uplinkInstanceId;

        [SetUp]
        public void SetUp()
        {
            _catalog = DefaultItemLibrary.CreateCatalog();
            _snapshot = BuildSnapshot(_catalog, out _questId, out _uplinkInstanceId);
        }

        [Test]
        public void CaptureThenRestoreKeepsBaseState()
        {
            SaveData data = GameSave.Capture(_snapshot);

            GameSnapshot restored;
            string error;
            Assert.IsTrue(GameSave.TryRestore(data, _catalog, out restored, out error), error);

            Assert.AreEqual(_snapshot.Campaign.Week, restored.Campaign.Week);
            Assert.AreEqual(_snapshot.Campaign.Day, restored.Campaign.Day);
            Assert.AreEqual(_snapshot.Campaign.TotalPaid, restored.Campaign.TotalPaid);
            Assert.AreEqual(_snapshot.Campaign.Status, restored.Campaign.Status);
            Assert.AreEqual(_snapshot.Campaign.Ending, restored.Campaign.Ending);

            Assert.AreEqual(_snapshot.Rack.Level, restored.Rack.Level);
            Assert.AreEqual(_snapshot.Insurance.Level, restored.Insurance.Level);
            Assert.AreEqual(_snapshot.Pod.Level, restored.Pod.Level);
            Assert.AreEqual(_snapshot.Craft.Level, restored.Craft.Level);
            Assert.AreEqual(_snapshot.Cpu.Level, restored.Cpu.Level);
            Assert.AreEqual(_snapshot.Comms.Level, restored.Comms.Level);

            Assert.AreEqual("dive_wild", restored.Pod.SelectedKeyItemId);
            Assert.AreEqual(_snapshot.Mine.Level, restored.Mine.Level);
            Assert.AreEqual(_snapshot.Mine.FilledCells, restored.Mine.FilledCells);
            Assert.AreEqual(_snapshot.Mine.FilledWeek, restored.Mine.FilledWeek);
            Assert.AreEqual(_snapshot.Mine.FilledDay, restored.Mine.FilledDay);

            AssertBagsEqual(_snapshot.Stash, restored.Stash);
            AssertBagsEqual(_snapshot.PlayerBag, restored.PlayerBag);
            AssertBagsEqual(_snapshot.Secure, restored.Secure);

            Assert.AreEqual(_snapshot.Comms.LockedLinks, restored.Comms.LockedLinks);
            Assert.AreEqual(QuestState.Active, restored.Quests.StateOf(_questId));

            string lockError;
            Assert.IsFalse(restored.Comms.CanModify(_uplinkInstanceId, out lockError));
            Assert.IsNotEmpty(lockError);

            Assert.AreEqual(_snapshot.Merchant.StockWeek, restored.Merchant.StockWeek);
            Assert.AreEqual(_snapshot.Merchant.StockDay, restored.Merchant.StockDay);
            Assert.AreEqual(_snapshot.Merchant.Offers.Count, restored.Merchant.Offers.Count);
            AssertBagsEqual(_snapshot.Merchant.Stock, restored.Merchant.Stock);

            Assert.AreEqual(_snapshot.NpcStocks.LastRefreshDay("chen"), restored.NpcStocks.LastRefreshDay("chen"));
            AssertBagsEqual(_snapshot.NpcStocks.StockFor("chen"), restored.NpcStocks.StockFor("chen"));

            Assert.IsEmpty(restored.Warnings);
        }

        [Test]
        public void JsonRoundTripKeepsDurabilityAndContainerContents()
        {
            SaveData data = GameSave.Capture(_snapshot);
            string json = JsonUtility.ToJson(data, true);

            SaveData parsed = JsonUtility.FromJson<SaveData>(json);
            Assert.IsNotNull(parsed);

            // 容器必须摊平在 Containers 里，物品只持有一个 ContainerId——
            // 嵌套的 ItemSave[] 会撞上 Unity 序列化器的 10 层深度上限。
            Assert.IsNotNull(parsed.Containers);
            Assert.AreEqual(1, parsed.Containers.Length);

            int partsContainerId = 0;
            for (int i = 0; i < parsed.PlayerBag.Items.Length; i++)
            {
                if (parsed.PlayerBag.Items[i].DefinitionId == "parts_case")
                {
                    partsContainerId = parsed.PlayerBag.Items[i].ContainerId;
                }
            }

            Assert.Greater(partsContainerId, 0);
            Assert.AreEqual(partsContainerId, parsed.Containers[0].Id);
            Assert.AreEqual(1, parsed.Containers[0].Items.Length);
            Assert.AreEqual("scrap", parsed.Containers[0].Items[0].DefinitionId);

            GameSnapshot restored;
            string error;
            Assert.IsTrue(GameSave.TryRestore(parsed, _catalog, out restored, out error), error);

            PlacedItem crate = FindByDefinition(restored.PlayerBag, "parts_case");
            Assert.IsNotNull(crate, "零件盒丢失。");
            Assert.IsNotNull(crate.Container, "容器内容物丢失。");
            Assert.AreEqual(1, crate.Container.Bag.Count);

            PlacedItem inner;
            Assert.IsTrue(crate.Container.Bag.TryGet(FirstInstanceId(crate.Container.Bag), out inner));
            Assert.AreEqual("scrap", inner.DefinitionId);

            PlacedItem blade = FindByDefinition(restored.PlayerBag, "blade");
            Assert.IsNotNull(blade);
            Assert.AreEqual(3, blade.Durability);

            PlacedItem ammo = FindByDefinition(restored.PlayerBag, "ammo");
            Assert.IsNotNull(ammo);
            Assert.AreEqual(7, ammo.Count);
        }

        [Test]
        public void RestoreRejectsContainerReferencedTwice()
        {
            SaveData data = GameSave.Capture(_snapshot);

            int containerId = 0;
            for (int i = 0; i < data.PlayerBag.Items.Length; i++)
            {
                if (data.PlayerBag.Items[i].ContainerId != 0)
                {
                    containerId = data.PlayerBag.Items[i].ContainerId;
                }
            }

            Assert.Greater(containerId, 0);

            for (int i = 0; i < data.PlayerBag.Items.Length; i++)
            {
                if (data.PlayerBag.Items[i].ContainerId == 0)
                {
                    data.PlayerBag.Items[i].ContainerId = containerId;
                    break;
                }
            }

            GameSnapshot restored;
            string error;
            Assert.IsFalse(GameSave.TryRestore(data, _catalog, out restored, out error));
            StringAssert.Contains("容器", error);
        }

        [Test]
        public void RestoreDropsStaleCommsLockWithoutLosingCampaign()
        {
            SaveData data = GameSave.Capture(_snapshot);
            data.Comms.LockedInstanceIds[0] = 424242;

            GameSnapshot restored;
            string error;
            Assert.IsTrue(GameSave.TryRestore(data, _catalog, out restored, out error), error);

            Assert.AreEqual(0, restored.Comms.LockedLinks);
            Assert.IsNotEmpty(restored.Warnings);
            Assert.AreEqual(QuestState.Active, restored.Quests.StateOf(_questId));
        }

        [Test]
        public void RestoreDropsRemovedQuest()
        {
            SaveData data = GameSave.Capture(_snapshot);
            data.Quests.Entries[0].QuestId = "q_no_longer_exists";

            GameSnapshot restored;
            string error;
            Assert.IsTrue(GameSave.TryRestore(data, _catalog, out restored, out error), error);

            Assert.AreEqual(QuestState.Offered, restored.Quests.StateOf("q_no_longer_exists"));
            Assert.IsEmpty(restored.Quests.ActiveQuests);
            Assert.IsNotEmpty(restored.Warnings);
        }

        [Test]
        public void RestoreMigratesLegacyNpcStockSize()
        {
            SaveData data = GameSave.Capture(_snapshot);
            Assert.IsNotNull(data.NpcMarket);
            Assert.IsNotNull(data.NpcMarket.Entries);
            Assert.Greater(data.NpcMarket.Entries.Length, 0);

            // 模拟契约卡加入前的 6×3 货架：清空内容避免坐标落到第 4 行。
            data.NpcMarket.Entries[0].Stock.Width = NpcMarket.StockWidth;
            data.NpcMarket.Entries[0].Stock.Height = 3;
            data.NpcMarket.Entries[0].Stock.Items = new ItemSave[0];

            GameSnapshot restored;
            string error;
            Assert.IsTrue(GameSave.TryRestore(data, _catalog, out restored, out error), error);
            Assert.AreEqual(
                NpcMarket.StockHeight,
                restored.NpcStocks.StockFor(data.NpcMarket.Entries[0].NpcId).Grid.Height);
        }

        [Test]
        public void RestoreRejectsUnknownItemDefinition()
        {
            SaveData data = GameSave.Capture(_snapshot);
            data.PlayerBag.Items[0].DefinitionId = "not_an_item";

            GameSnapshot restored;
            string error;
            Assert.IsFalse(GameSave.TryRestore(data, _catalog, out restored, out error));
            Assert.IsNull(restored);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void RestoreRejectsGridSizeMismatch()
        {
            SaveData data = GameSave.Capture(_snapshot);
            data.Stash.Width = 7;

            GameSnapshot restored;
            string error;
            Assert.IsFalse(GameSave.TryRestore(data, _catalog, out restored, out error));
            StringAssert.Contains("尺寸", error);
        }

        [Test]
        public void RestoreRejectsUnsupportedVersion()
        {
            SaveData data = GameSave.Capture(_snapshot);
            data.Version = GameSave.CurrentVersion + 1;

            GameSnapshot restored;
            string error;
            Assert.IsFalse(GameSave.TryRestore(data, _catalog, out restored, out error));
            Assert.IsNull(restored);
            StringAssert.Contains("版本", error);
        }

        [Test]
        public void RestoredBagContinuesInstanceIds()
        {
            SaveData data = GameSave.Capture(_snapshot);

            GameSnapshot restored;
            string error;
            Assert.IsTrue(GameSave.TryRestore(data, _catalog, out restored, out error), error);

            int maxSavedId = 0;
            for (int i = 0; i < data.PlayerBag.Items.Length; i++)
            {
                if (data.PlayerBag.Items[i].InstanceId > maxSavedId)
                {
                    maxSavedId = data.PlayerBag.Items[i].InstanceId;
                }
            }

            ItemDefinition wire = _catalog.Get("wire");
            PlacedItem added;
            Assert.IsTrue(restored.PlayerBag.TryAdd(wire.Id, wire.Shape, out added));
            Assert.Greater(added.InstanceId, maxSavedId);
        }

        private static void AssertBagsEqual(BagInventory expected, BagInventory actual)
        {
            Assert.AreEqual(expected.Grid.Width, actual.Grid.Width);
            Assert.AreEqual(expected.Grid.Height, actual.Grid.Height);
            Assert.AreEqual(expected.Count, actual.Count);

            IReadOnlyList<PlacedItem> expectedItems = expected.Items;
            for (int i = 0; i < expectedItems.Count; i++)
            {
                PlacedItem source = expectedItems[i];
                PlacedItem target;
                Assert.IsTrue(actual.TryGet(source.InstanceId, out target), "实例 " + source.InstanceId + " 丢失。");
                Assert.AreEqual(source.DefinitionId, target.DefinitionId);
                Assert.AreEqual(source.Origin, target.Origin);
                Assert.AreEqual(source.Rotation, target.Rotation);
                Assert.AreEqual(source.Count, target.Count);
                Assert.AreEqual(source.Durability, target.Durability);
                Assert.AreEqual(source.Rotatable, target.Rotatable);
                Assert.AreEqual(ContainerOps.HasContents(source), ContainerOps.HasContents(target));
            }
        }

        private static PlacedItem FindByDefinition(BagInventory bag, string definitionId)
        {
            IReadOnlyList<PlacedItem> items = bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].DefinitionId == definitionId)
                {
                    return items[i];
                }
            }

            return null;
        }

        private static int FirstInstanceId(BagInventory bag)
        {
            IReadOnlyList<PlacedItem> items = bag.Items;
            return items.Count > 0 ? items[0].InstanceId : 0;
        }

        private static GameSnapshot BuildSnapshot(
            ItemCatalog catalog,
            out string questId,
            out int uplinkInstanceId)
        {
            var rack = new StorageRack();
            var insurance = new InsuranceHub();
            var stash = new BagInventory(StorageRack.Width, StorageRack.HeightForLevel(rack.Level));
            var playerBag = new BagInventory(6, 5);
            var secure = new BagInventory(
                InsuranceHub.WidthForLevel(insurance.Level),
                InsuranceHub.HeightForLevel(insurance.Level));

            var wallet = new ItemBitWallet(stash, catalog);
            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments(), wallet);

            // 推进到第 1 周第 4 天，避免用「初始状态」掩盖恢复错误。
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(campaign.BeginExpedition());
                campaign.SettleExpedition();
            }

            var merchant = new Merchant(catalog);
            merchant.RestockForDay(2, 3, 777);

            var pod = new DivePod();
            var mine = new BitMine();
            mine.EnsureFilledForDay(2, 3);

            var craft = new CraftingStation();
            var cpu = new CpuCore();
            var comms = new CommsHub();
            var quests = new QuestLog();
            var npcStocks = new NpcMarket(catalog);

            // 仓库：容器 + 材料 + 加密钥盒（多件不同形状的东西一起过一遍序列化）。
            Place(catalog, stash, "weapon_crate", 0, 0, 0);
            Place(catalog, stash, "scrap", 2, 0, 0, 4);
            Place(catalog, stash, "circuit", 3, 0, 0, 2);
            Place(catalog, stash, "key_box", 2, 2, 0);
            Place(catalog, stash, "bit", 6, 0, 0, 123);

            // 背包：带耐久的武器 + 带内容物的容器 + 可堆叠弹药。
            PlacedItem blade = Place(catalog, playerBag, "blade", 2, 0, 0);
            blade.Durability = 3;
            Place(catalog, playerBag, "plate", 0, 0, 0);
            Place(catalog, playerBag, "ammo", 0, 2, 0, 7);

            PlacedItem parts = Place(catalog, playerBag, "parts_case", 4, 0, 0);
            ItemContainer partsBag = ContainerOps.Ensure(parts, catalog.Get("parts_case"));
            Assert.IsNotNull(partsBag);
            PlacedItem inner;
            string innerError;
            Assert.IsTrue(partsBag.Bag.TryAddAt(
                "scrap",
                catalog.Get("scrap").Shape,
                new Cell(0, 0),
                0,
                true,
                2,
                out inner,
                out innerError), innerError);

            // 保险箱只收 1×1 非武器/护甲。
            Place(catalog, secure, "coin", 0, 0, 0);

            // 潜网舱：放一把密钥并选中。
            PlacedItem key = Place(catalog, pod.Grid, "dive_wild", 0, 0, 0);
            Assert.IsTrue(pod.SelectKey(catalog, key.InstanceId));

            // CPU：芯片。
            Place(catalog, cpu.Grid, "chip_hp_1", 0, 0, 0);

            // 通讯中枢：上行链路 + 接一个任务（制造一把锁）。
            PlacedItem uplink = Place(catalog, comms.Grid, "uplink_junk", 0, 0, 0);
            uplinkInstanceId = uplink.InstanceId;

            NpcDefinition npc;
            Assert.IsTrue(NpcLibrary.TryFindByUplink("uplink_junk", out npc));
            List<QuestDefinition> npcQuests = QuestLibrary.ForNpc(npc.Id);
            Assert.IsNotEmpty(npcQuests);

            int today = QuestLog.Today(campaign);
            string acceptError;
            Assert.IsTrue(quests.TryAccept(npcQuests[0], comms, today, out acceptError), acceptError);
            questId = npcQuests[0].Id;

            npcStocks.EnsureAll(today, true);

            return new GameSnapshot
            {
                Campaign = campaign,
                Merchant = merchant,
                Pod = pod,
                Mine = mine,
                Craft = craft,
                Cpu = cpu,
                Insurance = insurance,
                Rack = rack,
                Comms = comms,
                Quests = quests,
                NpcStocks = npcStocks,
                Stash = stash,
                PlayerBag = playerBag,
                Secure = secure
            };
        }

        private static PlacedItem Place(
            ItemCatalog catalog,
            BagInventory bag,
            string itemId,
            int x,
            int y,
            int rotation,
            int count = 1)
        {
            ItemDefinition definition = catalog.Get(itemId);
            PlacedItem placed;
            string error;
            Assert.IsTrue(
                bag.TryAddAt(itemId, definition.Shape, new Cell(x, y), rotation, true, count, out placed, out error),
                itemId + " 放置失败：" + error);
            ContainerOps.Ensure(placed, definition);
            return placed;
        }
    }
}
