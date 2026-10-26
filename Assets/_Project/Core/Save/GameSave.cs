using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    /// <summary>
    /// 一次完整基地状态的只读容器。读档时由 <see cref="GameSave.TryRestore"/> 产出，
    /// 存档时由 <see cref="GameSave.Capture"/> 消费。<see cref="Warnings"/> 记录被丢弃的
    /// 陈旧引用（已删除的任务、失效的通讯锁等），由调用方决定要不要展示给玩家。
    /// </summary>
    public sealed class GameSnapshot
    {
        public CampaignState Campaign;
        public Merchant Merchant;
        public DivePod Pod;
        public BitMine Mine;
        public CraftingStation Craft;
        public CpuCore Cpu;
        public InsuranceHub Insurance;
        public StorageRack Rack;
        public CommsHub Comms;
        public QuestLog Quests;
        public NpcMarket NpcStocks;
        public BagInventory Stash;
        public BagInventory PlayerBag;
        public BagInventory Secure;

        public readonly List<string> Warnings = new List<string>();
    }

    /// <summary>
    /// 状态与存档数据之间的搬运层。
    ///
    /// 三条硬规则：
    /// 1. **读档要么完全成功、要么完全失败**。任何一件物品解析失败都会让整次读档中止，
    ///    绝不返回一个少了东西的基地——那比读档失败更难排查。
    /// 2. **未知内容不阻断读档**。已删除的任务、失效的通讯锁、不存在的报价只会被丢弃并
    ///    记进 <see cref="GameSnapshot.Warnings"/>：玩家不该因为一次内容更新而丢档。
    /// 3. **容器摊平存储**。Unity 的序列化器有 10 层深度上限，自引用的
    ///    <c>ItemSave[]</c> 嵌套会在序列化时直接报错，所以容器统一放进
    ///    <see cref="SaveData.Containers"/>，由 <see cref="ItemSave.ContainerId"/> 引用。
    /// </summary>
    public static class GameSave
    {
        public const int CurrentVersion = 1;

        private const int MaxGridDimension = 64;
        private const int MaxContainerDepth = 4;

        /// <summary>捕获容器时的游标：容器内容物要摊平到同一张表里，所以需要跨物品共享。</summary>
        private sealed class CaptureContext
        {
            public readonly List<ContainerSave> Containers = new List<ContainerSave>();
            public int NextId = 1;
        }

        private sealed class RestoreContext
        {
            public ItemCatalog Catalog;
            public Dictionary<int, ContainerSave> Containers;
            public HashSet<int> UsedContainers;
            public List<string> Warnings;
        }

        public static SaveData Capture(GameSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var context = new CaptureContext();
            var data = new SaveData
            {
                Version = CurrentVersion,
                Campaign = CaptureCampaign(snapshot.Campaign),
                Mine = CaptureMine(snapshot.Mine),
                Pod = CapturePod(snapshot.Pod, context),
                Craft = CaptureLevelBag(
                    snapshot.Craft != null ? snapshot.Craft.Level : 0,
                    snapshot.Craft != null ? snapshot.Craft.Grid : null,
                    context),
                Cpu = CaptureLevelBag(
                    snapshot.Cpu != null ? snapshot.Cpu.Level : 0,
                    snapshot.Cpu != null ? snapshot.Cpu.Grid : null,
                    context),
                Insurance = CaptureLevel(snapshot.Insurance != null ? snapshot.Insurance.Level : 0),
                Rack = CaptureLevel(snapshot.Rack != null ? snapshot.Rack.Level : 0),
                Comms = CaptureComms(snapshot.Comms, context),
                Merchant = CaptureMerchant(snapshot.Merchant, context),
                NpcMarket = CaptureNpcMarket(snapshot.NpcStocks, context),
                Quests = CaptureQuests(snapshot.Quests),
                Stash = CaptureBag(snapshot.Stash, context),
                PlayerBag = CaptureBag(snapshot.PlayerBag, context),
                Secure = CaptureBag(snapshot.Secure, context)
            };

            data.Containers = context.Containers.ToArray();
            return data;
        }

        public static bool TryRestore(
            SaveData data,
            ItemCatalog catalog,
            out GameSnapshot snapshot,
            out string error)
        {
            snapshot = null;
            error = null;

            if (data == null)
            {
                error = "存档内容为空。";
                return false;
            }

            if (data.Version != CurrentVersion)
            {
                error = "存档版本 " + data.Version + " 与当前版本 " + CurrentVersion + " 不兼容。";
                return false;
            }

            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            if (data.Campaign == null
                || data.Mine == null
                || data.Pod == null
                || data.Craft == null
                || data.Cpu == null
                || data.Insurance == null
                || data.Rack == null
                || data.Comms == null
                || data.Merchant == null
                || data.Stash == null
                || data.PlayerBag == null
                || data.Secure == null)
            {
                error = "存档缺少必要区块。";
                return false;
            }

            var warnings = new List<string>();
            var context = new RestoreContext
            {
                Catalog = catalog,
                Warnings = warnings
            };

            if (!BuildContainerMap(data.Containers, context, out error))
            {
                return false;
            }

            int insuranceLevel = ClampLevel(data.Insurance.Level, InsuranceHub.MaxLevel, "保险中枢", warnings);
            int rackLevel = ClampLevel(data.Rack.Level, StorageRack.MaxLevel, "仓储设备", warnings);
            int podLevel = ClampLevel(data.Pod.Level, DivePod.MaxLevel, "潜网舱", warnings);
            int craftLevel = ClampLevel(data.Craft.Level, CraftingStation.MaxLevel, "制作站", warnings);
            int cpuLevel = ClampLevel(data.Cpu.Level, CpuCore.MaxLevel, "中央处理器", warnings);
            int commsLevel = ClampLevel(data.Comms.Level, CommsHub.MaxLevel, "通讯中枢", warnings);
            int mineLevel = ClampLevel(data.Mine.Level, BitMine.MaxLevel, "比特矿场", warnings);

            // 仓库与保险箱先建：比特钱包直接读仓库里的 bit 物品，战役时钟必须排在它们之后。
            BagInventory stash;
            if (!TryRestoreBag(
                    data.Stash,
                    context,
                    StorageRack.Width,
                    StorageRack.HeightForLevel(rackLevel),
                    "仓库",
                    out stash,
                    out error))
            {
                return false;
            }

            BagInventory playerBag;
            if (!TryRestoreBag(data.PlayerBag, context, 6, 5, "背包", out playerBag, out error))
            {
                return false;
            }

            BagInventory secure;
            if (!TryRestoreBag(
                    data.Secure,
                    context,
                    InsuranceHub.WidthForLevel(insuranceLevel),
                    InsuranceHub.HeightForLevel(insuranceLevel),
                    "保险箱",
                    out secure,
                    out error))
            {
                return false;
            }

            var wallet = new ItemBitWallet(stash, catalog);

            CampaignState campaign;
            if (!TryRestoreCampaign(data.Campaign, wallet, out campaign, out error))
            {
                return false;
            }

            var pod = new DivePod();
            pod.RestoreLevel(podLevel, data.Pod.SelectedKeyItemId);
            BagInventory podBag;
            if (!TryRestoreBag(
                    data.Pod.Bag,
                    context,
                    DivePod.WidthForLevel(podLevel),
                    DivePod.HeightForLevel(podLevel),
                    "潜网舱",
                    out podBag,
                    out error))
            {
                return false;
            }

            pod.RestoreGrid(podBag);

            var craft = new CraftingStation();
            craft.RestoreLevel(craftLevel);
            BagInventory craftBag;
            if (!TryRestoreBag(
                    data.Craft.Bag,
                    context,
                    CraftingStation.WidthForLevel(craftLevel),
                    CraftingStation.HeightForLevel(craftLevel),
                    "制作站",
                    out craftBag,
                    out error))
            {
                return false;
            }

            craft.RestoreGrid(craftBag);

            var cpu = new CpuCore();
            cpu.RestoreLevel(cpuLevel);
            BagInventory cpuBag;
            if (!TryRestoreBag(
                    data.Cpu.Bag,
                    context,
                    CpuCore.WidthForLevel(cpuLevel),
                    CpuCore.HeightForLevel(cpuLevel),
                    "中央处理器",
                    out cpuBag,
                    out error))
            {
                return false;
            }

            cpu.RestoreGrid(cpuBag);

            var comms = new CommsHub();
            comms.RestoreLevel(commsLevel);
            BagInventory commsBag;
            if (!TryRestoreBag(
                    data.Comms.Bag,
                    context,
                    CommsHub.WidthForLevel(commsLevel),
                    CommsHub.HeightForLevel(commsLevel),
                    "通讯中枢",
                    out commsBag,
                    out error))
            {
                return false;
            }

            comms.RestoreGrid(commsBag);
            comms.RestoreLocks(ReadCommsLocks(data.Comms, commsBag, warnings));

            var merchant = new Merchant(catalog);
            BagInventory merchantStock;
            if (!TryRestoreBag(
                    data.Merchant.Stock,
                    context,
                    Merchant.DefaultStockWidth,
                    Merchant.DefaultStockHeight,
                    "商人物架",
                    out merchantStock,
                    out error))
            {
                return false;
            }

            if (data.Merchant.Week < 0 || data.Merchant.Day < 0)
            {
                error = "商人补货日期非法。";
                return false;
            }

            merchant.RestoreStock(
                data.Merchant.Week,
                data.Merchant.Day,
                merchantStock,
                ReadOffers(data.Merchant, context, merchantStock));

            var npcStocks = new NpcMarket(catalog);
            if (!RestoreNpcStocks(data.NpcMarket, context, npcStocks, out error))
            {
                return false;
            }

            QuestLog quests;
            if (!RestoreQuests(data.Quests, out quests, warnings))
            {
                error = "任务数据非法。";
                return false;
            }

            var mine = new BitMine();
            string mineError;
            if (!mine.TryRestore(mineLevel, data.Mine.Filled, data.Mine.FilledWeek, data.Mine.FilledDay, out mineError))
            {
                error = "矿场数据非法：" + mineError;
                return false;
            }

            var insurance = new InsuranceHub();
            insurance.RestoreLevel(insuranceLevel);

            var rack = new StorageRack();
            rack.RestoreLevel(rackLevel);

            if (context.Containers != null)
            {
                foreach (KeyValuePair<int, ContainerSave> pair in context.Containers)
                {
                    if (!context.UsedContainers.Contains(pair.Key))
                    {
                        warnings.Add("丢弃未被引用的容器 #" + pair.Key + "。");
                    }
                }
            }

            snapshot = new GameSnapshot
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
            snapshot.Warnings.AddRange(warnings);
            return true;
        }

        private static CampaignSave CaptureCampaign(CampaignState campaign)
        {
            if (campaign == null)
            {
                return null;
            }

            return new CampaignSave
            {
                Week = campaign.Week,
                Day = campaign.Day,
                TotalPaid = campaign.TotalPaid,
                Status = (int)campaign.Status,
                Ending = (int)campaign.Ending
            };
        }

        private static MineSave CaptureMine(BitMine mine)
        {
            if (mine == null)
            {
                return null;
            }

            var filled = new bool[mine.Width * mine.Height];
            for (int y = 0; y < mine.Height; y++)
            {
                for (int x = 0; x < mine.Width; x++)
                {
                    filled[y * mine.Width + x] = mine.IsFilled(x, y);
                }
            }

            return new MineSave
            {
                Level = mine.Level,
                Filled = filled,
                FilledWeek = mine.FilledWeek,
                FilledDay = mine.FilledDay
            };
        }

        private static PodSave CapturePod(DivePod pod, CaptureContext context)
        {
            if (pod == null)
            {
                return null;
            }

            return new PodSave
            {
                Level = pod.Level,
                SelectedKeyItemId = pod.SelectedKeyItemId,
                Bag = CaptureBag(pod.Grid, context)
            };
        }

        private static LevelSave CaptureLevel(int level)
        {
            return new LevelSave { Level = level };
        }

        private static LevelBagSave CaptureLevelBag(int level, BagInventory bag, CaptureContext context)
        {
            return new LevelBagSave { Level = level, Bag = CaptureBag(bag, context) };
        }

        private static CommsSave CaptureComms(CommsHub hub, CaptureContext context)
        {
            if (hub == null)
            {
                return null;
            }

            var instanceIds = new List<int>();
            var questIds = new List<string>();
            if (hub.Locks != null)
            {
                foreach (KeyValuePair<int, string> pair in hub.Locks)
                {
                    instanceIds.Add(pair.Key);
                    questIds.Add(pair.Value);
                }
            }

            return new CommsSave
            {
                Level = hub.Level,
                Bag = CaptureBag(hub.Grid, context),
                LockedInstanceIds = instanceIds.ToArray(),
                LockedQuestIds = questIds.ToArray()
            };
        }

        private static MerchantSave CaptureMerchant(Merchant merchant, CaptureContext context)
        {
            if (merchant == null)
            {
                return null;
            }

            IReadOnlyList<MerchantOffer> offers = merchant.Offers;
            var saves = new OfferSave[offers.Count];
            for (int i = 0; i < offers.Count; i++)
            {
                MerchantOffer offer = offers[i];
                saves[i] = new OfferSave
                {
                    ItemId = offer.ItemId,
                    Price = offer.Price,
                    Remaining = offer.Remaining,
                    Count = offer.Count,
                    InstanceId = offer.InstanceId,
                    IsBuyback = offer.IsBuyback
                };
            }

            return new MerchantSave
            {
                Week = merchant.StockWeek,
                Day = merchant.StockDay,
                Stock = CaptureBag(merchant.Stock, context),
                Offers = saves
            };
        }

        private static NpcMarketSave CaptureNpcMarket(NpcMarket market, CaptureContext context)
        {
            if (market == null)
            {
                return null;
            }

            IReadOnlyList<NpcDefinition> all = NpcLibrary.All;
            var entries = new List<NpcStockSave>();
            for (int i = 0; i < all.Count; i++)
            {
                BagInventory stock;
                if (!market.TryGetStock(all[i].Id, out stock))
                {
                    continue;
                }

                entries.Add(new NpcStockSave
                {
                    NpcId = all[i].Id,
                    LastRefreshDay = market.LastRefreshDay(all[i].Id),
                    Stock = CaptureBag(stock, context)
                });
            }

            return new NpcMarketSave { Entries = entries.ToArray() };
        }

        private static QuestSave CaptureQuests(QuestLog log)
        {
            if (log == null)
            {
                return null;
            }

            var entries = new List<QuestEntrySave>();
            foreach (KeyValuePair<string, QuestProgress> pair in log.ProgressMap)
            {
                QuestProgress progress = pair.Value;
                entries.Add(new QuestEntrySave
                {
                    QuestId = progress.QuestId,
                    State = (int)progress.State,
                    Count = progress.Count,
                    AcceptedDay = progress.AcceptedDay,
                    DeadlineDay = progress.DeadlineDay
                });
            }

            var cooldowns = new List<QuestCooldownSave>();
            foreach (KeyValuePair<string, int> pair in log.CooldownMap)
            {
                cooldowns.Add(new QuestCooldownSave { NpcId = pair.Key, UntilDay = pair.Value });
            }

            var order = new List<string>(log.AcceptedOrder);

            return new QuestSave
            {
                AcceptedOrder = order.ToArray(),
                Entries = entries.ToArray(),
                Cooldowns = cooldowns.ToArray()
            };
        }

        private static BagSave CaptureBag(BagInventory bag, CaptureContext context)
        {
            if (bag == null)
            {
                return null;
            }

            IReadOnlyList<PlacedItem> items = bag.Items;
            var saves = new ItemSave[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                saves[i] = CaptureItem(items[i], 0, context);
            }

            return new BagSave
            {
                Width = bag.Grid.Width,
                Height = bag.Grid.Height,
                Items = saves
            };
        }

        private static ItemSave CaptureItem(PlacedItem item, int depth, CaptureContext context)
        {
            var save = new ItemSave
            {
                InstanceId = item.InstanceId,
                DefinitionId = item.DefinitionId,
                X = item.Origin.X,
                Y = item.Origin.Y,
                Rotation = item.Rotation,
                Rotatable = item.Rotatable,
                Count = item.Count,
                Durability = item.Durability,
                ExpeditionBound = item.ExpeditionBound
            };

            if (item.Container == null || item.Container.Bag.Count == 0 || depth >= MaxContainerDepth)
            {
                return save;
            }

            IReadOnlyList<PlacedItem> inner = item.Container.Bag.Items;
            var containerSave = new ContainerSave
            {
                Id = context.NextId,
                Width = item.Container.Bag.Grid.Width,
                Height = item.Container.Bag.Grid.Height,
                Items = new ItemSave[inner.Count]
            };
            context.NextId++;
            context.Containers.Add(containerSave);

            for (int i = 0; i < inner.Count; i++)
            {
                containerSave.Items[i] = CaptureItem(inner[i], depth + 1, context);
            }

            save.ContainerId = containerSave.Id;
            return save;
        }

        private static bool BuildContainerMap(
            ContainerSave[] saves,
            RestoreContext context,
            out string error)
        {
            error = null;
            context.Containers = new Dictionary<int, ContainerSave>();
            context.UsedContainers = new HashSet<int>();

            if (saves == null)
            {
                return true;
            }

            for (int i = 0; i < saves.Length; i++)
            {
                ContainerSave save = saves[i];
                if (save == null)
                {
                    continue;
                }

                if (save.Id <= 0)
                {
                    error = "容器 ID 非法（" + save.Id + "）。";
                    return false;
                }

                if (context.Containers.ContainsKey(save.Id))
                {
                    error = "容器 ID 重复（" + save.Id + "）。";
                    return false;
                }

                if (save.Width < 1 || save.Width > MaxGridDimension || save.Height < 1 || save.Height > MaxGridDimension)
                {
                    error = "容器 #" + save.Id + " 的尺寸非法（" + save.Width + "×" + save.Height + "）。";
                    return false;
                }

                context.Containers.Add(save.Id, save);
            }

            return true;
        }

        private static bool TryRestoreBag(
            BagSave save,
            RestoreContext context,
            int expectedWidth,
            int expectedHeight,
            string label,
            out BagInventory bag,
            out string error)
        {
            bag = null;
            error = null;

            if (save == null)
            {
                error = label + " 数据缺失。";
                return false;
            }

            if (save.Width < 1 || save.Width > MaxGridDimension || save.Height < 1 || save.Height > MaxGridDimension)
            {
                error = label + " 的网格尺寸非法（" + save.Width + "×" + save.Height + "）。";
                return false;
            }

            if (save.Width != expectedWidth || save.Height != expectedHeight)
            {
                error = label + " 的网格尺寸 " + save.Width + "×" + save.Height
                        + " 与当前设施等级对应的 " + expectedWidth + "×" + expectedHeight + " 不符。";
                return false;
            }

            var result = new BagInventory(save.Width, save.Height);
            if (save.Items != null)
            {
                for (int i = 0; i < save.Items.Length; i++)
                {
                    if (!TryRestoreItem(result, save.Items[i], context, 1, label, out error))
                    {
                        return false;
                    }
                }
            }

            bag = result;
            return true;
        }

        private static bool TryRestoreItem(
            BagInventory bag,
            ItemSave save,
            RestoreContext context,
            int depth,
            string label,
            out string error)
        {
            error = null;

            if (save == null)
            {
                error = label + " 存在空物品条目。";
                return false;
            }

            ItemDefinition definition;
            if (string.IsNullOrEmpty(save.DefinitionId) || !context.Catalog.TryGet(save.DefinitionId, out definition))
            {
                error = label + " 引用了未知物品「" + save.DefinitionId + "」。";
                return false;
            }

            PlacedItem item;
            if (!bag.TryRestoreItem(
                    save.InstanceId,
                    definition.Id,
                    definition.Shape,
                    save.Rotation,
                    save.Rotatable,
                    new Cell(save.X, save.Y),
                    save.Count,
                    ClampDurability(save.Durability, definition),
                    save.ExpeditionBound,
                    out item,
                    out error))
            {
                error = label + "：" + error;
                return false;
            }

            if (save.ContainerId == 0)
            {
                return true;
            }

            if (depth > MaxContainerDepth)
            {
                error = label + " 的容器嵌套超过 " + MaxContainerDepth + " 层。";
                return false;
            }

            ContainerSave containerSave;
            if (!context.Containers.TryGetValue(save.ContainerId, out containerSave))
            {
                error = label + " 引用了不存在的容器 #" + save.ContainerId + "。";
                return false;
            }

            if (!context.UsedContainers.Add(save.ContainerId))
            {
                error = "容器 #" + save.ContainerId + " 被多个物品引用。";
                return false;
            }

            if (definition.ContainerSpec == null)
            {
                error = label + " 中的「" + definition.DisplayName + "」不支持容器，但存档里带着内容物。";
                return false;
            }

            if (containerSave.Width != definition.ContainerSpec.Width
                || containerSave.Height != definition.ContainerSpec.Height)
            {
                error = label + " 中「" + definition.DisplayName + "」的容器尺寸 "
                        + containerSave.Width + "×" + containerSave.Height + " 与定义 "
                        + definition.ContainerSpec.Width + "×" + definition.ContainerSpec.Height + " 不符。";
                return false;
            }

            ItemContainer container = ContainerOps.Ensure(item, definition);
            if (container == null)
            {
                error = label + " 中「" + definition.DisplayName + "」的容器无法创建。";
                return false;
            }

            if (containerSave.Items != null)
            {
                for (int i = 0; i < containerSave.Items.Length; i++)
                {
                    if (!TryRestoreItem(
                            container.Bag,
                            containerSave.Items[i],
                            context,
                            depth + 1,
                            label + " 的容器",
                            out error))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool TryRestoreCampaign(
            CampaignSave save,
            IBitWallet wallet,
            out CampaignState campaign,
            out string error)
        {
            campaign = null;
            error = null;

            if (save.Week < 1)
            {
                error = "存档周次非法（" + save.Week + "）。";
                return false;
            }

            if (save.Day < 1 || save.Day > CampaignState.DaysPerWeek)
            {
                error = "存档天数非法（" + save.Day + "）。";
                return false;
            }

            if (save.TotalPaid < 0)
            {
                error = "存档累计缴款非法（" + save.TotalPaid + "）。";
                return false;
            }

            if (save.Status < (int)CampaignStatus.Active || save.Status > (int)CampaignStatus.Defeat)
            {
                error = "存档战役状态非法（" + save.Status + "）。";
                return false;
            }

            if (save.Ending < (int)CampaignEnding.None || save.Ending > (int)CampaignEnding.Bankrupt)
            {
                error = "存档结局标记非法（" + save.Ending + "）。";
                return false;
            }

            campaign = CampaignState.Restore(
                CampaignState.CreateDefaultInstallments(),
                wallet,
                save.Week,
                save.Day,
                save.TotalPaid,
                (CampaignStatus)save.Status,
                (CampaignEnding)save.Ending);
            return true;
        }

        private static bool RestoreNpcStocks(
            NpcMarketSave save,
            RestoreContext context,
            NpcMarket market,
            out string error)
        {
            error = null;
            if (save == null || save.Entries == null)
            {
                return true;
            }

            for (int i = 0; i < save.Entries.Length; i++)
            {
                NpcStockSave entry = save.Entries[i];
                if (entry == null)
                {
                    continue;
                }

                NpcDefinition npc;
                if (string.IsNullOrEmpty(entry.NpcId) || !NpcLibrary.TryGet(entry.NpcId, out npc))
                {
                    context.Warnings.Add("丢弃已移除的联系人货架「" + entry.NpcId + "」。");
                    continue;
                }

                BagInventory stock;
                if (!TryRestoreNpcBag(entry.Stock, context, "联系人 " + entry.NpcId + " 的货架", out stock, out error))
                {
                    return false;
                }

                market.RestoreEntry(entry.NpcId, stock, entry.LastRefreshDay);
            }

            return true;
        }

        /// <summary>
        /// 联系人货架多一档兼容：契约卡加入前是 6×3，现在是 6×4。
        /// 老档按原尺寸读入后原地扩容，物品一律保留（扩容只增加格子）。
        /// </summary>
        private static bool TryRestoreNpcBag(
            BagSave save,
            RestoreContext context,
            string label,
            out BagInventory bag,
            out string error)
        {
            bag = null;
            error = null;

            bool legacy = save != null
                          && save.Width == NpcMarket.StockWidth
                          && save.Height == 3;

            if (!legacy)
            {
                return TryRestoreBag(save, context, NpcMarket.StockWidth, NpcMarket.StockHeight, label, out bag, out error);
            }

            if (!TryRestoreBag(save, context, NpcMarket.StockWidth, 3, label, out bag, out error))
            {
                return false;
            }

            if (!bag.ResizeTo(NpcMarket.StockWidth, NpcMarket.StockHeight))
            {
                error = label + " 扩容失败。";
                return false;
            }

            return true;
        }

        private static bool RestoreQuests(QuestSave save, out QuestLog log, List<string> warnings)
        {
            log = new QuestLog();
            if (save == null || save.Entries == null)
            {
                return true;
            }

            var restored = new List<QuestProgress>();
            for (int i = 0; i < save.Entries.Length; i++)
            {
                QuestEntrySave entry = save.Entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.QuestId))
                {
                    continue;
                }

                QuestDefinition definition;
                if (!QuestLibrary.TryGet(entry.QuestId, out definition))
                {
                    warnings.Add("丢弃已移除的任务「" + entry.QuestId + "」。");
                    continue;
                }

                if (entry.State < (int)QuestState.Offered || entry.State > (int)QuestState.Failed)
                {
                    warnings.Add("丢弃状态非法的任务「" + entry.QuestId + "」。");
                    continue;
                }

                var state = (QuestState)entry.State;
                if (state == QuestState.Offered)
                {
                    continue;
                }

                restored.Add(new QuestProgress
                {
                    QuestId = entry.QuestId,
                    State = state,
                    Count = entry.Count > 0 ? entry.Count : 0,
                    AcceptedDay = entry.AcceptedDay > 0 ? entry.AcceptedDay : 0,
                    DeadlineDay = entry.DeadlineDay > 0 ? entry.DeadlineDay : 0
                });
            }

            var order = new List<string>();
            if (save.AcceptedOrder != null)
            {
                for (int i = 0; i < save.AcceptedOrder.Length; i++)
                {
                    string id = save.AcceptedOrder[i];
                    if (!string.IsNullOrEmpty(id) && ContainsQuest(restored, id) && !order.Contains(id))
                    {
                        order.Add(id);
                    }
                }
            }

            // 存档里没进顺序表（或顺序表损坏）的任务仍然要显示出来，追加到末尾即可。
            for (int i = 0; i < restored.Count; i++)
            {
                if (!order.Contains(restored[i].QuestId))
                {
                    order.Add(restored[i].QuestId);
                }
            }

            var cooldowns = new List<KeyValuePair<string, int>>();
            if (save.Cooldowns != null)
            {
                for (int i = 0; i < save.Cooldowns.Length; i++)
                {
                    QuestCooldownSave entry = save.Cooldowns[i];
                    if (entry == null || string.IsNullOrEmpty(entry.NpcId) || entry.UntilDay <= 0)
                    {
                        continue;
                    }

                    cooldowns.Add(new KeyValuePair<string, int>(entry.NpcId, entry.UntilDay));
                }
            }

            log.RestoreState(restored, order, cooldowns);
            return true;
        }

        private static List<KeyValuePair<int, string>> ReadCommsLocks(
            CommsSave save,
            BagInventory commsBag,
            List<string> warnings)
        {
            var locks = new List<KeyValuePair<int, string>>();
            if (save.LockedInstanceIds == null || save.LockedQuestIds == null)
            {
                return locks;
            }

            int count = Math.Min(save.LockedInstanceIds.Length, save.LockedQuestIds.Length);
            if (save.LockedInstanceIds.Length != save.LockedQuestIds.Length)
            {
                warnings.Add("通讯锁数据长度不一致，已忽略多余部分。");
            }

            for (int i = 0; i < count; i++)
            {
                int instanceId = save.LockedInstanceIds[i];
                string questId = save.LockedQuestIds[i];
                PlacedItem item;
                if (instanceId <= 0 || !commsBag.TryGet(instanceId, out item))
                {
                    warnings.Add("丢弃指向已不存在链路的通讯锁。");
                    continue;
                }

                QuestDefinition definition;
                if (string.IsNullOrEmpty(questId) || !QuestLibrary.TryGet(questId, out definition))
                {
                    warnings.Add("丢弃指向已移除任务的通讯锁。");
                    continue;
                }

                locks.Add(new KeyValuePair<int, string>(instanceId, questId));
            }

            return locks;
        }

        private static List<MerchantOffer> ReadOffers(
            MerchantSave save,
            RestoreContext context,
            BagInventory stock)
        {
            var offers = new List<MerchantOffer>();
            if (save.Offers == null)
            {
                return offers;
            }

            for (int i = 0; i < save.Offers.Length; i++)
            {
                OfferSave offer = save.Offers[i];
                if (offer == null)
                {
                    continue;
                }

                ItemDefinition definition;
                if (string.IsNullOrEmpty(offer.ItemId) || !context.Catalog.TryGet(offer.ItemId, out definition))
                {
                    context.Warnings.Add("丢弃引用未知物品的商人报价。");
                    continue;
                }

                if (offer.Price < 0 || offer.Count < 1 || offer.Remaining < 0)
                {
                    context.Warnings.Add("丢弃数值非法的商人报价。");
                    continue;
                }

                int instanceId = 0;
                if (offer.Remaining > 0)
                {
                    PlacedItem stocked;
                    if (offer.InstanceId <= 0
                        || !stock.TryGet(offer.InstanceId, out stocked)
                        || stocked.DefinitionId != offer.ItemId)
                    {
                        context.Warnings.Add("丢弃货架上已不存在的商人报价。");
                        continue;
                    }

                    instanceId = offer.InstanceId;
                }

                offers.Add(new MerchantOffer
                {
                    ItemId = offer.ItemId,
                    Price = offer.Price,
                    Remaining = offer.Remaining,
                    Count = offer.Count,
                    InstanceId = instanceId,
                    IsBuyback = offer.IsBuyback
                });
            }

            return offers;
        }

        private static int ClampLevel(int level, int maxLevel, string label, List<string> warnings)
        {
            if (level >= 1 && level <= maxLevel)
            {
                return level;
            }

            int clamped = level < 1 ? 1 : maxLevel;
            warnings.Add(label + " 等级 " + level + " 超出 1–" + maxLevel + "，已修正为 " + clamped + "。");
            return clamped;
        }

        private static int ClampDurability(int durability, ItemDefinition definition)
        {
            int max = definition != null ? definition.DurabilityMax : 0;
            if (max <= 0)
            {
                return 0;
            }

            if (durability < 0)
            {
                return 0;
            }

            return durability > max ? max : durability;
        }

        private static bool ContainsQuest(List<QuestProgress> list, string questId)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].QuestId == questId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
