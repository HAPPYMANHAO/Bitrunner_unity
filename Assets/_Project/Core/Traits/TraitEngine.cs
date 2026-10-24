using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public struct TraitTurnEffect
    {
        public TraitDefinition Trait;
        public PlacedItem Source;
        public TraitEffect Effect;
        public int Amount;
        public StatusType Status;
        public int StatusTurns;
        public string Message;
    }

    public struct TraitUseEffect
    {
        public TraitDefinition Trait;
        public int TargetInstanceId;
        public int Amount;
        public StatusType Status;
        public int StatusTurns;
        public string Message;
    }

    public sealed class TraitEngine
    {
        public const int SurroundEmptyCap = 8;

        private readonly ItemCatalog _catalog;
        private readonly TraitCatalog _traits;
        private readonly Dictionary<CombatActor, Dictionary<int, int>> _pendingDamage =
            new Dictionary<CombatActor, Dictionary<int, int>>();

        public TraitEngine(ItemCatalog catalog, TraitCatalog traits = null)
        {
            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            _catalog = catalog;
            _traits = traits ?? TraitCatalog.Default;
        }

        public TraitCatalog Traits
        {
            get { return _traits; }
        }

        public int GetBonusDamage(CombatActor actor, PlacedItem item, ItemDefinition definition)
        {
            if (actor == null || item == null || definition == null)
            {
                return 0;
            }

            return GetStaticDamage(actor, item, definition) + GetPendingDamage(actor, item.InstanceId);
        }

        public int GetPendingDamage(CombatActor actor, int instanceId)
        {
            if (actor == null)
            {
                return 0;
            }

            Dictionary<int, int> pending;
            int amount;
            if (_pendingDamage.TryGetValue(actor, out pending)
                && pending.TryGetValue(instanceId, out amount))
            {
                return amount;
            }

            return 0;
        }

        public int GetStaticDamage(CombatActor actor, PlacedItem item, ItemDefinition definition)
        {
            if (actor == null || item == null || definition == null || definition.Traits == null)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < definition.Traits.Length; i++)
            {
                TraitDefinition trait;
                if (!_traits.TryGet(definition.Traits[i].TraitId, out trait))
                {
                    continue;
                }

                if (trait.Trigger != TraitTrigger.Passive)
                {
                    continue;
                }

                if (trait.Effect == TraitEffect.AddDamageFromShield)
                {
                    int divisor = trait.ShieldDivisor > 0 ? trait.ShieldDivisor : 1;
                    total += actor.Shield / divisor;
                    continue;
                }

                if (trait.Effect != TraitEffect.AddDamage)
                {
                    continue;
                }

                int amount = trait.AmountAtRank(definition.Traits[i].Rank);
                if (amount <= 0)
                {
                    continue;
                }

                switch (trait.Condition)
                {
                    case TraitCondition.None:
                        total += amount;
                        break;
                    case TraitCondition.Surround8EmptyCells:
                        total += CountSurround8Empty(actor.Bag, item) * amount;
                        break;
                    case TraitCondition.Surround8Armor:
                        total += CountSurround8Armor(actor.Bag, item) * amount;
                        break;
                }
            }

            return total;
        }

        public IReadOnlyList<TraitTurnEffect> ResolveTurnStart(CombatActor actor)
        {
            var effects = new List<TraitTurnEffect>();
            if (actor == null)
            {
                return effects;
            }

            IReadOnlyList<PlacedItem> items = actor.Bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                PlacedItem item = items[i];
                ItemDefinition definition;
                if (!_catalog.TryGet(item.DefinitionId, out definition) || definition.Traits == null)
                {
                    continue;
                }

                for (int t = 0; t < definition.Traits.Length; t++)
                {
                    TraitDefinition trait;
                    if (!_traits.TryGet(definition.Traits[t].TraitId, out trait)
                        || trait.Trigger != TraitTrigger.TurnStart)
                    {
                        continue;
                    }

                    int rank = definition.Traits[t].Rank;
                    int amount = trait.AmountAtRank(rank);
                    if (amount <= 0)
                    {
                        continue;
                    }

                    int count = 1;
                    switch (trait.Condition)
                    {
                        case TraitCondition.None:
                            break;
                        case TraitCondition.Surround8Armor:
                            count = CountSurround8Armor(actor.Bag, item);
                            break;
                        case TraitCondition.BagArmorAtMost:
                            if (CountBagArmor(actor.Bag) > trait.ConditionAtRank(rank))
                            {
                                continue;
                            }

                            break;
                        default:
                            continue;
                    }

                    if (count <= 0)
                    {
                        continue;
                    }

                    int total = amount * count;
                    effects.Add(new TraitTurnEffect
                    {
                        Trait = trait,
                        Source = item,
                        Effect = trait.Effect,
                        Amount = total,
                        Status = trait.Status,
                        StatusTurns = trait.StatusTurns,
                        Message = TurnStartMessage(trait, total)
                    });
                }
            }

            return effects;
        }

        public int GetPassiveEnergy(CombatActor actor)
        {
            if (actor == null)
            {
                return 0;
            }

            int total = 0;
            IReadOnlyList<PlacedItem> items = actor.Bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition;
                if (!_catalog.TryGet(items[i].DefinitionId, out definition) || definition.Traits == null)
                {
                    continue;
                }

                for (int t = 0; t < definition.Traits.Length; t++)
                {
                    TraitDefinition trait;
                    if (!_traits.TryGet(definition.Traits[t].TraitId, out trait)
                        || trait.Trigger != TraitTrigger.Passive
                        || trait.Effect != TraitEffect.AddEnergy)
                    {
                        continue;
                    }

                    total += trait.AmountAtRank(definition.Traits[t].Rank);
                }
            }

            return total;
        }

        /// <summary>
        /// 扫描「背包之外」的网格（中央处理器的芯片槽），汇总成一份全局加成快照。
        ///
        /// 合并规则刻意不对称：
        /// <list type="bullet">
        /// <item>生命上限、护甲基底**求和**——多张芯片可以叠加，代价是占掉更多 CPU 空间。</item>
        /// <item>基础能量**取最高**——否则塞满能量芯片就能无限行动，这条是 <c>DESIGN.md</c> §7.2 的硬约束。</item>
        /// </list>
        /// </summary>
        public GlobalModifiers EvaluateChips(BagInventory chipGrid)
        {
            GlobalModifiers totals = GlobalModifiers.None;
            if (chipGrid == null)
            {
                return totals;
            }

            IReadOnlyList<PlacedItem> items = chipGrid.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition;
                if (!_catalog.TryGet(items[i].DefinitionId, out definition) || definition.Traits == null)
                {
                    continue;
                }

                for (int t = 0; t < definition.Traits.Length; t++)
                {
                    TraitDefinition trait;
                    if (!_traits.TryGet(definition.Traits[t].TraitId, out trait)
                        || trait.Trigger != TraitTrigger.Passive)
                    {
                        continue;
                    }

                    int amount = trait.AmountAtRank(definition.Traits[t].Rank);
                    if (amount == 0)
                    {
                        continue;
                    }

                    switch (trait.Effect)
                    {
                        case TraitEffect.AddMaxHp:
                            totals.MaxHp += amount;
                            break;
                        case TraitEffect.AddArmorBase:
                            totals.ArmorBase += amount;
                            break;
                        case TraitEffect.AddBaseEnergy:
                            if (amount > totals.BaseEnergy)
                            {
                                totals.BaseEnergy = amount;
                            }

                            break;
                    }
                }
            }

            return totals;
        }

        public bool TryGetOnUseStatus(
            ItemDefinition definition,
            out StatusType status,
            out int stacks,
            out int turns)
        {
            status = StatusType.None;
            stacks = 0;
            turns = 0;
            if (definition == null || definition.Traits == null)
            {
                return false;
            }

            for (int i = 0; i < definition.Traits.Length; i++)
            {
                TraitDefinition trait;
                if (!_traits.TryGet(definition.Traits[i].TraitId, out trait)
                    || trait.Trigger != TraitTrigger.OnUse
                    || trait.Effect != TraitEffect.GrantStatus
                    || trait.Status == StatusType.None)
                {
                    continue;
                }

                status = trait.Status;
                stacks = trait.AmountAtRank(definition.Traits[i].Rank);
                turns = trait.StatusTurns;
                return true;
            }

            return false;
        }

        private static string TurnStartMessage(TraitDefinition trait, int amount)
        {
            switch (trait.Effect)
            {
                case TraitEffect.AddShield:
                    return trait.DisplayName + "：获得 " + amount + " 护盾";
                case TraitEffect.ConsumeStatus:
                    return trait.DisplayName + "：清除 " + amount + " 层" + ItemText.StatusName(trait.Status);
                case TraitEffect.GrantStatus:
                    return trait.DisplayName + "：获得 " + amount + " 层" + ItemText.StatusName(trait.Status);
                default:
                    return trait.DisplayName;
            }
        }

        public IReadOnlyList<TraitUseEffect> ApplyOnUse(
            CombatActor actor,
            PlacedItem item,
            ItemDefinition definition)
        {
            var effects = new List<TraitUseEffect>();
            if (actor == null || item == null || definition == null || definition.Traits == null)
            {
                return effects;
            }

            for (int i = 0; i < definition.Traits.Length; i++)
            {
                TraitDefinition trait;
                if (!_traits.TryGet(definition.Traits[i].TraitId, out trait)
                    || trait.Trigger != TraitTrigger.OnUse)
                {
                    continue;
                }

                int amount = trait.AmountAtRank(definition.Traits[i].Rank);
                if (amount <= 0)
                {
                    continue;
                }

                if (trait.Target == TraitTarget.Surround8Weapons)
                {
                    ApplySurroundWeaponDamage(actor, item, trait, amount, effects);
                    continue;
                }

                if (trait.Effect == TraitEffect.GrantStatus && trait.Status != StatusType.None)
                {
                    effects.Add(new TraitUseEffect
                    {
                        Trait = trait,
                        TargetInstanceId = 0,
                        Amount = amount,
                        Status = trait.Status,
                        StatusTurns = trait.StatusTurns,
                        Message = trait.DisplayName + "：施加 " + amount + " 层" + ItemText.StatusName(trait.Status)
                    });
                    continue;
                }

                if (trait.Effect == TraitEffect.AddDamage)
                {
                    AddPending(actor, item.InstanceId, amount, trait.StackRule);
                    effects.Add(new TraitUseEffect
                    {
                        Trait = trait,
                        TargetInstanceId = item.InstanceId,
                        Amount = amount,
                        Message = trait.DisplayName + "：本回合 +" + amount + " 伤害"
                    });
                }
            }

            return effects;
        }

        public void ResetTurn(CombatActor actor)
        {
            if (actor == null)
            {
                return;
            }

            _pendingDamage.Remove(actor);
        }

        public int CountSurround8Empty(BagInventory bag, PlacedItem item)
        {
            if (bag == null || item == null)
            {
                return 0;
            }

            List<Cell> cells = SurroundCells(bag, item);
            int count = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                if (bag.Grid.IsFree(cells[i].X, cells[i].Y))
                {
                    count++;
                }
            }

            return Math.Min(SurroundEmptyCap, count);
        }

        public int CountSurround8Armor(BagInventory bag, PlacedItem item)
        {
            if (bag == null || item == null)
            {
                return 0;
            }

            List<Cell> cells = SurroundCells(bag, item);
            var counted = new HashSet<int>();
            for (int i = 0; i < cells.Count; i++)
            {
                int occupant = bag.Grid.GetOccupant(cells[i].X, cells[i].Y);
                if (occupant <= 0 || occupant == item.InstanceId || counted.Contains(occupant))
                {
                    continue;
                }

                PlacedItem other;
                ItemDefinition otherDefinition;
                if (!bag.TryGet(occupant, out other)
                    || !_catalog.TryGet(other.DefinitionId, out otherDefinition))
                {
                    continue;
                }

                if (otherDefinition.Category == ItemCategory.Armor)
                {
                    counted.Add(occupant);
                }
            }

            return counted.Count;
        }

        public int CountBagArmor(BagInventory bag)
        {
            if (bag == null)
            {
                return 0;
            }

            int count = 0;
            IReadOnlyList<PlacedItem> items = bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition;
                if (_catalog.TryGet(items[i].DefinitionId, out definition)
                    && definition.Category == ItemCategory.Armor)
                {
                    count++;
                }
            }

            return count;
        }

        public static List<Cell> SurroundCells(BagInventory bag, PlacedItem item)
        {
            var result = new List<Cell>();
            if (bag == null || item == null)
            {
                return result;
            }

            var seen = new HashSet<Cell>();
            IReadOnlyList<Cell> cells = item.GetOccupiedCells();
            for (int i = 0; i < cells.Count; i++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0)
                        {
                            continue;
                        }

                        var neighbour = new Cell(cells[i].X + dx, cells[i].Y + dy);
                        if (!bag.Grid.InBounds(neighbour.X, neighbour.Y))
                        {
                            continue;
                        }

                        if (seen.Add(neighbour))
                        {
                            result.Add(neighbour);
                        }
                    }
                }
            }

            return result;
        }

        private void ApplySurroundWeaponDamage(
            CombatActor actor,
            PlacedItem item,
            TraitDefinition trait,
            int amount,
            List<TraitUseEffect> effects)
        {
            List<Cell> cells = SurroundCells(actor.Bag, item);
            var applied = new HashSet<int>();
            for (int i = 0; i < cells.Count; i++)
            {
                int occupant = actor.Bag.Grid.GetOccupant(cells[i].X, cells[i].Y);
                if (occupant <= 0 || occupant == item.InstanceId || applied.Contains(occupant))
                {
                    continue;
                }

                PlacedItem other;
                ItemDefinition otherDefinition;
                if (!actor.Bag.TryGet(occupant, out other)
                    || !_catalog.TryGet(other.DefinitionId, out otherDefinition)
                    || !otherDefinition.IsWeapon)
                {
                    continue;
                }

                applied.Add(occupant);
                int before = GetPendingDamage(actor, occupant);
                AddPending(actor, occupant, amount, trait.StackRule);
                effects.Add(new TraitUseEffect
                {
                    Trait = trait,
                    TargetInstanceId = occupant,
                    Amount = GetPendingDamage(actor, occupant) - before,
                    Message = trait.DisplayName + "：" + otherDefinition.DisplayName
                        + " 本回合 +" + amount + " 伤害"
                });
            }
        }

        private void AddPending(CombatActor actor, int instanceId, int amount, TraitStackRule rule)
        {
            Dictionary<int, int> pending;
            if (!_pendingDamage.TryGetValue(actor, out pending))
            {
                pending = new Dictionary<int, int>();
                _pendingDamage.Add(actor, pending);
            }

            int current;
            pending.TryGetValue(instanceId, out current);
            pending[instanceId] = rule == TraitStackRule.Stack
                ? current + amount
                : Math.Max(current, amount);
        }
    }
}
