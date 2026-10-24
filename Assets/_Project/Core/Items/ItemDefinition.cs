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
        public ContainerSpec ContainerSpec;

        public int EnergyCost = 1;
        public int Cooldown;

        public int Damage;
        public DamageType Type = DamageType.Melee;
        public int Shield;
        public int MaxDurability;
        public int Heal;
        public int BonusEnergy;

        public int ArmorMelee;
        public int ArmorProjectile;
        public int ArmorBurn;
        public int ArmorToxin;
        public int ArmorAsphyxiation;

        public StatusType AppliedStatus = StatusType.None;
        public int StatusTurns = 2;
        public int StatusStacks = 1;

        public StatusType TurnStartSelfStatus = StatusType.None;
        public int TurnStartSelfStatusTurns = 1;
        public int TurnStartSelfStatusStacks = 1;
        public StatusType TurnStartCleanseStatus = StatusType.None;
        public int TurnStartCleanseStacks = 1;

        public int ShieldToDamageDivisor;

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

        public int DurabilityMax
        {
            get
            {
                if (MaxDurability > 0)
                {
                    return MaxDurability;
                }

                return HasDurability ? 8 : 0;
            }
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
