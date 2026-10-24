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
                    Level = 2,
                    DisplayName = "刀刃",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Blade,
                    Traits = new[] { new TraitRef(TraitCatalog.KeenEdge, 1) },
                    EnergyCost = 1,
                    Damage = 5,
                    Value = 50
                },
                new ItemDefinition
                {
                    Id = "scrap_blade",
                    Level = 1,
                    DisplayName = "废铁刃",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Blade,
                    EnergyCost = 1,
                    Cooldown = 1,
                    Damage = 5,
                    Value = 25
                },
                new ItemDefinition
                {
                    Id = "shiv",
                    Level = 1,
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
                    Level = 1,
                    DisplayName = "铁棍",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Blade,
                    Traits = new[] { new TraitRef(TraitCatalog.HeavySmash, 1) },
                    EnergyCost = 1,
                    Damage = 4,
                    MaxDurability = 8,
                    Value = 24
                },
                new ItemDefinition
                {
                    Id = "rod",
                    Level = 3,
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
                    Traits = new[] { new TraitRef(TraitCatalog.OverheatStrike, 1) },
                    Value = 120,
                    Description = "攻击时施加 1 层灼烧（持续 2 回合）。"
                },
                new ItemDefinition
                {
                    Id = "pulse",
                    Level = 2,
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
                    Value = 50
                },
                new ItemDefinition
                {
                    Id = "nail_gun",
                    Level = 2,
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
                    Value = 50
                },
                new ItemDefinition
                {
                    Id = "plate",
                    Level = 3,
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
                    Value = 120
                },
                new ItemDefinition
                {
                    Id = "scrap_armor",
                    Level = 2,
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
                    Value = 50
                },
                new ItemDefinition
                {
                    Id = "heavy_plate",
                    Level = 3,
                    DisplayName = "重型装甲",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Shield | ItemTag.Armor,
                    Traits = new[] { new TraitRef(TraitCatalog.HeavyArmor, 1) },
                    EnergyCost = 2,
                    Shield = 3,
                    ArmorMelee = 16,
                    ArmorProjectile = 10,
                    ArmorBurn = 4,
                    ArmorToxin = 2,
                    MaxDurability = 10,
                    Value = 120
                },
                new ItemDefinition
                {
                    Id = "scrap_vest",
                    Level = 1,
                    DisplayName = "废铁背心",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Shield,
                    Traits = new[] { new TraitRef(TraitCatalog.LightFrame, 1) },
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
                    Level = 2,
                    DisplayName = "偏导器",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Shield,
                    EnergyCost = 1,
                    Shield = 2,
                    ArmorMelee = 3,
                    ArmorProjectile = 10,
                    Traits = new[] { new TraitRef(TraitCatalog.EvasionGuard, 1) },
                    Value = 50,
                    Description = "获得 2 点护盾，并施加 1 层闪避（持续 2 回合）。"
                },
                new ItemDefinition
                {
                    Id = "chip",
                    Level = 3,
                    DisplayName = "超频芯片",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Chip | ItemTag.Energy,
                    EnergyCost = 0,
                    Traits = new[] { new TraitRef(TraitCatalog.EnergyCell, 1) },
                    Value = 120,
                    Description = "被动：提供 1 点额外能量。"
                },
                new ItemDefinition
                {
                    Id = "jammer",
                    Level = 2,
                    DisplayName = "信号干扰器",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Tool | ItemTag.Chip,
                    EnergyCost = 1,
                    Cooldown = 2,
                    Traits = new[] { new TraitRef(TraitCatalog.SignalJam, 1) },
                    Value = 50
                },
                new ItemDefinition
                {
                    Id = "patch",
                    Level = 1,
                    DisplayName = "修复包",
                    Category = ItemCategory.Consumable,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Self,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Consumable,
                    EnergyCost = 1,
                    Heal = 8,
                    Traits = new[] { new TraitRef(TraitCatalog.Nanites, 1) },
                    ConsumeOnUse = true,
                    Value = 25,
                    Description = "恢复 8 点生命，并施加 2 层纳米修复（持续 3 回合）。"
                },
                new ItemDefinition
                {
                    Id = "medkit",
                    Level = 2,
                    DisplayName = "急救包",
                    Category = ItemCategory.Consumable,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Self,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Consumable,
                    EnergyCost = 2,
                    Heal = 12,
                    ConsumeOnUse = true,
                    Value = 50
                },
                new ItemDefinition
                {
                    Id = "heat_suit",
                    Level = 3,
                    DisplayName = "隔热服",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Shield | ItemTag.Armor,
                    EnergyCost = 0,
                    ArmorBurn = 12,
                    ArmorMelee = 2,
                    ArmorToxin = 1,
                    Value = 120
                },
                new ItemDefinition
                {
                    Id = "filter",
                    Level = 2,
                    DisplayName = "滤毒罐",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Shield | ItemTag.Tool,
                    EnergyCost = 0,
                    ArmorToxin = 12,
                    ArmorAsphyxiation = 8,
                    ArmorBurn = 2,
                    Value = 50
                },
                new ItemDefinition
                {
                    Id = "respirator",
                    Level = 2,
                    DisplayName = "呼吸器",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#", "#"),
                    Tags = ItemTag.Shield | ItemTag.Tool,
                    EnergyCost = 0,
                    ArmorAsphyxiation = 12,
                    ArmorToxin = 6,
                    Value = 50
                },
                new ItemDefinition
                {
                    Id = "toxin_dart",
                    Level = 2,
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
                    Value = 50
                },
                new ItemDefinition
                {
                    Id = "overclock_core",
                    Level = 3,
                    DisplayName = "超频核心",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Chip | ItemTag.Energy,
                    EnergyCost = 0,
                    Traits = new[]
                    {
                        new TraitRef(TraitCatalog.EnergyCell, 2),
                        new TraitRef(TraitCatalog.OverloadLoad, 1)
                    },
                    Value = 120,
                    Description = "能量上限 +2；每回合开始获得 1 层过载（下回合能量 -1）。"
                },
                new ItemDefinition
                {
                    Id = "coolant_loop",
                    Level = 3,
                    DisplayName = "冷却回路",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Tool | ItemTag.Cooling | ItemTag.Mechanical,
                    EnergyCost = 0,
                    Traits = new[]
                    {
                        new TraitRef(TraitCatalog.OverloadPurge, 1),
                        new TraitRef(TraitCatalog.CoolantFlow, 1)
                    },
                    Value = 120,
                    Description = "每回合开始清除 1 层过载，并获得 1 层冷却（持续 3 回合）。"
                },
                new ItemDefinition
                {
                    Id = "targeting_array",
                    Level = 3,
                    DisplayName = "靶向阵列",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Chip | ItemTag.Energy | ItemTag.Targeting,
                    EnergyCost = 0,
                    Traits = new[] { new TraitRef(TraitCatalog.OverclockFlow, 1) },
                    Value = 120,
                    Description = "战斗每回合开始获得 1 层超频（每层使攻击伤害 +1，持续 3 回合）。"
                },
                new ItemDefinition
                {
                    Id = "ram_piston",
                    Level = 3,
                    DisplayName = "冲压活塞",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Mechanical,
                    EnergyCost = 1,
                    Cooldown = 1,
                    Traits = new[] { new TraitRef(TraitCatalog.ShieldPiston, 1) },
                    MaxDurability = 8,
                    Value = 120,
                    Description = "造成等同于当前护盾一半的伤害（每 2 点护盾 +1 伤害）。"
                },
                new ItemDefinition
                {
                    Id = "corrosive_dart",
                    Level = 3,
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
                    Traits = new[] { new TraitRef(TraitCatalog.CorrosionDose, 1) },
                    Value = 120,
                    Description = "造成 2 点毒素伤害，并施加 2 层腐蚀（持续 3 回合）。"
                },
                new ItemDefinition
                {
                    Id = "fence_module",
                    Level = 3,
                    DisplayName = "赃物模块",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Self,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Tool | ItemTag.Salvage,
                    EnergyCost = 1,
                    Cooldown = 1,
                    Shield = 1,
                    Value = 120,
                    Description = "获得 1 点护盾；回收标签可与数据/机械相邻触发额外护盾。"
                },
                new ItemDefinition
                {
                    Id = "weapon_crate",
                    Level = 3,
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
                    Level = 3,
                    DisplayName = "护甲箱",
                    Category = ItemCategory.Container,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##", "##", "##"),
                    Tags = ItemTag.Container | ItemTag.Tool,
                    MaxStack = 1,
                    Value = 120,
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
                    Level = 2,
                    DisplayName = "零件盒",
                    Category = ItemCategory.Container,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Container | ItemTag.Tool,
                    MaxStack = 1,
                    Value = 50,
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
                    Level = 2,
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
                    Level = 3,
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
                    Value = 120
                },
                new ItemDefinition
                {
                    Id = "dive_basic",
                    Level = 1,
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
                    Level = 1,
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
                    Level = 2,
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
                    Level = 3,
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
                    Level = 1,
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
                    Level = 1,
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
                    Level = 2,
                    DisplayName = "电路板",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Mechanical | ItemTag.Salvage | ItemTag.Energy,
                    EnergyCost = 0,
                    Value = 50
                },
                new ItemDefinition
                {
                    Id = "alloy",
                    Level = 2,
                    DisplayName = "合金板",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Mechanical | ItemTag.Salvage,
                    EnergyCost = 0,
                    Value = 50
                },
                new ItemDefinition
                {
                    Id = "stone",
                    Level = 1,
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
                    Level = 1,
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
                    Level = 1,
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
                    Level = 1,
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
                    Level = 1,
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
                    Level = 1,
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
                    Level = 4,
                    DisplayName = "数据雕像",
                    Category = ItemCategory.Loot,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Data,
                    EnergyCost = 0,
                    Value = 290
                },
                new ItemDefinition
                {
                    Id = "coin",
                    Level = 2,
                    DisplayName = "数据币",
                    Category = ItemCategory.Loot,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Data,
                    EnergyCost = 0,
                    Value = 50
                },
                new ItemDefinition
                {
                    Id = "trinket",
                    Level = 1,
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
                    Level = 3,
                    DisplayName = "数据核心",
                    Category = ItemCategory.Loot,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Data,
                    EnergyCost = 0,
                    Value = 120
                },

                // ---------- 中央处理器芯片 ----------
                // 只插 CPU，不进战斗背包。Tags 用 CpuChip 而不是 Chip：
                // 后者是敌人配装「科技件」槽的筛选标签，混用会让敌人装备只对玩家生效的芯片。
                new ItemDefinition
                {
                    Id = "chip_hp_1",
                    Level = 1,
                    DisplayName = "生命芯片 I",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.CpuChip,
                    Traits = new[] { new TraitRef(TraitCatalog.HpChip, 1) },
                    EnergyCost = 0,
                    Value = 20,
                    Description = "插在中央处理器上，永久提高生命上限。"
                },
                new ItemDefinition
                {
                    Id = "chip_hp_2",
                    Level = 2,
                    DisplayName = "生命芯片 II",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.CpuChip,
                    Traits = new[] { new TraitRef(TraitCatalog.HpChip, 2) },
                    EnergyCost = 0,
                    Value = 50
                },
                new ItemDefinition
                {
                    Id = "chip_hp_3",
                    Level = 3,
                    DisplayName = "生命芯片 III",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#", "#"),
                    Tags = ItemTag.CpuChip,
                    Traits = new[] { new TraitRef(TraitCatalog.HpChip, 3) },
                    EnergyCost = 0,
                    Value = 120
                },
                new ItemDefinition
                {
                    Id = "chip_hp_4",
                    Level = 4,
                    DisplayName = "生命芯片 IV",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.CpuChip,
                    Traits = new[] { new TraitRef(TraitCatalog.HpChip, 4) },
                    EnergyCost = 0,
                    Value = 290
                },
                new ItemDefinition
                {
                    Id = "chip_hp_5",
                    Level = 5,
                    DisplayName = "生命芯片 V",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.CpuChip,
                    Traits = new[] { new TraitRef(TraitCatalog.HpChip, 5) },
                    EnergyCost = 0,
                    Value = 700
                },
                new ItemDefinition
                {
                    Id = "chip_armor_1",
                    Level = 1,
                    DisplayName = "底甲芯片 I",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.CpuChip,
                    Traits = new[] { new TraitRef(TraitCatalog.ArmorChip, 1) },
                    EnergyCost = 0,
                    Value = 20,
                    Description = "五系护甲基底 +1，不依赖背包里有没有护甲。"
                },
                new ItemDefinition
                {
                    Id = "chip_armor_2",
                    Level = 2,
                    DisplayName = "底甲芯片 II",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.CpuChip,
                    Traits = new[] { new TraitRef(TraitCatalog.ArmorChip, 2) },
                    EnergyCost = 0,
                    Value = 50
                },
                new ItemDefinition
                {
                    Id = "chip_armor_3",
                    Level = 3,
                    DisplayName = "底甲芯片 III",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.CpuChip,
                    Traits = new[] { new TraitRef(TraitCatalog.ArmorChip, 3) },
                    EnergyCost = 0,
                    Value = 120
                },
                new ItemDefinition
                {
                    Id = "chip_armor_4",
                    Level = 4,
                    DisplayName = "底甲芯片 IV",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.CpuChip,
                    Traits = new[] { new TraitRef(TraitCatalog.ArmorChip, 4) },
                    EnergyCost = 0,
                    Value = 290
                },
                new ItemDefinition
                {
                    Id = "chip_armor_5",
                    Level = 5,
                    DisplayName = "底甲芯片 V",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#", "#"),
                    Tags = ItemTag.CpuChip,
                    Traits = new[] { new TraitRef(TraitCatalog.ArmorChip, 5) },
                    EnergyCost = 0,
                    Value = 700
                },

                // 能量芯片一定带代价（生命上限为负）。能量 +1 等于每回合多一次行动，
                // 白给会让「打哪一件」的选择彻底消失——参考《杀戮尖塔》的能量遗物。
                new ItemDefinition
                {
                    Id = "chip_energy_1",
                    Level = 1,
                    DisplayName = "能量芯片 I",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.CpuChip | ItemTag.Energy,
                    Traits = new[]
                    {
                        new TraitRef(TraitCatalog.BaseEnergyChip, 1),
                        new TraitRef(TraitCatalog.EnergyDrawback, 1)
                    },
                    EnergyCost = 0,
                    Value = 20,
                    Description = "基础能量 +1，代价是生命上限 −18。全游戏只生效一张。"
                },
                new ItemDefinition
                {
                    Id = "chip_energy_2",
                    Level = 2,
                    DisplayName = "能量芯片 II",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.CpuChip | ItemTag.Energy,
                    Traits = new[]
                    {
                        new TraitRef(TraitCatalog.BaseEnergyChip, 2),
                        new TraitRef(TraitCatalog.EnergyDrawback, 2)
                    },
                    EnergyCost = 0,
                    Value = 50,
                    Description = "基础能量 +1，代价是生命上限 −15。"
                },
                new ItemDefinition
                {
                    Id = "chip_energy_3",
                    Level = 3,
                    DisplayName = "能量芯片 III",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.CpuChip | ItemTag.Energy,
                    Traits = new[]
                    {
                        new TraitRef(TraitCatalog.BaseEnergyChip, 3),
                        new TraitRef(TraitCatalog.EnergyDrawback, 3)
                    },
                    EnergyCost = 0,
                    Value = 120,
                    Description = "基础能量 +1，代价是生命上限 −12。"
                },
                new ItemDefinition
                {
                    Id = "chip_energy_4",
                    Level = 4,
                    DisplayName = "能量芯片 IV",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.CpuChip | ItemTag.Energy,
                    Traits = new[]
                    {
                        new TraitRef(TraitCatalog.BaseEnergyChip, 4),
                        new TraitRef(TraitCatalog.EnergyDrawback, 4)
                    },
                    EnergyCost = 0,
                    Value = 290,
                    Description = "基础能量 +1，代价是生命上限 −9。"
                },
                new ItemDefinition
                {
                    Id = "chip_energy_5",
                    Level = 5,
                    DisplayName = "能量芯片 V",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.CpuChip | ItemTag.Energy,
                    Traits = new[]
                    {
                        new TraitRef(TraitCatalog.BaseEnergyChip, 5),
                        new TraitRef(TraitCatalog.EnergyDrawback, 5)
                    },
                    EnergyCost = 0,
                    Value = 700,
                    Description = "基础能量 +1，代价是生命上限 −6。"
                },

                // ---------- 密钥盒 ----------
                // 密钥是永久物，越深网的密钥体积越大（1 → 2 → 4 → 6 → 9 格）。
                // 不收进容器的话，五把密钥会吃掉仓库一整块空间，所以单独给一个只放密钥的盒子。
                // 注意：潜网舱只认「舱内」的密钥，盒子里的密钥必须先拖进潜网舱才能出发。
                new ItemDefinition
                {
                    Id = "key_box",
                    Level = 2,
                    DisplayName = "密钥盒",
                    Category = ItemCategory.Container,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Container | ItemTag.Tool,
                    MaxStack = 1,
                    Value = 50,
                    ContainerSpec = new ContainerSpec
                    {
                        Width = 6,
                        Height = 4,
                        AllowedCategories = new[] { ItemCategory.Key }
                    },
                    Description = "只放潜网密钥的盒子。6×4 刚好装下全部五把密钥（22 格）。"
                },

                // ---------- T3 材料 ----------
                // 材料只做制作与设施升级的输入，价值刻意压在装备之下（装备 T3 = 120）。
                new ItemDefinition
                {
                    Id = "servo",
                    Level = 3,
                    DisplayName = "伺服组件",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Mechanical | ItemTag.Salvage,
                    EnergyCost = 0,
                    Value = 60
                },
                new ItemDefinition
                {
                    Id = "capacitor",
                    Level = 3,
                    DisplayName = "电容组",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Energy | ItemTag.Mechanical,
                    EnergyCost = 0,
                    Value = 60
                },
                new ItemDefinition
                {
                    Id = "coolant_tank",
                    Level = 3,
                    DisplayName = "冷却罐",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#", "#"),
                    Tags = ItemTag.Cooling | ItemTag.Mechanical,
                    EnergyCost = 0,
                    Value = 60
                },

                // ---------- T4 材料 ----------
                new ItemDefinition
                {
                    Id = "superalloy",
                    Level = 4,
                    DisplayName = "超合金板",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Mechanical | ItemTag.Salvage,
                    EnergyCost = 0,
                    Value = 140
                },
                new ItemDefinition
                {
                    Id = "optic_fiber",
                    Level = 4,
                    DisplayName = "光纤束",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Data | ItemTag.Energy,
                    EnergyCost = 0,
                    Value = 140
                },
                new ItemDefinition
                {
                    Id = "plasma_cell",
                    Level = 4,
                    DisplayName = "等离子电池",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#", "#"),
                    Tags = ItemTag.Energy | ItemTag.Mechanical,
                    EnergyCost = 0,
                    Value = 140
                },

                // ---------- T5 材料 ----------
                new ItemDefinition
                {
                    Id = "singularity_core",
                    Level = 5,
                    DisplayName = "奇点核心",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Energy | ItemTag.Data,
                    EnergyCost = 0,
                    Value = 320
                },
                new ItemDefinition
                {
                    Id = "monopole_wire",
                    Level = 5,
                    DisplayName = "单极导线",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("###"),
                    Tags = ItemTag.Energy | ItemTag.Mechanical,
                    EnergyCost = 0,
                    Value = 320
                },
                new ItemDefinition
                {
                    Id = "corona_shard",
                    Level = 5,
                    DisplayName = "日冕残片",
                    Category = ItemCategory.Material,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Data | ItemTag.Salvage,
                    EnergyCost = 0,
                    Value = 320
                },

                // ---------- T4 武器 ----------
                // 数值按 docs/ITEM_TIER_PLAN.md §4.4：1 能量 ≈ 17 伤、2 能量 ≈ 30 伤、耐久 21。
                new ItemDefinition
                {
                    Id = "rail_lance",
                    Level = 4,
                    DisplayName = "磁轨长矛",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("##", "#."),
                    Tags = ItemTag.Blade | ItemTag.Projectile,
                    EnergyCost = 2,
                    Damage = 30,
                    Type = DamageType.Projectile,
                    Traits = new[] { new TraitRef(TraitCatalog.KeenEdge, 4) },
                    MaxDurability = 21,
                    Value = 290,
                    Description = "充能后贯穿发射。用完会把周围武器一起带进节奏。"
                },
                new ItemDefinition
                {
                    Id = "plasma_cutter",
                    Level = 4,
                    DisplayName = "等离子切割器",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("###"),
                    Tags = ItemTag.Blade | ItemTag.Mechanical,
                    EnergyCost = 1,
                    Damage = 17,
                    Type = DamageType.Burn,
                    Traits = new[] { new TraitRef(TraitCatalog.OverheatStrike, 4) },
                    MaxDurability = 21,
                    Value = 290,
                    Description = "1 能量的稳定烧灼，命中叠 2 层过热。"
                },
                new ItemDefinition
                {
                    Id = "flechette_gun",
                    Level = 4,
                    DisplayName = "破片枪",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Chip | ItemTag.Projectile,
                    EnergyCost = 1,
                    Damage = 14,
                    Type = DamageType.Projectile,
                    AmmoItemId = "ammo",
                    AmmoPerUse = 2,
                    Traits = new[] { new TraitRef(TraitCatalog.KeenEdge, 3) },
                    MaxDurability = 21,
                    Value = 290,
                    Description = "一次打掉 2 发弹药，伤害低于同级近战，但不占近身位。"
                },
                new ItemDefinition
                {
                    Id = "reactor_maul",
                    Level = 4,
                    DisplayName = "反应堆重锤",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Blade | ItemTag.Mechanical,
                    EnergyCost = 2,
                    Damage = 26,
                    Type = DamageType.Melee,
                    Traits = new[] { new TraitRef(TraitCatalog.HeavySmash, 4) },
                    MaxDurability = 21,
                    Value = 290,
                    Description = "占 4 格、吃 2 能量，用周围空格换伤害。"
                },

                // ---------- T5 武器 ----------
                new ItemDefinition
                {
                    Id = "polymer_dagger",
                    Level = 5,
                    DisplayName = "高分子匕首",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Blade,
                    EnergyCost = 1,
                    Damage = 25,
                    Type = DamageType.Melee,
                    Traits = new[] { new TraitRef(TraitCatalog.KeenEdge, 5) },
                    MaxDurability = 28,
                    Value = 700,
                    Description = "一格 25 伤。全游戏最贵的性价比，代价是要用命去捡。"
                },
                new ItemDefinition
                {
                    Id = "singularity_hammer",
                    Level = 5,
                    DisplayName = "奇点锤",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Blade | ItemTag.Mechanical,
                    EnergyCost = 2,
                    Damage = 44,
                    Type = DamageType.Melee,
                    Traits = new[] { new TraitRef(TraitCatalog.HeavySmash, 5) },
                    MaxDurability = 28,
                    Value = 700,
                    Description = "44 基础伤害，再加周围空格加成。一击能打掉半个 T5 血条。"
                },
                new ItemDefinition
                {
                    Id = "arc_reaver",
                    Level = 5,
                    DisplayName = "弧光撕裂者",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Blade | ItemTag.Energy,
                    EnergyCost = 1,
                    Damage = 22,
                    Type = DamageType.Burn,
                    Traits = new[]
                    {
                        new TraitRef(TraitCatalog.OverheatStrike, 5),
                        new TraitRef(TraitCatalog.ShortCircuitCharge, 4)
                    },
                    MaxDurability = 28,
                    Value = 700,
                    Description = "烧灼 + 短路双状态，靠状态叠死对手而不是靠单发。"
                },
                new ItemDefinition
                {
                    Id = "void_needler",
                    Level = 5,
                    DisplayName = "虚空针",
                    Category = ItemCategory.Weapon,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Enemy,
                    Shape = ItemShape.Parse("#", "#"),
                    Tags = ItemTag.Toxin | ItemTag.Projectile | ItemTag.Chip,
                    EnergyCost = 1,
                    Damage = 18,
                    Type = DamageType.Toxin,
                    Traits = new[]
                    {
                        new TraitRef(TraitCatalog.CorrosionDose, 5),
                        new TraitRef(TraitCatalog.SignalJam, 3)
                    },
                    MaxDurability = 28,
                    Value = 700,
                    Description = "腐蚀 4 层 + 过载，专治高护甲单位。"
                },

                // ---------- T4 护甲 ----------
                new ItemDefinition
                {
                    Id = "reactive_plating",
                    Level = 4,
                    DisplayName = "反应装甲",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Shield | ItemTag.Armor,
                    EnergyCost = 1,
                    Shield = 7,
                    ArmorMelee = 19,
                    ArmorProjectile = 10,
                    ArmorBurn = 4,
                    ArmorToxin = 2,
                    ArmorAsphyxiation = 2,
                    Traits = new[] { new TraitRef(TraitCatalog.HeavyArmor, 4) },
                    MaxDurability = 21,
                    Value = 290,
                    Description = "近战 19，配重甲词条：周围每件护甲再叠护盾。"
                },
                new ItemDefinition
                {
                    Id = "ablative_shell",
                    Level = 4,
                    DisplayName = "烧蚀外壳",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Shield | ItemTag.Armor,
                    EnergyCost = 1,
                    Shield = 7,
                    ArmorMelee = 8,
                    ArmorProjectile = 13,
                    ArmorBurn = 10,
                    ArmorToxin = 4,
                    ArmorAsphyxiation = 2,
                    MaxDurability = 21,
                    Value = 290,
                    Description = "偏抛射与烧伤，克磁轨狙击手与等离子切割器。"
                },
                new ItemDefinition
                {
                    Id = "hazmat_rig",
                    Level = 4,
                    DisplayName = "隔离装具",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "#."),
                    Tags = ItemTag.Shield | ItemTag.Tool,
                    EnergyCost = 0,
                    Shield = 5,
                    ArmorMelee = 5,
                    ArmorProjectile = 5,
                    ArmorBurn = 6,
                    ArmorToxin = 13,
                    ArmorAsphyxiation = 13,
                    Traits = new[] { new TraitRef(TraitCatalog.LightFrame, 4) },
                    MaxDurability = 21,
                    Value = 290,
                    Description = "毒与窒息双 13，且不吃能量。3 格换一整套环境防护。"
                },

                // ---------- T5 护甲 ----------
                new ItemDefinition
                {
                    Id = "corona_aegis",
                    Level = 5,
                    DisplayName = "日冕护盾甲",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Shield | ItemTag.Armor,
                    EnergyCost = 1,
                    Shield = 10,
                    ArmorMelee = 24,
                    ArmorProjectile = 13,
                    ArmorBurn = 6,
                    ArmorToxin = 6,
                    ArmorAsphyxiation = 6,
                    Traits = new[] { new TraitRef(TraitCatalog.HeavyArmor, 5) },
                    MaxDurability = 28,
                    Value = 700,
                    Description = "近战 24 = 46% 减伤。五系全部不空，是本作的「校服」。"
                },
                new ItemDefinition
                {
                    Id = "phase_weave",
                    Level = 5,
                    DisplayName = "相位织层",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Shield,
                    EnergyCost = 1,
                    Shield = 6,
                    ArmorMelee = 10,
                    ArmorProjectile = 10,
                    ArmorBurn = 10,
                    ArmorToxin = 10,
                    ArmorAsphyxiation = 10,
                    Traits = new[]
                    {
                        new TraitRef(TraitCatalog.EvasionGuard, 5),
                        new TraitRef(TraitCatalog.LightFrame, 5)
                    },
                    MaxDurability = 28,
                    Value = 700,
                    Description = "只用 2 格换五系各 10。减伤不如日冕，但省下的格子能再塞一件武器。"
                },
                new ItemDefinition
                {
                    Id = "void_suit",
                    Level = 5,
                    DisplayName = "虚空服",
                    Category = ItemCategory.Armor,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Shield | ItemTag.Tool,
                    EnergyCost = 1,
                    Shield = 8,
                    ArmorMelee = 8,
                    ArmorProjectile = 8,
                    ArmorBurn = 10,
                    ArmorToxin = 20,
                    ArmorAsphyxiation = 20,
                    Traits = new[] { new TraitRef(TraitCatalog.CacheField, 3) },
                    MaxDurability = 28,
                    Value = 700,
                    Description = "毒与窒息各 20，配合缓存场把护盾一路带进下一回合。"
                },

                // ---------- T4 工具 / 消耗品 ----------
                new ItemDefinition
                {
                    Id = "nano_stitcher",
                    Level = 4,
                    DisplayName = "纳米缝合器",
                    Category = ItemCategory.Consumable,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Self,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.Consumable,
                    EnergyCost = 1,
                    Heal = 24,
                    Traits = new[] { new TraitRef(TraitCatalog.Nanites, 4) },
                    ConsumeOnUse = true,
                    Value = 290,
                    Description = "恢复 24 生命并叠 3 层纳米修复。一次回满半条命。"
                },
                new ItemDefinition
                {
                    Id = "overdrive_relay",
                    Level = 4,
                    DisplayName = "超驱继电器",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.Chip | ItemTag.Energy,
                    EnergyCost = 0,
                    Traits = new[]
                    {
                        new TraitRef(TraitCatalog.EnergyCell, 3),
                        new TraitRef(TraitCatalog.OverloadLoad, 3)
                    },
                    Value = 290,
                    Description = "能量上限 +3，每回合叠 1 层过载。装上去就得靠冷却件压住。"
                },
                new ItemDefinition
                {
                    Id = "targeting_lattice",
                    Level = 4,
                    DisplayName = "瞄准晶格",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#", "#"),
                    Tags = ItemTag.Chip | ItemTag.Targeting,
                    EnergyCost = 0,
                    Traits = new[]
                    {
                        new TraitRef(TraitCatalog.OverclockFlow, 4),
                        new TraitRef(TraitCatalog.ShortCircuitCharge, 3)
                    },
                    Value = 290,
                    Description = "每回合自动叠超频，越拖越痛。配合多段攻击是纯增益。"
                },

                // ---------- T5 工具 / 消耗品 ----------
                new ItemDefinition
                {
                    Id = "full_repair_kit",
                    Level = 5,
                    DisplayName = "全修套装",
                    Category = ItemCategory.Consumable,
                    Activation = ItemActivation.Active,
                    Target = ItemTarget.Self,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Consumable,
                    EnergyCost = 2,
                    Heal = 40,
                    Traits = new[] { new TraitRef(TraitCatalog.Nanites, 5) },
                    ConsumeOnUse = true,
                    Value = 700,
                    Description = "恢复 40 生命，约等于一整条 T1 血条。4 格换一条命。"
                },
                new ItemDefinition
                {
                    Id = "null_field_generator",
                    Level = 5,
                    DisplayName = "零场发生器",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.Tool | ItemTag.Shield,
                    EnergyCost = 0,
                    Traits = new[]
                    {
                        new TraitRef(TraitCatalog.EvasionGuard, 5),
                        new TraitRef(TraitCatalog.CacheField, 5)
                    },
                    Value = 700,
                    Description = "每回合开始获得闪避，并把护盾带过回合。纯防守件。"
                },
                new ItemDefinition
                {
                    Id = "plasma_furnace",
                    Level = 5,
                    DisplayName = "等离子熔炉",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "#."),
                    Tags = ItemTag.Tool | ItemTag.Energy | ItemTag.Mechanical,
                    EnergyCost = 0,
                    Traits = new[]
                    {
                        new TraitRef(TraitCatalog.OverheatStrike, 5),
                        new TraitRef(TraitCatalog.OverclockFlow, 5)
                    },
                    Value = 700,
                    Description = "被动叠超频，且每次命中额外施加过热。放在哪一格都生效。"
                },

                // ---------- 深网密钥（T4 / T5） ----------
                // 体积随档递增：1 / 2 / 4 / 6 / 9 格。最深那把 3×3，潜网舱至少要 3×3 才放得下。
                new ItemDefinition
                {
                    Id = "dive_deep",
                    Level = 4,
                    DisplayName = "深空潜网密钥",
                    Category = ItemCategory.Key,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("###", "###"),
                    Tags = ItemTag.None,
                    EnergyCost = 0,
                    Value = 0,
                    Description = "占 6 格。潜网舱 Lv.4 起可用。"
                },
                new ItemDefinition
                {
                    Id = "dive_core",
                    Level = 5,
                    DisplayName = "核心潜网密钥",
                    Category = ItemCategory.Key,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("###", "###", "###"),
                    Tags = ItemTag.None,
                    EnergyCost = 0,
                    Value = 0,
                    Description = "占 9 格，3×3。潜网舱 Lv.5 起可用。"
                },

                // ---------- 通讯上行链路（只能插进通讯中枢）----------
                // 体积规则与关卡密钥一致：档位越高占得越多。所以「同时联系几个人」
                // 也是一道格子题，中枢 Lv.4（4×3=12 格）差 1 格放不下全部四条。
                new ItemDefinition
                {
                    Id = "uplink_junk",
                    Level = 1,
                    DisplayName = "旧频道上行链路",
                    Category = ItemCategory.Uplink,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#"),
                    Tags = ItemTag.None,
                    EnergyCost = 0,
                    Value = 0,
                    Description = "一段没人清理的旧信道。放上通讯中枢就能联系回收商老陈。"
                },
                new ItemDefinition
                {
                    Id = "uplink_arms",
                    Level = 2,
                    DisplayName = "军械上行链路",
                    Category = ItemCategory.Uplink,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##"),
                    Tags = ItemTag.None,
                    EnergyCost = 0,
                    Value = 0,
                    Description = "带加密握手的军用信道。联系军械贩 K-7，他不管现在是第几周。"
                },
                new ItemDefinition
                {
                    Id = "uplink_intel",
                    Level = 3,
                    DisplayName = "情报上行链路",
                    Category = ItemCategory.Uplink,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("##", "##"),
                    Tags = ItemTag.None,
                    EnergyCost = 0,
                    Value = 0,
                    Description = "占 4 格。联系掮客白鸦。他卖的东西别处买不到。"
                },
                new ItemDefinition
                {
                    Id = "uplink_seed",
                    Level = 4,
                    DisplayName = "核心上行链路",
                    Category = ItemCategory.Uplink,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("###", "###"),
                    Tags = ItemTag.None,
                    EnergyCost = 0,
                    Value = 0,
                    Description = "占 6 格。核心环里唯一还在应答的信号源。"
                },

                // 密码测序器：C-9 只做物品本体（白鸦的货 + q_intel_1 的奖励），
                // 危机模式的判定在 C-10。它必须是 Tool 不是 Key——潜网舱按
                // Category == Key 挑密钥，用 Key 会被误选成「未知密钥」。
                new ItemDefinition
                {
                    Id = "cipher_sequencer",
                    Level = 3,
                    DisplayName = "密码测序器",
                    Category = ItemCategory.Tool,
                    Activation = ItemActivation.Passive,
                    Shape = ItemShape.Parse("#", "#"),
                    Tags = ItemTag.Data | ItemTag.Tool,
                    EnergyCost = 0,
                    Value = 0,
                    Description = "占 1×2。放进潜网舱能解开关卡锁着的另一条路。它不会用掉。"
                }
            });
        }
    }
}
