using System.Collections.Generic;
using System.Text;

namespace BitRunner.Core
{
    public static class ItemText
    {
        public static string CategoryName(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Weapon:
                    return "武器";
                case ItemCategory.Armor:
                    return "护甲";
                case ItemCategory.Tool:
                    return "工具";
                case ItemCategory.Consumable:
                    return "消耗品";
                case ItemCategory.Loot:
                    return "战利品";
                case ItemCategory.Material:
                    return "材料";
                case ItemCategory.Key:
                    return "潜网密钥";
                case ItemCategory.Currency:
                    return "货币";
                case ItemCategory.Ammo:
                    return "弹药";
                case ItemCategory.Action:
                    return "行动";
                case ItemCategory.Container:
                    return "容器";
                case ItemCategory.Uplink:
                    return "上行链路";
                default:
                    return "物品";
            }
        }

        public static string DamageTypeName(DamageType type)
        {
            switch (type)
            {
                case DamageType.Melee:
                    return "近战";
                case DamageType.Projectile:
                    return "抛射";
                case DamageType.Burn:
                    return "烧伤";
                case DamageType.Toxin:
                    return "毒素";
                case DamageType.Asphyxiation:
                    return "窒息";
                default:
                    return "伤害";
            }
        }

        public static string DamageTypeShort(DamageType type)
        {
            switch (type)
            {
                case DamageType.Melee:
                    return "近";
                case DamageType.Projectile:
                    return "抛";
                case DamageType.Burn:
                    return "烧";
                case DamageType.Toxin:
                    return "毒";
                case DamageType.Asphyxiation:
                    return "窒";
                default:
                    return "?";
            }
        }

        public static string ArmorSummary(ItemDefinition definition)
        {
            var parts = new System.Collections.Generic.List<string>();
            AppendArmor(parts, "近战", definition.ArmorMelee);
            AppendArmor(parts, "抛射", definition.ArmorProjectile);
            AppendArmor(parts, "烧伤", definition.ArmorBurn);
            AppendArmor(parts, "毒素", definition.ArmorToxin);
            AppendArmor(parts, "窒息", definition.ArmorAsphyxiation);
            return parts.Count == 0 ? string.Empty : string.Join("  ", parts);
        }

        public static string ArmorSummaryDetailed(ItemDefinition definition)
        {
            var parts = new System.Collections.Generic.List<string>();
            AppendArmorDetailed(parts, "近战", definition.ArmorMelee);
            AppendArmorDetailed(parts, "抛射", definition.ArmorProjectile);
            AppendArmorDetailed(parts, "烧伤", definition.ArmorBurn);
            AppendArmorDetailed(parts, "毒素", definition.ArmorToxin);
            AppendArmorDetailed(parts, "窒息", definition.ArmorAsphyxiation);
            return parts.Count == 0 ? string.Empty : string.Join("  ", parts);
        }

        private static void AppendArmor(System.Collections.Generic.List<string> parts, string name, int value)
        {
            if (value > 0)
            {
                parts.Add(name + " " + value);
            }
        }

        private static void AppendArmorDetailed(System.Collections.Generic.List<string> parts, string name, int value)
        {
            if (value > 0)
            {
                parts.Add(name + " " + value + " (" + ArmorMath.ReductionPercent(value) + "%)");
            }
        }

        public static string StatusName(StatusType status)
        {
            switch (status)
            {
                case StatusType.None:
                    return "状态";
                case StatusType.Overload:
                    return "过载";
                case StatusType.ShortCircuit:
                    return "短路";
                case StatusType.Cache:
                    return "缓存";
                case StatusType.Overclock:
                    return "超频";
                case StatusType.Coolant:
                    return "冷却";
                case StatusType.Corrosion:
                    return "腐蚀";
                case StatusType.Overheat:
                    return "过热";
                case StatusType.Nanorepair:
                    return "纳米修复";
                case StatusType.Evasion:
                    return "闪避";
                default:
                    return "状态";
            }
        }

        public static string StatusDescription(StatusType status)
        {
            return BuffLibrary.Description(status);
        }

        public static string Describe(
            ItemDefinition definition,
            PlacedItem item = null,
            int ammoCount = -1,
            int damageOverride = -1,
            int shieldOverride = -1)
        {
            if (definition == null)
            {
                return "未知物品";
            }

            var builder = new StringBuilder();
            builder.Append(definition.DisplayName);
            if (item != null && item.Count > 1)
            {
                builder.Append(" ×").Append(item.Count);
            }

            builder.Append('\n')
                .Append(CategoryName(definition.Category))
                .Append("   价值 ").Append(definition.Value);

            var stats = new List<string>();
            int damage = damageOverride >= 0 ? damageOverride : definition.Damage;
            int shield = shieldOverride >= 0 ? shieldOverride : definition.Shield;

            if (definition.Activation == ItemActivation.Active && definition.EnergyCost > 0)
            {
                stats.Add("能量 " + definition.EnergyCost);
            }

            if (definition.Cooldown > 0)
            {
                stats.Add("冷却 " + definition.Cooldown);
            }

            if (damage > 0)
            {
                stats.Add("伤害 " + damage + " " + DamageTypeName(definition.Type));
            }

            if (shield > 0)
            {
                stats.Add("护盾 " + shield);
            }

            string armor = ArmorSummaryDetailed(definition);
            if (!string.IsNullOrEmpty(armor))
            {
                stats.Add("护甲 " + armor);
            }

            if (definition.Heal > 0)
            {
                stats.Add("恢复 " + definition.Heal + " 生命");
            }

            if (item != null)
            {
                int durability = ItemDurability.Current(item, definition);
                if (durability > 0)
                {
                    stats.Add("耐久 " + durability + "/" + definition.DurabilityMax);
                }
            }

            if (definition.UsesAmmo)
            {
                stats.Add(ammoCount >= 0
                    ? "弹药 " + definition.AmmoPerUse + "/次（持有 " + ammoCount + "）"
                    : "弹药 " + definition.AmmoPerUse + "/次");
            }

            if (stats.Count > 0)
            {
                builder.Append('\n').Append(string.Join("   ", stats));
            }

            var specials = new List<string>();
            if (definition.ConsumeOnUse)
            {
                specials.Add("使用后消耗");
            }

            if (specials.Count > 0)
            {
                builder.Append('\n').Append("特殊效果：");
                for (int i = 0; i < specials.Count; i++)
                {
                    builder.Append('\n').Append("· ").Append(specials[i]);
                }
            }

            var traitLines = TraitLines(definition);
            if (traitLines.Count > 0)
            {
                builder.Append('\n').Append("词条：");
                for (int i = 0; i < traitLines.Count; i++)
                {
                    builder.Append('\n').Append("· ").Append(traitLines[i]);
                }
            }

            if (!string.IsNullOrEmpty(definition.Description))
            {
                builder.Append('\n').Append("说明：").Append(definition.Description);
            }

            if (definition.Assist != null)
            {
                builder.Append('\n').Append("探索效果：");
                if (definition.Assist.LootPercent != 0)
                {
                    builder.Append('\n').Append("· 战利品 ").Append(Signed(definition.Assist.LootPercent)).Append("pp");
                }
                if (definition.Assist.EnemyPercent != 0)
                {
                    builder.Append('\n').Append("· 战斗 ").Append(Signed(definition.Assist.EnemyPercent)).Append("pp");
                }
                if (definition.Assist.DamagePercent != 0)
                {
                    builder.Append('\n').Append("· 受伤 ").Append(Signed(definition.Assist.DamagePercent)).Append("pp");
                }
                if (definition.Assist.AlertDelta != 0)
                {
                    builder.Append('\n').Append("· 警戒 ").Append(Signed(definition.Assist.AlertDelta)).Append("（提高遭遇敌人概率）");
                }
                builder.Append('\n').Append("（每次确认行动消耗 1 点耐久）");
            }

            if (definition.ContainerSpec != null)
            {
                builder.Append('\n')
                    .Append("容器：内部 ")
                    .Append(definition.ContainerSpec.Width)
                    .Append("×")
                    .Append(definition.ContainerSpec.Height)
                    .Append("（")
                    .Append(definition.ContainerSpec.FilterText)
                    .Append("）");
                if (item != null && item.Container != null)
                {
                    builder.Append("   已存 ")
                        .Append(item.Container.Bag.Count)
                        .Append(" 件 / ")
                        .Append(item.Container.FreeCellCount)
                        .Append(" 格空余");
                }
            }

            // 标签只是标签：给玩家分类/筛选用，不产生任何数值。
            if (definition.Tags != ItemTag.None)
            {
                builder.Append('\n').Append("标签：").Append(TagList(definition.Tags));
            }

            return builder.ToString();
        }

        private static string Signed(int value)
        {
            return value > 0 ? "+" + value : value.ToString();
        }

        public static string TagName(ItemTag tag)
        {
            switch (tag)
            {
                case ItemTag.Blade:
                    return "利刃";
                case ItemTag.Shield:
                    return "护盾";
                case ItemTag.Armor:
                    return "护甲";
                case ItemTag.Chip:
                    return "芯片";
                case ItemTag.Tool:
                    return "工具";
                case ItemTag.Consumable:
                    return "消耗品";
                case ItemTag.Data:
                    return "数据";
                case ItemTag.Mechanical:
                    return "机械";
                case ItemTag.Projectile:
                    return "弹道";
                case ItemTag.Energy:
                    return "能量";
                case ItemTag.Toxin:
                    return "毒素";
                case ItemTag.Cooling:
                    return "冷却";
                case ItemTag.Salvage:
                    return "回收";
                case ItemTag.Drone:
                    return "无人机";
                case ItemTag.Targeting:
                    return "瞄准";
                case ItemTag.Container:
                    return "容器";
                case ItemTag.CpuChip:
                    return "芯片槽";
                case ItemTag.Assist:
                    return "探索辅助";
                default:
                    return "无";
            }
        }

        public static string TagList(ItemTag tags)
        {
            var names = new List<string>();
            AppendTag(names, tags, ItemTag.Blade);
            AppendTag(names, tags, ItemTag.Shield);
            AppendTag(names, tags, ItemTag.Armor);
            AppendTag(names, tags, ItemTag.Chip);
            AppendTag(names, tags, ItemTag.Tool);
            AppendTag(names, tags, ItemTag.Consumable);
            AppendTag(names, tags, ItemTag.Data);
            AppendTag(names, tags, ItemTag.Mechanical);
            AppendTag(names, tags, ItemTag.Projectile);
            AppendTag(names, tags, ItemTag.Energy);
            AppendTag(names, tags, ItemTag.Toxin);
            AppendTag(names, tags, ItemTag.Cooling);
            AppendTag(names, tags, ItemTag.Salvage);
            AppendTag(names, tags, ItemTag.Drone);
            AppendTag(names, tags, ItemTag.Targeting);
            AppendTag(names, tags, ItemTag.Container);
            AppendTag(names, tags, ItemTag.CpuChip);
            AppendTag(names, tags, ItemTag.Assist);
            return names.Count == 0 ? "无" : string.Join(" / ", names);
        }

        private static void AppendTag(List<string> names, ItemTag tags, ItemTag tag)
        {
            if ((tags & tag) != 0)
            {
                names.Add(TagName(tag));
            }
        }

        public static IReadOnlyList<string> TraitLines(ItemDefinition definition)
        {
            var lines = new List<string>();
            if (definition == null || definition.Traits == null)
            {
                return lines;
            }

            for (int i = 0; i < definition.Traits.Length; i++)
            {
                TraitRef reference = definition.Traits[i];
                TraitDefinition trait;
                if (!TraitCatalog.Default.TryGet(reference.TraitId, out trait))
                {
                    continue;
                }

                int rank = definition.GetTraitRank(reference.TraitId);
                string body = string.Format(
                    trait.DescriptionFormat,
                    trait.AmountAtRank(rank),
                    trait.ConditionAtRank(rank));
                lines.Add(trait.DisplayName + " " + RankText(rank) + "：" + body);
            }

            return lines;
        }

        private static string RankText(int rank)
        {
            switch (rank)
            {
                case 1:
                    return "I";
                case 2:
                    return "II";
                case 3:
                    return "III";
                case 4:
                    return "IV";
                case 5:
                    return "V";
                default:
                    return "I";
            }
        }
    }
}
