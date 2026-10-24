using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class EnemyLoadoutSlot
    {
        public string Id;
        public bool Required = true;
        public ItemCategory Category;
        public ItemTag Tag = ItemTag.None;
        public bool RequireDamageType;
        public DamageType DamageType;
        public bool AllowRanged;
        public int MinCells;
        public int MaxCells = int.MaxValue;
        public int MinWidth = 1;
        public int MaxWidth = EnemyLibrary.BagWidth;
        public int MinHeight = 1;
        public int MaxHeight = EnemyLibrary.BagHeight;
        public int MinHeal;
        public int MinValue;
        public int MaxValue = int.MaxValue;
        public int Count = 1;
        public string[] AllowedIds;
        public string[] ExcludedIds;

        public bool Matches(ItemDefinition definition)
        {
            if (definition == null || definition.Category != Category)
            {
                return false;
            }

            if (Tag != ItemTag.None && (definition.Tags & Tag) == 0)
            {
                return false;
            }

            if (RequireDamageType && definition.Type != DamageType)
            {
                return false;
            }

            if (!AllowRanged && definition.UsesAmmo)
            {
                return false;
            }

            int cells = definition.Shape.CellCount;
            if (cells < MinCells || cells > MaxCells)
            {
                return false;
            }

            if (definition.Shape.Width < MinWidth || definition.Shape.Width > MaxWidth)
            {
                return false;
            }

            if (definition.Shape.Height < MinHeight || definition.Shape.Height > MaxHeight)
            {
                return false;
            }

            if (definition.Heal < MinHeal)
            {
                return false;
            }

            if (definition.Value < MinValue || definition.Value > MaxValue)
            {
                return false;
            }

            if (AllowedIds != null && AllowedIds.Length > 0 && Array.IndexOf(AllowedIds, definition.Id) < 0)
            {
                return false;
            }

            if (ExcludedIds != null && Array.IndexOf(ExcludedIds, definition.Id) >= 0)
            {
                return false;
            }

            return true;
        }
    }

    public sealed class EnemyLoadoutProfile
    {
        public EnemyLoadoutSlot[] Required = new EnemyLoadoutSlot[0];
        public EnemyLoadoutSlot[] Optional = new EnemyLoadoutSlot[0];
        public int OptionalValueBudget = 100;
    }

    public static class EnemyLoadoutProfiles
    {
        private static readonly Dictionary<string, EnemyLoadoutProfile> Profiles =
            new Dictionary<string, EnemyLoadoutProfile>(StringComparer.Ordinal)
            {
                { "scavenger", new EnemyLoadoutProfile
                    {
                        Required = new[] { WeaponSmall("weapon"), Loot("loot") },
                        Optional = new[] { ArmorSmall("armor"), Loot("extra-loot") },
                        OptionalValueBudget = 90
                    }
                },
                { "enforcer", new EnemyLoadoutProfile
                    {
                        Required = new[] { ArmorAny("armor"), WeaponSmall("weapon-a"), WeaponSmall("weapon-b") },
                        Optional = new[] { Heal("heal"), Loot("loot") },
                        OptionalValueBudget = 80
                    }
                },
                { "drone", new EnemyLoadoutProfile
                    {
                        Required = new[] { Ranged("weapon"), Ammo("ammo"), Tech("tech") },
                        Optional = new[] { Loot("loot") },
                        OptionalValueBudget = 70
                    }
                },
                { "bulwark", new EnemyLoadoutProfile
                    {
                        Required = new[] { ArmorLarge("large-armor"), ArmorSmall("small-armor"), WeaponSmall("weapon"), Heal("heal") },
                        Optional = new[] { Loot("loot") },
                        OptionalValueBudget = 70
                    }
                },
                { "shocker", new EnemyLoadoutProfile
                    {
                        Required = new[] { WeaponMedium("weapon"), Tech("tech"), Heal("heal") },
                        Optional = new[] { Loot("loot") },
                        OptionalValueBudget = 70
                    }
                },
                { "hacker", new EnemyLoadoutProfile
                    {
                        Required = new[] { Tech("tech"), Ranged("weapon"), Ammo("ammo") },
                        Optional = new[] { Loot("loot") },
                        OptionalValueBudget = 70
                    }
                },
                { "poisoner", new EnemyLoadoutProfile
                    {
                        Required = new[] { ToxinWeapon("weapon"), Heal("heal") },
                        Optional = new[] { Loot("loot"), Ammo("ammo") },
                        OptionalValueBudget = 80
                    }
                },
                { "suffocator", new EnemyLoadoutProfile
                    {
                        Required = new[] { AsphyxiationWeapon("weapon"), ArmorSmall("armor"), Heal("heal") },
                        Optional = new[] { Loot("loot") },
                        OptionalValueBudget = 70
                    }
                },
                { "wild_dog", new EnemyLoadoutProfile
                    {
                        Required = new[] { Bite("bite"), Loot("loot") },
                        Optional = new[] { Loot("extra-loot") },
                        OptionalValueBudget = 20
                    }
                },
                { "squirrel", new EnemyLoadoutProfile
                    {
                        Required = new[] { Bite("bite"), Loot("loot") },
                        Optional = new[] { Loot("extra-loot") },
                        OptionalValueBudget = 20
                    }
                },
                { "raider", new EnemyLoadoutProfile
                    {
                        Required = new[] { WeaponSmall("weapon-a"), WeaponSmall("weapon-b"), ArmorSmall("armor"), Loot("loot") },
                        Optional = new[] { Heal("heal") },
                        OptionalValueBudget = 60
                    }
                }
            };

        public static EnemyLoadoutProfile Get(string archetypeId)
        {
            EnemyLoadoutProfile profile;
            return Profiles.TryGetValue(archetypeId, out profile) ? profile : null;
        }

        private static EnemyLoadoutSlot ArmorLarge(string id)
        {
            return new EnemyLoadoutSlot
            {
                Id = id,
                Category = ItemCategory.Armor,
                Tag = ItemTag.Shield,
                MinCells = 4,
                MinWidth = 2,
                MinHeight = 2,
                MinValue = 1
            };
        }

        private static EnemyLoadoutSlot ArmorSmall(string id)
        {
            return new EnemyLoadoutSlot
            {
                Id = id,
                Category = ItemCategory.Armor,
                Tag = ItemTag.Shield,
                MaxCells = 2,
                MaxWidth = 2,
                MaxHeight = 2,
                MinValue = 1
            };
        }

        private static EnemyLoadoutSlot ArmorAny(string id)
        {
            return new EnemyLoadoutSlot
            {
                Id = id,
                Category = ItemCategory.Armor,
                Tag = ItemTag.Shield,
                MinValue = 1
            };
        }

        private static EnemyLoadoutSlot WeaponSmall(string id)
        {
            return new EnemyLoadoutSlot
            {
                Id = id,
                Category = ItemCategory.Weapon,
                Tag = ItemTag.Blade,
                MaxCells = 2,
                MaxWidth = 2,
                MaxHeight = 2,
                MinValue = 1
            };
        }

        private static EnemyLoadoutSlot WeaponMedium(string id)
        {
            return new EnemyLoadoutSlot
            {
                Id = id,
                Category = ItemCategory.Weapon,
                Tag = ItemTag.Blade,
                MaxCells = 3,
                MaxWidth = 2,
                MaxHeight = 3,
                MinValue = 1
            };
        }

        private static EnemyLoadoutSlot Ranged(string id)
        {
            return new EnemyLoadoutSlot
            {
                Id = id,
                Category = ItemCategory.Weapon,
                RequireDamageType = true,
                DamageType = DamageType.Projectile,
                AllowRanged = true,
                MaxCells = 2,
                MaxWidth = 2,
                MaxHeight = 2,
                MinValue = 1
            };
        }

        private static EnemyLoadoutSlot ToxinWeapon(string id)
        {
            return new EnemyLoadoutSlot
            {
                Id = id,
                Category = ItemCategory.Weapon,
                RequireDamageType = true,
                DamageType = DamageType.Toxin,
                MaxCells = 2,
                MaxWidth = 2,
                MaxHeight = 2,
                MinValue = 1
            };
        }

        private static EnemyLoadoutSlot AsphyxiationWeapon(string id)
        {
            return new EnemyLoadoutSlot
            {
                Id = id,
                Category = ItemCategory.Weapon,
                RequireDamageType = true,
                DamageType = DamageType.Asphyxiation,
                MaxCells = 2,
                MaxWidth = 2,
                MaxHeight = 2,
                MinValue = 1
            };
        }

        private static EnemyLoadoutSlot Bite(string id)
        {
            return new EnemyLoadoutSlot
            {
                Id = id,
                Category = ItemCategory.Weapon,
                AllowedIds = new[] { "wild_bite" },
                MaxCells = 1,
                MaxWidth = 1,
                MaxHeight = 1
            };
        }

        private static EnemyLoadoutSlot Heal(string id)
        {
            return new EnemyLoadoutSlot
            {
                Id = id,
                Category = ItemCategory.Consumable,
                MinHeal = 1,
                MinValue = 1
            };
        }

        private static EnemyLoadoutSlot Tech(string id)
        {
            return new EnemyLoadoutSlot
            {
                Id = id,
                Category = ItemCategory.Tool,
                Tag = ItemTag.Chip,
                MaxCells = 1,
                MaxWidth = 1,
                MaxHeight = 1
            };
        }

        private static EnemyLoadoutSlot Ammo(string id)
        {
            return new EnemyLoadoutSlot
            {
                Id = id,
                Category = ItemCategory.Ammo,
                Count = 4,
                MaxCells = 1,
                MaxWidth = 1,
                MaxHeight = 1
            };
        }

        private static EnemyLoadoutSlot Loot(string id)
        {
            return new EnemyLoadoutSlot
            {
                Id = id,
                Category = ItemCategory.Loot,
                MinValue = 1
            };
        }
    }

    public static class EnemyLoadoutGenerator
    {
        public static bool TryGenerate(
            EnemyArchetype archetype,
            ItemCatalog catalog,
            int seed,
            out EnemyItemPlacement[] placements)
        {
            placements = null;
            if (archetype == null || catalog == null)
            {
                return false;
            }

            EnemyLoadoutProfile profile = EnemyLoadoutProfiles.Get(archetype.Id);
            if (profile == null)
            {
                placements = archetype.Items;
                return true;
            }

            var random = new Random(seed);
            var bag = new BagInventory(EnemyLibrary.BagWidth, EnemyLibrary.BagHeight);
            var result = new List<EnemyItemPlacement>();

            for (int i = 0; i < profile.Required.Length; i++)
            {
                if (!TryPlaceSlot(bag, catalog, profile.Required[i], random, result))
                {
                    return false;
                }
            }

            int remainingBudget = Math.Max(0, profile.OptionalValueBudget);
            for (int i = 0; i < profile.Optional.Length; i++)
            {
                int spent;
                TryPlaceOptional(bag, catalog, profile.Optional[i], random, remainingBudget, result, out spent);
                remainingBudget -= spent;
            }

            placements = result.ToArray();
            return placements.Length > 0;
        }

        private static bool TryPlaceSlot(
            BagInventory bag,
            ItemCatalog catalog,
            EnemyLoadoutSlot slot,
            Random random,
            List<EnemyItemPlacement> result)
        {
            List<ItemDefinition> candidates = Candidates(catalog, slot);
            if (candidates.Count == 0)
            {
                return false;
            }

            Shuffle(candidates, random);
            for (int i = 0; i < candidates.Count; i++)
            {
                ItemDefinition definition = candidates[i];
                int rotation;
                Cell origin;
                if (!TryFindPlacement(bag, definition, random, out rotation, out origin))
                {
                    continue;
                }

                PlacedItem placed;
                string error;
                if (!bag.TryAddAt(
                        definition.Id,
                        definition.Shape,
                        origin,
                        rotation,
                        true,
                        slot.Count,
                        out placed,
                        out error))
                {
                    continue;
                }

                result.Add(new EnemyItemPlacement
                {
                    ItemId = definition.Id,
                    X = origin.X,
                    Y = origin.Y,
                    Rotation = rotation,
                    Count = slot.Count
                });
                return true;
            }

            return false;
        }

        private static bool TryPlaceOptional(
            BagInventory bag,
            ItemCatalog catalog,
            EnemyLoadoutSlot slot,
            Random random,
            int remainingBudget,
            List<EnemyItemPlacement> result,
            out int spent)
        {
            spent = 0;
            if (remainingBudget <= 0)
            {
                return false;
            }

            List<ItemDefinition> candidates = Candidates(catalog, slot);
            Shuffle(candidates, random);
            for (int i = 0; i < candidates.Count; i++)
            {
                ItemDefinition definition = candidates[i];
                int cost = definition.Value * Math.Max(1, slot.Count);
                if (cost > remainingBudget)
                {
                    continue;
                }

                int rotation;
                Cell origin;
                if (!TryFindPlacement(bag, definition, random, out rotation, out origin))
                {
                    continue;
                }

                PlacedItem placed;
                string error;
                if (!bag.TryAddAt(
                        definition.Id,
                        definition.Shape,
                        origin,
                        rotation,
                        true,
                        slot.Count,
                        out placed,
                        out error))
                {
                    continue;
                }

                result.Add(new EnemyItemPlacement
                {
                    ItemId = definition.Id,
                    X = origin.X,
                    Y = origin.Y,
                    Rotation = rotation,
                    Count = slot.Count
                });
                spent = cost;
                return true;
            }

            return false;
        }

        private static List<ItemDefinition> Candidates(ItemCatalog catalog, EnemyLoadoutSlot slot)
        {
            var candidates = new List<ItemDefinition>();
            foreach (ItemDefinition definition in catalog.Definitions)
            {
                if (slot.Matches(definition))
                {
                    candidates.Add(definition);
                }
            }

            candidates.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            return candidates;
        }

        private static bool TryFindPlacement(
            BagInventory bag,
            ItemDefinition definition,
            Random random,
            out int rotation,
            out Cell origin)
        {
            var options = new List<PlacementOption>();
            for (int r = 0; r < 4; r++)
            {
                ItemShape shape = definition.Shape.Rotate(r);
                if (shape.Width > bag.Grid.Width || shape.Height > bag.Grid.Height)
                {
                    continue;
                }

                for (int y = 0; y <= bag.Grid.Height - shape.Height; y++)
                {
                    for (int x = 0; x <= bag.Grid.Width - shape.Width; x++)
                    {
                        var candidate = new Cell(x, y);
                        if (bag.Grid.CanPlace(shape, candidate))
                        {
                            options.Add(new PlacementOption(r, candidate));
                        }
                    }
                }
            }

            if (options.Count == 0)
            {
                rotation = 0;
                origin = Cell.Zero;
                return false;
            }

            PlacementOption picked = options[random.Next(options.Count)];
            rotation = picked.Rotation;
            origin = picked.Origin;
            return true;
        }

        private static void Shuffle<T>(List<T> values, Random random)
        {
            for (int i = values.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T swap = values[i];
                values[i] = values[j];
                values[j] = swap;
            }
        }

        private readonly struct PlacementOption
        {
            public readonly int Rotation;
            public readonly Cell Origin;

            public PlacementOption(int rotation, Cell origin)
            {
                Rotation = rotation;
                Origin = origin;
            }
        }
    }
}
