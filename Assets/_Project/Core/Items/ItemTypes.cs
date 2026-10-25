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
        Container,

        /// <summary>
        /// 通讯上行链路：只能插进通讯中枢，代表「一条被占用的信道」。
        ///
        /// **刻意和 <see cref="Key"/> 分开**。ContainerSpec 只有 AllowedCategories /
        /// RequiredAnyTags / AllowNestedContainers 三重白名单过滤，没有排除机制——
        /// 如果上行链路也用 Key，就做不到「潜网舱与密钥盒只收关卡密钥」。
        /// 而且 DivePod.EnsureSelection 只按 Category == Key 挑第一个，
        /// 混进去会被当成关卡密钥选中，然后报「未知密钥」。
        /// </summary>
        Uplink
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
        Defeat,
        PlayerRetreat,
        EnemyFled
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
        Container = 1 << 15,

        /// <summary>
        /// 只能插进中央处理器的芯片。刻意和 <see cref="Chip"/> 分开：
        /// 后者是敌人配装里「科技件」槽位的筛选标签，混用会让敌人去装备只对玩家生效的芯片。
        /// </summary>
        CpuChip = 1 << 16
    }
}
