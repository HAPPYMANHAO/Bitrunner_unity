using System;

namespace BitRunner.Core
{
    /// <summary>
    /// 背包之外的全局加成（目前唯一来源是中央处理器的芯片）。
    ///
    /// <see cref="TraitEngine"/> 平时只扫描 <c>actor.Bag</c>，而芯片插在 CPU 网格上，
    /// 战斗时并不在背包里。于是这里把「芯片侧」的加成单独汇总成一个快照，
    /// 在创建 <see cref="CombatActor"/> 时一次性注入。
    ///
    /// 为什么用快照而不是战斗中实时读：芯片随时可换，若在战斗进行中换芯片，
    /// 生命上限与护甲基底会当场变化，护盾/减伤的结算会出现跨回合不一致。
    /// 快照让一场战斗内的数值恒定，换芯片从下一场战斗开始生效。
    /// </summary>
    public struct GlobalModifiers
    {
        /// <summary>
        /// 生命上限加成。**允许为负**：能量芯片的副作用就写在这里。
        /// 这是「增加能量必定有副作用」这条规则的落点，见 <see cref="TraitEffect.AddMaxHp"/>。
        /// </summary>
        public int MaxHp;

        /// <summary>
        /// 基础能量加成。全游戏只生效一张，取所有芯片里的最高值，
        /// 不是求和——否则塞满能量芯片就能无限行动。
        /// </summary>
        public int BaseEnergy;

        /// <summary>五系护甲基底，与背包里护甲的被动护甲相加。</summary>
        public int ArmorBase;

        public bool IsEmpty
        {
            get { return MaxHp == 0 && BaseEnergy == 0 && ArmorBase == 0; }
        }

        public static GlobalModifiers None
        {
            get { return default(GlobalModifiers); }
        }

        public GlobalModifiers Combine(GlobalModifiers other)
        {
            return new GlobalModifiers
            {
                MaxHp = MaxHp + other.MaxHp,
                BaseEnergy = Math.Max(BaseEnergy, other.BaseEnergy),
                ArmorBase = ArmorBase + other.ArmorBase
            };
        }

        /// <summary>把加成落到生命上限上。永远至少留 1 点，避免负加成把角色直接压成 0 血。</summary>
        public int ApplyMaxHp(int baseMaxHp)
        {
            return Math.Max(1, baseMaxHp + MaxHp);
        }

        /// <summary>把加成落到底层护甲上。护甲不能是负数。</summary>
        public int ApplyArmorBase()
        {
            return ArmorBase > 0 ? ArmorBase : 0;
        }

        public string Describe()
        {
            if (IsEmpty)
            {
                return "无";
            }

            string text = string.Empty;
            if (MaxHp != 0)
            {
                text += (MaxHp > 0 ? "HP +" : "HP ") + MaxHp;
            }

            if (BaseEnergy != 0)
            {
                text += (text.Length > 0 ? "，" : string.Empty) + "能量 +" + BaseEnergy;
            }

            if (ArmorBase != 0)
            {
                text += (text.Length > 0 ? "，" : string.Empty) + "底甲 +" + ArmorBase;
            }

            return text;
        }
    }
}
