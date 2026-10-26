namespace BitRunner.Core
{
    public struct TagRank
    {
        public ItemTag Tag;
        public int Rank;

        public TagRank(ItemTag tag, int rank)
        {
            Tag = tag;
            Rank = rank;
        }
    }

    public sealed class ItemDefinition
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public ItemCategory Category = ItemCategory.Material;
        public ItemActivation Activation = ItemActivation.Active;
        public ItemTarget Target = ItemTarget.None;
        public ItemShape Shape;
        public ItemTag Tags = ItemTag.None;
        public TagRank[] TagRanks;

        /// <summary>物品等级 1–5。掉落、商店、敌人配装都按它过滤。</summary>
        public int Level = 1;

        /// <summary>
        /// 特殊效果词条。词条 rank 不能超过物品自身等级（由 <see cref="ItemCatalog"/> 强校验），
        /// 单件最多 4 条。
        /// </summary>
        public TraitRef[] Traits;

        public ContainerSpec ContainerSpec;

        /// <summary>探索辅助标志的效果；普通物品为 null。</summary>
        public AssistSpec Assist;

        public int EnergyCost = 1;
        public int Cooldown;

        public int Damage;
        public DamageType Type = DamageType.Melee;
        public int Shield;

        /// <summary>
        /// 最大耐久。必须显式填写：-1 = 未声明（武器/护甲会被 <see cref="ItemCatalog"/> 拒绝），
        /// 0 = 不会损坏（例如野生动物的撕咬），大于 0 = 可消耗的耐久上限。
        /// </summary>
        public int MaxDurability = -1;
        public int Heal;

        public int ArmorMelee;
        public int ArmorProjectile;
        public int ArmorBurn;
        public int ArmorToxin;
        public int ArmorAsphyxiation;

        public bool ConsumeOnUse;
        public int Value;

        public int MaxStack = 1;
        public string AmmoItemId;
        public int AmmoPerUse = 1;

        public bool IsStackable
        {
            get { return MaxStack > 1; }
        }

        public int GetTagRank(ItemTag tag)
        {
            if ((Tags & tag) == 0)
            {
                return 0;
            }

            if (TagRanks != null)
            {
                for (int i = 0; i < TagRanks.Length; i++)
                {
                    if (TagRanks[i].Tag == tag)
                    {
                        return ClampRank(TagRanks[i].Rank);
                    }
                }
            }

            return 1;
        }

        private static int ClampRank(int rank)
        {
            if (rank < 1)
            {
                return 1;
            }

            if (rank > 5)
            {
                return 5;
            }

            return rank;
        }

        public bool HasTrait(string traitId)
        {
            return GetTraitRank(traitId) > 0;
        }

        public int GetTraitRank(string traitId)
        {
            if (Traits == null || string.IsNullOrEmpty(traitId))
            {
                return 0;
            }

            for (int i = 0; i < Traits.Length; i++)
            {
                if (Traits[i].TraitId == traitId)
                {
                    return ClampRank(Traits[i].Rank);
                }
            }

            return 0;
        }

        public bool UsesAmmo
        {
            get { return !string.IsNullOrEmpty(AmmoItemId) && AmmoPerUse > 0; }
        }

        public bool IsActive
        {
            get { return Activation == ItemActivation.Active; }
        }

        public bool IsWeapon
        {
            get { return Category == ItemCategory.Weapon && IsActive; }
        }

        public bool HasDurability
        {
            get { return Category == ItemCategory.Weapon || Category == ItemCategory.Armor; }
        }

        /// <summary>生效的耐久上限；未声明或为 0 时返回 0（不追踪耐久）。不做任何隐式兜底。</summary>
        public int DurabilityMax
        {
            get { return MaxDurability > 0 ? MaxDurability : 0; }
        }

        public int GetArmor(DamageType type)
        {
            switch (type)
            {
                case DamageType.Melee:
                    return ArmorMelee;
                case DamageType.Projectile:
                    return ArmorProjectile;
                case DamageType.Burn:
                    return ArmorBurn;
                case DamageType.Toxin:
                    return ArmorToxin;
                case DamageType.Asphyxiation:
                    return ArmorAsphyxiation;
                default:
                    return 0;
            }
        }
    }
}
