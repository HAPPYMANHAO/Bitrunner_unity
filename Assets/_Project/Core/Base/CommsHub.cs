using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    /// <summary>
    /// 通讯中枢：和特殊 NPC 打交道的唯一通道。
    ///
    /// 它用格子制装**通讯上行链路**（<see cref="ItemCategory.Uplink"/>），
    /// 一条链路 = 一位能联系的 NPC = 一条可承载任务的信道。
    ///
    /// 并行做任务的限制不在任务系统里，而在这里：接一个任务就把它那条链路
    /// **锁死在格子里**，直到任务领完奖励才解锁。所以「能同时做几个任务」
    /// 变成了「中枢能放下几条链路」——限制可看见、可投资，而不是一个抽象数字。
    /// </summary>
    public sealed class CommsHub
    {
        public const int MaxLevel = 5;

        /// <summary>链路被任务占用时，移动/取出会被拒绝。</summary>
        private readonly Dictionary<int, string> _locks = new Dictionary<int, string>();

        public int Level { get; private set; }

        public BagInventory Grid { get; private set; }

        public CommsHub()
        {
            Level = 1;
            Grid = new BagInventory(WidthForLevel(Level), HeightForLevel(Level));
            Grid.ItemModificationGuard = GuardItemModification;
        }

        /// <summary>
        /// 读档恢复等级。锁表不在这里恢复——必须先灌入链路物品，再由
        /// <see cref="RestoreLocks"/> 校验实例是否还存在。
        /// </summary>
        internal void RestoreLevel(int level)
        {
            Level = level < 1 ? 1 : (level > MaxLevel ? MaxLevel : level);
            Grid = new BagInventory(WidthForLevel(Level), HeightForLevel(Level));
            Grid.ItemModificationGuard = GuardItemModification;
        }

        internal void RestoreGrid(BagInventory grid)
        {
            if (grid == null)
            {
                return;
            }

            grid.ItemModificationGuard = GuardItemModification;
            Grid = grid;
        }

        internal void RestoreLocks(IEnumerable<KeyValuePair<int, string>> locks)
        {
            _locks.Clear();
            if (locks == null)
            {
                return;
            }

            foreach (KeyValuePair<int, string> pair in locks)
            {
                _locks[pair.Key] = pair.Value;
            }
        }

        internal IReadOnlyDictionary<int, string> Locks
        {
            get { return _locks; }
        }

        private string GuardItemModification(int instanceId)
        {
            string error;
            return CanModify(instanceId, out error) ? null : error;
        }

        /// <summary>L1 2×2 / L2 3×2 / L3 3×3 / L4 4×3 / L5 4×4（4 / 6 / 9 / 12 / 16 格）。</summary>
        public static int WidthForLevel(int level)
        {
            switch (level)
            {
                case 1:
                    return 2;
                case 2:
                case 3:
                    return 3;
                default:
                    return 4;
            }
        }

        public static int HeightForLevel(int level)
        {
            switch (level)
            {
                case 1:
                case 2:
                    return 2;
                case 3:
                case 4:
                    return 3;
                default:
                    return 4;
            }
        }

        public int Width
        {
            get { return Grid.Grid.Width; }
        }

        public int Height
        {
            get { return Grid.Grid.Height; }
        }

        public bool CanUpgrade
        {
            get { return Level < MaxLevel; }
        }

        public int UpgradeCost
        {
            get
            {
                switch (Level)
                {
                    case 1:
                        return 250;
                    case 2:
                        return 500;
                    case 3:
                        return 900;
                    case 4:
                        return 1500;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// L4→L5 刻意**不用** crisis_core：中枢是危机模式的前置（白鸦卖测序器），
        /// 升级若需要危机产物就成循环依赖。
        /// </summary>
        public string[] UpgradeMaterialIds
        {
            get
            {
                switch (Level)
                {
                    case 1:
                        return new[] { "scrap", "scrap", "wire" };
                    case 2:
                        return new[] { "alloy", "circuit" };
                    case 3:
                        return new[] { "circuit", "servo" };
                    case 4:
                        return new[] { "superalloy", "optic_fiber" };
                    default:
                        return new string[0];
                }
            }
        }

        /// <summary>只有通讯上行链路能插进来。关卡密钥（Key）放不进去。</summary>
        public static bool CanAccept(ItemDefinition definition)
        {
            return definition != null && definition.Category == ItemCategory.Uplink;
        }

        /// <summary>已联系的 NPC id，按档位从低到高排序，方便 UI 直接铺。</summary>
        public IReadOnlyList<string> ContactedNpcIds
        {
            get
            {
                var pairs = new List<NpcDefinition>();
                IReadOnlyList<PlacedItem> items = Grid.Items;
                for (int i = 0; i < items.Count; i++)
                {
                    NpcDefinition npc;
                    if (!NpcLibrary.TryFindByUplink(items[i].DefinitionId, out npc))
                    {
                        continue;
                    }

                    bool exists = false;
                    for (int j = 0; j < pairs.Count; j++)
                    {
                        if (pairs[j].Id == npc.Id)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        pairs.Add(npc);
                    }
                }

                pairs.Sort((left, right) => left.Tier.CompareTo(right.Tier));

                var ids = new List<string>(pairs.Count);
                for (int i = 0; i < pairs.Count; i++)
                {
                    ids.Add(pairs[i].Id);
                }

                return ids;
            }
        }

        public int TotalLinks
        {
            get { return Grid.Count; }
        }

        public int LockedLinks
        {
            get { return _locks.Count; }
        }

        public int FreeLinks
        {
            get { return Math.Max(0, Grid.Count - _locks.Count); }
        }

        public int CountUplinks(string npcId)
        {
            NpcDefinition npc;
            if (!NpcLibrary.TryGet(npcId, out npc) || string.IsNullOrEmpty(npc.UplinkItemId))
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<PlacedItem> items = Grid.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].DefinitionId == npc.UplinkItemId)
                {
                    count++;
                }
            }

            return count;
        }

        public int CountLockedUplinks(string npcId)
        {
            NpcDefinition npc;
            if (!NpcLibrary.TryGet(npcId, out npc) || string.IsNullOrEmpty(npc.UplinkItemId))
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<PlacedItem> items = Grid.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].DefinitionId == npc.UplinkItemId && IsLocked(items[i].InstanceId))
                {
                    count++;
                }
            }

            return count;
        }

        public bool HasUplinkFor(string npcId)
        {
            PlacedItem item;
            return TryGetUplink(npcId, out item);
        }

        /// <summary>找该 NPC 的链路实例（优先返回没被锁的那条）。</summary>
        public bool TryGetUplink(string npcId, out PlacedItem item)
        {
            item = null;
            NpcDefinition npc;
            if (!NpcLibrary.TryGet(npcId, out npc))
            {
                return false;
            }

            PlacedItem lockedCandidate = null;
            IReadOnlyList<PlacedItem> items = Grid.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].DefinitionId != npc.UplinkItemId)
                {
                    continue;
                }

                if (!IsLocked(items[i].InstanceId))
                {
                    item = items[i];
                    return true;
                }

                if (lockedCandidate == null)
                {
                    lockedCandidate = items[i];
                }
            }

            item = lockedCandidate;
            return item != null;
        }

        /// <summary>把某位 NPC 的链路锁给这个任务。已经锁给同一个任务时视为成功。</summary>
        public bool TryLockForQuest(string npcId, string questId, out string error)
        {
            if (string.IsNullOrEmpty(questId))
            {
                error = "任务无效。";
                return false;
            }

            PlacedItem item;
            if (!TryGetUplink(npcId, out item))
            {
                error = "通讯中枢里没有这位联系人。";
                return false;
            }

            string current;
            if (_locks.TryGetValue(item.InstanceId, out current))
            {
                if (current == questId)
                {
                    error = null;
                    return true;
                }

                error = "这条上行链路正在处理另一个任务。";
                return false;
            }

            _locks.Add(item.InstanceId, questId);
            error = null;
            return true;
        }

        public bool UnlockForQuest(string questId)
        {
            if (string.IsNullOrEmpty(questId))
            {
                return false;
            }

            int target = 0;
            foreach (KeyValuePair<int, string> pair in _locks)
            {
                if (pair.Value == questId)
                {
                    target = pair.Key;
                    break;
                }
            }

            return target != 0 && _locks.Remove(target);
        }

        public bool IsLocked(int instanceId)
        {
            return _locks.ContainsKey(instanceId);
        }

        /// <summary>被哪个任务锁着。没锁返回 null。</summary>
        public string LockedQuestOf(int instanceId)
        {
            string questId;
            return _locks.TryGetValue(instanceId, out questId) ? questId : null;
        }

        /// <summary>玩家想移动/取出某件物品前调用。UI 拖拽源用它拦下锁定链路。</summary>
        public bool CanModify(int instanceId, out string error)
        {
            string questId = LockedQuestOf(instanceId);
            if (questId == null)
            {
                error = null;
                return true;
            }

            QuestDefinition definition;
            string title = QuestLibrary.TryGet(questId, out definition) ? definition.Title : questId;
            error = "上行链路正在处理「" + title + "」，完成并领取后才能移动。";
            return false;
        }

        public bool TryUpgrade(CampaignState campaign, BagInventory stash, out string error)
        {
            if (campaign == null)
            {
                throw new ArgumentNullException(nameof(campaign));
            }

            if (Level >= MaxLevel)
            {
                error = "通讯中枢已满级。";
                return false;
            }

            if (!FacilityUpgrade.TryPay(campaign, stash, UpgradeCost, UpgradeMaterialIds, out error))
            {
                return false;
            }

            int nextLevel = Level + 1;

            // 用 ResizeTo 而不是 FacilityGrid.Resize：前者原地改尺寸、保留
            // PlacedItem 实例，所以 _locks 里的 instanceId 在升级后依然有效。
            // （FacilityGrid.Resize 会重建实例，锁会全部失效。）
            // 尺寸逐级只增不减，所以 ResizeTo 不可能失败。
            Grid.ResizeTo(WidthForLevel(nextLevel), HeightForLevel(nextLevel));

            Level = nextLevel;
            error = null;
            return true;
        }
    }
}
