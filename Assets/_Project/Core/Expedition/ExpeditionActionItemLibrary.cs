using System.Collections.Generic;

namespace BitRunner.Core
{
    public static class ExpeditionActionItemLibrary
    {
        private static readonly string[] Ids =
        {
            "scan", "force", "search", "detour", "sneak",
            "track", "forage", "climb", "hide", "rush",
            "breach", "hack", "suppress", "slip",
            "decode", "trace", "jam", "dive", "listen", "retreat"
        };

        private static readonly string[] Names =
        {
            "扫描", "破门", "搜索", "绕行", "潜入",
            "追踪", "采集", "翻越", "隐蔽", "突进",
            "爆破", "接入", "压制", "潜行",
            "解析", "回波", "干扰", "跃入", "监听", "撤退"
        };

        private static readonly string[] Glyphs =
        {
            "扫", "破", "搜", "绕", "潜",
            "追", "采", "越", "隐", "冲",
            "爆", "接", "压", "潜",
            "析", "波", "扰", "跃", "听", "退"
        };

        public static string Glyph(string actionId)
        {
            for (int i = 0; i < Ids.Length; i++)
            {
                if (Ids[i] == actionId)
                {
                    return Glyphs[i];
                }
            }

            return "行";
        }

        public static ItemCatalog CreateCatalog()
        {
            var definitions = new List<ItemDefinition>();
            for (int i = 0; i < Ids.Length; i++)
            {
                definitions.Add(new ItemDefinition
                {
                    Id = Ids[i],
                    DisplayName = Names[i],
                    Category = ItemCategory.Action,
                    Activation = ItemActivation.Passive,
                    Target = ItemTarget.None,
                    Shape = ItemShape.Parse("##", "##"),
                    Value = 0
                });
            }

            return new ItemCatalog(definitions);
        }

        public static string DisplayName(string actionId)
        {
            for (int i = 0; i < Ids.Length; i++)
            {
                if (Ids[i] == actionId)
                {
                    return Names[i];
                }
            }

            return actionId;
        }
    }
}
