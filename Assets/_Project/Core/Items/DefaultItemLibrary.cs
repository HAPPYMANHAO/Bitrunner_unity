namespace BitRunner.Core
{
    public static class DefaultItemLibrary
    {
        public static ItemCatalog CreateCatalog()
        {
            return new ItemCatalog(new[]
            {
                new ItemDefinition
                {
                    Id = "blade",
                    DisplayName = "刀刃",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Blade,
                    EnergyCost = 1,
                    Damage = 5,
                    Value = 40
                },
                new ItemDefinition
                {
                    Id = "scrap_blade",
                    DisplayName = "废铁刃",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Blade,
                    EnergyCost = 1,
                    Cooldown = 1,
                    Damage = 5,
                    Value = 30
                },
                new ItemDefinition
                {
                    Id = "shiv",
                    DisplayName = "短刺",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Blade,
                    EnergyCost = 1,
                    Damage = 3,
                    MaxDurability = 6,
                    Value = 18
                },
                new ItemDefinition
                {
                    Id = "club",
                    DisplayName = "铁棍",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Blade,
                    EnergyCost = 1,
                    Damage = 4,
                    MaxDurability = 8,
                    Value = 24
                },
                new ItemDefinition
                {
                    Id = "rod",
                    DisplayName = "电击棍",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("#", "#", "#"),
                    Tags = ItemTag.Blade,
                    EnergyCost = 2,
                    Cooldown = 1,
                    Damage = 8,
                    Type = DamageType.Burn,
                    AppliedStatus = StatusType.Overheat,
                    StatusTurns = 2,
                    StatusStacks = 1,
                    Value = 65,
                    Description = "攻击时施加 1 层灼烧（持续 2 回合）。"
                },
                new ItemDefinition
                {
                    Id = "pulse",
                    DisplayName = "脉冲发射器",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Chip | ItemTag.Projectile,
                    EnergyCost = 1,
                    Damage = 4,
                    Type = DamageType.Projectile,
                    AmmoItemId = "ammo",
                    AmmoPerUse = 1,
                    Value = 55
                },
                new ItemDefinition
                {
                    Id = "nail_gun",
                    DisplayName = "钉枪",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Chip | ItemTag.Projectile,
                    EnergyCost = 1,
                    Cooldown = 1,
                    Damage = 3,
                    Type = DamageType.Projectile,
                    AmmoItemId = "ammo",
                    AmmoPerUse = 1,
                    MaxDurability = 6,
                    Value = 45
                },
                new ItemDefinition
                {
                    Id = "plate",
                    DisplayName = "装甲板",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Shield | ItemTag.Armor,
                    EnergyCost = 1,
                    Shield = 3,
                    ArmorMelee = 12,
                    ArmorProjectile = 8,
                    ArmorBurn = 2,
                    ArmorToxin = 1,
                    Value = 50
                },
                new ItemDefinition
                {
                    Id = "scrap_armor",
                    DisplayName = "废铁甲",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Shield | ItemTag.Armor,
                    EnergyCost = 1,
                    Shield = 2,
                    ArmorMelee = 8,
                    ArmorProjectile = 5,
                    ArmorBurn = 2,
                    ArmorToxin = 1,
                    MaxDurability = 8,
                    Value = 35
                },
                new ItemDefinition
                {
                    Id = "heavy_plate",
                    DisplayName = "重型装甲",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Shield | ItemTag.Armor,
                    EnergyCost = 2,
                    Shield = 3,
                    ArmorMelee = 16,
                    ArmorProjectile = 10,
                    ArmorBurn = 4,
                    ArmorToxin = 2,
                    MaxDurability = 10,
                    Value = 75
                },
                new ItemDefinition
                {
                    Id = "scrap_vest",
                    DisplayName = "废铁背心",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Shield,
                    EnergyCost = 1,
                    Shield = 1,
                    ArmorMelee = 5,
                    ArmorProjectile = 4,
                    MaxDurability = 6,
                    Value = 22
                },
                new ItemDefinition
                {
                    Id = "buckler",
                    DisplayName = "偏导器",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Shield,
                    EnergyCost = 1,
                    Shield = 2,
                    ArmorMelee = 3,
                    ArmorProjectile = 10,
                    AppliedStatus = StatusType.Evasion,
                    StatusTurns = 2,
                    StatusStacks = 1,
                    Value = 30,
                    Description = "获得 2 点护盾，并施加 1 层闪避（持续 2 回合）。"
                },
                new ItemDefinition
                {
                    Id = "chip",
                    DisplayName = "超频芯片",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Chip | ItemTag.Energy,
                    EnergyCost = 0,
                    BonusEnergy = 1,
                    Value = 70,
                    Description = "被动：提供 1 点额外能量。"
                },
                new ItemDefinition
                {
                    Id = "jammer",
                    DisplayName = "信号干扰器",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Tool | ItemTag.Chip,
                    EnergyCost = 1,
                    Cooldown = 2,
                    AppliedStatus = StatusType.Overload,
                    StatusTurns = 1,
                    Value = 45
                },
                new ItemDefinition
                {
                    Id = "patch",
                    DisplayName = "修复包",
                    Category = ItemCategory.Consumable,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Self,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Consumable,
                    EnergyCost = 1,
                    Heal = 8,
                    AppliedStatus = StatusType.Nanorepair,
                    StatusTurns = 3,
                    StatusStacks = 2,
                    ConsumeOnUse = true,
                    Value = 25,
                    Description = "恢复 8 点生命，并施加 2 层纳米修复（持续 3 回合）。"
                },
                new ItemDefinition
                {
                    Id = "medkit",
                    DisplayName = "急救包",
                    Category = ItemCategory.Consumable,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Self,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Consumable,
                    EnergyCost = 2,
                    Heal = 12,
                    ConsumeOnUse = true,
                    Value = 45
                },
                new ItemDefinition
                {
                    Id = "heat_suit",
                    DisplayName = "隔热服",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Shield | ItemTag.Armor,
                    EnergyCost = 0,
                    ArmorBurn = 12,
                    ArmorMelee = 2,
                    ArmorToxin = 1,
                    Value = 60
                },
                new ItemDefinition
                {
                    Id = "filter",
                    DisplayName = "滤毒罐",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Shield | ItemTag.Tool,
                    EnergyCost = 0,
                    ArmorToxin = 12,
                    ArmorAsphyxiation = 8,
                    ArmorBurn = 2,
                    Value = 45
                },
                new ItemDefinition
                {
                    Id = "respirator",
                    DisplayName = "呼吸器",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#", "#"),
                    Tags = ItemTag.Shield | ItemTag.Tool,
                    EnergyCost = 0,
                    ArmorAsphyxiation = 12,
                    ArmorToxin = 6,
                    Value = 45
                },
                new ItemDefinition
                {
                    Id = "toxin_dart",
                    DisplayName = "毒素针",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Tool | ItemTag.Chip | ItemTag.Toxin | ItemTag.Projectile,
                    EnergyCost = 1,
                    Cooldown = 1,
                    Damage = 3,
                    Type = DamageType.Toxin,
                    Value = 35
                },
                new ItemDefinition
                {
                    Id = "overclock_core",
                    DisplayName = "超频核心",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Chip | ItemTag.Energy,
                    EnergyCost = 0,
                    BonusEnergy = 2,
                    TurnStartSelfStatus = StatusType.Overload,
                    TurnStartSelfStatusTurns = 1,
                    TurnStartSelfStatusStacks = 1,
                    Value = 95,
                    Description = "能量上限 +2；每回合开始获得 1 层过载（下回合能量 -1）。"
                },
                new ItemDefinition
                {
                    Id = "coolant_loop",
                    DisplayName = "冷却回路",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Tool | ItemTag.Cooling | ItemTag.Mechanical,
                    EnergyCost = 0,
                    TurnStartCleanseStatus = StatusType.Overload,
                    TurnStartCleanseStacks = 1,
                    TurnStartSelfStatus = StatusType.Coolant,
                    TurnStartSelfStatusTurns = 3,
                    TurnStartSelfStatusStacks = 1,
                    Value = 80,
                    Description = "每回合开始清除 1 层过载，并获得 1 层冷却（持续 3 回合）。"
                },
                new ItemDefinition
                {
                    Id = "targeting_array",
                    DisplayName = "靶向阵列",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Chip | ItemTag.Energy | ItemTag.Targeting,
                    EnergyCost = 0,
                    TurnStartSelfStatus = StatusType.Overclock,
                    TurnStartSelfStatusTurns = 3,
                    TurnStartSelfStatusStacks = 1,
                    Value = 75,
                    Description = "战斗每回合开始获得 1 层超频（每层使攻击伤害 +1，持续 3 回合）。"
                },
                new ItemDefinition
                {
                    Id = "ram_piston",
                    DisplayName = "冲压活塞",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Mechanical,
                    EnergyCost = 1,
                    Cooldown = 1,
                    ShieldToDamageDivisor = 2,
                    MaxDurability = 8,
                    Value = 85,
                    Description = "造成等同于当前护盾一半的伤害（每 2 点护盾 +1 伤害）。"
                },
                new ItemDefinition
                {
                    Id = "corrosive_dart",
                    DisplayName = "腐蚀飞针",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Toxin | ItemTag.Projectile | ItemTag.Chip,
                    EnergyCost = 1,
                    Cooldown = 1,
                    Damage = 2,
                    Type = DamageType.Toxin,
                    AppliedStatus = StatusType.Corrosion,
                    StatusTurns = 3,
                    StatusStacks = 2,
                    Value = 65,
                    Description = "造成 2 点毒素伤害，并施加 2 层腐蚀（持续 3 回合）。"
                },
                new ItemDefinition
                {
                    Id = "fence_module",
                    DisplayName = "赃物模块",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Self,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Tool | ItemTag.Salvage,
                    EnergyCost = 1,
                    Cooldown = 1,
                    Shield = 1,
                    Value = 60,
                    Description = "获得 1 点护盾；回收标签可与数据/机械相邻触发额外护盾。"
                },
                new ItemDefinition
                {
                    Id = "weapon_crate",
                    DisplayName = "武器箱",
                    Category = ItemCategory.Container,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##", "##", "##", "##"),
                    Tags = ItemTag.Container | ItemTag.Tool,
                    MaxStack = 1,
                    Value = 120,
                    ContainerSpec = new ContainerSpec
                    {
                        Width = 8,
                        Height = 10,
                        AllowedCategories = new[] { ItemCategory.Weapon }
                    }
                },
                new ItemDefinition
                {
                    Id = "armor_case",
                    DisplayName = "护甲箱",
                    Category = ItemCategory.Container,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##", "##", "##"),
                    Tags = ItemTag.Container | ItemTag.Tool,
                    MaxStack = 1,
                    Value = 100,
                    ContainerSpec = new ContainerSpec
                    {
                        Width = 6,
                        Height = 8,
                        AllowedCategories = new[] { ItemCategory.Armor }
                    }
                },
                new ItemDefinition
                {
                    Id = "parts_case",
                    DisplayName = "零件盒",
                    Category = ItemCategory.Container,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Container | ItemTag.Tool,
                    MaxStack = 1,
                    Value = 60,
                    ContainerSpec = new ContainerSpec
                    {
                        Width = 6,
                        Height = 4,
                        AllowedCategories = new[] { ItemCategory.Material, ItemCategory.Tool }
                    }
                },
                new ItemDefinition
                {
                    Id = "med_pouch",
                    DisplayName = "医疗包",
                    Category = ItemCategory.Container,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Container | ItemTag.Tool,
                    MaxStack = 1,
                    Value = 50,
                    ContainerSpec = new ContainerSpec
                    {
                        Width = 5,
                        Height = 4,
                        AllowedCategories = new[] { ItemCategory.Consumable }
                    }
                },
                new ItemDefinition
                {
                    Id = "vacuum",
                    DisplayName = "真空泵",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Mechanical | ItemTag.Tool,
                    EnergyCost = 2,
                    Cooldown = 2,
                    Damage = 5,
                    Type = DamageType.Asphyxiation,
                    Value = 60
                },
                new ItemDefinition
                {
                    Id = "dive_basic",
                    DisplayName = "基础潜网密钥",
                    Category = ItemCategory.Key,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.None,
                    EnergyCost = 0,
                    Value = 0
                },
                new ItemDefinition
                {
                    Id = "dive_wild",
                    DisplayName = "旷野密钥",
                    Category = ItemCategory.Key,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.None,
                    EnergyCost = 0,
                    Value = 0
                },
                new ItemDefinition
                {
                    Id = "dive_military",
                    DisplayName = "军用潜网密钥",
                    Category = ItemCategory.Key,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.None,
                    EnergyCost = 0,
                    Value = 0
                },
                new ItemDefinition
                {
                    Id = "dive_signal",
                    DisplayName = "信号潜网密钥",
                    Category = ItemCategory.Key,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.None,
                    EnergyCost = 0,
                    Value = 0
                },
                new ItemDefinition
                {
                    Id = "scrap",
                    DisplayName = "废料",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Mechanical | ItemTag.Salvage,
                    EnergyCost = 0,
                    Value = 8
                },
                new ItemDefinition
                {
                    Id = "wire",
                    DisplayName = "导线",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Mechanical | ItemTag.Salvage | ItemTag.Energy,
                    EnergyCost = 0,
                    Value = 12
                },
                new ItemDefinition
                {
                    Id = "circuit",
                    DisplayName = "电路板",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Mechanical | ItemTag.Salvage | ItemTag.Energy,
                    EnergyCost = 0,
                    Value = 24
                },
                new ItemDefinition
                {
                    Id = "alloy",
                    DisplayName = "合金板",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Mechanical | ItemTag.Salvage,
                    EnergyCost = 0,
                    Value = 30
                },
                new ItemDefinition
                {
                    Id = "stone",
                    DisplayName = "石头",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.None,
                    EnergyCost = 0,
                    Value = 2
                },
                new ItemDefinition
                {
                    Id = "branch",
                    DisplayName = "树枝",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.None,
                    EnergyCost = 0,
                    Value = 3
                },
                new ItemDefinition
                {
                    Id = "junk",
                    DisplayName = "垃圾",
                    Category = ItemCategory.Loot,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.None,
                    EnergyCost = 0,
                    Value = 1
                },
                new ItemDefinition
                {
                    Id = "bit",
                    DisplayName = "比特",
                    Category = ItemCategory.Currency,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.None,
                    EnergyCost = 0,
                    MaxStack = 999999,
                    Value = 0
                },
                new ItemDefinition
                {
                    Id = "ammo",
                    DisplayName = "弹药",
                    Category = ItemCategory.Ammo,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Mechanical,
                    EnergyCost = 0,
                    MaxStack = 60,
                    Value = 2
                },
                new ItemDefinition
                {
                    Id = "wild_bite",
                    DisplayName = "撕咬",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Blade,
                    EnergyCost = 1,
                    Damage = 2,
                    Value = 0
                },
                new ItemDefinition
                {
                    Id = "idol",
                    DisplayName = "数据雕像",
                    Category = ItemCategory.Loot,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Data,
                    EnergyCost = 0,
                    Value = 150
                },
                new ItemDefinition
                {
                    Id = "coin",
                    DisplayName = "数据币",
                    Category = ItemCategory.Loot,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Data,
                    EnergyCost = 0,
                    Value = 40
                },
                new ItemDefinition
                {
                    Id = "trinket",
                    DisplayName = "小饰物",
                    Category = ItemCategory.Loot,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Data,
                    EnergyCost = 0,
                    Value = 15
                },
                new ItemDefinition
                {
                    Id = "data_core",
                    DisplayName = "数据核心",
                    Category = ItemCategory.Loot,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Data,
                    EnergyCost = 0,
                    Value = 70
                }
            });
        }
    }
}
