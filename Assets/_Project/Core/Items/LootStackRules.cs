using System;

namespace BitRunner.Core
{
    /// <summary>
    /// 战利品堆叠数量。掉落池按物品 id 抽取，堆叠物品（弹药等）
    /// 再随机成 2–6 个一组；不可堆叠物品保持 1。
    /// </summary>
    public static class LootStackRules
    {
        public static int Roll(ItemDefinition definition, int seed)
        {
            if (definition == null)
            {
                return 1;
            }

            int max = definition.MaxStack;
            if (max <= 1)
            {
                return 1;
            }

            int min = 1;
            int cap = Math.Min(max, 3);
            if (definition.Category == ItemCategory.Ammo)
            {
                min = 2;
                cap = Math.Min(max, 6);
            }

            if (min > cap)
            {
                min = 1;
            }

            if (cap <= 1)
            {
                return 1;
            }

            return new Random(seed).Next(min, cap + 1);
        }
    }
}
