using System.Collections.Generic;
using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    /// <summary>
    /// 任务系统。核心要锁住的两件事：
    /// 1. 并行限制不是数字，是「接取即锁定上行链路」；
    /// 2. 失败路径必须解锁，否则超时会把通讯中枢永久锁死。
    /// </summary>
    public class QuestTests
    {
        private static ItemCatalog Catalog()
        {
            return DefaultItemLibrary.CreateCatalog();
        }

        private static CampaignState Campaign()
        {
            return new CampaignState(CampaignState.CreateDefaultInstallments(), 1000);
        }

        private static BagInventory EmptyStash()
        {
            return new BagInventory(6, 5);
        }

        /// <summary>L5 中枢（16 格），四条链路都能放进去。</summary>
        private static CommsHub HubWith(ItemCatalog catalog, params string[] uplinkIds)
        {
            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments(), 100000);
            var hub = new CommsHub();
            var stash = new BagInventory(6, 10);
            string[] mats =
            {
                "scrap", "scrap", "wire", "alloy", "circuit", "circuit",
                "servo", "superalloy", "optic_fiber"
            };
            for (int i = 0; i < mats.Length; i++)
            {
                PlacedItem placed;
                Assert.IsTrue(stash.TryAdd(mats[i], catalog.Get(mats[i]).Shape, out placed), mats[i]);
            }

            while (hub.Level < CommsHub.MaxLevel)
            {
                string upgradeError;
                Assert.IsTrue(hub.TryUpgrade(campaign, stash, out upgradeError), upgradeError);
            }

            for (int i = 0; i < uplinkIds.Length; i++)
            {
                PlacedItem placed;
                Assert.IsTrue(hub.Grid.TryAdd(uplinkIds[i], catalog.Get(uplinkIds[i]).Shape, out placed), uplinkIds[i]);
            }

            return hub;
        }

        private static QuestDefinition Q(string id)
        {
            return QuestLibrary.Get(id);
        }

        /// <summary>把 q_junk_1 做完并领掉，好让 q_junk_2 / q_junk_3 出现。</summary>
        private static void FinishJunkIntro(
            QuestLog log, CommsHub hub, CampaignState campaign, BagInventory stash, ItemCatalog catalog)
        {
            string error;
            Assert.IsTrue(log.TryAccept(Q("q_junk_1"), hub, 0, out error), error);
            log.ReportDiveCleared("dive_wild", false);
            Assert.IsTrue(log.TryClaim(Q("q_junk_1"), hub, campaign, stash, catalog, out error), error);
        }

        /// <summary>把 q_arms_1 做完并领掉，好让 q_arms_2 出现。</summary>
        private static void FinishArmsIntro(
            QuestLog log, CommsHub hub, CampaignState campaign, BagInventory stash, ItemCatalog catalog)
        {
            string error;
            Assert.IsTrue(log.TryAccept(Q("q_arms_1"), hub, QuestLog.Today(campaign), out error), error);
            for (int i = 0; i < 10; i++)
            {
                log.ReportKill("scavenger", false);
            }

            Assert.IsTrue(log.TryClaim(Q("q_arms_1"), hub, campaign, stash, catalog, out error), error);
        }

        [Test]
        public void QuestCannotBeOfferedWithoutContact()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubWith(catalog);
            var log = new QuestLog();

            string reason;
            Assert.IsFalse(log.CanOffer(Q("q_junk_1"), hub, 0, out reason));
            StringAssert.Contains("还没有联系上他", reason);
        }

        [Test]
        public void AcceptingAQuestLocksItsUplink()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubWith(catalog, "uplink_junk");
            var log = new QuestLog();
            var campaign = Campaign();

            Assert.AreEqual(QuestState.Offered, log.StateOf("q_junk_1"));

            string error;
            Assert.IsTrue(log.TryAccept(Q("q_junk_1"), hub, QuestLog.Today(campaign), out error), error);

            Assert.AreEqual(QuestState.Active, log.StateOf("q_junk_1"));
            PlacedItem uplink;
            Assert.IsTrue(hub.TryGetUplink(NpcLibrary.Junk, out uplink));
            Assert.IsTrue(hub.IsLocked(uplink.InstanceId));
            Assert.IsFalse(hub.CanModify(uplink.InstanceId, out error));
        }

        /// <summary>
        /// 一条上行链路只承载一个任务。SEED 的 q_seed_1 / q_seed_2 都没有前置，
        /// 所以第二个被拒的原因只可能是链路占用。
        /// </summary>
        [Test]
        public void OneUplinkCarriesOnlyOneQuest()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubWith(catalog, "uplink_seed");
            var log = new QuestLog();
            var campaign = Campaign();

            string error;
            Assert.IsTrue(log.TryAccept(Q("q_seed_1"), hub, 0, out error), error);

            string reason;
            Assert.IsFalse(log.CanOffer(Q("q_seed_2"), hub, 0, out reason));
            StringAssert.Contains("正在处理另一个任务", reason);
            Assert.IsFalse(log.TryAccept(Q("q_seed_2"), hub, 0, out error));
        }

        [Test]
        public void ClaimingUnlocksTheUplink()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubWith(catalog, "uplink_junk");
            var log = new QuestLog();
            var campaign = Campaign();
            BagInventory stash = EmptyStash();

            string error;
            Assert.IsTrue(log.TryAccept(Q("q_junk_1"), hub, 0, out error), error);
            log.ReportDiveCleared("dive_wild", false);
            Assert.AreEqual(QuestState.Ready, log.StateOf("q_junk_1"));

            // 达成之后仍然锁着——否则可以「达成但没领奖」时把链路抽走换别人，零成本过桥。
            PlacedItem uplink;
            Assert.IsTrue(hub.TryGetUplink(NpcLibrary.Junk, out uplink));
            Assert.IsTrue(hub.IsLocked(uplink.InstanceId));

            Assert.IsTrue(log.TryClaim(Q("q_junk_1"), hub, campaign, stash, catalog, out error), error);
            Assert.AreEqual(QuestState.Claimed, log.StateOf("q_junk_1"));
            Assert.IsFalse(hub.IsLocked(uplink.InstanceId));
            Assert.AreEqual(1080, campaign.Bit, "初始 1000 + 奖励 80");
        }

        [Test]
        public void KillReportsDriveKillObjectives()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubWith(catalog, "uplink_arms");
            var log = new QuestLog();

            string error;
            Assert.IsTrue(log.TryAccept(Q("q_arms_1"), hub, 0, out error), error);

            for (int i = 0; i < 9; i++)
            {
                log.ReportKill("scavenger", false);
            }

            Assert.AreEqual(QuestState.Active, log.StateOf("q_arms_1"));
            Assert.AreEqual(9, log.Get("q_arms_1").Count);

            log.ReportKill("scavenger", false);
            Assert.AreEqual(QuestState.Ready, log.StateOf("q_arms_1"));

            // 超出目标不会溢出。
            log.ReportKill("scavenger", false);
            Assert.AreEqual(10, log.Get("q_arms_1").Count);
        }

        [Test]
        public void DiveReportsOnlyMatchTheTargetSite()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubWith(catalog, "uplink_junk");
            var log = new QuestLog();

            string error;
            Assert.IsTrue(log.TryAccept(Q("q_junk_1"), hub, 0, out error), error);

            log.ReportDiveCleared("dive_military", false);
            Assert.AreEqual(QuestState.Active, log.StateOf("q_junk_1"), "军用的通关不该算旷野");

            log.ReportDiveCleared("dive_wild", false);
            Assert.AreEqual(QuestState.Ready, log.StateOf("q_junk_1"));
        }

        [Test]
        public void CollectObjectivesUseAbsoluteCountAndDowngrade()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubWith(catalog, "uplink_junk");
            var log = new QuestLog();
            var campaign = Campaign();
            BagInventory stash = EmptyStash();

            // q_junk_2 需要先完成 q_junk_1。
            string error;
            Assert.IsTrue(log.TryAccept(Q("q_junk_1"), hub, 0, out error), error);
            log.ReportDiveCleared("dive_wild", false);
            Assert.IsTrue(log.TryClaim(Q("q_junk_1"), hub, campaign, stash, catalog, out error), error);

            Assert.IsTrue(log.TryAccept(Q("q_junk_2"), hub, 0, out error), error);

            for (int i = 0; i < 4; i++)
            {
                PlacedItem placed;
                Assert.IsTrue(stash.TryAdd("scrap", catalog.Get("scrap").Shape, out placed));
            }

            log.RefreshStatic(catalog, stash, null, campaign);
            Assert.AreEqual(4, log.Get("q_junk_2").Count);
            Assert.AreEqual(QuestState.Active, log.StateOf("q_junk_2"));

            PlacedItem fifth;
            Assert.IsTrue(stash.TryAdd("scrap", catalog.Get("scrap").Shape, out fifth));
            log.RefreshStatic(catalog, stash, null, campaign);
            Assert.AreEqual(QuestState.Ready, log.StateOf("q_junk_2"));

            // 卖掉两个就掉回 Active——否则能空手领奖。
            Assert.IsTrue(ItemSearch.TryConsume(stash, "scrap", 2));
            log.RefreshStatic(catalog, stash, null, campaign);
            Assert.AreEqual(QuestState.Active, log.StateOf("q_junk_2"));
        }

        [Test]
        public void ClaimConsumesTheDeliveredItems()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubWith(catalog, "uplink_junk");
            var log = new QuestLog();
            var campaign = Campaign();
            BagInventory stash = EmptyStash();

            string error;
            Assert.IsTrue(log.TryAccept(Q("q_junk_1"), hub, 0, out error), error);
            log.ReportDiveCleared("dive_wild", false);
            Assert.IsTrue(log.TryClaim(Q("q_junk_1"), hub, campaign, stash, catalog, out error), error);

            Assert.IsTrue(log.TryAccept(Q("q_junk_2"), hub, 0, out error), error);
            for (int i = 0; i < 5; i++)
            {
                PlacedItem placed;
                Assert.IsTrue(stash.TryAdd("scrap", catalog.Get("scrap").Shape, out placed));
            }

            log.RefreshStatic(catalog, stash, null, campaign);
            Assert.AreEqual(QuestState.Ready, log.StateOf("q_junk_2"));

            Assert.IsTrue(log.TryClaim(Q("q_junk_2"), hub, campaign, stash, catalog, out error), error);
            Assert.AreEqual(0, ItemSearch.CountOf(stash, "scrap"));
            Assert.AreEqual(1, ItemSearch.CountOf(stash, "uplink_arms"), "奖励的上行链路要进仓库");
        }

        [Test]
        public void ClaimFailsWhenTheRewardDoesNotFit()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubWith(catalog, "uplink_junk");
            var log = new QuestLog();
            var campaign = Campaign();

            // 4 格：q_junk_3 的奖励是 2 个修复包（各 1 格），塞满时放不下。
            var stash = new BagInventory(2, 2);

            string error;
            FinishJunkIntro(log, hub, campaign, stash, catalog);

            Assert.IsTrue(log.TryAccept(Q("q_junk_3"), hub, 0, out error), "q_junk_3 该是可接的");
            log.ReportMineCollected();
            log.ReportMineCollected();
            log.ReportMineCollected();
            Assert.AreEqual(QuestState.Ready, log.StateOf("q_junk_3"));

            var blockers = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                PlacedItem blocker;
                Assert.IsTrue(stash.TryAdd("scrap", catalog.Get("scrap").Shape, out blocker));
                blockers.Add(blocker.InstanceId);
            }

            Assert.IsFalse(log.TryClaim(Q("q_junk_3"), hub, campaign, stash, catalog, out error));
            StringAssert.Contains("放不下", error);
            Assert.AreEqual(QuestState.Ready, log.StateOf("q_junk_3"), "领奖失败不能改变状态");

            stash.Remove(blockers[0]);
            stash.Remove(blockers[1]);

            Assert.IsTrue(log.TryClaim(Q("q_junk_3"), hub, campaign, stash, catalog, out error), error);
            Assert.AreEqual(2, ItemSearch.CountOf(stash, "patch"));
        }

        [Test]
        public void PrerequisiteQuestMustBeClaimed()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubWith(catalog, "uplink_junk");
            var log = new QuestLog();

            string reason;
            Assert.IsFalse(log.CanOffer(Q("q_junk_2"), hub, 0, out reason));
            StringAssert.Contains("前置任务", reason);
            Assert.IsTrue(log.CanOffer(Q("q_junk_1"), hub, 0, out reason), reason);
        }

        [Test]
        public void TimeoutFailsTheQuestAndUnlocksTheUplink()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubWith(catalog, "uplink_arms");
            var log = new QuestLog();
            var campaign = Campaign();
            BagInventory stash = EmptyStash();
            int today = QuestLog.Today(campaign);

            FinishArmsIntro(log, hub, campaign, stash, catalog);

            string error;
            Assert.IsTrue(log.TryAccept(Q("q_arms_2"), hub, today, out error), error);
            Assert.AreEqual(today + 5, log.Get("q_arms_2").DeadlineDay);

            PlacedItem uplink;
            Assert.IsTrue(hub.TryGetUplink(NpcLibrary.Arms, out uplink));
            Assert.IsTrue(hub.IsLocked(uplink.InstanceId));

            log.Tick(hub, today + 5);
            Assert.AreEqual(QuestState.Active, log.StateOf("q_arms_2"), "到期限当天还算数");

            log.Tick(hub, today + 6);
            Assert.AreEqual(QuestState.Failed, log.StateOf("q_arms_2"));
            Assert.IsFalse(hub.IsLocked(uplink.InstanceId), "超时必须解锁，否则中枢会被永久锁死");

            // 冷却 2 天内不能再接。
            string reason;
            Assert.IsFalse(log.CanOffer(Q("q_arms_2"), hub, today + 7, out reason));
            StringAssert.Contains("还要等", reason);
            Assert.IsTrue(log.CanOffer(Q("q_arms_2"), hub, today + 8, out reason), reason);
        }

        [Test]
        public void AbandonUnlocksAndStartsCooldown()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubWith(catalog, "uplink_junk");
            var log = new QuestLog();

            string error;
            Assert.IsTrue(log.TryAccept(Q("q_junk_1"), hub, 0, out error), error);
            Assert.IsTrue(log.Abandon(Q("q_junk_1"), hub, 0, out error), error);

            Assert.AreEqual(QuestState.Failed, log.StateOf("q_junk_1"));
            PlacedItem uplink;
            Assert.IsTrue(hub.TryGetUplink(NpcLibrary.Junk, out uplink));
            Assert.IsFalse(hub.IsLocked(uplink.InstanceId));

            string reason;
            Assert.IsFalse(log.CanOffer(Q("q_junk_1"), hub, 1, out reason));
            Assert.IsTrue(log.CanOffer(Q("q_junk_1"), hub, 3, out reason), reason);
        }

        [Test]
        public void MineReportsDriveCollectMineObjective()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubWith(catalog, "uplink_junk");
            var log = new QuestLog();
            var campaign = Campaign();
            BagInventory stash = EmptyStash();

            string error;
            FinishJunkIntro(log, hub, campaign, stash, catalog);
            Assert.IsTrue(log.TryAccept(Q("q_junk_3"), hub, 0, out error), error);

            log.ReportMineCollected();
            log.ReportMineCollected();
            Assert.AreEqual(QuestState.Active, log.StateOf("q_junk_3"));

            log.ReportMineCollected();
            Assert.AreEqual(QuestState.Ready, log.StateOf("q_junk_3"));
        }

        [Test]
        public void UplinkRewardsLandInTheStash()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubWith(catalog, "uplink_junk");
            var log = new QuestLog();
            var campaign = Campaign();
            BagInventory stash = EmptyStash();

            string error;
            Assert.IsTrue(log.TryAccept(Q("q_junk_1"), hub, 0, out error), error);
            log.ReportDiveCleared("dive_wild", false);
            Assert.IsTrue(log.TryClaim(Q("q_junk_1"), hub, campaign, stash, catalog, out error), error);

            Assert.IsTrue(log.TryAccept(Q("q_junk_2"), hub, 0, out error), error);
            for (int i = 0; i < 5; i++)
            {
                PlacedItem placed;
                Assert.IsTrue(stash.TryAdd("scrap", catalog.Get("scrap").Shape, out placed));
            }

            log.RefreshStatic(catalog, stash, null, campaign);
            Assert.IsTrue(log.TryClaim(Q("q_junk_2"), hub, campaign, stash, catalog, out error), error);

            Assert.AreEqual(1, ItemSearch.CountOf(stash, "uplink_arms"));
            Assert.AreEqual(ItemCategory.Uplink, catalog.Get("uplink_arms").Category);
        }

        [Test]
        public void ActiveQuestsOnlyListsUnclaimedOnes()
        {
            ItemCatalog catalog = Catalog();
            CommsHub hub = HubWith(catalog, "uplink_junk", "uplink_seed");
            var log = new QuestLog();
            var campaign = Campaign();
            BagInventory stash = EmptyStash();

            string error;
            Assert.IsTrue(log.TryAccept(Q("q_junk_1"), hub, 0, out error), error);
            Assert.IsTrue(log.TryAccept(Q("q_seed_1"), hub, 0, out error), error);
            Assert.AreEqual(2, log.ActiveQuests.Count);

            log.ReportDiveCleared("dive_wild", false);
            Assert.AreEqual(2, log.ActiveQuests.Count, "Ready 仍然算进行中");

            Assert.IsTrue(log.TryClaim(Q("q_junk_1"), hub, campaign, stash, catalog, out error), error);
            Assert.AreEqual(1, log.ActiveQuests.Count);
        }

        // ---------- 数据完整性 ----------

        [Test]
        public void EveryQuestReferencesExistingContent()
        {
            ItemCatalog catalog = Catalog();

            foreach (QuestDefinition quest in QuestLibrary.All)
            {
                NpcDefinition npc;
                Assert.IsTrue(NpcLibrary.TryGet(quest.NpcId, out npc), quest.Id + " 指向未知 NPC");

                bool npcOffersIt = false;
                for (int i = 0; i < npc.QuestPool.Length; i++)
                {
                    if (npc.QuestPool[i] == quest.Id)
                    {
                        npcOffersIt = true;
                        break;
                    }
                }

                Assert.IsTrue(npcOffersIt, quest.Id + " 不在 " + npc.Id + " 的任务池里");
                Assert.Greater(quest.TargetCount, 0, quest.Id + " 的目标数量必须是正数");

                if (!string.IsNullOrEmpty(quest.RequiresQuestId))
                {
                    QuestDefinition previous;
                    Assert.IsTrue(QuestLibrary.TryGet(quest.RequiresQuestId, out previous),
                        quest.Id + " 的前置任务不存在");
                }

                if (!string.IsNullOrEmpty(quest.TargetId))
                {
                    if (quest.Objective == QuestObjectiveKind.ClearDive)
                    {
                        DiveKeyDefinition dive;
                        Assert.IsTrue(DiveKeyLibrary.TryGetKey(quest.TargetId, out dive),
                            quest.Id + " 指向未知地点 " + quest.TargetId);
                    }
                    else if (quest.Objective == QuestObjectiveKind.CollectItem)
                    {
                        ItemDefinition item;
                        Assert.IsTrue(catalog.TryGet(quest.TargetId, out item),
                            quest.Id + " 指向未知物品 " + quest.TargetId);
                    }
                    else if (quest.Objective == QuestObjectiveKind.KillTagged)
                    {
                        Assert.IsNotNull(EnemyLibrary.Get(quest.TargetId),
                            quest.Id + " 指向未知敌人 " + quest.TargetId);
                    }
                }

                if (quest.RewardItemIds != null)
                {
                    for (int i = 0; i < quest.RewardItemIds.Length; i++)
                    {
                        ItemDefinition reward;
                        Assert.IsTrue(catalog.TryGet(quest.RewardItemIds[i], out reward),
                            quest.Id + " 的奖励物品不存在：" + quest.RewardItemIds[i]);
                    }
                }

                if (!string.IsNullOrEmpty(quest.RewardUplinkItemId))
                {
                    ItemDefinition uplink;
                    Assert.IsTrue(catalog.TryGet(quest.RewardUplinkItemId, out uplink),
                        quest.Id + " 的奖励链路不存在");
                    Assert.AreEqual(ItemCategory.Uplink, uplink.Category);
                }
            }
        }

        [Test]
        public void EveryNpcReferencesExistingContent()
        {
            ItemCatalog catalog = Catalog();
            var seenUplinks = new HashSet<string>();

            foreach (NpcDefinition npc in NpcLibrary.All)
            {
                ItemDefinition uplink;
                Assert.IsTrue(catalog.TryGet(npc.UplinkItemId, out uplink), npc.Id + " 的上行链路不存在");
                Assert.AreEqual(ItemCategory.Uplink, uplink.Category, npc.Id + " 的链路类别不对");
                Assert.IsTrue(seenUplinks.Add(npc.UplinkItemId), "两条上行链路共用了同一个物品");
                Assert.AreEqual(npc.Tier, uplink.Level, npc.Id + " 的档位与链路等级不一致");

                for (int i = 0; i < npc.StockPool.Length; i++)
                {
                    ItemDefinition stock;
                    Assert.IsTrue(catalog.TryGet(npc.StockPool[i], out stock),
                        npc.Id + " 的货架物品不存在：" + npc.StockPool[i]);
                    Assert.Greater(stock.Value, 0, npc.Id + " 卖的东西必须有价值：" + npc.StockPool[i]);
                }

                for (int i = 0; i < npc.QuestPool.Length; i++)
                {
                    QuestDefinition quest;
                    Assert.IsTrue(QuestLibrary.TryGet(npc.QuestPool[i], out quest));
                    Assert.AreEqual(npc.Id, quest.NpcId);
                }
            }

            Assert.AreEqual(4, NpcLibrary.All.Count);
            Assert.AreEqual(4, seenUplinks.Count);
        }

        /// <summary>
        /// NPC 的「更好」= 绕开周次限制。这条测试锁住这个不对称：
        /// K-7 的货架里必须有高于 W1 商人上限（T2）的东西。
        /// </summary>
        [Test]
        public void NpcStockIgnoresTheWeeklyMerchantCap()
        {
            ItemCatalog catalog = Catalog();
            NpcDefinition arms;
            Assert.IsTrue(NpcLibrary.TryGet(NpcLibrary.Arms, out arms));

            int weekOneCap = Merchant.MaxTierForWeek(1);
            Assert.AreEqual(2, weekOneCap);

            int highest = 0;
            for (int i = 0; i < arms.StockPool.Length; i++)
            {
                int level = catalog.Get(arms.StockPool[i]).Level;
                if (level > highest)
                {
                    highest = level;
                }
            }

            Assert.Greater(highest, weekOneCap, "NPC 货架必须存在高于 W1 商人上限的货");
        }

        // ---------- NPC 货架 ----------

        [Test]
        public void NpcPriceIsFourTimesValue()
        {
            ItemCatalog catalog = Catalog();
            ItemDefinition definition = catalog.Get("rail_lance");

            Assert.AreEqual(290, definition.Value);
            Assert.AreEqual(1160, NpcMarket.PriceOf(definition));

            // 比主商人贵：绕过周次门是要付代价的。
            Assert.Greater(NpcMarket.PriceMultiplier, Merchant.BuyPriceMultiplier);
        }

        [Test]
        public void NpcStockRefreshFillsTheShelf()
        {
            ItemCatalog catalog = Catalog();
            var market = new NpcMarket(catalog);
            NpcDefinition npc;
            Assert.IsTrue(NpcLibrary.TryGet(NpcLibrary.Arms, out npc));

            market.EnsureStock(npc, 1, 0, true);
            BagInventory stock = market.StockFor(npc.Id);

            Assert.AreEqual(NpcMarket.StockSize, stock.Count);
            Assert.Greater(stock.Count, 0);
        }

        [Test]
        public void BuyingFromNpcMovesTheItemAndSpendsBit()
        {
            ItemCatalog catalog = Catalog();
            var market = new NpcMarket(catalog);
            NpcDefinition npc;
            Assert.IsTrue(NpcLibrary.TryGet(NpcLibrary.Junk, out npc));
            market.EnsureStock(npc, 0, 0, true);

            BagInventory stock = market.StockFor(npc.Id);
            PlacedItem offer = stock.Items[0];
            ItemDefinition definition = catalog.Get(offer.DefinitionId);
            int price = NpcMarket.PriceOf(definition);

            var campaign = new CampaignState(CampaignState.CreateDefaultInstallments(), price);
            var stash = new BagInventory(6, 5);

            string error;
            Assert.IsTrue(market.TryBuy(npc, offer.InstanceId, campaign, stash, out error), error);

            Assert.AreEqual(0, campaign.Bit);
            Assert.AreEqual(1, ItemSearch.CountOf(stash, definition.Id));
            Assert.AreEqual(NpcMarket.StockSize - 1, stock.Count);
        }

        [Test]
        public void BuyingFailsWithoutSpaceOrBit()
        {
            ItemCatalog catalog = Catalog();
            var market = new NpcMarket(catalog);
            NpcDefinition npc;
            Assert.IsTrue(NpcLibrary.TryGet(NpcLibrary.Junk, out npc));
            market.EnsureStock(npc, 0, 0, true);

            BagInventory stock = market.StockFor(npc.Id);
            PlacedItem offer = stock.Items[0];
            ItemDefinition definition = catalog.Get(offer.DefinitionId);
            int price = NpcMarket.PriceOf(definition);

            var poor = new CampaignState(CampaignState.CreateDefaultInstallments(), price - 1);
            var stash = new BagInventory(6, 5);

            string error;
            Assert.IsFalse(market.TryBuy(npc, offer.InstanceId, poor, stash, out error));
            StringAssert.Contains("bit 不足", error);
            Assert.AreEqual(NpcMarket.StockSize, stock.Count, "买不成库存不该变动");

            var rich = new CampaignState(CampaignState.CreateDefaultInstallments(), price * 2);
            var tinyStash = new BagInventory(1, 1);
            PlacedItem blocker;
            Assert.IsTrue(tinyStash.TryAdd("scrap", catalog.Get("scrap").Shape, out blocker));

            Assert.IsFalse(market.TryBuy(npc, offer.InstanceId, rich, tinyStash, out error));
            StringAssert.Contains("空间", error);
            Assert.AreEqual(price * 2, rich.Bit, "买不成不能扣钱");
        }

        [Test]
        public void TodayIsAnAbsoluteDayAcrossWeeks()
        {
            var campaign = Campaign();

            // W1D1 起手 = 第 1 天。用绝对天而不是「本周第几天」，超时判定跨周才不会算错
            // （W1D7 = 7，W2D1 = 8，日期连续递增）。
            Assert.AreEqual(1, QuestLog.Today(campaign));
            Assert.AreEqual(
                (campaign.Week - 1) * CampaignState.DaysPerWeek + campaign.Day,
                QuestLog.Today(campaign));
        }
    }
}
