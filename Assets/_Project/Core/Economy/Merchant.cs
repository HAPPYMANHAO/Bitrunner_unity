using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class MerchantOffer
    {
        public string ItemId;
        public int Price;
        public int Remaining;
        public int Count = 1;
        public int InstanceId;
        public bool IsBuyback;
    }

    public sealed class Merchant
    {
        public const int DefaultStockSize = 6;
        public const int DefaultStockWidth = 6;
        public const int DefaultStockHeight = 5;
        public const int BuyPriceMultiplier = 3;

        /// <summary>
        /// 收购价占物品价值的百分比。低于 100 是为了让「用」比「卖」划算——
        /// 否则捡到就变现永远优于带着装备冒险，装备的存在意义会被经济系统吃掉。
        /// </summary>
        public const int SellRatioPercent = 50;

        private readonly ItemCatalog _catalog;
        private readonly string[] _eligibleItemIds;
        private readonly List<MerchantOffer> _offers = new List<MerchantOffer>();

        public Merchant(ItemCatalog catalog)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            _catalog = catalog;
            Stock = new BagInventory(DefaultStockWidth, DefaultStockHeight);

            var ids = new List<string>();
            foreach (ItemDefinition definition in catalog.Definitions)
            {
                if (definition.Category == ItemCategory.Loot
                    || definition.Category == ItemCategory.Material
                    || definition.Category == ItemCategory.Key
                    || definition.Category == ItemCategory.Uplink
                    || definition.Category == ItemCategory.Currency
                    || definition.Category == ItemCategory.Quest
                    || definition.Category == ItemCategory.Container)
                {
                    continue;
                }

                if (definition.Value <= 0)
                {
                    continue;
                }

                ids.Add(definition.Id);
            }

            ids.Sort(StringComparer.Ordinal);
            _eligibleItemIds = ids.ToArray();
        }

        /// <summary>收购价。至少给 1 bit，保证再破的东西也卖得掉。</summary>
        public static int SaleValue(ItemDefinition definition, int count)
        {
            if (definition == null || definition.Value <= 0)
            {
                return 0;
            }

            int amount = definition.Value * (count > 0 ? count : 1) * SellRatioPercent / 100;
            return amount > 0 ? amount : 1;
        }

        public IReadOnlyList<MerchantOffer> Offers
        {
            get { return _offers; }
        }

        public IReadOnlyList<string> EligibleItemIds
        {
            get { return _eligibleItemIds; }
        }

        public BagInventory Stock { get; private set; }

        public int StockWeek { get; private set; }

        public int StockDay { get; private set; }

        public bool HasStockFor(int week, int day)
        {
            return StockWeek == week && StockDay == day;
        }

        public void RestockForDay(int week, int day, int seed, int stockSize = DefaultStockSize)
        {
            Restock(seed, stockSize, MaxTierForWeek(week));
            StockWeek = week;
            StockDay = day;
        }

        /// <summary>
        /// 商店库存的档位上限：W1 最高 T2，W2–W3 最高 T3，W4 最高 T4，W5 起解禁 T5。
        /// 和掉落门控是两道独立的门——钱再多，开局也买不到 T5。
        /// </summary>
        public static int MaxTierForWeek(int week)
        {
            if (week <= 1)
            {
                return 2;
            }

            if (week <= 3)
            {
                return 3;
            }

            return week <= 4 ? 4 : 5;
        }

        public void Restock(int seed, int stockSize = DefaultStockSize)
        {
            Restock(seed, stockSize, 5);
        }

        public void Restock(int seed, int stockSize, int maxTier)
        {
            _offers.Clear();
            ClearStock();

            if (_eligibleItemIds.Length == 0 || stockSize <= 0)
            {
                return;
            }

            var ids = new List<string>(_eligibleItemIds);
            if (maxTier < 5)
            {
                for (int i = ids.Count - 1; i >= 0; i--)
                {
                    ItemDefinition candidate;
                    if (_catalog.TryGet(ids[i], out candidate) && candidate.Level > maxTier)
                    {
                        ids.RemoveAt(i);
                    }
                }
            }

            var random = new Random(seed);

            for (int i = ids.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                string swap = ids[i];
                ids[i] = ids[j];
                ids[j] = swap;
            }

            int count = Math.Min(stockSize, ids.Count);
            for (int i = 0; i < count; i++)
            {
                ItemDefinition definition = _catalog.Get(ids[i]);

                PlacedItem placed;
                if (!Stock.TryAdd(definition.Id, definition.Shape, out placed))
                {
                    continue;
                }

                _offers.Add(new MerchantOffer
                {
                    ItemId = ids[i],
                    Price = definition.Value * BuyPriceMultiplier,
                    Remaining = 1,
                    Count = 1,
                    InstanceId = placed.InstanceId,
                    IsBuyback = false
                });
            }
        }

        public int FindOfferIndexByInstance(int instanceId)
        {
            for (int i = 0; i < _offers.Count; i++)
            {
                if (_offers[i].InstanceId == instanceId)
                {
                    return i;
                }
            }

            return -1;
        }

        public bool TryBuyAt(
            int offerIndex,
            CampaignState campaign,
            BagInventory target,
            Cell origin,
            int rotation,
            out PlacedItem placed,
            out string error)
        {
            if (campaign == null)
            {
                throw new ArgumentNullException(nameof(campaign));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            placed = null;

            if (offerIndex < 0 || offerIndex >= _offers.Count)
            {
                error = "未知商品。";
                return false;
            }

            MerchantOffer offer = _offers[offerIndex];
            if (offer.Remaining <= 0)
            {
                error = "已售罄。";
                return false;
            }

            if (campaign.Bit < offer.Price)
            {
                error = "bit 不足。";
                return false;
            }

            ItemDefinition definition = _catalog.Get(offer.ItemId);
            if (!target.TryStackAt(definition.Id, definition.Shape, origin, rotation, offer.Count, definition.MaxStack, out placed, out error))
            {
                error = "仓库空间不足。";
                return false;
            }

            if (!campaign.TrySpend(offer.Price))
            {
                target.Remove(placed.InstanceId);
                placed = null;
                error = "bit 不足。";
                return false;
            }

            CompleteSale(offer);
            error = null;
            return true;
        }

        public bool TryBuy(int offerIndex, CampaignState campaign, BagInventory stash, out PlacedItem placed, out string error)
        {
            if (campaign == null)
            {
                throw new ArgumentNullException(nameof(campaign));
            }

            if (stash == null)
            {
                throw new ArgumentNullException(nameof(stash));
            }

            placed = null;

            if (offerIndex < 0 || offerIndex >= _offers.Count)
            {
                error = "未知商品。";
                return false;
            }

            MerchantOffer offer = _offers[offerIndex];
            if (offer.Remaining <= 0)
            {
                error = "已售罄。";
                return false;
            }

            if (campaign.Bit < offer.Price)
            {
                error = "bit 不足。";
                return false;
            }

            ItemDefinition definition = _catalog.Get(offer.ItemId);
            if (!stash.TryAdd(definition.Id, definition.Shape, true, offer.Count, out placed))
            {
                error = "仓库空间不足。";
                return false;
            }

            if (!campaign.TrySpend(offer.Price))
            {
                stash.Remove(placed.InstanceId);
                placed = null;
                error = "bit 不足。";
                return false;
            }

            CompleteSale(offer);
            error = null;
            return true;
        }

        private void CompleteSale(MerchantOffer offer)
        {
            offer.Remaining--;
            if (offer.InstanceId != 0)
            {
                Stock.Remove(offer.InstanceId);
                offer.InstanceId = 0;
            }
        }

        private void ClearStock()
        {
            var items = new List<PlacedItem>(Stock.Items);
            for (int i = 0; i < items.Count; i++)
            {
                Stock.Remove(items[i].InstanceId);
            }
        }

        /// <summary>
        /// 读档恢复：直接换上还原好的货架与报价表。与 <see cref="RestockForDay"/> 的区别是
        /// 这里不重掷货品——存档要的是玩家离开时的那一格货架，包括已经卖出的空位与购回条目。
        /// </summary>
        internal void RestoreStock(int week, int day, BagInventory stock, IEnumerable<MerchantOffer> offers)
        {
            if (stock != null)
            {
                Stock = stock;
            }

            StockWeek = week;
            StockDay = day;

            _offers.Clear();
            if (offers == null)
            {
                return;
            }

            foreach (MerchantOffer offer in offers)
            {
                if (offer != null)
                {
                    _offers.Add(offer);
                }
            }
        }

        public bool TrySellToStock(
            BagInventory source,
            int instanceId,
            CampaignState campaign,
            Cell origin,
            int rotation,
            out int value,
            out string error)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (campaign == null)
            {
                throw new ArgumentNullException(nameof(campaign));
            }

            value = 0;

            PlacedItem item;
            if (!source.TryGet(instanceId, out item))
            {
                error = "未知物品。";
                return false;
            }

            ItemDefinition definition;
            if (!_catalog.TryGet(item.DefinitionId, out definition))
            {
                error = "未知物品定义。";
                return false;
            }

            if (definition.Category == ItemCategory.Key
                || definition.Category == ItemCategory.Currency
                || definition.Category == ItemCategory.Quest)
            {
                if (definition.Category == ItemCategory.Key)
                {
                    error = "潜网密钥不能出售。";
                }
                else if (definition.Category == ItemCategory.Quest)
                {
                    error = "任务契约不能出售。";
                }
                else
                {
                    error = "比特不能作为商品出售。";
                }

                return false;
            }

            if (ContainerOps.HasContents(item))
            {
                error = "请先清空容器再出售。";
                return false;
            }

            PlacedItem placed;
            if (!Stock.TryStackAt(definition.Id, definition.Shape, origin, rotation, item.Count, definition.MaxStack, out placed, out error))
            {
                error = "货架空间不足，无法出售。";
                return false;
            }

            if (!source.Remove(instanceId))
            {
                Stock.Remove(placed.InstanceId);
                error = "未知物品。";
                return false;
            }

            value = SaleValue(definition, item.Count);
            campaign.AddBit(value);
            _offers.Add(new MerchantOffer
            {
                ItemId = definition.Id,
                Price = value,
                Remaining = 1,
                Count = item.Count,
                InstanceId = placed.InstanceId,
                IsBuyback = true
            });
            error = null;
            return true;
        }

        public bool TrySell(BagInventory source, int instanceId, CampaignState campaign, out int value, out string error)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (campaign == null)
            {
                throw new ArgumentNullException(nameof(campaign));
            }

            value = 0;

            PlacedItem item;
            if (!source.TryGet(instanceId, out item))
            {
                error = "未知物品。";
                return false;
            }

            ItemDefinition definition;
            if (!_catalog.TryGet(item.DefinitionId, out definition))
            {
                error = "未知物品定义。";
                return false;
            }

            if (definition.Category == ItemCategory.Key
                || definition.Category == ItemCategory.Currency
                || definition.Category == ItemCategory.Quest)
            {
                if (definition.Category == ItemCategory.Key)
                {
                    error = "潜网密钥不能出售。";
                }
                else if (definition.Category == ItemCategory.Quest)
                {
                    error = "任务契约不能出售。";
                }
                else
                {
                    error = "比特不能作为商品出售。";
                }

                return false;
            }

            if (ContainerOps.HasContents(item))
            {
                error = "请先清空容器再出售。";
                return false;
            }

            int count = item.Count;
            source.Remove(instanceId);
            value = SaleValue(definition, count);
            campaign.AddBit(value);
            error = null;
            return true;
        }
    }
}
