namespace BitRunner.Core
{
    /// <summary>
    /// 探索辅助标志的效果。数值都是百分点（pp）：直接加减最终结果概率。
    /// 光源不直接加敌人概率，而是抬高警戒；警戒再影响遭遇敌人的概率。
    /// </summary>
    public sealed class AssistSpec
    {
        /// <summary>同系列只能装备一个：light / muffle / ward。</summary>
        public string Family;

        public int Tier = 1;

        /// <summary>战利品结果概率 ±pp。</summary>
        public int LootPercent;

        /// <summary>战斗结果概率 ±pp（消音器为负）。</summary>
        public int EnemyPercent;

        /// <summary>受伤结果概率 ±pp（护符为负）。</summary>
        public int DamagePercent;

        /// <summary>每次确认行动时永久提高的警戒值（光源为正）。</summary>
        public int AlertDelta;

        public bool HasEffect
        {
            get
            {
                return LootPercent != 0 || EnemyPercent != 0 || DamagePercent != 0 || AlertDelta != 0;
            }
        }
    }
}
