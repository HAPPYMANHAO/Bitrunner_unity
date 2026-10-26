using BitRunner.Core;
using NUnit.Framework;

namespace BitRunner.Tests
{
    public sealed class NpcBagFlowTests
    {
        private ItemCatalog _catalog;
        private BagInventory _stash;
        private BagInventory _bag;
        private CampaignState _campaign;
        private CommsHub _comms;
        private QuestLog _quests;
        private NpcMarket _market;
        private NpcDefinition _npc;

        [SetUp]
        public void SetUp()
        {
            _catalog = DefaultItemLibrary.CreateCatalog();

            var rack = new StorageRack();
            _stash = new BagInventory(StorageRack.Width, StorageRack.HeightForLevel(rack.Level));
            _bag = new BagInventory(6, 5);

            var wallet = new ItemBitWallet(_stash, _catalog);
            _campaign = new CampaignState(CampaignState.CreateDefaultInstallments(), wallet);
            _campaign.AddBit(1000);

            _comms = new CommsHub();
            _quests = new QuestLog();
            _market = new NpcMarket(_catalog);

            Assert.IsTrue(NpcLibrary.TryGet(NpcLibrary.Junk, out _npc));

            ItemDefinition uplink = _catalog.Get(_npc.UplinkItemId);
            PlacedItem placed;
            string error;
            Assert.IsTrue(_comms.Grid.TryAdd(uplink.Id, uplink.Shape, out placed), "上行链路放置失败。");

            _market.EnsureAll(QuestLog.Today(_campaign), true);
            NpcBagFlow.SyncAll(_market, _quests, _comms, _campaign, _bag, _stash, _catalog);
        }

        [Test]
        public void SyncPutsOfferableContractsOnTheBoard()
        {
            BagInventory board = _market.StockFor(_npc.Id);

            Assert.IsTrue(NpcBagFlow.HasCard(board, CardId("q_junk_1")), "可接任务应摆在柜台上。");
            Assert.IsFalse(NpcBagFlow.HasCard(board, CardId("q_junk_2")), "前置未完成的任务不该出现。");
        }

        [Test]
        public void TakingContractAcceptsQuestAndLocksLink()
        {
            BagInventory board = _market.StockFor(_npc.Id);
            PlacedItem card = FindCard(board, "q_junk_1");
            Assert.IsNotNull(card);

            string message;
            Assert.IsTrue(NpcBagFlow.TryAccept(
                _npc, _market, _quests, _comms, QuestLog.Today(_campaign), card.InstanceId, _bag, _catalog, out message), message);

            Assert.AreEqual(QuestState.Active, _quests.StateOf("q_junk_1"));
            Assert.IsFalse(NpcBagFlow.HasCard(board, CardId("q_junk_1")), "柜台上的契约应被拿走。");
            Assert.IsTrue(NpcBagFlow.HasCard(_bag, CardId("q_junk_1")), "契约应进入玩家背包。");
            Assert.AreEqual(1, _comms.LockedLinks);
        }

        [Test]
        public void ReturningContractConsumesDeliveryAndPaysOut()
        {
            BagInventory board = _market.StockFor(_npc.Id);
            PlacedItem card = FindCard(board, "q_junk_1");
            string message;
            Assert.IsTrue(NpcBagFlow.TryAccept(
                _npc, _market, _quests, _comms, QuestLog.Today(_campaign), card.InstanceId, _bag, _catalog, out message), message);

            // q_junk_1 = 通关一次旷野潜网。
            _quests.ReportDiveCleared("dive_wild", false);
            Assert.AreEqual(QuestState.Ready, _quests.StateOf("q_junk_1"));

            int bitBefore = _campaign.Bit;
            PlacedItem owned = FindCard(_bag, "q_junk_1");
            Assert.IsNotNull(owned);

            Assert.IsTrue(NpcBagFlow.TryTurnIn(
                _npc, _market, _quests, _comms, _campaign, owned.InstanceId, _bag, _bag, _stash, _catalog, out message), message);

            Assert.AreEqual(QuestState.Claimed, _quests.StateOf("q_junk_1"));
            Assert.AreEqual(bitBefore + 80, _campaign.Bit);
            Assert.IsFalse(NpcBagFlow.HasCard(_bag, CardId("q_junk_1")), "交付后契约应被销毁。");
            Assert.AreEqual(0, _comms.LockedLinks, "交付后链路上锁应解除。");

            // 下一环（需要先完成 q_junk_1）在柜台同步后应该出现。
            NpcBagFlow.SyncAll(_market, _quests, _comms, _campaign, _bag, _stash, _catalog);
            Assert.IsTrue(NpcBagFlow.HasCard(board, CardId("q_junk_2")), "后续任务应自动补上柜台。");
        }

        [Test]
        public void ReturningContractEarlyIsRefused()
        {
            BagInventory board = _market.StockFor(_npc.Id);
            PlacedItem card = FindCard(board, "q_junk_1");
            string message;
            Assert.IsTrue(NpcBagFlow.TryAccept(
                _npc, _market, _quests, _comms, QuestLog.Today(_campaign), card.InstanceId, _bag, _catalog, out message), message);

            PlacedItem owned = FindCard(_bag, "q_junk_1");
            Assert.IsFalse(NpcBagFlow.TryTurnIn(
                _npc, _market, _quests, _comms, _campaign, owned.InstanceId, _bag, _bag, _stash, _catalog, out message));
            Assert.AreEqual(QuestState.Active, _quests.StateOf("q_junk_1"));
            Assert.IsTrue(NpcBagFlow.HasCard(_bag, CardId("q_junk_1")));
        }

        [Test]
        public void BuyingGoodsSpendsBitAndMovesItem()
        {
            BagInventory board = _market.StockFor(_npc.Id);
            PlacedItem goods = null;
            foreach (PlacedItem item in board.Items)
            {
                ItemDefinition definition;
                if (_catalog.TryGet(item.DefinitionId, out definition) && definition.Category != ItemCategory.Quest)
                {
                    goods = item;
                    break;
                }
            }

            Assert.IsNotNull(goods, "柜台应该有商品。");
            int price = NpcMarket.PriceOf(_catalog.Get(goods.DefinitionId));
            int bitBefore = _campaign.Bit;
            Assert.GreaterOrEqual(bitBefore, price);

            string message;
            Assert.IsTrue(NpcBagFlow.TryBuy(_npc, _market, goods.InstanceId, _campaign, _bag, _catalog, out message), message);

            Assert.AreEqual(bitBefore - price, _campaign.Bit);
            Assert.IsTrue(Contains(_bag, goods.DefinitionId), "买到的商品应进入背包。");
            Assert.IsNull(FindRaw(board, goods.InstanceId), "买走的商品应离开柜台。");
        }

        [Test]
        public void BuyingWithoutBitIsRefused()
        {
            BagInventory board = _market.StockFor(_npc.Id);
            PlacedItem goods = null;
            foreach (PlacedItem item in board.Items)
            {
                ItemDefinition definition;
                if (_catalog.TryGet(item.DefinitionId, out definition) && definition.Category != ItemCategory.Quest)
                {
                    goods = item;
                    break;
                }
            }

            Assert.IsNotNull(goods);
            _campaign.TrySpend(_campaign.Bit);

            string message;
            Assert.IsFalse(NpcBagFlow.TryBuy(_npc, _market, goods.InstanceId, _campaign, _bag, _catalog, out message));
            Assert.IsNotNull(FindRaw(board, goods.InstanceId));
        }

        [Test]
        public void SyncRepairsMissingContract()
        {
            BagInventory board = _market.StockFor(_npc.Id);
            PlacedItem card = FindCard(board, "q_junk_1");
            string message;
            Assert.IsTrue(NpcBagFlow.TryAccept(
                _npc, _market, _quests, _comms, QuestLog.Today(_campaign), card.InstanceId, _bag, _catalog, out message), message);

            PlacedItem owned = FindCard(_bag, "q_junk_1");
            _bag.Remove(owned.InstanceId);
            Assert.IsFalse(NpcBagFlow.HasCard(_bag, CardId("q_junk_1")));

            NpcBagFlow.SyncAll(_market, _quests, _comms, _campaign, _bag, _stash, _catalog);
            Assert.IsTrue(NpcBagFlow.HasCard(_bag, CardId("q_junk_1")), "进行中的任务缺卡时应自动补回。");
        }

        [Test]
        public void ContractIsRejectedBySecureContainerAndMerchantStock()
        {
            ItemDefinition card = _catalog.Get(CardId("q_junk_1"));
            Assert.IsFalse(SecureContainerRules.IsEligible(card), "契约不该能进保险箱。");

            var merchant = new Merchant(_catalog);
            foreach (string id in merchant.EligibleItemIds)
            {
                Assert.IsFalse(QuestItemLibrary.IsQuestCard(id), "契约不该出现在商人进货池里。");
            }
        }

        private static string CardId(string questId)
        {
            return QuestItemLibrary.DefinitionIdOf(questId);
        }

        private static PlacedItem FindCard(BagInventory bag, string questId)
        {
            return bag.CountOf(CardId(questId)) > 0 ? FindByDefinition(bag, CardId(questId)) : null;
        }

        private static bool Contains(BagInventory bag, string definitionId)
        {
            return bag.CountOf(definitionId) > 0;
        }

        private static PlacedItem FindByDefinition(BagInventory bag, string definitionId)
        {
            foreach (PlacedItem item in bag.Items)
            {
                if (item.DefinitionId == definitionId)
                {
                    return item;
                }
            }

            return null;
        }

        private static PlacedItem FindRaw(BagInventory bag, int instanceId)
        {
            PlacedItem item;
            return bag.TryGet(instanceId, out item) ? item : null;
        }
    }
}
