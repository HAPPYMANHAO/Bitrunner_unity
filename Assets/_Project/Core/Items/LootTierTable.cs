using System.Collections.Generic;

namespace BitRunner.Core
{
    /// <summary>
    /// 掉落池的档位加权。
    ///
    /// 地点的档位区间与档内比例写在 <see cref="DiveKeyDefinition"/> / <see cref="DiveRecipe"/> 上，
    /// 这里负责把「某档占百分之几」换算成一张可以均匀抽样的数组：
    /// 每件物品重复 <c>档权重 / 该档件数</c> 次，于是「档的总份额」由表控制，
    /// 档内每件物品平分，不会因为某档只有一件东西就把它抽到刷屏。
    /// </summary>
    public static class LootTierTable
    {
        /// <summary>权重放大倍数。档权重是百分数，乘上去换成整数重复次数。</summary>
        public const int WeightScale = 10;

        /// <summary>
        /// 按档位区间与档内比例展开掉落池。
        /// 目录里查不到的 id 按最低档处理，绝不静默丢弃——否则一个拼错的 id 会变成「某个地点永远不掉那件东西」。
        /// </summary>
        public static string[] Expand(string[] pool, ItemCatalog catalog, int minTier, int[] tierWeights)
        {
            if (pool == null || pool.Length == 0)
            {
                return pool;
            }

            if (catalog == null || tierWeights == null || tierWeights.Length == 0 || minTier <= 0)
            {
                return pool;
            }

            int maxTier = minTier + tierWeights.Length - 1;
            var buckets = new List<string>[tierWeights.Length];
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = new List<string>();
            }

            for (int i = 0; i < pool.Length; i++)
            {
                string id = pool[i];
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                int tier = minTier;
                ItemDefinition definition;
                if (catalog.TryGet(id, out definition))
                {
                    tier = definition.Level;
                }

                if (tier < minTier || tier > maxTier)
                {
                    continue;
                }

                buckets[tier - minTier].Add(id);
            }

            int total = 0;
            for (int i = 0; i < buckets.Length; i++)
            {
                total += buckets[i].Count;
            }

            if (total == 0)
            {
                return pool;
            }

            var expanded = new List<string>(total * WeightScale);
            for (int tier = 0; tier < buckets.Length; tier++)
            {
                List<string> bucket = buckets[tier];
                if (bucket.Count == 0 || tierWeights[tier] <= 0)
                {
                    continue;
                }

                int repeat = tierWeights[tier] * WeightScale / bucket.Count;
                if (repeat < 1)
                {
                    repeat = 1;
                }

                for (int i = 0; i < bucket.Count; i++)
                {
                    for (int r = 0; r < repeat; r++)
                    {
                        expanded.Add(bucket[i]);
                    }
                }
            }

            return expanded.Count > 0 ? expanded.ToArray() : pool;
        }

        /// <summary>
        /// 这次掉落里「至少出现一件」的档位上限，用于 UI 与日志描述。
        /// 返回 0 表示没有档位信息。
        /// </summary>
        public static int TopTier(int minTier, int[] tierWeights)
        {
            if (tierWeights == null || tierWeights.Length == 0 || minTier <= 0)
            {
                return 0;
            }

            for (int i = tierWeights.Length - 1; i >= 0; i--)
            {
                if (tierWeights[i] > 0)
                {
                    return minTier + i;
                }
            }

            return 0;
        }
    }
}
