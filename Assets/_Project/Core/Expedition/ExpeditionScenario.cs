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

        /// <summary>选择这个行动会前往哪个区域 id；null = 随机换一个区域。</summary>
        public string NextZoneId;
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
        private readonly List<ZoneTemplate> _zones;
        private ZoneTemplate _zone;
        private string _pendingZoneId;

        public int Seed { get; private set; }
        public ExpeditionSiteTheme Theme { get; private set; }
        public string SiteId { get; private set; }
        public string Title { get; private set; }
        public string IntroText { get; private set; }
        public int Stage { get; private set; }

        /// <summary>当前所在区域。玩家每走一步都可能换到一个有不同选项的地方。</summary>
        public string ZoneId
        {
            get { return _zone != null ? _zone.Id : null; }
        }

        public string ZoneName
        {
            get { return _zone != null ? _zone.Name : Title; }
        }

        public string ZoneBlurb
        {
            get { return _zone != null ? _zone.Blurb : IntroText; }
        }

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
            _zones = CreateZones(theme);
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
            int stageCount = plan.StageCount > 0 ? plan.StageCount : 12;

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
                _pendingZoneId = null;
                BuildRetreatOnly();
            }
            else
            {
                _pendingZoneId = action.NextZoneId;
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
            _pendingZoneId = null;
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
            _zone = ResolveZone();

            var pool = new List<ActionTemplate>();
            for (int i = 0; i < _zone.ActionIds.Length; i++)
            {
                ActionTemplate template = ActionLibrary.Find(_zone.ActionIds[i]);
                if (template != null)
                {
                    pool.Add(template);
                }
            }

            Shuffle(pool);

            int count = Math.Min(4, pool.Count);
            for (int i = 0; i < count; i++)
            {
                _options.Add(CreateAction(pool[i], _zone));
            }

            if (_options.Count == 0)
            {
                // 数据兜底：区域没配行动时至少给一个能走的选择。
                _options.Add(CreateAction(ActionLibrary.Fallback, _zone));
            }

            _options.Add(CreateRetreat());
        }

        /// <summary>
        /// 下一个区域：优先走上一个行动指定的分支，否则随机换一个**不同于当前**的区域。
        /// 「同一条路线被无限重复」就是这样被消掉的。
        /// </summary>
        private ZoneTemplate ResolveZone()
        {
            if (!string.IsNullOrEmpty(_pendingZoneId))
            {
                ZoneTemplate pending = FindZone(_pendingZoneId);
                _pendingZoneId = null;
                if (pending != null)
                {
                    return pending;
                }
            }

            if (_zones.Count == 0)
            {
                return ZoneTemplate.Empty;
            }

            if (_zones.Count == 1)
            {
                return _zones[0];
            }

            for (int attempt = 0; attempt < 8; attempt++)
            {
                ZoneTemplate candidate = _zones[_random.Next(_zones.Count)];
                if (_zone == null || candidate.Id != _zone.Id)
                {
                    return candidate;
                }
            }

            return _zones[0];
        }

        private ZoneTemplate FindZone(string zoneId)
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                if (_zones[i].Id == zoneId)
                {
                    return _zones[i];
                }
            }

            return null;
        }

        private void BuildRetreatOnly()
        {
            _options.Clear();
            _options.Add(CreateRetreat());
        }

        /// <summary>
        /// 一条潜网里会经过的地点。每个地点只给一小撮行动，部分行动会指定下一站，
        /// 所以同样的密钥也有不同的走法：选「追踪」和选「翻越」会看到不同的后续选项。
        /// </summary>
        private static List<ZoneTemplate> CreateZones(ExpeditionSiteTheme theme)
        {
            switch (theme)
            {
                case ExpeditionSiteTheme.Wild:
                    return new List<ZoneTemplate>
                    {
                        new ZoneTemplate("wild_brush", "灌木带",
                            "齐腰的草叶擦过裤腿，断枝下面压着没被拿走的补给。",
                            new[] { "track", "forage", "hide", "detour", "climb" },
                            new[] { "track" }, new[] { "wild_ridge" }),
                        new ZoneTemplate("wild_ridge", "兽径高地",
                            "坡顶风大，能看到很远的动静，也很容易从很远的地方被看见。",
                            new[] { "track", "climb", "rush", "hide", "scan" },
                            new[] { "climb" }, new[] { "wild_ruin" }),
                        new ZoneTemplate("wild_camp", "废弃营地",
                            "帐篷塌了一半，炉灰还有余温——有人比你早到，也可能还没走。",
                            new[] { "forage", "search", "sneak", "hide", "listen" },
                            new[] { "search" }, new[] { "wild_ruin" }),
                        new ZoneTemplate("wild_ruin", "倒塌围墙",
                            "水泥碎块堆成斜坡，墙后是一条能绕到更深处的小路。",
                            new[] { "climb", "force", "sneak", "detour", "search" },
                            new[] { "force" }, new[] { "wild_den" }),
                        new ZoneTemplate("wild_stream", "浅滩",
                            "水只到脚踝，声音却不小；沿着河道走能避开大部分视线。",
                            new[] { "forage", "sneak", "track", "detour", "climb" },
                            new[] { "track" }, new[] { "wild_camp" }),
                        new ZoneTemplate("wild_den", "兽穴",
                            "骨头堆在洞口，里面有什么东西正慢慢转醒。",
                            new[] { "force", "suppress", "track", "hide", "rush" },
                            new[] { "force" }, new[] { "wild_brush" })
                    };
                case ExpeditionSiteTheme.Military:
                    return new List<ZoneTemplate>
                    {
                        new ZoneTemplate("mil_perimeter", "外围防线",
                            "铁丝网后面是空的哨位，探照灯按固定节奏扫过去。",
                            new[] { "scan", "slip", "suppress", "breach", "detour" },
                            new[] { "breach" }, new[] { "mil_gate" }),
                        new ZoneTemplate("mil_gate", "闸门岗",
                            "闸门半开着，读卡器还在闪；里面的人知道你会来。",
                            new[] { "force", "breach", "hack", "scan", "suppress" },
                            new[] { "hack" }, new[] { "mil_server" }),
                        new ZoneTemplate("mil_server", "加密机房",
                            "冷却风扇全停了，机柜里只剩一条还在通电的线路。",
                            new[] { "hack", "listen", "decode", "slip", "trace" },
                            new[] { "decode" }, new[] { "mil_armory" }),
                        new ZoneTemplate("mil_armory", "军械库",
                            "货架被翻过，但角落还有没被带走的整箱弹药。",
                            new[] { "search", "force", "jam", "detour", "hack" },
                            new[] { "search" }, new[] { "mil_barracks" }),
                        new ZoneTemplate("mil_barracks", "兵营",
                            "床铺是空的，墙上的值班表被人用笔划掉了两行。",
                            new[] { "sneak", "listen", "search", "suppress", "hide" },
                            new[] { "listen" }, new[] { "mil_tower" }),
                        new ZoneTemplate("mil_tower", "瞭望塔",
                            "从这里能看完整片营地，也能被整片营地看见。",
                            new[] { "scan", "climb", "trace", "suppress", "slip" },
                            new[] { "climb" }, new[] { "mil_perimeter" })
                    };
                case ExpeditionSiteTheme.Signal:
                    return new List<ZoneTemplate>
                    {
                        new ZoneTemplate("sig_noise", "噪点区",
                            "空气里全是断断续续的脉冲，像有人在很远的地方说话。",
                            new[] { "decode", "listen", "trace", "jam", "scan" },
                            new[] { "trace" }, new[] { "sig_relay" }),
                        new ZoneTemplate("sig_relay", "中继塔",
                            "塔身绕着一圈蓝光，靠得越近，设备越热。",
                            new[] { "hack", "jam", "trace", "detour", "listen" },
                            new[] { "jam" }, new[] { "sig_well" }),
                        new ZoneTemplate("sig_well", "信号深井",
                            "垂直的井壁看不见底，只能听见某样东西在下面换相。",
                            new[] { "dive", "trace", "decode", "slip", "listen" },
                            new[] { "dive" }, new[] { "sig_lattice" }),
                        new ZoneTemplate("sig_lattice", "信号格栅",
                            "网格状的通道把信号切得很碎，每一步都要重新校准。",
                            new[] { "scan", "listen", "slip", "force", "decode" },
                            new[] { "scan" }, new[] { "sig_pulse" }),
                        new ZoneTemplate("sig_pulse", "脉冲回廊",
                            "走廊尽头每隔几秒就亮一次，亮的时候什么都看不见。",
                            new[] { "decode", "trace", "scan", "jam", "hack" },
                            new[] { "hack" }, new[] { "sig_null" }),
                        new ZoneTemplate("sig_null", "静默层",
                            "所有信号到这里都被吃掉了，连自己的心跳都显得很响。",
                            new[] { "sneak", "slip", "listen", "decode", "hide" },
                            new[] { "listen" }, new[] { "sig_noise" })
                    };
                case ExpeditionSiteTheme.Orbital:
                    return new List<ZoneTemplate>
                    {
                        new ZoneTemplate("orb_dock", "断裂泊位",
                            "对接环还在缓慢自转，另一侧是没有护栏的真空。",
                            new[] { "scan", "tether", "drift", "search", "patch_hull" },
                            new[] { "tether" }, new[] { "orb_hull" }),
                        new ZoneTemplate("orb_hull", "漏压舱段",
                            "耳边是持续的气流声，舱压正在一格一格往下掉。",
                            new[] { "patch_hull", "scan", "tether", "burn_through", "drift" },
                            new[] { "patch_hull" }, new[] { "orb_vacuum" }),
                        new ZoneTemplate("orb_vacuum", "真空缺口",
                            "甲板到头了，中间只有漂浮的碎片和一条垂着的系缆。",
                            new[] { "drift", "tether", "scan", "burn_through", "patch_hull" },
                            new[] { "drift" }, new[] { "orb_core" }),
                        new ZoneTemplate("orb_core", "货运核心",
                            "整段货舱被卡在梁架之间，集装箱还在缓慢地互相碰撞。",
                            new[] { "burn_through", "patch_hull", "search", "scan", "drift" },
                            new[] { "search" }, new[] { "orb_cargo" }),
                        new ZoneTemplate("orb_cargo", "货舱夹层",
                            "夹层里堆着没登记的箱子，标签全被撕掉了。",
                            new[] { "search", "burn_through", "drift", "tether", "scan" },
                            new[] { "burn_through" }, new[] { "orb_lifeline" }),
                        new ZoneTemplate("orb_lifeline", "生命线",
                            "整条补给索横在真空里，另一头拴着还亮着灯的舱门。",
                            new[] { "tether", "patch_hull", "drift", "scan", "search" },
                            new[] { "tether" }, new[] { "orb_dock" })
                    };
                case ExpeditionSiteTheme.Core:
                    return new List<ZoneTemplate>
                    {
                        new ZoneTemplate("core_ring", "外环带",
                            "环带在脚下缓慢移动，热浪把远处的轮廓烤得发晃。",
                            new[] { "decode", "vent", "synchronize", "overload", "scan" },
                            new[] { "synchronize" }, new[] { "core_phase" }),
                        new ZoneTemplate("core_vent", "散热廊",
                            "管道里全是高温蒸汽，走错一步就是整条手臂。",
                            new[] { "vent", "synchronize", "scan", "decode", "overload" },
                            new[] { "vent" }, new[] { "core_heart" }),
                        new ZoneTemplate("core_phase", "换相间隙",
                            "核心的节律在这里出现空隙，只有几拍的时间能穿过去。",
                            new[] { "synchronize", "decode", "strike", "vent", "scan" },
                            new[] { "strike" }, new[] { "core_heart" }),
                        new ZoneTemplate("core_heart", "核心壁",
                            "再往前就是它本身。温度、噪声、光，全都在同一个频率上。",
                            new[] { "strike", "overload", "vent", "decode", "synchronize" },
                            new[] { "overload" }, new[] { "core_shell" }),
                        new ZoneTemplate("core_shell", "外壳层",
                            "金属壳被反复烧蚀过，留下层层叠叠的焦痕。",
                            new[] { "scan", "decode", "vent", "strike", "synchronize" },
                            new[] { "scan" }, new[] { "core_echo" }),
                        new ZoneTemplate("core_echo", "回响腔",
                            "每个动作都会在腔体里回来一次，很难分辨哪一声才是当下。",
                            new[] { "decode", "synchronize", "strike", "vent", "scan" },
                            new[] { "decode" }, new[] { "core_ring" })
                    };
                default:
                    return new List<ZoneTemplate>
                    {
                        new ZoneTemplate("fac_corridor", "断裂走廊",
                            "应急灯隔一段亮一盏，地面铺着散落的线缆。",
                            new[] { "scan", "sneak", "search", "detour", "listen" },
                            new[] { "sneak" }, new[] { "fac_office" }),
                        new ZoneTemplate("fac_gate", "封锁闸门",
                            "闸门只放下了一半，门后人影一晃而过。",
                            new[] { "force", "breach", "suppress", "scan", "detour" },
                            new[] { "breach" }, new[] { "fac_shaft" }),
                        new ZoneTemplate("fac_office", "值班室",
                            "茶杯还在桌上，终端屏幕亮着拒绝访问的红字。",
                            new[] { "search", "hack", "hide", "listen", "sneak" },
                            new[] { "hack" }, new[] { "fac_archive" }),
                        new ZoneTemplate("fac_shaft", "维护竖井",
                            "竖井通向下面几层，梯子上全是锈。",
                            new[] { "climb", "slip", "trace", "detour", "scan" },
                            new[] { "climb" }, new[] { "fac_roof" }),
                        new ZoneTemplate("fac_archive", "档案间",
                            "一柜子一柜子的旧文件，有几格抽屉是空的。",
                            new[] { "search", "hack", "listen", "hide", "trace" },
                            new[] { "search" }, new[] { "fac_gate" }),
                        new ZoneTemplate("fac_roof", "天台",
                            "风把积灰吹成一道一道，下面几层的声音都能听见。",
                            new[] { "scan", "climb", "suppress", "hide", "sneak" },
                            new[] { "climb" }, new[] { "fac_corridor" })
                    };
            }
        }

        /// <summary>所有可出现的行动模板。区域只挑其中几个，行动 id 与「行动卡」物品保持一致。</summary>
        private static class ActionLibrary
        {
            private static readonly Dictionary<string, ActionTemplate> Map = Build();

            public static readonly ActionTemplate Fallback = new ActionTemplate(
                "detour", "绕行观察", "绕", "先绕开正面，寻找更安全的路线。", ExpeditionRisk.Medium);

            public static ActionTemplate Find(string id)
            {
                ActionTemplate template;
                return id != null && Map.TryGetValue(id, out template) ? template : null;
            }

            private static Dictionary<string, ActionTemplate> Build()
            {
                var map = new Dictionary<string, ActionTemplate>();
                Add(map, new ActionTemplate("scan", "静默扫描", "扫", "先读取这层空间的信号和热源。", ExpeditionRisk.Low));
                Add(map, new ActionTemplate("force", "正面破门", "破", "直接撞开入口，用速度换取主动。", ExpeditionRisk.High));
                Add(map, new ActionTemplate("search", "翻找物资", "搜", "把能拆的柜子和箱子都翻一遍。", ExpeditionRisk.Medium));
                Add(map, new ActionTemplate("detour", "绕行观察", "绕", "先绕开正面，寻找更安全的路线。", ExpeditionRisk.Medium));
                Add(map, new ActionTemplate("sneak", "撬开侧门", "潜", "从维护通道靠近，尽量不惊动里面的人。", ExpeditionRisk.Medium));
                Add(map, new ActionTemplate("track", "追踪足迹", "迹", "沿着踩倒的草叶判断前面的动静。", ExpeditionRisk.Low));
                Add(map, new ActionTemplate("forage", "搜刮野地", "采", "翻找灌木和废弃营地，找能带走的东西。", ExpeditionRisk.Medium));
                Add(map, new ActionTemplate("climb", "翻越障碍", "越", "越过倒塌的围栏，直接进入另一侧。", ExpeditionRisk.Medium));
                Add(map, new ActionTemplate("hide", "隐蔽观察", "隐", "压低身体，等附近的动静先过去。", ExpeditionRisk.Low));
                Add(map, new ActionTemplate("rush", "强行突进", "冲", "趁对方还没反应过来冲过去。", ExpeditionRisk.High));
                Add(map, new ActionTemplate("breach", "爆破侧门", "破", "用暴力打开一条进入内部的通道。", ExpeditionRisk.High));
                Add(map, new ActionTemplate("hack", "接入终端", "接", "从维护口接进旧网络，换取情报。", ExpeditionRisk.Medium));
                Add(map, new ActionTemplate("suppress", "压制火力", "压", "先发制人，打掉最靠近的威胁。", ExpeditionRisk.High));
                Add(map, new ActionTemplate("slip", "沿墙摸进", "潜", "贴着阴影绕开正门和摄像头。", ExpeditionRisk.Medium));
                Add(map, new ActionTemplate("decode", "解析信号", "析", "把噪声里的重复脉冲拆成可读的路径。", ExpeditionRisk.Low));
                Add(map, new ActionTemplate("trace", "追踪回波", "追", "跟着最强的回波深入节点。", ExpeditionRisk.Medium));
                Add(map, new ActionTemplate("jam", "干扰中继", "扰", "短暂切断监视，为自己争取一段空窗。", ExpeditionRisk.Medium));
                Add(map, new ActionTemplate("dive", "跃入深井", "跃", "不排队列，直接跳进核心通道。", ExpeditionRisk.High));
                Add(map, new ActionTemplate("listen", "监听对话", "听", "在暗处听巡逻者交流，寻找漏洞。", ExpeditionRisk.Low));
                Add(map, new ActionTemplate("patch_hull", "补漏封舱", "补", "用随身材料堵住漏点，给自己留一条退路。", ExpeditionRisk.Medium));
                Add(map, new ActionTemplate("tether", "系缆跳跃", "缆", "扣上缆绳，跨过断裂的舱段。", ExpeditionRisk.Medium));
                Add(map, new ActionTemplate("burn_through", "切割舱壁", "割", "直接切穿舱壁，走最短但不安全的那条路。", ExpeditionRisk.High));
                Add(map, new ActionTemplate("drift", "静默漂浮", "漂", "关掉照明，随风漂过去。", ExpeditionRisk.Low));
                Add(map, new ActionTemplate("vent", "泄压降温", "泄", "先把热量排掉，否则连靠近都做不到。", ExpeditionRisk.Medium));
                Add(map, new ActionTemplate("overload", "过载主环", "载", "把核心推向过载，逼它提前换相。", ExpeditionRisk.High));
                Add(map, new ActionTemplate("synchronize", "同步相位", "同", "把自己的节奏对齐它，趁间隙穿过去。", ExpeditionRisk.Medium));
                Add(map, new ActionTemplate("strike", "打击核心", "击", "直接对还在运转的东西动手。", ExpeditionRisk.High));
                return map;
            }

            private static void Add(Dictionary<string, ActionTemplate> map, ActionTemplate template)
            {
                map[template.Id] = template;
            }
        }

        private ExpeditionActionOption CreateAction(ActionTemplate template, ZoneTemplate zone)
        {
            var action = new ExpeditionActionOption
            {
                Id = template.Id,
                Label = template.Label,
                Glyph = template.Glyph,
                Description = template.Description,
                Risk = template.Risk,
                NextZoneId = zone != null ? zone.BranchFor(template.Id) : null
            };

            int stage = Stage;

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
                // 深度加成封顶：探索拉到 12 段以上时，线性增长会让一次失误直接清空血条。
                int depth = Math.Min(Stage, 6);
                entry.Damage = _hazardDamage + depth * 2;
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

        /// <summary>一个地点：名字、说明、可选行动，以及「哪个行动通向哪个地点」。</summary>
        private sealed class ZoneTemplate
        {
            public static readonly ZoneTemplate Empty = new ZoneTemplate(
                "empty", "未知区域", "看不清前面的结构，只能先走一步算一步。",
                new[] { "detour" }, null, null);

            public readonly string Id;
            public readonly string Name;
            public readonly string Blurb;
            public readonly string[] ActionIds;

            private readonly string[] _branchFrom;
            private readonly string[] _branchTo;

            public ZoneTemplate(
                string id,
                string name,
                string blurb,
                string[] actionIds,
                string[] branchFrom,
                string[] branchTo)
            {
                Id = id;
                Name = name;
                Blurb = blurb;
                ActionIds = actionIds ?? new string[0];
                _branchFrom = branchFrom;
                _branchTo = branchTo;
            }

            public string BranchFor(string actionId)
            {
                if (actionId == null || _branchFrom == null || _branchTo == null)
                {
                    return null;
                }

                for (int i = 0; i < _branchFrom.Length && i < _branchTo.Length; i++)
                {
                    if (_branchFrom[i] == actionId)
                    {
                        return _branchTo[i];
                    }
                }

                return null;
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
