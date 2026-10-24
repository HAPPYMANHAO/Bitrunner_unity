using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class StatusEffect
    {
        public StatusType Type;
        public int Turns;
        public int Stacks;
    }

    public sealed class CombatActor
    {
        private readonly Dictionary<int, int> _cooldowns = new Dictionary<int, int>();
        private readonly HashSet<int> _usedThisTurn = new HashSet<int>();
        private readonly List<StatusEffect> _statuses = new List<StatusEffect>();

        public CombatActor(string id, string displayName, int maxHp, int baseEnergy, BagInventory bag, bool isPlayer)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Actor needs an id.", nameof(id));
            }

            if (bag == null)
            {
                throw new ArgumentNullException(nameof(bag));
            }

            if (maxHp <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHp));
            }

            Id = id;
            DisplayName = displayName;
            MaxHp = maxHp;
            Hp = maxHp;
            BaseEnergy = baseEnergy;
            Energy = baseEnergy;
            Bag = bag;
            IsPlayer = isPlayer;
        }

        public string Id { get; private set; }

        public string DisplayName { get; private set; }

        public int MaxHp { get; private set; }

        public int Hp { get; internal set; }

        public int BaseEnergy { get; private set; }

        public int Energy { get; internal set; }

        public int Shield { get; internal set; }

        public BagInventory Bag { get; private set; }

        public bool IsPlayer { get; private set; }

        public ICombatBrain Brain { get; set; }

        public bool IsAlive
        {
            get { return Hp > 0; }
        }

        public IReadOnlyList<StatusEffect> Statuses
        {
            get { return _statuses; }
        }

        public int GetCooldown(int instanceId)
        {
            int value;
            return _cooldowns.TryGetValue(instanceId, out value) ? value : 0;
        }

        public bool IsOnCooldown(int instanceId)
        {
            return GetCooldown(instanceId) > 0;
        }

        public void SetCooldown(int instanceId, int turns)
        {
            if (turns <= 0)
            {
                _cooldowns.Remove(instanceId);
            }
            else
            {
                _cooldowns[instanceId] = turns;
            }
        }

        public void ClearCooldown(int instanceId)
        {
            _cooldowns.Remove(instanceId);
        }

        public void TickCooldowns()
        {
            var keys = new List<int>(_cooldowns.Keys);
            keys.Sort();

            for (int i = 0; i < keys.Count; i++)
            {
                int key = keys[i];
                int value = _cooldowns[key];
                if (value <= 1)
                {
                    _cooldowns.Remove(key);
                }
                else
                {
                    _cooldowns[key] = value - 1;
                }
            }
        }

        public bool HasUsedThisTurn(int instanceId)
        {
            return _usedThisTurn.Contains(instanceId);
        }

        public void MarkUsed(int instanceId)
        {
            _usedThisTurn.Add(instanceId);
        }

        public void ResetTurnUsage()
        {
            _usedThisTurn.Clear();
        }

        public int GetStatusStacks(StatusType type)
        {
            for (int i = 0; i < _statuses.Count; i++)
            {
                if (_statuses[i].Type == type)
                {
                    return _statuses[i].Stacks;
                }
            }

            return 0;
        }

        public bool HasStatus(StatusType type)
        {
            return GetStatusStacks(type) > 0;
        }

        public void ApplyStatus(StatusType type, int turns, int stacks)
        {
            if (type == StatusType.None || turns <= 0 || stacks <= 0)
            {
                return;
            }

            BuffDefinition definition = BuffLibrary.Get(type);
            if (definition != null)
            {
                if (definition.StackMode == BuffStackMode.Unique)
                {
                    stacks = 1;
                }

                stacks = Math.Min(stacks, definition.MaxStacks);
            }

            for (int i = 0; i < _statuses.Count; i++)
            {
                if (_statuses[i].Type == type)
                {
                    _statuses[i].Turns = Math.Max(_statuses[i].Turns, turns);
                    _statuses[i].Stacks = Math.Min(
                        _statuses[i].Stacks + stacks,
                        definition == null ? int.MaxValue : definition.MaxStacks);
                    return;
                }
            }

            _statuses.Add(new StatusEffect { Type = type, Turns = turns, Stacks = stacks });
        }

        public void ConsumeStatus(StatusType type)
        {
            for (int i = _statuses.Count - 1; i >= 0; i--)
            {
                if (_statuses[i].Type == type)
                {
                    _statuses.RemoveAt(i);
                }
            }
        }

        public int ConsumeStatusStacks(StatusType type, int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            for (int i = 0; i < _statuses.Count; i++)
            {
                if (_statuses[i].Type != type)
                {
                    continue;
                }

                int consumed = Math.Min(_statuses[i].Stacks, amount);
                _statuses[i].Stacks -= consumed;
                if (_statuses[i].Stacks <= 0)
                {
                    _statuses.RemoveAt(i);
                }

                return consumed;
            }

            return 0;
        }

        public void TickStatuses()
        {
            for (int i = _statuses.Count - 1; i >= 0; i--)
            {
                if (!BuffLibrary.Decays(_statuses[i].Type))
                {
                    continue;
                }

                _statuses[i].Turns--;
                if (_statuses[i].Turns <= 0)
                {
                    _statuses.RemoveAt(i);
                }
            }
        }

        public int Heal(int amount)
        {
            if (amount <= 0 || !IsAlive)
            {
                return 0;
            }

            int before = Hp;
            Hp = Math.Min(MaxHp, Hp + amount);
            return Hp - before;
        }

        public void SetHp(int value)
        {
            Hp = Math.Max(0, Math.Min(MaxHp, value));
        }

        public void SetShield(int value)
        {
            Shield = Math.Max(0, value);
        }

        public void ResetForCombat()
        {
            Hp = MaxHp;
            Energy = BaseEnergy;
            Shield = 0;
            _cooldowns.Clear();
            _usedThisTurn.Clear();
            _statuses.Clear();
        }
    }
}
