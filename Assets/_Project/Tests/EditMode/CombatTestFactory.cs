using System.Collections.Generic;
using BitRunner.Core;

namespace BitRunner.Tests
{
    internal static class CombatTestFactory
    {
        public static ItemDefinition Weapon(string id, int damage, int apCost, int cooldown, ItemTag tags, string pattern, int value = 10, DamageType type = DamageType.Melee)
        {
            return new ItemDefinition
            {
                Id = id,
                DisplayName = id,
                Category = ItemCategory.Weapon,
                Activation = ItemActivation.Active,
                Target = ItemTarget.Enemy,
                Shape = ItemShape.Parse(pattern.Split('\n')),
                Tags = tags,
                EnergyCost = apCost,
                Cooldown = cooldown,
                Damage = damage,
                Type = type,
                Value = value
            };
        }

        public static ItemDefinition Passive(string id, int block, int bonusEnergy, ItemTag tags, string pattern, int value = 10)
        {
            return new ItemDefinition
            {
                Id = id,
                DisplayName = id,
                Category = ItemCategory.Armor,
                Activation = ItemActivation.Passive,
                Target = ItemTarget.None,
                Shape = ItemShape.Parse(pattern.Split('\n')),
                Tags = tags,
                EnergyCost = 0,
                Shield = block,

                // 词条 rank 不能超过物品等级，测试里按能量点数抬档。
                Level = bonusEnergy > 1 ? bonusEnergy : 1,
                Traits = bonusEnergy > 0
                    ? new[] { new TraitRef(TraitCatalog.EnergyCell, bonusEnergy) }
                    : null,
                Value = value
            };
        }

        public static ItemDefinition Armor(
            string id,
            int melee,
            int projectile,
            int burn,
            int toxin,
            int asphyxiation,
            string pattern,
            int value = 10)
        {
            return new ItemDefinition
            {
                Id = id,
                DisplayName = id,
                Category = ItemCategory.Armor,
                Activation = ItemActivation.Passive,
                Target = ItemTarget.None,
                Shape = ItemShape.Parse(pattern.Split('\n')),
                Tags = ItemTag.Shield | ItemTag.Armor,
                EnergyCost = 0,
                ArmorMelee = melee,
                ArmorProjectile = projectile,
                ArmorBurn = burn,
                ArmorToxin = toxin,
                ArmorAsphyxiation = asphyxiation,
                Value = value
            };
        }

        public static ItemDefinition ActiveSelf(string id, int apCost, int block, int heal, bool consume, StatusType status, int statusTurns, ItemTag tags, string pattern)
        {
            return new ItemDefinition
            {
                Id = id,
                DisplayName = id,
                Category = ItemCategory.Tool,
                Activation = ItemActivation.Active,
                Target = ItemTarget.Self,
                Shape = ItemShape.Parse(pattern.Split('\n')),
                Tags = tags,
                EnergyCost = apCost,
                Shield = block,
                Heal = heal,
                ConsumeOnUse = consume,
                Traits = StatusTrait(status),
                Value = 10
            };
        }

        public static ItemDefinition ActiveEnemy(string id, int apCost, int cooldown, StatusType status, int statusTurns, ItemTag tags, string pattern)
        {
            return new ItemDefinition
            {
                Id = id,
                DisplayName = id,
                Category = ItemCategory.Tool,
                Activation = ItemActivation.Active,
                Target = ItemTarget.Enemy,
                Shape = ItemShape.Parse(pattern.Split('\n')),
                Tags = tags,
                EnergyCost = apCost,
                Cooldown = cooldown,
                Traits = StatusTrait(status),
                Value = 10
            };
        }

        private static TraitRef[] StatusTrait(StatusType status)
        {
            string traitId;
            switch (status)
            {
                case StatusType.Overload:
                    traitId = TraitCatalog.SignalJam;
                    break;
                case StatusType.ShortCircuit:
                    traitId = TraitCatalog.ShortCircuitCharge;
                    break;
                case StatusType.Cache:
                    traitId = TraitCatalog.CacheField;
                    break;
                case StatusType.Overheat:
                    traitId = TraitCatalog.OverheatStrike;
                    break;
                case StatusType.Corrosion:
                    traitId = TraitCatalog.CorrosionDose;
                    break;
                case StatusType.Nanorepair:
                    traitId = TraitCatalog.Nanites;
                    break;
                case StatusType.Evasion:
                    traitId = TraitCatalog.EvasionGuard;
                    break;
                default:
                    return null;
            }

            return new[] { new TraitRef(traitId, 1) };
        }

        public static ItemDefinition Loot(string id, int value, string pattern)
        {
            return new ItemDefinition
            {
                Id = id,
                DisplayName = id,
                Category = ItemCategory.Loot,
                Activation = ItemActivation.Passive,
                Target = ItemTarget.None,
                Shape = ItemShape.Parse(pattern.Split('\n')),
                Tags = ItemTag.Data,
                EnergyCost = 0,
                Value = value
            };
        }

        public static ItemCatalog Catalog(params ItemDefinition[] definitions)
        {
            return new ItemCatalog(definitions);
        }

        public static CombatActor Actor(string id, int hp, int ap, int width, int height, ItemCatalog catalog, params string[] itemIds)
        {
            var actor = new CombatActor(id, id, hp, ap, new BagInventory(width, height), id == "player");
            for (int i = 0; i < itemIds.Length; i++)
            {
                PlacedItem placed;
                if (!actor.Bag.TryAdd(itemIds[i], catalog.Get(itemIds[i]).Shape, out placed))
                {
                    throw new System.InvalidOperationException("Cannot fit item '" + itemIds[i] + "' into actor '" + id + "'.");
                }
            }

            return actor;
        }

        public static PlacedItem PlaceAt(CombatActor actor, ItemCatalog catalog, string itemId, int x, int y)
        {
            PlacedItem item;
            string error;
            if (!actor.Bag.TryAddAt(itemId, catalog.Get(itemId).Shape, new Cell(x, y), 0, true, out item, out error))
            {
                throw new System.InvalidOperationException(error);
            }

            return item;
        }

        public static PlacedItem FirstItem(CombatActor actor)
        {
            IReadOnlyList<PlacedItem> items = actor.Bag.Items;
            return items.Count > 0 ? items[0] : null;
        }

        public static PlacedItem FindItem(CombatActor actor, string definitionId)
        {
            IReadOnlyList<PlacedItem> items = actor.Bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].DefinitionId == definitionId)
                {
                    return items[i];
                }
            }

            return null;
        }
    }
}
