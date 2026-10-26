using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    /// <summary>
    /// NPC 柜台的格子语法。
    ///
    /// 一张柜台（<see cref="NpcMarket.StockFor"/>）同时摆商品与契约卡：
    /// - **拖出**：商品 = 买下（扣 bit），契约卡 = 接单（链路上锁，卡进你的包/仓）；
    /// - **拖入**：只有契约卡有意义 = 交货（校验目标与上缴物），其余一律拒绝。
    ///
    /// 所有入口都返回一句可直接当台词/提示用的话，UI 不需要再拼字符串。
    /// </summary>
    public static class NpcBagFlow
    {
        public static void SyncAll(
            NpcMarket market,
            QuestLog log,
            CommsHub hub,
            CampaignState campaign,
            BagInventory playerBag,
            BagInventory stash,
            ItemCatalog catalog)
        {
            if (market == null || log == null || catalog == null)
            {
                return;
            }

            int today = QuestLog.Today(campaign);
            IReadOnlyList<NpcDefinition> all = NpcLibrary.All;
            for (int i = 0; i < all.Count; i++)
            {
                SyncNpc(all[i], market, log, hub, today, playerBag, stash, catalog);
            }
        }

        /// <summary>
        /// 把一位 NPC 的柜台收拾成与任务状态一致的样子：
        /// 可接的契约必须有一张在柜台上；进行中/待交付的契约必须在玩家背包或仓库里，
        /// 其余位置一律清掉。幂等，可以在每次刷新时调用。
        /// </summary>
        public static void SyncNpc(
            NpcDefinition npc,
            NpcMarket market,
            QuestLog log,
            CommsHub hub,
            int today,
            BagInventory playerBag,
            BagInventory stash,
            ItemCatalog catalog)
        {
            if (npc == null || market == null || log == null || catalog == null)
            {
                return;
            }

            BagInventory board = market.StockFor(npc.Id);
            List<QuestDefinition> quests = QuestLibrary.ForNpc(npc.Id);

            for (int i = 0; i < quests.Count; i++)
            {
                QuestDefinition quest = quests[i];
                string cardId = QuestItemLibrary.DefinitionIdOf(quest.Id);
                ItemDefinition card;
                if (!catalog.TryGet(cardId, out card))
                {
                    continue;
                }

                QuestState state = log.StateOf(quest.Id);
                bool offered = false;
                if (state == QuestState.Offered && hub != null)
                {
                    string reason;
                    offered = log.CanOffer(quest, hub, today, out reason);
                }

                if (offered)
                {
                    while (CountCards(board, cardId) > 1)
                    {
                        RemoveOne(board, cardId);
                    }

                    if (!HasCard(board, cardId))
                    {
                        board.TryAdd(card.Id, card.Shape, out _);
                    }

                    // 柜台上有了，玩家手上就不该有——否则会出现两张可交的契约。
                    while (HasCard(playerBag, cardId))
                    {
                        RemoveOne(playerBag, cardId);
                    }

                    while (HasCard(stash, cardId))
                    {
                        RemoveOne(stash, cardId);
                    }

                    continue;
                }

                while (HasCard(board, cardId))
                {
                    RemoveOne(board, cardId);
                }

                if (state != QuestState.Active && state != QuestState.Ready)
                {
                    // 已领取/已失败：契约作废，不该继续占格子。
                    while (HasCard(playerBag, cardId))
                    {
                        RemoveOne(playerBag, cardId);
                    }

                    while (HasCard(stash, cardId))
                    {
                        RemoveOne(stash, cardId);
                    }

                    continue;
                }

                // 进行中：恰好一张，丢了就补回来（例如旧档或异常丢卡）。
                int have = CountCards(playerBag, cardId) + CountCards(stash, cardId);
                while (have > 1)
                {
                    if (CountCards(playerBag, cardId) > 1)
                    {
                        RemoveOne(playerBag, cardId);
                    }
                    else if (CountCards(stash, cardId) > 1)
                    {
                        RemoveOne(stash, cardId);
                    }
                    else
                    {
                        break;
                    }

                    have--;
                }

                if (have == 0 && playerBag != null)
                {
                    if (!playerBag.TryAdd(card.Id, card.Shape, out _) && stash != null)
                    {
                        stash.TryAdd(card.Id, card.Shape, out _);
                    }
                }
            }
        }

        /// <summary>从柜台买一件商品到目标背包/仓库。</summary>
        public static bool TryBuy(
            NpcDefinition npc,
            NpcMarket market,
            int instanceId,
            CampaignState campaign,
            BagInventory target,
            ItemCatalog catalog,
            out string message)
        {
            message = null;
            if (npc == null || market == null || campaign == null || target == null || catalog == null)
            {
                message = "无法交易。";
                return false;
            }

            BagInventory board = market.StockFor(npc.Id);
            PlacedItem offer;
            if (!board.TryGet(instanceId, out offer))
            {
                message = "这件货已经不在了。";
                return false;
            }

            ItemDefinition definition;
            if (!catalog.TryGet(offer.DefinitionId, out definition))
            {
                message = "未知物品。";
                return false;
            }

            if (definition.Category == ItemCategory.Quest)
            {
                message = "契约要拿走才算接单。";
                return false;
            }

            int price = NpcMarket.PriceOf(definition);
            if (campaign.Bit < price)
            {
                message = "差 " + (price - campaign.Bit) + " bit。我这儿不赊账。";
                return false;
            }

            PlacedItem placed;
            if (!target.TryAdd(definition.Id, definition.Shape, out placed))
            {
                message = "放不下。先腾个格子。";
                return false;
            }

            if (!campaign.TrySpend(price))
            {
                target.Remove(placed.InstanceId);
                message = "bit 不足。";
                return false;
            }

            board.Remove(instanceId);
            message = "买下 " + definition.DisplayName + "，花 " + price + " bit。";
            return true;
        }

        /// <summary>
        /// 从柜台拿走契约 = 接单。链条顺序是「先放进目标背包 → 再上锁」，
        /// 放不下或接单校验失败时会把刚放进去的卡撤回，柜台上的那张不动。
        /// </summary>
        public static bool TryAccept(
            NpcDefinition npc,
            NpcMarket market,
            QuestLog log,
            CommsHub hub,
            int today,
            int cardInstanceId,
            BagInventory target,
            ItemCatalog catalog,
            out string message)
        {
            message = null;
            if (npc == null || market == null || log == null || target == null || catalog == null)
            {
                message = "无法接单。";
                return false;
            }

            BagInventory board = market.StockFor(npc.Id);
            PlacedItem card;
            if (!board.TryGet(cardInstanceId, out card))
            {
                message = "契约不在柜台上。";
                return false;
            }

            string questId;
            if (!QuestItemLibrary.TryQuestId(card.DefinitionId, out questId))
            {
                message = "这不是契约。";
                return false;
            }

            QuestDefinition quest;
            if (!QuestLibrary.TryGet(questId, out quest) || quest.NpcId != npc.Id)
            {
                message = "这张契约不是他的。";
                return false;
            }

            ItemDefinition cardDefinition;
            if (!catalog.TryGet(card.DefinitionId, out cardDefinition))
            {
                message = "未知契约。";
                return false;
            }

            PlacedItem placed;
            if (!target.TryAdd(cardDefinition.Id, cardDefinition.Shape, out placed))
            {
                message = "背包/仓库放不下这张契约。";
                return false;
            }

            string error;
            if (!log.TryAccept(quest, hub, today, out error))
            {
                target.Remove(placed.InstanceId);
                message = error;
                return false;
            }

            board.Remove(cardInstanceId);
            message = "接取「" + quest.Title + "」。契约占你 1 格，办完拿回来。";
            return true;
        }

        /// <summary>
        /// 把契约交回去 = 结算。目标必须已达成；上缴物从仓库优先、再从背包自动扣除，
        /// 奖励（bit + 物品）按现有规则发放，成功后契约销毁、链路上锁解除。
        /// </summary>
        public static bool TryTurnIn(
            NpcDefinition npc,
            NpcMarket market,
            QuestLog log,
            CommsHub hub,
            CampaignState campaign,
            int cardInstanceId,
            BagInventory source,
            BagInventory playerBag,
            BagInventory stash,
            ItemCatalog catalog,
            out string message)
        {
            message = null;
            if (npc == null || log == null || source == null || catalog == null)
            {
                message = "无法交付。";
                return false;
            }

            PlacedItem card;
            if (!source.TryGet(cardInstanceId, out card))
            {
                message = "契约不在这里。";
                return false;
            }

            string questId;
            if (!QuestItemLibrary.TryQuestId(card.DefinitionId, out questId))
            {
                message = "这不是契约。";
                return false;
            }

            QuestDefinition quest;
            if (!QuestLibrary.TryGet(questId, out quest) || quest.NpcId != npc.Id)
            {
                message = "这张契约不是他的。";
                return false;
            }

            // 交付前重算一次持有类目标，避免「刚把材料从仓库搬到背包」时状态还停在 Active。
            log.RefreshStatic(catalog, stash, playerBag, campaign);

            QuestState state = log.StateOf(quest.Id);
            if (state == QuestState.Active)
            {
                message = quest.IsHoldingObjective
                    ? "东西还没凑齐（需要 " + quest.TargetCount + " 个）。"
                    : "事还没办完。";
                return false;
            }

            if (state != QuestState.Ready)
            {
                message = "这张契约已经没用了。";
                return false;
            }

            string error;
            if (!log.TryClaim(quest, hub, campaign, stash, playerBag, catalog, out error))
            {
                message = error;
                return false;
            }

            source.Remove(cardInstanceId);
            message = "交付「" + quest.Title + "」。报酬已入仓。";
            return true;
        }

        public static int CountCards(BagInventory bag, string definitionId)
        {
            return bag == null || string.IsNullOrEmpty(definitionId) ? 0 : bag.CountOf(definitionId);
        }

        public static bool HasCard(BagInventory bag, string definitionId)
        {
            return CountCards(bag, definitionId) > 0;
        }

        private static bool RemoveOne(BagInventory bag, string definitionId)
        {
            if (bag == null || string.IsNullOrEmpty(definitionId))
            {
                return false;
            }

            IReadOnlyList<PlacedItem> items = bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].DefinitionId == definitionId)
                {
                    return bag.Remove(items[i].InstanceId);
                }
            }

            return false;
        }
    }
}
