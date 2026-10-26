using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    /// <summary>
    /// NPC 专属货架。
    ///
    /// 和 <see cref="Merchant"/> 的三点差别，全是刻意的：
    /// 1. **不吃 <see cref="Merchant.MaxTierForWeek"/>**——这是「更好的购买选项」唯一的实质含义。
    ///    W1 就能从 K-7 买到 T4，只要你弄到链路并付得起。
    /// 2. 价格是 <see cref="PriceMultiplier"/> 倍（比商人贵 33%），作为绕过时间门的代价。
    /// 3. **不收购**。否则玩家会绕开主商人的 50% 折价，把经济设计打穿。
    /// </summary>
    public sealed class NpcMarket
    {
        public const int StockWidth = 6;
        public const int StockHeight = 4;
        public const int StockSize = 3;
        public const int RefreshDays = 2;
        public const int PriceMultiplier = 4;

        private readonly ItemCatalog _catalog;
        private readonly Dictionary<string, BagInventory> _stocks = new Dictionary<string, BagInventory>();
        private readonly Dictionary<string, int> _lastRefreshDay = new Dictionary<string, int>();

        public NpcMarket(ItemCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            _catalog = catalog;
        }

        public static int PriceOf(ItemDefinition definition)
        {
            return definition == null ? 0 : definition.Value * PriceMultiplier;
        }

        public BagInventory StockFor(string npcId)
        {
            BagInventory stock;
            if (!_stocks.TryGetValue(npcId, out stock))
            {
                stock = new BagInventory(StockWidth, StockHeight);
                _stocks.Add(npcId, stock);
            }

            return stock;
        }

        internal bool TryGetStock(string npcId, out BagInventory stock)
        {
            return _stocks.TryGetValue(npcId, out stock);
        }

        internal int LastRefreshDay(string npcId)
        {
            int day;
            return _lastRefreshDay.TryGetValue(npcId, out day) ? day : 0;
        }

        /// <summary>读档恢复一位联系人的货架与刷新日，不重掷货品。</summary>
        internal void RestoreEntry(string npcId, BagInventory stock, int lastRefreshDay)
        {
            if (string.IsNullOrEmpty(npcId))
            {
                return;
            }

            if (stock != null)
            {
                _stocks[npcId] = stock;
            }

            _lastRefreshDay[npcId] = lastRefreshDay > 0 ? lastRefreshDay : 0;
        }

        /// <summary>到期才补货（每 <see cref="RefreshDays"/> 天）。</summary>
        public void EnsureAll(int today, bool force = false)
        {
            IReadOnlyList<NpcDefinition> all = NpcLibrary.All;
            for (int i = 0; i < all.Count; i++)
            {
                EnsureStock(all[i], i, today, force);
            }
        }

        public void EnsureStock(NpcDefinition npc, int npcIndex, int today, bool force = false)
        {
            if (npc == null)
            {
                return;
            }

            int last;
            if (!force
                && _lastRefreshDay.TryGetValue(npc.Id, out last)
                && today - last < npc.RefreshDays)
            {
                return;
            }

            BagInventory stock = StockFor(npc.Id);
            Clear(stock);

            if (npc.StockPool == null || npc.StockPool.Length == 0)
            {
                _lastRefreshDay[npc.Id] = today;
                return;
            }

            var random = new Random(today * 7919 + npcIndex * 104729 + 17);
            var picked = new List<string>();
            int guard = 0;
            while (picked.Count < npc.StockSize && guard < 200)
            {
                guard++;
                string candidate = npc.StockPool[random.Next(npc.StockPool.Length)];
                if (picked.Contains(candidate))
                {
                    continue;
                }

                ItemDefinition definition;
                if (!_catalog.TryGet(candidate, out definition))
                {
                    continue;
                }

                PlacedItem placed;
                if (stock.TryAdd(definition.Id, definition.Shape, out placed))
                {
                    picked.Add(candidate);
                }
            }

            _lastRefreshDay[npc.Id] = today;
        }

        /// <summary>点击购买（不走拖拽）：扣 bit → 进仓库。仓库放不下就整笔取消。</summary>
        public bool TryBuy(
            NpcDefinition npc,
            int instanceId,
            CampaignState campaign,
            BagInventory stash,
            out string error)
        {
            if (npc == null)
            {
                error = "联系人无效。";
                return false;
            }

            if (campaign == null || stash == null)
            {
                error = "无法购买。";
                return false;
            }

            BagInventory stock = StockFor(npc.Id);
            PlacedItem offer;
            if (!stock.TryGet(instanceId, out offer))
            {
                error = "这件货已经不在了。";
                return false;
            }

            ItemDefinition definition;
            if (!_catalog.TryGet(offer.DefinitionId, out definition))
            {
                error = "未知物品。";
                return false;
            }

            if (definition.Category == ItemCategory.Quest)
            {
                error = "契约要拿走才算接单。";
                return false;
            }

            int price = PriceOf(definition);
            if (campaign.Bit < price)
            {
                error = "bit 不足，需要 " + price + "。";
                return false;
            }

            PlacedItem placed;
            if (!stash.TryAdd(definition.Id, definition.Shape, out placed))
            {
                error = "仓库没有空间。";
                return false;
            }

            if (!campaign.TrySpend(price))
            {
                stash.Remove(placed.InstanceId);
                error = "bit 不足。";
                return false;
            }

            stock.Remove(instanceId);
            error = null;
            return true;
        }

        private static void Clear(BagInventory stock)
        {
            IReadOnlyList<PlacedItem> items = stock.Items;
            var ids = new List<int>(items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                ids.Add(items[i].InstanceId);
            }

            for (int i = 0; i < ids.Count; i++)
            {
                stock.Remove(ids[i]);
            }
        }
    }
}
