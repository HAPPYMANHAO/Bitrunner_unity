using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public enum ExpeditionSiteTheme
    {
        Wild,
        Facility,
        Military,
        Signal,

        /// <summary>深空潜网：失压的轨道残骸，气压与低温是主要威胁。</summary>
        Orbital,

        /// <summary>核心潜网：终局区域，只有一台还在运转的东西。</summary>
        Core
    }

    public enum ExpeditionRisk
    {
        Low,
        Medium,
        High
    }

    public enum ExpeditionOutcomeKind
    {
        Loot,
        Combat,
        Damage,
        Clue,
        Alert,
        Nothing,
        Exit
    }

    public struct ExpeditionContext
    {
        public int Hp;
        public int MaxHp;
        public int BagFree;
        public int Alert;
    }

    public sealed class ExpeditionOutcomeEntry
    {
        public ExpeditionOutcomeKind Kind;
        public int Weight;
        public string Text;
        public string[] LootIds;
        public string EnemyId;
        public int Damage;
        public int AlertDelta;
    }

    public sealed class ExpeditionActionOption
    {
        public string Id;
        public string Label;
        public string Glyph;
        public string Description;
        public ExpeditionRisk Risk;
        public bool IsExit;
        public List<ExpeditionOutcomeEntry> Outcomes = new List<ExpeditionOutcomeEntry>();
    }

    public sealed class ExpeditionResolution
    {
        public ExpeditionActionOption Action;
        public ExpeditionOutcomeEntry Outcome;
        public string Text;
        public bool IsExit;
        public bool AdvancedStage;

        public ExpeditionOutcomeKind Kind
        {
            get { return Outcome != null ? Outcome.Kind : ExpeditionOutcomeKind.Nothing; }
        }

        public string[] LootIds
        {
            get { return Outcome != null ? Outcome.LootIds : null; }
        }

        public string EnemyId
        {
            get { return Outcome != null ? Outcome.EnemyId : null; }
        }

        public int Damage
        {
            get { return Outcome != null ? Outcome.Damage : 0; }
        }
    }

    public sealed class ExpeditionScenario
    {
        /// <summary>每个潜网的探索阶段数，由潜网密钥决定（旧数据缺省 3）。</summary>
        public int StageCount { get; private set; }

        private readonly Random _random;
        private readonly string[] _lootPool;
        private readonly string[] _enemyPool;
        private readonly int _lootPerFind;
        private readonly int _hazardDamage;
        private readonly List<ExpeditionActionOption> _options = new List<ExpeditionActionOption>();

        public int Seed { get; private set; }
        public ExpeditionSiteTheme Theme { get; private set; }
        public string SiteId { get; private set; }
        public string Title { get; private set; }
        public string IntroText { get; private set; }
        public int Stage { get; private set; }
        public int Alert { get; private set; }
        public bool IsComplete { get; private set; }
        public string LastResultText { get; private set; }

        public IReadOnlyList<ExpeditionActionOption> CurrentOptions
        {
            get { return _options; }
        }

        private ExpeditionScenario(
            int seed,
            ExpeditionSiteTheme theme,
            string siteId,
            string title,
            string intro,
            string[] lootPool,
            string[] enemyPool,
            int lootPerFind,
            int hazardDamage,
            int stageCount)
        {
            Seed = seed;
            Theme = theme;
            SiteId = siteId;
            Title = title;
            IntroText = intro;
            _lootPool = lootPool != null && lootPool.Length > 0 ? lootPool : new[] { "scrap" };
            _enemyPool = enemyPool != null && enemyPool.Length > 0 ? enemyPool : new[] { "scavenger" };
            _lootPerFind = Math.Max(1, lootPerFind);
            _hazardDamage = Math.Max(1, hazardDamage);
            StageCount = Math.Max(2, stageCount);
            _random = new Random(seed);
            LastResultText = intro;
            BuildOptions();
        }

        public static ExpeditionScenario Generate(int seed, DivePlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException("plan");
            }

            ExpeditionSiteTheme theme = ThemeFor(plan.KeyItemId);
            string siteTitle = TitleFor(theme, plan.Name);
            string intro = IntroFor(theme);
            string[] loot = plan.LootPool;
            string[] enemies = plan.EnemyPool;
            int lootPerFind = 2 + Math.Max(0, plan.LootCount) / 5;
            int hazardDamage = 4 + Math.Max(0, plan.HazardCount) / 3;
            int stageCount = plan.StageCount > 0 ? plan.StageCount : 3;

            return new ExpeditionScenario(
                seed,
                theme,
                plan.KeyItemId,
                siteTitle,
                intro,
                loot,
                enemies,
                lootPerFind,
                hazardDamage,
                stageCount);
        }

        public ExpeditionResolution Resolve(string actionId, ExpeditionContext context)
        {
            return Resolve(actionId, context, null);
        }

        public ExpeditionResolution Resolve(
            string actionId,
            ExpeditionContext context,
            IReadOnlyList<AssistSpec> assists)
        {
            if (IsComplete)
            {
                return null;
            }

            ExpeditionActionOption action = Find(actionId);
            if (action == null)
            {
                return null;
            }

            if (!action.IsExit)
            {
                int alertBonus = AssistAlertDelta(assists);
                if (alertBonus != 0)
                {
                    Alert = Math.Max(0, Alert + alertBonus);
                }

                context.Alert = Alert;
            }

            ExpeditionOutcomeEntry outcome = PickOutcome(action, context, assists);
            if (outcome == null)
            {
                return null;
            }

            var resolution = new ExpeditionResolution
            {
                Action = action,
                Outcome = outcome,
                Text = outcome.Text,
                IsExit = outcome.Kind == ExpeditionOutcomeKind.Exit
            };

            LastResultText = outcome.Text;

            if (resolution.IsExit)
            {
                IsComplete = true;
                _options.Clear();
                return resolution;
            }

            if (outcome.AlertDelta != 0)
            {
                Alert = Math.Max(0, Alert + outcome.AlertDelta);
            }

            Stage++;
            if (Stage >= StageCount)
            {
                BuildRetreatOnly();
            }
            else
            {
                BuildOptions();
            }

            resolution.AdvancedStage = true;
            return resolution;
        }

        public bool AdvanceAfterItemUse(out string error)
        {
            if (IsComplete)
            {
                error = "探索已经结束。";
                return false;
            }

            if (Stage >= StageCount)
            {
                error = "已经没有继续深入的时间。";
                return false;
            }

            Stage++;
            if (Stage >= StageCount)
            {
                BuildRetreatOnly();
            }
            else
            {
                BuildOptions();
            }

            error = null;
            return true;
        }

        public ExpeditionActionOption Find(string actionId)
        {
            for (int i = 0; i < _options.Count; i++)
            {
                if (_options[i].Id == actionId)
                {
                    return _options[i];
                }
            }

            return null;
        }

        private void BuildOptions()
        {
            _options.Clear();
            List<ActionTemplate> pool = CreatePool();
            Shuffle(pool);

            int count = Math.Min(4, pool.Count);
            for (int i = 0; i < count; i++)
            {
                _options.Add(CreateAction(pool[i]));
            }

            _options.Add(CreateRetreat());
        }

        private void BuildRetreatOnly()
        {
            _options.Clear();
            _options.Add(CreateRetreat());
        }

        private List<ActionTemplate> CreatePool()
        {
            switch (Theme)
            {
                case ExpeditionSiteTheme.Wild:
                    return new List<ActionTemplate>
                    {
                        new ActionTemplate("track", "追踪足迹", "迹", "沿着踩倒的草叶判断前面的动静。", ExpeditionRisk.Low),
                        new ActionTemplate("forage", "搜刮野地", "采", "翻找灌木和废弃营地，找能带走的东西。", ExpeditionRisk.Medium),
                        new ActionTemplate("climb", "翻越障碍", "越", "越过倒塌的围栏，直接进入另一侧。", ExpeditionRisk.Medium),
                        new ActionTemplate("hide", "隐蔽观察", "隐", "压低身体，等附近的动静先过去。", ExpeditionRisk.Low),
                        new ActionTemplate("rush", "强行突进", "冲", "趁对方还没反应过来冲过去。", ExpeditionRisk.High)
                    };
                case ExpeditionSiteTheme.Military:
                    return new ActionTemplatePool(new[]
                    {
                        new ActionTemplate("scan", "扫描防线", "扫", "读取巡逻节点和门禁的响应规律。", ExpeditionRisk.Low),
                        new ActionTemplate("breach", "爆破侧门", "破", "用暴力打开一条进入内部的通道。", ExpeditionRisk.High),
                        new ActionTemplate("hack", "接入终端", "接", "从维护口接进旧网络，换取情报。", ExpeditionRisk.Medium),
                        new ActionTemplate("suppress", "压制火力", "压", "先发制人，打掉最靠近的威胁。", ExpeditionRisk.High),
                        new ActionTemplate("slip", "沿墙摸进", "潜", "贴着阴影绕开正门和摄像头。", ExpeditionRisk.Medium)
                    });
                case ExpeditionSiteTheme.Signal:
                    return new ActionTemplatePool(new[]
                    {
                        new ActionTemplate("decode", "解析信号", "析", "把噪声里的重复脉冲拆成可读的路径。", ExpeditionRisk.Low),
                        new ActionTemplate("trace", "追踪回波", "追", "跟着最强的回波深入节点。", ExpeditionRisk.Medium),
                        new ActionTemplate("jam", "干扰中继", "扰", "短暂切断监视，为自己争取一段空窗。", ExpeditionRisk.Medium),
                        new ActionTemplate("dive", "跃入深井", "跃", "不排队列，直接跳进核心通道。", ExpeditionRisk.High),
                        new ActionTemplate("listen", "监听对话", "听", "在暗处听巡逻者交流，寻找漏洞。", ExpeditionRisk.Low)
                    });
                case ExpeditionSiteTheme.Orbital:
                    return new ActionTemplatePool(new[]
                    {
                        new ActionTemplate("scan", "扫描舱压", "扫", "先确认哪几段还在加压，哪几段已经真空。", ExpeditionRisk.Low),
                        new ActionTemplate("patch_hull", "补漏封舱", "补", "用随身材料堵住漏点，给自己留一条退路。", ExpeditionRisk.Medium),
                        new ActionTemplate("tether", "系缆跳跃", "缆", "扣上缆绳，跨过断裂的舱段。", ExpeditionRisk.Medium),
                        new ActionTemplate("burn_through", "切割舱壁", "割", "直接切穿舱壁，走最短但不安全的那条路。", ExpeditionRisk.High),
                        new ActionTemplate("drift", "静默漂浮", "漂", "关掉照明，随风漂过去。", ExpeditionRisk.Low)
                    });
                case ExpeditionSiteTheme.Core:
                    return new ActionTemplatePool(new[]
                    {
                        new ActionTemplate("decode", "读取主序", "序", "从还在运行的核心上读出它的节律。", ExpeditionRisk.Low),
                        new ActionTemplate("vent", "泄压降温", "泄", "先把热量排掉，否则连靠近都做不到。", ExpeditionRisk.Medium),
                        new ActionTemplate("overload", "过载主环", "载", "把核心推向过载，逼它提前换相。", ExpeditionRisk.High),
                        new ActionTemplate("synchronize", "同步相位", "同", "把自己的节奏对齐它，趁间隙穿过去。", ExpeditionRisk.Medium),
                        new ActionTemplate("strike", "打击核心", "击", "直接对还在运转的东西动手。", ExpeditionRisk.High)
                    });
                default:
                    return new List<ActionTemplate>
                    {
                        new ActionTemplate("scan", "静默扫描", "扫", "先读取这层空间的信号和热源。", ExpeditionRisk.Low),
                        new ActionTemplate("sneak", "撬开侧门", "潜", "从维护通道靠近，尽量不惊动里面的人。", ExpeditionRisk.Medium),
                        new ActionTemplate("force", "正面破门", "破", "直接撞开入口，用速度换取主动。", ExpeditionRisk.High),
                        new ActionTemplate("search", "翻找物资", "搜", "把能拆的柜子和箱子都翻一遍。", ExpeditionRisk.Medium),
                        new ActionTemplate("detour", "绕行观察", "绕", "先绕开正面，寻找更安全的路线。", ExpeditionRisk.Medium)
                    };
            }
        }

        private ExpeditionActionOption CreateAction(ActionTemplate template)
        {
            var action = new ExpeditionActionOption
            {
                Id = template.Id,
                Label = template.Label,
                Glyph = template.Glyph,
                Description = template.Description,
                Risk = template.Risk
            };

            int stage = Stage;
            int damage = _hazardDamage + stage * 2;

            if (template.Id == "scan" || template.Id == "decode" || template.Id == "track" || template.Id == "listen"
                || template.Id == "drift" || template.Id == "synchronize" || template.Id == "vent")
            {
                Add(action, ExpeditionOutcomeKind.Clue, 46, "你读出了下一步的节奏。视野里出现了新的选择。", 0);
                Add(action, ExpeditionOutcomeKind.Loot, 22, "扫描结果里夹着一处未登记的物资点。", 0);
                Add(action, ExpeditionOutcomeKind.Alert, 20, "你的扫描触发了对方的监听程序。", 1);
                Add(action, ExpeditionOutcomeKind.Nothing, 12, "信号很干净，暂时没有更多发现。", 0);
            }
            else if (template.Id == "sneak" || template.Id == "slip" || template.Id == "hide" || template.Id == "trace")
            {
                Add(action, ExpeditionOutcomeKind.Loot, 40, "你摸到一处被遮挡的储物点，里面还有东西。", 0);
                Add(action, ExpeditionOutcomeKind.Clue, 20, "你听见了巡逻者的脚步，路线变得更清楚。", 0);
                Add(action, ExpeditionOutcomeKind.Combat, 24, "阴影里有人先看见了你。", 1);
                Add(action, ExpeditionOutcomeKind.Damage, 10, "你被地上的断线绊倒，擦伤了手臂。", 0);
                Add(action, ExpeditionOutcomeKind.Nothing, 6, "你贴着墙通过，没有惊动任何人。", 0);
            }
            else if (template.Id == "force" || template.Id == "breach" || template.Id == "suppress" || template.Id == "rush" || template.Id == "dive"
                     || template.Id == "burn_through" || template.Id == "overload" || template.Id == "strike")
            {
                Add(action, ExpeditionOutcomeKind.Combat, 52 + stage * 3, "门后的威胁被惊动了。", 1);
                Add(action, ExpeditionOutcomeKind.Loot, 28, "你用蛮力拆开了一个上锁的柜子。", 0);
                Add(action, ExpeditionOutcomeKind.Damage, 14, "碎屑和电弧一起扑了过来。", 0);
                Add(action, ExpeditionOutcomeKind.Alert, 6, "整层区域都听见了你的动静。", 2);
            }
            else if (template.Id == "search" || template.Id == "forage" || template.Id == "hack"
                     || template.Id == "patch_hull" || template.Id == "tether")
            {
                Add(action, ExpeditionOutcomeKind.Loot, 58, "你找到了能带走的东西。", 0);
                Add(action, ExpeditionOutcomeKind.Clue, 18, "一件旧记录说明了前面的布局。", 0);
                Add(action, ExpeditionOutcomeKind.Combat, 14, "翻找的声音引来了看守。", 1);
                Add(action, ExpeditionOutcomeKind.Damage, 10, "柜子后面的线路突然短路。", 0);
            }
            else
            {
                Add(action, ExpeditionOutcomeKind.Clue, 32, "你从侧面看懂了这里的结构，下一步更稳。", 0);
                Add(action, ExpeditionOutcomeKind.Nothing, 30, "你绕了一圈，没有找到更好的入口。", 0);
                Add(action, ExpeditionOutcomeKind.Combat, 20, "你绕进了另一个人的视线。", 1);
                Add(action, ExpeditionOutcomeKind.Damage, 18, "脚下的结构塌了一块。", 0);
            }

            return action;
        }

        private ExpeditionActionOption CreateRetreat()
        {
            var action = new ExpeditionActionOption
            {
                Id = "retreat",
                Label = "撤退",
                Glyph = "退",
                Description = "沿原路离开。带走已经拿到的东西，不再深入。",
                Risk = ExpeditionRisk.Low,
                IsExit = true
            };

            Add(action, ExpeditionOutcomeKind.Exit, 100, "你退回入口，带着已经拿到的东西离开。", 0);
            return action;
        }

        private void Add(ExpeditionActionOption action, ExpeditionOutcomeKind kind, int weight, string text, int alertDelta)
        {
            var entry = new ExpeditionOutcomeEntry
            {
                Kind = kind,
                Weight = Math.Max(1, weight),
                Text = text,
                AlertDelta = alertDelta
            };

            if (kind == ExpeditionOutcomeKind.Loot)
            {
                entry.LootIds = PickLoot(_lootPerFind + (Stage > 0 ? 1 : 0));
                if (entry.LootIds.Length > 0)
                {
                    entry.Text = text + " 找到 " + entry.LootIds.Length + " 组物资。";
                }
            }
            else if (kind == ExpeditionOutcomeKind.Combat)
            {
                entry.EnemyId = _enemyPool[_random.Next(_enemyPool.Length)];
            }
            else if (kind == ExpeditionOutcomeKind.Damage)
            {
                entry.Damage = _hazardDamage + Stage * 2;
            }

            action.Outcomes.Add(entry);
        }

        private ExpeditionOutcomeEntry PickOutcome(
            ExpeditionActionOption action,
            ExpeditionContext context,
            IReadOnlyList<AssistSpec> assists)
        {
            return ExpeditionOdds.Pick(action, context, assists, _random);
        }

        private static int AssistAlertDelta(IReadOnlyList<AssistSpec> assists)
        {
            if (assists == null)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < assists.Count; i++)
            {
                if (assists[i] != null)
                {
                    total += assists[i].AlertDelta;
                }
            }

            return total;
        }

        private string[] PickLoot(int count)
        {
            int amount = Math.Max(1, count);
            var result = new string[amount];
            for (int i = 0; i < amount; i++)
            {
                result[i] = _lootPool[_random.Next(_lootPool.Length)];
            }

            return result;
        }

        private static ExpeditionSiteTheme ThemeFor(string keyItemId)
        {
            if (keyItemId == "dive_wild")
            {
                return ExpeditionSiteTheme.Wild;
            }

            if (keyItemId == "dive_military")
            {
                return ExpeditionSiteTheme.Military;
            }

            if (keyItemId == "dive_signal")
            {
                return ExpeditionSiteTheme.Signal;
            }

            if (keyItemId == "dive_deep")
            {
                return ExpeditionSiteTheme.Orbital;
            }

            if (keyItemId == "dive_core")
            {
                return ExpeditionSiteTheme.Core;
            }

            return ExpeditionSiteTheme.Facility;
        }

        private static string TitleFor(ExpeditionSiteTheme theme, string planName)
        {
            string suffix;
            switch (theme)
            {
                case ExpeditionSiteTheme.Wild:
                    suffix = "野地边缘";
                    break;
                case ExpeditionSiteTheme.Military:
                    suffix = "军用节点";
                    break;
                case ExpeditionSiteTheme.Signal:
                    suffix = "信号深井";
                    break;
                case ExpeditionSiteTheme.Orbital:
                    suffix = "失压残骸";
                    break;
                case ExpeditionSiteTheme.Core:
                    suffix = "核心环带";
                    break;
                default:
                    suffix = "废弃设施";
                    break;
            }

            return string.IsNullOrEmpty(planName) ? suffix : planName + " · " + suffix;
        }

        private static string IntroFor(ExpeditionSiteTheme theme)
        {
            switch (theme)
            {
                case ExpeditionSiteTheme.Wild:
                    return "风从倒塌的围栏间穿过。远处有东西在动，脚下是一条还没人走过的土路。";
                case ExpeditionSiteTheme.Military:
                    return "闸门后的应急灯还亮着。旧防线没有完全断电，任何一次靠近都可能被记录。";
                case ExpeditionSiteTheme.Signal:
                    return "通道深处传来不属于这里的脉冲。你听见的不是回声，而是某个仍在运行的系统。";
                case ExpeditionSiteTheme.Orbital:
                    return "重力只剩一点点。断裂的舱段之间没有地板，只有系缆和真空——每一步都在赌哪块甲板还连着。";
                case ExpeditionSiteTheme.Core:
                    return "这里没有走廊，只有环绕核心的环带。热浪把空气烤得扭曲，你能听见它在换相。";
                default:
                    return "你停在断裂的走廊前。空气里有金属和尘土的味道，深处偶尔传来电流声。";
            }
        }

        private void Shuffle<T>(IList<T> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                T swap = items[i];
                items[i] = items[j];
                items[j] = swap;
            }
        }

        private sealed class ActionTemplate
        {
            public string Id;
            public string Label;
            public string Glyph;
            public string Description;
            public ExpeditionRisk Risk;

            public ActionTemplate(string id, string label, string glyph, string description, ExpeditionRisk risk)
            {
                Id = id;
                Label = label;
                Glyph = glyph;
                Description = description;
                Risk = risk;
            }
        }

        private sealed class ActionTemplatePool : List<ActionTemplate>
        {
            public ActionTemplatePool(IEnumerable<ActionTemplate> items) : base(items)
            {
            }
        }
    }

    public static class ExpeditionText
    {
        public static string RiskName(ExpeditionRisk risk)
        {
            switch (risk)
            {
                case ExpeditionRisk.Low:
                    return "低风险";
                case ExpeditionRisk.Medium:
                    return "中风险";
                default:
                    return "高风险";
            }
        }

        public static string OutcomeName(ExpeditionOutcomeKind kind)
        {
            switch (kind)
            {
                case ExpeditionOutcomeKind.Loot:
                    return "物资";
                case ExpeditionOutcomeKind.Combat:
                    return "战斗";
                case ExpeditionOutcomeKind.Damage:
                    return "受伤";
                case ExpeditionOutcomeKind.Clue:
                    return "线索";
                case ExpeditionOutcomeKind.Alert:
                    return "警戒";
                case ExpeditionOutcomeKind.Exit:
                    return "撤离";
                default:
                    return "未发现";
            }
        }
    }
}
