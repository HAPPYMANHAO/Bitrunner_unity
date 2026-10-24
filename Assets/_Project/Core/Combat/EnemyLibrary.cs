using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class EnemyItemPlacement
    {
        public string ItemId;
        public int X;
        public int Y;
        public int Rotation;
        public int Count = 1;
    }

    public sealed class EnemyArchetype
    {
        public string Id;
        public string DisplayName;
        public int MaxHp;
        public int BaseEnergy;
        public EnemyItemPlacement[] Items;
    }

    public static class EnemyLibrary
    {
        public const int BagWidth = 4;
        public const int BagHeight = 3;

        private static readonly EnemyArchetype[] Archetypes =
        {
            new EnemyArchetype
            {
                Id = "scavenger",
                DisplayName = "拾荒者",
                MaxHp = 20,
                BaseEnergy = 3,
                Items = new[]
                {
                    Item("scrap_blade", 0, 0),
                    Item("coin", 2, 0),
                    Item("coin", 3, 0),
                    Item("idol", 0, 1)
                }
            },
            new EnemyArchetype
            {
                Id = "enforcer",
                DisplayName = "执法者",
                MaxHp = 24,
                BaseEnergy = 3,
                Items = new[]
                {
                    Item("plate", 0, 0),
                    Item("scrap_blade", 2, 0),
                    Item("scrap_blade", 2, 1),
                    Item("coin", 2, 2)
                }
            },
            new EnemyArchetype
            {
                Id = "drone",
                DisplayName = "巡逻无人机",
                MaxHp = 16,
                BaseEnergy = 2,
                Items = new[]
                {
                    Item("pulse", 0, 0),
                    Item("chip", 2, 0),
                    Item("coin", 3, 0),
                    Item("coin", 0, 1),
                    Item("ammo", 3, 1, 4)
                }
            },
            new EnemyArchetype
            {
                Id = "bulwark",
                DisplayName = "重装守卫",
                MaxHp = 34,
                BaseEnergy = 2,
                Items = new[]
                {
                    Item("plate", 0, 0),
                    Item("buckler", 2, 0),
                    Item("scrap_blade", 2, 1),
                    Item("patch", 0, 2)
                }
            },
            new EnemyArchetype
            {
                Id = "shocker",
                DisplayName = "电击兵",
                MaxHp = 22,
                BaseEnergy = 3,
                Items = new[]
                {
                    Item("rod", 0, 0),
                    Item("jammer", 2, 0),
                    Item("patch", 3, 0),
                    Item("coin", 2, 1)
                }
            },
            new EnemyArchetype
            {
                Id = "hacker",
                DisplayName = "黑客",
                MaxHp = 18,
                BaseEnergy = 3,
                Items = new[]
                {
                    Item("jammer", 0, 0),
                    Item("chip", 2, 0),
                    Item("pulse", 2, 1),
                    Item("coin", 0, 1),
                    Item("ammo", 1, 0, 4)
                }
            },
            new EnemyArchetype
            {
                Id = "poisoner",
                DisplayName = "施毒者",
                MaxHp = 18,
                BaseEnergy = 3,
                Items = new[]
                {
                    Item("toxin_dart", 0, 0),
                    Item("patch", 2, 0),
                    Item("coin", 3, 0),
                    Item("coin", 0, 1)
                }
            },
            new EnemyArchetype
            {
                Id = "suffocator",
                DisplayName = "窒息者",
                MaxHp = 20,
                BaseEnergy = 2,
                Items = new[]
                {
                    Item("vacuum", 0, 0),
                    Item("buckler", 0, 1),
                    Item("patch", 2, 0),
                    Item("coin", 2, 1)
                }
            },
            new EnemyArchetype
            {
                Id = "wild_dog",
                DisplayName = "野犬",
                MaxHp = 8,
                BaseEnergy = 2,
                Items = new[]
                {
                    Item("wild_bite", 0, 0),
                    Item("junk", 1, 0),
                    Item("stone", 2, 0)
                }
            },
            new EnemyArchetype
            {
                Id = "squirrel",
                DisplayName = "松鼠",
                MaxHp = 5,
                BaseEnergy = 2,
                Items = new[]
                {
                    Item("wild_bite", 0, 0),
                    Item("branch", 1, 0),
                    Item("junk", 3, 0)
                }
            },
            new EnemyArchetype
            {
                Id = "raider",
                DisplayName = "掠夺者",
                MaxHp = 26,
                BaseEnergy = 3,
                Items = new[]
                {
                    Item("scrap_blade", 0, 0),
                    Item("scrap_blade", 2, 0),
                    Item("buckler", 0, 1),
                    Item("coin", 2, 1)
                }
            }
        };

        public static IReadOnlyList<EnemyArchetype> All
        {
            get { return Archetypes; }
        }

        public static string[] AllIds
        {
            get
            {
                var ids = new string[Archetypes.Length];
                for (int i = 0; i < Archetypes.Length; i++)
                {
                    ids[i] = Archetypes[i].Id;
                }

                return ids;
            }
        }

        public static EnemyArchetype Get(string id)
        {
            for (int i = 0; i < Archetypes.Length; i++)
            {
                if (Archetypes[i].Id == id)
                {
                    return Archetypes[i];
                }
            }

            return null;
        }

        public static CombatActor Create(string id, ItemCatalog catalog, int bonusHp)
        {
            return Create(id, catalog, bonusHp, 0);
        }

        public static CombatActor Create(string id, ItemCatalog catalog, int bonusHp, int seed)
        {
            EnemyArchetype archetype = Get(id) ?? Archetypes[0];
            var actor = new CombatActor(
                archetype.Id,
                archetype.DisplayName,
                archetype.MaxHp + Math.Max(0, bonusHp),
                archetype.BaseEnergy,
                new BagInventory(BagWidth, BagHeight),
                false);
            actor.Brain = new EnemyBrain();

            EnemyItemPlacement[] placements;
            if (!EnemyLoadoutGenerator.TryGenerate(archetype, catalog, seed, out placements)
                || placements == null
                || placements.Length == 0)
            {
                placements = archetype.Items;
            }

            for (int i = 0; i < placements.Length; i++)
            {
                Place(actor.Bag, catalog, placements[i]);
            }

            ItemDurability.EnsureBag(actor.Bag, catalog);
            return actor;
        }

        private static void Place(BagInventory bag, ItemCatalog catalog, EnemyItemPlacement placement)
        {
            if (bag == null || catalog == null || placement == null)
            {
                return;
            }

            ItemDefinition definition;
            if (!catalog.TryGet(placement.ItemId, out definition))
            {
                return;
            }

            PlacedItem placed;
            string error;
            if (!bag.TryAddAt(
                    placement.ItemId,
                    definition.Shape,
                    new Cell(placement.X, placement.Y),
                    placement.Rotation,
                    true,
                    placement.Count,
                    out placed,
                    out error))
            {
                bag.TryAdd(placement.ItemId, definition.Shape, true, placement.Count, out placed);
            }
        }

        public static string[] PickTypes(Random random, int count)
        {
            var ids = new List<string>(AllIds);
            Shuffle(ids, random);

            var result = new string[Math.Max(0, count)];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = ids[i % ids.Count];
            }

            return result;
        }

        public static string PickMinion(int seed, string excludeId, int index)
        {
            var candidates = new List<string>();
            for (int i = 0; i < Archetypes.Length; i++)
            {
                if (Archetypes[i].Id != excludeId)
                {
                    candidates.Add(Archetypes[i].Id);
                }
            }

            if (candidates.Count == 0)
            {
                return Archetypes[0].Id;
            }

            var random = new Random(seed + index * 7919);
            return candidates[random.Next(candidates.Count)];
        }

        private static void Shuffle(List<string> values, Random random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                string swap = values[i];
                values[i] = values[j];
                values[j] = swap;
            }
        }

        private static EnemyItemPlacement Item(string itemId, int x, int y, int count = 1)
        {
            return new EnemyItemPlacement
            {
                ItemId = itemId,
                X = x,
                Y = y,
                Rotation = 0,
                Count = count
            };
        }
    }
}
