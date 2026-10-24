using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class CombatEngine
    {
        private readonly ItemCatalog _catalog;
        private readonly IReadOnlyList<SynergyRule> _synergyRules;
        private readonly List<CombatActor> _enemies;
        private readonly List<CombatEvent> _events = new List<CombatEvent>();
        private CombatActor _activeEnemy;

        public CombatEngine(CombatActor player, IEnumerable<CombatActor> enemies, ItemCatalog catalog)
            : this(player, enemies, catalog, SynergyResolver.DefaultRules)
        {
        }

        public CombatEngine(CombatActor player, IEnumerable<CombatActor> enemies, ItemCatalog catalog, IReadOnlyList<SynergyRule> synergyRules)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (enemies == null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            if (catalog == null)
            {
                throw new ArgumentNullException(nameof(catalog));
            }

            Player = player;
            _enemies = new List<CombatActor>(enemies);
            if (_enemies.Count == 0)
            {
                throw new ArgumentException("At least one enemy is required.", nameof(enemies));
            }

            _catalog = catalog;
            _synergyRules = synergyRules ?? SynergyResolver.DefaultRules;
            Phase = CombatPhase.NotStarted;
        }

        public CombatActor Player { get; private set; }

        public IReadOnlyList<CombatActor> Enemies
        {
            get { return _enemies; }
        }

        public IReadOnlyList<CombatEvent> Events
        {
            get { return _events; }
        }

        public CombatPhase Phase { get; private set; }

        public int Round { get; private set; }

        public bool IsFinished
        {
            get { return Phase == CombatPhase.Victory || Phase == CombatPhase.Defeat; }
        }

        public ItemCatalog Catalog
        {
            get { return _catalog; }
        }

        public void Start()
        {
            if (Phase != CombatPhase.NotStarted)
            {
                throw new InvalidOperationException("Combat has already started.");
            }

            Round = 1;
            Phase = CombatPhase.PlayerTurn;
            Emit(CombatEventType.CombatStarted, Player.Id, null, null, 0, 0, "战斗开始");
            StartActorTurn(Player);
        }

        public void EndTurn()
        {
            if (Phase != CombatPhase.PlayerTurn)
            {
                return;
            }

            EndActorTurn(Player);
            if (IsFinished || CheckVictory())
            {
                return;
            }

            Phase = CombatPhase.EnemyTurn;

            for (int i = 0; i < _enemies.Count; i++)
            {
                CombatActor enemy = _enemies[i];
                if (!enemy.IsAlive)
                {
                    continue;
                }

                _activeEnemy = enemy;
                StartActorTurn(enemy);
                if (enemy.IsAlive)
                {
                    RunBrain(enemy);
                    EndActorTurn(enemy);
                }

                _activeEnemy = null;

                if (!Player.IsAlive)
                {
                    EndCombat(CombatPhase.Defeat);
                    return;
                }

                if (IsFinished || CheckVictory())
                {
                    return;
                }
            }

            if (CheckVictory())
            {
                return;
            }

            Round++;
            Phase = CombatPhase.PlayerTurn;
            StartActorTurn(Player);
        }

        public bool TryUseItem(CombatActor actor, int instanceId, CombatActor target, out string error)
        {
            if (!CanUseItem(actor, GetItem(actor, instanceId), out error))
            {
                return false;
            }

            PlacedItem item;
            actor.Bag.TryGet(instanceId, out item);
            ItemDefinition definition = _catalog.Get(item.DefinitionId);

            if (definition.Target == ItemTarget.Enemy)
            {
                if (target == null || !target.IsAlive)
                {
                error = "需要活着的目标。";
                return false;
            }

            if (actor.IsPlayer == target.IsPlayer)
            {
                error = "目标必须在敌方。";
                return false;
            }
        }
        else if (definition.Target == ItemTarget.Self)
        {
            if (target != null && target != actor)
            {
                error = "该物品只能对自己使用。";
                return false;
            }
        }

            CombatActor actualTarget = definition.Target == ItemTarget.Enemy ? target : actor;

            int cost = GetEnergyCost(actor, item);
            actor.Energy -= cost;
            actor.MarkUsed(instanceId);

            Emit(CombatEventType.ItemUsed, actor.Id, actualTarget.Id, definition.Id, cost, 0, actor.DisplayName + "使用 " + definition.DisplayName);

            if (definition.UsesAmmo)
            {
                actor.Bag.TryRemoveAmount(definition.AmmoItemId, definition.AmmoPerUse);
                Emit(CombatEventType.AmmoConsumed, actor.Id, actualTarget.Id, definition.Id, definition.AmmoPerUse, 0, "消耗 " + definition.AmmoPerUse + " 弹药");
            }

            int damage = GetDamage(actor, item);
            if (damage > 0 && definition.Target == ItemTarget.Enemy)
            {
                ApplyDamage(actualTarget, damage, actor, definition.Type);
                if (IsFinished)
                {
                    error = null;
                    return true;
                }
            }

            int block = GetShield(actor, item);
            if (block > 0)
            {
                actor.Shield += block;
                Emit(CombatEventType.ShieldGained, actor.Id, actor.Id, definition.Id, block, 0, actor.DisplayName + "获得 " + block + " 护盾");
            }

            if (definition.Heal > 0)
            {
                int healed = actualTarget.Heal(definition.Heal);
                if (healed > 0)
                {
                    Emit(CombatEventType.Healed, actor.Id, actualTarget.Id, definition.Id, healed, 0, actualTarget.DisplayName + "恢复 " + healed + " 生命");
                }
            }

            if (definition.AppliedStatus != StatusType.None)
            {
                actualTarget.ApplyStatus(definition.AppliedStatus, definition.StatusTurns, definition.StatusStacks);
                Emit(CombatEventType.StatusApplied, actor.Id, actualTarget.Id, definition.Id, definition.StatusStacks, 0, actualTarget.DisplayName + "获得 " + ItemText.StatusName(definition.AppliedStatus) + "×" + definition.StatusStacks);
            }

            int cooldown = GetCooldownAfterUse(actor, item);
            if (cooldown > 0)
            {
                actor.SetCooldown(instanceId, cooldown);
            }

            if (definition.Category == ItemCategory.Weapon
                || (definition.Category == ItemCategory.Armor && block > 0))
            {
                ConsumeDurability(actor, item, definition);
            }

            if (definition.ConsumeOnUse)
            {
                actor.ClearCooldown(instanceId);
                actor.Bag.Remove(instanceId);
                Emit(CombatEventType.ItemConsumed, actor.Id, actualTarget.Id, definition.Id, 0, 0, definition.DisplayName + " 已消耗");
            }

            CheckVictory();
            error = null;
            return true;
        }

        public bool CanUseItem(CombatActor actor, PlacedItem item, out string error)
        {
            if (IsFinished)
            {
                error = "战斗已结束。";
                return false;
            }

            if (actor == null)
            {
                error = "没有角色。";
                return false;
            }

            if (item == null)
            {
                error = "没有物品。";
                return false;
            }

            if (!actor.IsAlive)
            {
                error = "角色已倒下。";
                return false;
            }

            if (!IsActorsTurn(actor))
            {
                error = "不是该角色的回合。";
                return false;
            }

            ItemDefinition definition;
            if (!_catalog.TryGet(item.DefinitionId, out definition))
            {
                error = "未知物品定义。";
                return false;
            }

            if (!definition.IsActive && definition.Shield <= 0)
            {
                error = "被动物品无法使用。";
                return false;
            }

            if (actor.HasUsedThisTurn(item.InstanceId))
            {
                error = "本回合已经使用过。";
                return false;
            }

            if (actor.IsOnCooldown(item.InstanceId))
            {
                error = "物品还在冷却。";
                return false;
            }

            if (actor.Energy < GetEnergyCost(actor, item))
            {
                error = "能量不足。";
                return false;
            }

            if (definition.UsesAmmo && actor.Bag.CountOf(definition.AmmoItemId) < definition.AmmoPerUse)
            {
                error = "弹药不足。";
                return false;
            }

            error = null;
            return true;
        }

        public bool CanUseItemPreview(CombatActor actor, PlacedItem item, out string error)
        {
            if (IsFinished)
            {
                error = "战斗已结束。";
                return false;
            }

            if (actor == null || item == null)
            {
                error = "没有角色或物品。";
                return false;
            }

            if (!actor.IsAlive)
            {
                error = "角色已倒下。";
                return false;
            }

            ItemDefinition definition;
            if (!_catalog.TryGet(item.DefinitionId, out definition))
            {
                error = "未知物品定义。";
                return false;
            }

            if (!definition.IsActive && definition.Shield <= 0)
            {
                error = "被动物品无法使用。";
                return false;
            }

            if (actor.GetCooldown(item.InstanceId) > 1)
            {
                error = "物品仍会处于冷却。";
                return false;
            }

            int projectedEnergy = actor.BaseEnergy + GetPassiveEnergy(actor) - actor.GetStatusStacks(StatusType.Overload);
            if (projectedEnergy < GetEnergyCost(actor, item))
            {
                error = "下回合能量不足。";
                return false;
            }

            if (definition.UsesAmmo && actor.Bag.CountOf(definition.AmmoItemId) < definition.AmmoPerUse)
            {
                error = "弹药不足。";
                return false;
            }

            error = null;
            return true;
        }

        public PlacedItem GetItem(CombatActor actor, int instanceId)
        {
            if (actor == null)
            {
                return null;
            }

            PlacedItem item;
            return actor.Bag.TryGet(instanceId, out item) ? item : null;
        }

        public ItemDefinition GetDefinition(PlacedItem item)
        {
            if (item == null)
            {
                return null;
            }

            ItemDefinition definition;
            return _catalog.TryGet(item.DefinitionId, out definition) ? definition : null;
        }

        public int GetDamage(CombatActor actor, PlacedItem item)
        {
            ItemDefinition definition = GetDefinition(item);
            if (definition == null)
            {
                return 0;
            }

            SynergyBonus bonus = GetSynergyBonus(actor, item.InstanceId);
            int damage = definition.Damage + bonus.Damage;
            if (definition.ShieldToDamageDivisor > 0 && actor != null)
            {
                damage += actor.Shield / definition.ShieldToDamageDivisor;
            }

            if (actor != null)
            {
                damage += actor.GetStatusStacks(StatusType.Overclock);
            }

            return Math.Max(0, damage);
        }

        public int GetArmor(CombatActor actor, DamageType type)
        {
            if (actor == null)
            {
                return 0;
            }

            int total = 0;
            IReadOnlyList<PlacedItem> items = actor.Bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition = GetDefinition(items[i]);
                if (definition != null && definition.Activation == ItemActivation.Passive)
                {
                    total += definition.GetArmor(type);
                }
            }

            return total;
        }

        public int GetDamageAfterArmor(CombatActor target, int rawDamage, DamageType type)
        {
            if (target == null)
            {
                return rawDamage;
            }

            int amount = rawDamage + target.GetStatusStacks(StatusType.ShortCircuit) * 2;
            return ArmorMath.DamageAfterArmor(amount, GetArmor(target, type));
        }

        public int GetDamageAfterShieldAndArmor(CombatActor target, int rawDamage, DamageType type)
        {
            if (target == null)
            {
                return rawDamage;
            }

            int amount = rawDamage + target.GetStatusStacks(StatusType.ShortCircuit) * 2;
            int afterShield = Math.Max(0, amount - target.Shield);
            return ArmorMath.DamageAfterArmor(afterShield, GetArmor(target, type));
        }

        public int GetShield(CombatActor actor, PlacedItem item)
        {
            ItemDefinition definition = GetDefinition(item);
            if (definition == null)
            {
                return 0;
            }

            SynergyBonus bonus = GetSynergyBonus(actor, item.InstanceId);
            int shield = definition.Shield + bonus.Shield;
            if (actor != null)
            {
                shield += actor.GetStatusStacks(StatusType.Coolant);
            }

            return Math.Max(0, shield);
        }

        public int GetEnergyCost(CombatActor actor, PlacedItem item)
        {
            ItemDefinition definition = GetDefinition(item);
            return definition == null ? 0 : Math.Max(0, definition.EnergyCost);
        }

        public int GetAmmoCount(CombatActor actor, ItemDefinition definition)
        {
            if (actor == null || definition == null || !definition.UsesAmmo)
            {
                return 0;
            }

            return actor.Bag.CountOf(definition.AmmoItemId);
        }

        public int GetCooldownAfterUse(CombatActor actor, PlacedItem item)
        {
            ItemDefinition definition = GetDefinition(item);
            if (definition == null || definition.Cooldown <= 0)
            {
                return 0;
            }

            SynergyBonus bonus = GetSynergyBonus(actor, item.InstanceId);
            int remaining = definition.Cooldown - bonus.CooldownReduction;
            return remaining <= 0 ? 0 : remaining + 1;
        }

        public int GetPassiveEnergy(CombatActor actor)
        {
            Dictionary<int, SynergyBonus> bonuses = SynergyResolver.Resolve(actor.Bag, _catalog, _synergyRules);
            int total = 0;

            IReadOnlyList<PlacedItem> items = actor.Bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition;
                if (!_catalog.TryGet(items[i].DefinitionId, out definition) || definition.Activation != ItemActivation.Passive)
                {
                    continue;
                }

                total += definition.BonusEnergy + SynergyResolver.Get(bonuses, items[i].InstanceId).Energy;
            }

            return total;
        }

        public int GetPassiveShield(CombatActor actor)
        {
            Dictionary<int, SynergyBonus> bonuses = SynergyResolver.Resolve(actor.Bag, _catalog, _synergyRules);
            int total = 0;

            IReadOnlyList<PlacedItem> items = actor.Bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition;
                if (!_catalog.TryGet(items[i].DefinitionId, out definition) || definition.Activation != ItemActivation.Passive)
                {
                    continue;
                }

                total += definition.Shield + SynergyResolver.Get(bonuses, items[i].InstanceId).Shield;
            }

            return total;
        }

        public CombatActor FindActor(string actorId)
        {
            if (actorId == null)
            {
                return null;
            }

            if (Player.Id == actorId)
            {
                return Player;
            }

            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].Id == actorId)
                {
                    return _enemies[i];
                }
            }

            return null;
        }

        private bool IsActorsTurn(CombatActor actor)
        {
            if (actor == Player)
            {
                return Phase == CombatPhase.PlayerTurn;
            }

            return Phase == CombatPhase.EnemyTurn && _activeEnemy == actor;
        }

        private void StartActorTurn(CombatActor actor)
        {
            actor.TickCooldowns();
            actor.ResetTurnUsage();
            ApplyTurnStartCleanses(actor);

            int ap = actor.BaseEnergy + GetPassiveEnergy(actor);
            int overload = actor.GetStatusStacks(StatusType.Overload);
            if (overload > 0)
            {
                ap -= overload;
                actor.ConsumeStatus(StatusType.Overload);
            }

            actor.Energy = Math.Max(0, ap);

            if (actor.HasStatus(StatusType.Cache))
            {
                actor.ConsumeStatus(StatusType.Cache);
            }
            else
            {
                actor.Shield = 0;
            }

            ApplyTurnStartBuffs(actor);
            ApplyTurnStartItemStatuses(actor);

            if (!actor.IsAlive)
            {
                if (actor.IsPlayer)
                {
                    EndCombat(CombatPhase.Defeat);
                }

                return;
            }

            Emit(CombatEventType.TurnStarted, actor.Id, null, null, actor.Energy, actor.Shield, actor.DisplayName + "的回合");
        }

        private void EndActorTurn(CombatActor actor)
        {
            int corrosion = actor.GetStatusStacks(StatusType.Corrosion);
            if (corrosion > 0 && actor.IsAlive)
            {
                ApplyDirectDamage(actor, corrosion, actor, DamageType.Toxin, "腐蚀");
            }

            actor.TickStatuses();
            Emit(CombatEventType.TurnEnded, actor.Id, null, null, 0, 0, actor.DisplayName + "结束回合");
        }

        private void ApplyTurnStartCleanses(CombatActor actor)
        {
            IReadOnlyList<PlacedItem> items = actor.Bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition = GetDefinition(items[i]);
                if (definition == null
                    || definition.Activation != ItemActivation.Passive
                    || definition.TurnStartCleanseStatus == StatusType.None)
                {
                    continue;
                }

                int removed = actor.ConsumeStatusStacks(
                    definition.TurnStartCleanseStatus,
                    Math.Max(1, definition.TurnStartCleanseStacks));
                if (removed > 0)
                {
                    Emit(
                        CombatEventType.StatusConsumed,
                        actor.Id,
                        actor.Id,
                        definition.Id,
                        removed,
                        0,
                        actor.DisplayName + "清除了 " + removed + " 层" + ItemText.StatusName(definition.TurnStartCleanseStatus));
                }
            }
        }

        private void ApplyTurnStartItemStatuses(CombatActor actor)
        {
            IReadOnlyList<PlacedItem> items = actor.Bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                ItemDefinition definition = GetDefinition(items[i]);
                if (definition == null
                    || definition.Activation != ItemActivation.Passive
                    || definition.TurnStartSelfStatus == StatusType.None)
                {
                    continue;
                }

                int stacks = Math.Max(1, definition.TurnStartSelfStatusStacks);
                actor.ApplyStatus(
                    definition.TurnStartSelfStatus,
                    Math.Max(1, definition.TurnStartSelfStatusTurns),
                    stacks);
                Emit(
                    CombatEventType.StatusApplied,
                    actor.Id,
                    actor.Id,
                    definition.Id,
                    stacks,
                    0,
                    actor.DisplayName + "获得 " + ItemText.StatusName(definition.TurnStartSelfStatus) + "×" + stacks);
            }
        }

        private void ApplyTurnStartBuffs(CombatActor actor)
        {
            int overheat = actor.GetStatusStacks(StatusType.Overheat);
            if (overheat > 0)
            {
                ApplyDamage(actor, overheat * 2, actor, DamageType.Burn);
            }

            if (!actor.IsAlive)
            {
                return;
            }

            int repair = actor.GetStatusStacks(StatusType.Nanorepair);
            if (repair > 0)
            {
                int healed = actor.Heal(repair);
                if (healed > 0)
                {
                    Emit(CombatEventType.Healed, actor.Id, actor.Id, null, healed, 0, actor.DisplayName + "恢复 " + healed + " 生命");
                }
            }
        }

        private void RunBrain(CombatActor enemy)
        {
            if (enemy.Brain == null)
            {
                return;
            }

            int guard = 0;
            while (enemy.IsAlive && !IsFinished && guard < 16)
            {
                guard++;

                CombatAction action = enemy.Brain.Decide(this, enemy);
                if (action.Type == CombatActionType.EndTurn)
                {
                    break;
                }

                string error;
                if (!TryUseItem(enemy, action.ItemInstanceId, FindActor(action.TargetActorId), out error))
                {
                    break;
                }
            }
        }

        public int GetMaxDurability(PlacedItem item)
        {
            ItemDefinition definition = GetDefinition(item);
            return definition == null ? 0 : definition.DurabilityMax;
        }

        public int GetDurability(PlacedItem item)
        {
            return ItemDurability.Current(item, GetDefinition(item));
        }

        public void ConsumeDurability(CombatActor actor, PlacedItem item, ItemDefinition definition)
        {
            if (actor == null || item == null || definition == null || definition.DurabilityMax <= 0)
            {
                return;
            }

            int current = ItemDurability.Current(item, definition);
            current--;
            if (current <= 0)
            {
                actor.ClearCooldown(item.InstanceId);
                actor.Bag.Remove(item.InstanceId);
                Emit(
                    CombatEventType.ItemBroken,
                    actor.Id,
                    actor.Id,
                    definition.Id,
                    0,
                    0,
                    definition.DisplayName + " 已经损坏");
                return;
            }

            item.Durability = current;
            Emit(
                CombatEventType.DurabilityLost,
                actor.Id,
                actor.Id,
                definition.Id,
                current,
                0,
                definition.DisplayName + " 耐久 " + current + "/" + definition.DurabilityMax);
        }

        private void DamageArmorDurability(CombatActor target)
        {
            if (target == null)
            {
                return;
            }

            IReadOnlyList<PlacedItem> items = target.Bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                PlacedItem item = items[i];
                ItemDefinition definition = GetDefinition(item);
                if (definition == null || definition.Category != ItemCategory.Armor)
                {
                    continue;
                }

                ConsumeDurability(target, item, definition);
            }
        }

        private void ApplyDamage(CombatActor target, int rawDamage, CombatActor source, DamageType type)
        {
            int raw = rawDamage + target.GetStatusStacks(StatusType.ShortCircuit) * 2;
            if (raw <= 0)
            {
                return;
            }

            if (target.GetStatusStacks(StatusType.Evasion) > 0)
            {
                target.ConsumeStatusStacks(StatusType.Evasion, 1);
                Emit(
                    CombatEventType.Damage,
                    source == null ? target.Id : source.Id,
                    target.Id,
                    null,
                    0,
                    0,
                    target.DisplayName + "闪避了攻击");
                return;
            }

            int shieldAbsorbed = Math.Min(target.Shield, raw);
            target.Shield -= shieldAbsorbed;
            int afterShield = Math.Max(0, raw - shieldAbsorbed);
            int armor = GetArmor(target, type);
            int hpLoss = ArmorMath.DamageAfterArmor(afterShield, armor);
            target.Hp = Math.Max(0, target.Hp - hpLoss);

            string message = target.DisplayName + "受到 " + hpLoss + " 点"
                             + ItemText.DamageTypeName(type) + "伤害";
            if (shieldAbsorbed > 0)
            {
                message += "（护盾吸收 " + shieldAbsorbed + "）";
            }

            if (armor > 0)
            {
                message += "（护甲减免 " + ArmorMath.ReductionPercent(armor) + "%）";
            }

            Emit(
                CombatEventType.Damage,
                source == null ? target.Id : source.Id,
                target.Id,
                null,
                hpLoss,
                shieldAbsorbed,
                message);
            DamageArmorDurability(target);

            if (!target.IsAlive)
            {
                Emit(CombatEventType.ActorDied, target.Id, null, null, 0, 0, target.DisplayName + "倒下了");
                if (target.IsPlayer)
                {
                    EndCombat(CombatPhase.Defeat);
                }
            }
        }

        private void ApplyDirectDamage(
            CombatActor target,
            int amount,
            CombatActor source,
            DamageType type,
            string label)
        {
            if (target == null || amount <= 0 || !target.IsAlive)
            {
                return;
            }

            int before = target.Hp;
            target.Hp = Math.Max(0, target.Hp - amount);
            int lost = before - target.Hp;
            if (lost <= 0)
            {
                return;
            }

            Emit(
                CombatEventType.Damage,
                source == null ? target.Id : source.Id,
                target.Id,
                null,
                lost,
                0,
                target.DisplayName + "受到 " + lost + " 点" + label + "伤害");
            if (!target.IsAlive)
            {
                Emit(CombatEventType.ActorDied, target.Id, null, null, 0, 0, target.DisplayName + "倒下了");
                if (target.IsPlayer)
                {
                    EndCombat(CombatPhase.Defeat);
                }
            }
        }

        private bool CheckVictory()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].IsAlive)
                {
                    return false;
                }
            }

            EndCombat(CombatPhase.Victory);
            return true;
        }

        private void EndCombat(CombatPhase phase)
        {
            if (IsFinished)
            {
                return;
            }

            Phase = phase;
            Emit(CombatEventType.CombatEnded, Player.Id, null, null, 0, 0, phase == CombatPhase.Victory ? "胜利" : "失败");
        }

        public SynergyBonus GetSynergyBonus(CombatActor actor, int instanceId)
        {
            if (actor == null)
            {
                return default(SynergyBonus);
            }

            Dictionary<int, SynergyBonus> bonuses = SynergyResolver.Resolve(actor.Bag, _catalog, _synergyRules);
            return SynergyResolver.Get(bonuses, instanceId);
        }

        private void Emit(CombatEventType type, string actorId, string targetId, string itemId, int amount, int blocked, string message)
        {
            _events.Add(new CombatEvent
            {
                Type = type,
                ActorId = actorId,
                TargetId = targetId,
                ItemId = itemId,
                Amount = amount,
                Shielded = blocked,
                Message = message
            });
        }
    }
}
