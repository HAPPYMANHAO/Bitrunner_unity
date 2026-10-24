using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class ItemCatalog
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 5;

        /// <summary>
        /// 单件物品的词条条数硬上限。T5 允许 2–4 条，4 就是这条硬线；
        /// 每档的建议条数见 <see cref="MaxTraitsForTier"/>。
        /// </summary>
        public const int HardTraitLimit = 4;

        private readonly Dictionary<string, ItemDefinition> _definitions = new Dictionary<string, ItemDefinition>();

        public ItemCatalog(IEnumerable<ItemDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            foreach (ItemDefinition definition in definitions)
            {
                if (definition == null)
                {
                    throw new ArgumentException("Item definitions cannot be null.", nameof(definitions));
                }

                if (string.IsNullOrEmpty(definition.Id))
                {
                    throw new ArgumentException("Every item definition needs an Id.", nameof(definitions));
                }

                if (definition.Shape == null)
                {
                    throw new ArgumentException("Item '" + definition.Id + "' needs a shape.", nameof(definitions));
                }

                if (HasDuplicateTraits(definition))
                {
                    throw new ArgumentException("Item '" + definition.Id + "' lists the same trait twice.", nameof(definitions));
                }

                if (definition.Level < MinLevel || definition.Level > MaxLevel)
                {
                    throw new ArgumentException(
                        "Item '" + definition.Id + "' has level " + definition.Level + ", outside " + MinLevel + "–" + MaxLevel + ".",
                        nameof(definitions));
                }

                if (!HasValidTraits(definition))
                {
                    throw new ArgumentException(
                        "Item '" + definition.Id + "' has a trait rank above its tier or too many traits.",
                        nameof(definitions));
                }

                if (HasFreeEnergy(definition))
                {
                    throw new ArgumentException(
                        "Item '" + definition.Id + "' raises base energy without a cost. "
                        + "增加能量必定有副作用（参考《杀戮尖塔》的能量遗物）。",
                        nameof(definitions));
                }

                if (_definitions.ContainsKey(definition.Id))
                {
                    throw new ArgumentException("Duplicate item id '" + definition.Id + "'.", nameof(definitions));
                }

                _definitions.Add(definition.Id, definition);
            }
        }

        public int Count
        {
            get { return _definitions.Count; }
        }

        public IEnumerable<ItemDefinition> Definitions
        {
            get { return _definitions.Values; }
        }

        public bool TryGet(string id, out ItemDefinition definition)
        {
            return _definitions.TryGetValue(id, out definition);
        }

        public ItemDefinition Get(string id)
        {
            ItemDefinition definition;
            if (!_definitions.TryGetValue(id, out definition))
            {
                throw new KeyNotFoundException("Unknown item id '" + id + "'.");
            }

            return definition;
        }

        /// <summary>
        /// 每档建议的词条条数上限（内容编写用，不是硬校验）：
        /// T1 1 条、T2 1–2 条、T3 2 条、T4 2–3 条、T5 2–4 条。
        /// 硬校验只拦「rank 超过 tier」和「超过 <see cref="HardTraitLimit"/>」。
        /// </summary>
        public static int MaxTraitsForTier(int tier)
        {
            if (tier <= 1)
            {
                return 1;
            }

            if (tier == 2)
            {
                return 2;
            }

            if (tier == 3)
            {
                return 2;
            }

            return tier == 4 ? 3 : HardTraitLimit;
        }

        private static bool HasValidTraits(ItemDefinition definition)
        {
            if (definition.Traits == null || definition.Traits.Length == 0)
            {
                return true;
            }

            if (definition.Traits.Length > HardTraitLimit)
            {
                return false;
            }

            for (int i = 0; i < definition.Traits.Length; i++)
            {
                int rank = definition.Traits[i].Rank;
                if (rank < 1 || rank > MaxLevel || rank > definition.Level)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 「增加能量必定有副作用」——参考《杀戮尖塔》的能量遗物，白给的能量会让
        /// 「每回合多打一次」变成无脑最优解，行动选择的空间被抹平。
        ///
        /// 判定范围只覆盖 <see cref="TraitEffect.AddBaseEnergy"/>（CPU 能量芯片）：
        /// 背包内的 <see cref="TraitEffect.AddEnergy"/> 走物品本身的价值与耐久来平衡，
        /// 例如超频核心（能量 +2 / 每回合 1 层过载）——那已经是副作用了。
        /// CPU 芯片没有每回合的钩子可用，所以代价统一落在生命上限上。
        /// </summary>
        private static bool HasFreeEnergy(ItemDefinition definition)
        {
            if (definition.Traits == null || definition.Traits.Length == 0)
            {
                return false;
            }

            bool grantsEnergy = false;
            bool paysCost = false;
            for (int i = 0; i < definition.Traits.Length; i++)
            {
                TraitDefinition trait;
                if (!TraitCatalog.Default.TryGet(definition.Traits[i].TraitId, out trait))
                {
                    continue;
                }

                int amount = trait.AmountAtRank(definition.Traits[i].Rank);
                if (trait.Effect == TraitEffect.AddBaseEnergy && amount > 0)
                {
                    grantsEnergy = true;
                }
                else if (trait.Effect == TraitEffect.AddMaxHp && amount < 0)
                {
                    paysCost = true;
                }
            }

            return grantsEnergy && !paysCost;
        }

        private static bool HasDuplicateTraits(ItemDefinition definition)
        {
            if (definition.Traits == null || definition.Traits.Length < 2)
            {
                return false;
            }

            for (int i = 0; i < definition.Traits.Length; i++)
            {
                for (int j = i + 1; j < definition.Traits.Length; j++)
                {
                    if (definition.Traits[i].TraitId == definition.Traits[j].TraitId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
