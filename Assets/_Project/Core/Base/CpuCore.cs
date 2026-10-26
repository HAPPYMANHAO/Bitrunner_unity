using System;

namespace BitRunner.Core
{
    /// <summary>
    /// 中央处理器：整个游戏里**唯一不占战斗背包格子**的成长载体。
    ///
    /// 芯片插在 <see cref="Grid"/> 上，效果经 <see cref="TraitEngine.EvaluateChips"/>
    /// 汇总成 <see cref="GlobalModifiers"/>，在战斗开始时注入。它不随装备丢失，
    /// 所以这是「死了会变弱」与「我一直在变强」能同时成立的地方。
    ///
    /// 空间用**格子制**而不是属性点制：与全局背包同构，玩家可以直接复用已有的
    /// 空间管理直觉，而芯片形状（1×1 / 1×2 / 2×2）本身又是一台小型解谜。
    /// </summary>
    public sealed class CpuCore
    {
        public const int MaxLevel = 5;

        public int Level { get; private set; }

        public BagInventory Grid { get; private set; }

        public CpuCore()
        {
            Level = 1;
            Grid = new BagInventory(WidthForLevel(1), HeightForLevel(1));
        }

        internal void RestoreLevel(int level)
        {
            Level = level < 1 ? 1 : (level > MaxLevel ? MaxLevel : level);
            Grid = new BagInventory(WidthForLevel(Level), HeightForLevel(Level));
        }

        internal void RestoreGrid(BagInventory grid)
        {
            if (grid != null)
            {
                Grid = grid;
            }
        }

        /// <summary>L1 2×2 / L2 3×3 / L3 3×4 / L4 4×4 / L5 5×4（见 ITEM_TIER_PLAN §6.2）。</summary>
        public static int WidthForLevel(int level)
        {
            switch (level)
            {
                case 1:
                    return 2;
                case 2:
                    return 3;
                case 3:
                    return 3;
                case 4:
                    return 4;
                default:
                    return 5;
            }
        }

        public static int HeightForLevel(int level)
        {
            switch (level)
            {
                case 1:
                    return 2;
                case 2:
                    return 3;
                case 3:
                case 4:
                case 5:
                    return 4;
                default:
                    return 2;
            }
        }

        public int Width
        {
            get { return Grid.Grid.Width; }
        }

        public int Height
        {
            get { return Grid.Grid.Height; }
        }

        public bool CanUpgrade
        {
            get { return Level < MaxLevel; }
        }

        /// <summary>能插的芯片档位上限 = CPU 等级。低级 CPU 插不进高级芯片。</summary>
        public int MaxChipTier
        {
            get { return Level; }
        }

        public int UpgradeCost
        {
            get
            {
                switch (Level)
                {
                    case 1:
                        return 400;
                    case 2:
                        return 800;
                    case 3:
                        return 1400;
                    case 4:
                        return 2600;
                    default:
                        return 0;
                }
            }
        }

        public string[] UpgradeMaterialIds
        {
            get
            {
                switch (Level)
                {
                    case 1:
                        return new[] { "scrap", "scrap", "wire" };
                    case 2:
                        return new[] { "circuit", "alloy" };
                    case 3:
                        return new[] { "circuit", "circuit", "alloy", "alloy" };
                    case 4:
                        return new[] { "circuit", "circuit", "circuit", "alloy", "alloy", "alloy" };
                    default:
                        return new string[0];
                }
            }
        }

        /// <summary>扫描当前芯片，得到这份 CPU 提供的全局加成。UI 与战斗创建都用它。</summary>
        public GlobalModifiers EvaluateModifiers(ItemCatalog catalog)
        {
            if (catalog == null)
            {
                return GlobalModifiers.None;
            }

            return new TraitEngine(catalog).EvaluateChips(Grid);
        }

        /// <summary>只有带 <see cref="ItemTag.CpuChip"/> 的芯片能插进 CPU。</summary>
        public static bool CanAccept(ItemDefinition definition)
        {
            return definition != null
                   && definition.Category == ItemCategory.Tool
                   && (definition.Tags & ItemTag.CpuChip) != 0;
        }

        /// <summary>芯片档位不能超过 CPU 等级。</summary>
        public bool Accepts(ItemDefinition definition)
        {
            return CanAccept(definition) && definition.Level <= MaxChipTier;
        }

        public bool TryUpgrade(CampaignState campaign, BagInventory stash, out string error)
        {
            if (campaign == null)
            {
                throw new ArgumentNullException(nameof(campaign));
            }

            if (Level >= MaxLevel)
            {
                error = "中央处理器已满级。";
                return false;
            }

            if (!FacilityUpgrade.TryPay(campaign, stash, UpgradeCost, UpgradeMaterialIds, out error))
            {
                return false;
            }

            int nextLevel = Level + 1;
            Grid.ResizeTo(WidthForLevel(nextLevel), HeightForLevel(nextLevel));
            Level = nextLevel;
            error = null;
            return true;
        }
    }
}
