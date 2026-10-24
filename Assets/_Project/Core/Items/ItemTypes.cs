using System;

namespace BitRunner.Core
{
    public enum ItemCategory
    {
        Weapon,
        Armor,
        Tool,
        Consumable,
        Loot,
        Material,
        Key,
        Currency,
        Ammo,
        Action,
        Container
    }

    public enum ItemActivation
    {
        Active,
        Passive
    }

    public enum ItemTarget
    {
        None,
        Self,
        Enemy
    }

    public enum DamageType
    {
        Melee,
        Projectile,
        Burn,
        Toxin,
        Asphyxiation
    }

    public enum StatusType
    {
        None,
        Overload,
        ShortCircuit,
        Cache,
        Overclock,
        Coolant,
        Corrosion,
        Overheat,
        Nanorepair,
        Evasion
    }

    public enum SynergyEffect
    {
        Damage,
        Shield,
        Energy,
        CooldownReduction
    }

    public enum SynergyCondition
    {
        Adjacent,
        SameRow,
        SameColumn
    }

    public enum CombatPhase
    {
        NotStarted,
        PlayerTurn,
        EnemyTurn,
        Victory,
        Defeat
    }

    [Flags]
    public enum ItemTag
    {
        None = 0,
        Blade = 1 << 0,
        Shield = 1 << 1,
        Armor = 1 << 2,
        Chip = 1 << 3,
        Tool = 1 << 4,
        Consumable = 1 << 5,
        Data = 1 << 6,
        Mechanical = 1 << 7,
        Projectile = 1 << 8,
        Energy = 1 << 9,
        Toxin = 1 << 10,
        Cooling = 1 << 11,
        Salvage = 1 << 12,
        Drone = 1 << 13,
        Targeting = 1 << 14,
        Container = 1 << 15
    }
}
