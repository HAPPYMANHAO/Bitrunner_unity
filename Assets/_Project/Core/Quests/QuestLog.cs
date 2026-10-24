using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    /// <summary>
    /// 任务状态机 + 进度累计。
    ///
    /// **没有「同时进行上限」这个数字。** 并行限制由两颗钉子实现：
    /// 一条 <see cref="ItemCategory.Uplink"/> 只承载一个任务（接取时锁进
    /// <see cref="CommsHub"/>），而中枢的格子数是有限的。所以想做更多任务，
    /// 只能升中枢。
    ///
    /// 进度上报只有三个入口（<see cref="ReportKill"/> / <see cref="ReportDiveCleared"/> /
    /// <see cref="RefreshStatic"/>），刻意不做事件总线——任务数量是十位数，
    /// 每次物品移动都扫一遍背包不值得。
    /// </summary>
    public sealed class QuestLog
    {
        /// <summary>任务失败后该 NPC 冷却几天才重新发任务，防止故意失败刷池子。</summary>
        public const int FailCooldownDays = 2;

        private readonly Dictionary<string, QuestProgress> _progress = new Dictionary<string, QuestProgress>();
        private readonly Dictionary<string, int> _cooldownUntilDay = new Dictionary<string, int>();

        /// <summary>接取顺序，给 UI 一个稳定的排列。</summary>
        private readonly List<string> _acceptedOrder = new List<string>();

        /// <summary>周×7+日 的绝对天序号。超时判定用绝对量，跨周才不会算错。</summary>
        public static int Today(CampaignState campaign)
        {
            if (campaign == null)
            {
                return 0;
            }

            return (campaign.Week - 1) * CampaignState.DaysPerWeek + campaign.Day;
        }

        /// <summary>进行中（含已成待领）的任务，按接取顺序。</summary>
        public IReadOnlyList<QuestProgress> ActiveQuests
        {
            get
            {
                var result = new List<QuestProgress>();
                for (int i = 0; i < _acceptedOrder.Count; i++)
                {
                    QuestProgress progress = Get(_acceptedOrder[i]);
                    if (progress != null
                        && (progress.State == QuestState.Active || progress.State == QuestState.Ready))
                    {
                        result.Add(progress);
                    }
                }

                return result;
            }
        }

        public QuestProgress Get(string questId)
        {
            QuestProgress progress;
            return _progress.TryGetValue(questId, out progress) ? progress : null;
        }

        public QuestState StateOf(string questId)
        {
            QuestProgress progress = Get(questId);
            return progress == null ? QuestState.Offered : progress.State;
        }

        /// <summary>这位 NPC 当前还能不能再接一个任务（他的链路没被占）。</summary>
        public static bool NpcIsFree(CommsHub hub, string npcId)
        {
            if (hub == null)
            {
                return false;
            }

            PlacedItem item;
            return hub.TryGetUplink(npcId, out item) && !hub.IsLocked(item.InstanceId);
        }

        public bool CanOffer(QuestDefinition definition, CommsHub hub, int today, out string reason)
        {
            if (definition == null)
            {
                reason = "任务无效。";
                return false;
            }

            QuestProgress progress = Get(definition.Id);
            if (progress != null)
            {
                if (progress.State == QuestState.Active || progress.State == QuestState.Ready)
                {
                    reason = "任务进行中。";
                    return false;
                }

                if (progress.State == QuestState.Claimed)
                {
                    reason = "任务已完成。";
                    return false;
                }

                int until;
                if (_cooldownUntilDay.TryGetValue(definition.Id, out until) && today < until)
                {
                    reason = "对方还在气头上（还要等 " + (until - today) + " 天）。";
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(definition.RequiresQuestId)
                && StateOf(definition.RequiresQuestId) != QuestState.Claimed)
            {
                reason = "前置任务未完成。";
                return false;
            }

            // 链路必须在中枢里，而且没被别的任务占着。
            PlacedItem uplink;
            if (hub == null || !hub.TryGetUplink(definition.NpcId, out uplink))
            {
                reason = "还没有联系上他。";
                return false;
            }

            if (hub.IsLocked(uplink.InstanceId))
            {
                reason = "这条上行链路正在处理另一个任务。";
                return false;
            }

            reason = null;
            return true;
        }

        public bool TryAccept(QuestDefinition definition, CommsHub hub, int today, out string error)
        {
            if (!CanOffer(definition, hub, today, out error))
            {
                return false;
            }

            if (!hub.TryLockForQuest(definition.NpcId, definition.Id, out error))
            {
                return false;
            }

            QuestProgress progress = Get(definition.Id);
            if (progress == null)
            {
                progress = new QuestProgress { QuestId = definition.Id };
                _progress.Add(definition.Id, progress);
                _acceptedOrder.Add(definition.Id);
            }

            progress.State = QuestState.Active;
            progress.Count = 0;
            progress.AcceptedDay = today;
            progress.DeadlineDay = definition.TimeLimitDays > 0
                ? today + definition.TimeLimitDays
                : 0;

            error = null;
            return true;
        }

        /// <summary>
        /// 领奖。顺序是「先扣交付物、再发奖励」：扣完腾出来的格子正好用来装奖励，
        /// 否则仓库满的时候会出现「明明交了货却领不了奖」。发奖失败会把扣掉的还回去。
        /// </summary>
        public bool TryClaim(
            QuestDefinition definition,
            CommsHub hub,
            CampaignState campaign,
            BagInventory stash,
            ItemCatalog catalog,
            out string error)
        {
            if (definition == null)
            {
                error = "任务无效。";
                return false;
            }

            QuestProgress progress = Get(definition.Id);
            if (progress == null || progress.State != QuestState.Ready)
            {
                error = "任务还没有完成。";
                return false;
            }

            if (stash == null)
            {
                error = "仓库不可用。";
                return false;
            }

            // 1) 交付类目标：先把东西扣掉。
            int restoredNeeded = 0;
            if (definition.IsHoldingObjective)
            {
                if (ItemSearch.CountOf(stash, definition.TargetId) < definition.TargetCount)
                {
                    error = "交付物不足。";
                    return false;
                }

                if (!ItemSearch.TryConsume(stash, definition.TargetId, definition.TargetCount))
                {
                    error = "交付物不足。";
                    return false;
                }

                restoredNeeded = definition.TargetCount;
            }

            // 2) 发奖励。
            var added = new List<int>();
            bool granted = GiveAll(stash, catalog, definition, added);
            if (!granted)
            {
                Rollback(stash, added);

                if (restoredNeeded > 0)
                {
                    ItemDefinition target;
                    if (catalog != null && catalog.TryGet(definition.TargetId, out target))
                    {
                        stash.AddAmount(target.Id, target.Shape, restoredNeeded, target.MaxStack);
                    }
                }

                error = "仓库放不下奖励，先腾出空间。";
                return false;
            }

            // 3) 钱、解锁、状态。
            if (definition.RewardBit > 0 && campaign != null)
            {
                campaign.AddBit(definition.RewardBit);
            }

            if (hub != null)
            {
                hub.UnlockForQuest(definition.Id);
            }

            progress.State = QuestState.Claimed;
            progress.Count = definition.TargetCount;
            error = null;
            return true;
        }

        public bool Abandon(QuestDefinition definition, CommsHub hub, int today, out string error)
        {
            if (definition == null)
            {
                error = "任务无效。";
                return false;
            }

            QuestProgress progress = Get(definition.Id);
            if (progress == null
                || (progress.State != QuestState.Active && progress.State != QuestState.Ready))
            {
                error = "没有进行中的这个任务。";
                return false;
            }

            Fail(definition, hub, today);
            error = null;
            return true;
        }

        /// <summary>事件型目标累加。返回推进了几个任务，供 UI 决定要不要提示。</summary>
        public int Report(QuestObjectiveKind kind, string targetId, int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int advanced = 0;
            foreach (KeyValuePair<string, QuestProgress> pair in _progress)
            {
                QuestProgress progress = pair.Value;
                if (progress.State != QuestState.Active)
                {
                    continue;
                }

                QuestDefinition definition;
                if (!QuestLibrary.TryGet(pair.Key, out definition) || definition.Objective != kind)
                {
                    continue;
                }

                // Map 自己算（绝对量），不在这里累加。
                if (definition.UsesAbsoluteProgress)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(definition.TargetId)
                    && definition.TargetId != targetId)
                {
                    continue;
                }

                progress.Count += amount;
                if (progress.Count > definition.TargetCount)
                {
                    progress.Count = definition.TargetCount;
                }

                if (progress.Count >= definition.TargetCount)
                {
                    progress.State = QuestState.Ready;
                    advanced++;
                }
            }

            return advanced;
        }

        public int ReportKill(string enemyId, bool isCrisisBoss)
        {
            int advanced = Report(QuestObjectiveKind.KillEnemies, null, 1);
            if (!string.IsNullOrEmpty(enemyId))
            {
                advanced += Report(QuestObjectiveKind.KillTagged, enemyId, 1);
            }

            if (isCrisisBoss)
            {
                advanced += Report(QuestObjectiveKind.DefeatCrisisBoss, enemyId, 1);
            }

            return advanced;
        }

        public int ReportDiveCleared(string siteId, bool crisis)
        {
            int advanced = Report(QuestObjectiveKind.ClearDive, siteId, 1);
            if (crisis)
            {
                advanced += Report(QuestObjectiveKind.ClearCrisis, null, 1);
            }

            return advanced;
        }

        public int ReportMineCollected()
        {
            return Report(QuestObjectiveKind.CollectMine, null, 1);
        }

        /// <summary>
        /// 绝对量目标（持有物品 / 累计缴款）的重算。基地页刷新时调用一次即可，
        /// 不需要在每次拖拽时判定。
        /// </summary>
        public void RefreshStatic(
            ItemCatalog catalog,
            BagInventory stash,
            BagInventory playerBag,
            CampaignState campaign)
        {
            foreach (KeyValuePair<string, QuestProgress> pair in _progress)
            {
                QuestProgress progress = pair.Value;
                if (progress.State != QuestState.Active && progress.State != QuestState.Ready)
                {
                    continue;
                }

                QuestDefinition definition;
                if (!QuestLibrary.TryGet(pair.Key, out definition) || !definition.UsesAbsoluteProgress)
                {
                    continue;
                }

                int count;
                if (definition.Objective == QuestObjectiveKind.PayDebt)
                {
                    count = campaign != null ? campaign.TotalPaid : 0;
                }
                else
                {
                    count = (stash != null ? ItemSearch.CountOf(stash, definition.TargetId) : 0)
                            + (playerBag != null ? ItemSearch.CountOf(playerBag, definition.TargetId) : 0);
                }

                progress.Count = count > definition.TargetCount ? definition.TargetCount : count;

                // 达成后又被卖掉/丢掉时，状态要退回去，否则能空手领奖。
                progress.State = Reached(definition, count) ? QuestState.Ready : QuestState.Active;
            }
        }

        /// <summary>超时判定。每次进入新的一天时调一次。</summary>
        public void Tick(CommsHub hub, int today)
        {
            var expired = new List<string>();
            foreach (KeyValuePair<string, QuestProgress> pair in _progress)
            {
                QuestProgress progress = pair.Value;
                if (progress.State != QuestState.Active && progress.State != QuestState.Ready)
                {
                    continue;
                }

                if (progress.IsExpired(today))
                {
                    expired.Add(pair.Key);
                }
            }

            for (int i = 0; i < expired.Count; i++)
            {
                QuestDefinition definition;
                if (QuestLibrary.TryGet(expired[i], out definition))
                {
                    Fail(definition, hub, today);
                }
            }
        }

        private static bool Reached(QuestDefinition definition, int count)
        {
            return definition != null && count >= definition.TargetCount;
        }

        /// <summary>失败路径必须解锁链路，否则超时会把中枢永久锁死。</summary>
        private void Fail(QuestDefinition definition, CommsHub hub, int today)
        {
            QuestProgress progress = Get(definition.Id);
            if (progress != null)
            {
                progress.State = QuestState.Failed;
            }

            if (hub != null)
            {
                hub.UnlockForQuest(definition.Id);
            }

            _cooldownUntilDay[definition.Id] = today + FailCooldownDays;
        }

        private static bool GiveAll(
            BagInventory stash,
            ItemCatalog catalog,
            QuestDefinition definition,
            List<int> added)
        {
            if (catalog == null)
            {
                return true;
            }

            if (definition.RewardItemIds != null)
            {
                for (int i = 0; i < definition.RewardItemIds.Length; i++)
                {
                    if (!Give(stash, catalog, definition.RewardItemIds[i], added))
                    {
                        return false;
                    }
                }
            }

            if (!string.IsNullOrEmpty(definition.RewardUplinkItemId))
            {
                if (!Give(stash, catalog, definition.RewardUplinkItemId, added))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Give(
            BagInventory stash,
            ItemCatalog catalog,
            string itemId,
            List<int> added)
        {
            ItemDefinition definition;
            if (!catalog.TryGet(itemId, out definition))
            {
                return false;
            }

            PlacedItem placed;
            if (!stash.TryAdd(definition.Id, definition.Shape, out placed))
            {
                return false;
            }

            added.Add(placed.InstanceId);
            return true;
        }

        private static void Rollback(BagInventory stash, List<int> added)
        {
            for (int i = 0; i < added.Count; i++)
            {
                stash.Remove(added[i]);
            }

            added.Clear();
        }
    }
}
