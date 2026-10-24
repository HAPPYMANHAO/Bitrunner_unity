using System.Collections.Generic;

namespace BitRunner.Core
{
    public enum BuffStackMode
    {
        Intensity,
        Charges,
        Unique
    }

    public enum BuffTickPhase
    {
        None,
        TurnStart,
        TurnEnd
    }

    public sealed class BuffDefinition
    {
        public StatusType Type;
        public string DisplayName;
        public string Description;
        public bool IsDebuff;
        public bool Decays = true;
        public int MaxStacks = 99;
        public int DefaultTurns = 2;
        public BuffStackMode StackMode = BuffStackMode.Intensity;
        public BuffTickPhase TickPhase = BuffTickPhase.None;
    }

    public static class BuffLibrary
    {
        private static readonly Dictionary<StatusType, BuffDefinition> Definitions =
            new Dictionary<StatusType, BuffDefinition>
            {
                {
                    StatusType.Overload,
                    new BuffDefinition
                    {
                        Type = StatusType.Overload,
                        DisplayName = "过载",
                        Description = "每层使下回合能量 -1，消耗后移除。",
                        IsDebuff = true,
                        Decays = false,
                        MaxStacks = 5,
                        DefaultTurns = 1,
                        TickPhase = BuffTickPhase.TurnStart
                    }
                },
                {
                    StatusType.ShortCircuit,
                    new BuffDefinition
                    {
                        Type = StatusType.ShortCircuit,
                        DisplayName = "短路",
                        Description = "每层使受到的伤害 +2。",
                        IsDebuff = true,
                        MaxStacks = 5,
                        DefaultTurns = 2,
                        TickPhase = BuffTickPhase.TurnEnd
                    }
                },
                {
                    StatusType.Cache,
                    new BuffDefinition
                    {
                        Type = StatusType.Cache,
                        DisplayName = "缓存",
                        Description = "回合开始时保留当前护盾。",
                        IsDebuff = false,
                        Decays = false,
                        MaxStacks = 1,
                        DefaultTurns = 1,
                        StackMode = BuffStackMode.Unique,
                        TickPhase = BuffTickPhase.TurnStart
                    }
                },
                {
                    StatusType.Overclock,
                    new BuffDefinition
                    {
                        Type = StatusType.Overclock,
                        DisplayName = "超频",
                        Description = "每层使造成的伤害 +1。",
                        IsDebuff = false,
                        MaxStacks = 5,
                        DefaultTurns = 3,
                        TickPhase = BuffTickPhase.TurnEnd
                    }
                },
                {
                    StatusType.Coolant,
                    new BuffDefinition
                    {
                        Type = StatusType.Coolant,
                        DisplayName = "冷却",
                        Description = "每层使获得的护盾 +1。",
                        IsDebuff = false,
                        MaxStacks = 5,
                        DefaultTurns = 3,
                        TickPhase = BuffTickPhase.TurnEnd
                    }
                },
                {
                    StatusType.Corrosion,
                    new BuffDefinition
                    {
                        Type = StatusType.Corrosion,
                        DisplayName = "腐蚀",
                        Description = "每层在回合结束时造成 1 点真实伤害。",
                        IsDebuff = true,
                        MaxStacks = 8,
                        DefaultTurns = 3,
                        TickPhase = BuffTickPhase.TurnEnd
                    }
                },
                {
                    StatusType.Overheat,
                    new BuffDefinition
                    {
                        Type = StatusType.Overheat,
                        DisplayName = "过热",
                        Description = "每层在回合开始时造成 2 点可被护盾吸收的伤害。",
                        IsDebuff = true,
                        MaxStacks = 6,
                        DefaultTurns = 2,
                        TickPhase = BuffTickPhase.TurnStart
                    }
                },
                {
                    StatusType.Nanorepair,
                    new BuffDefinition
                    {
                        Type = StatusType.Nanorepair,
                        DisplayName = "纳米修复",
                        Description = "每层在回合开始时恢复 1 点生命。",
                        IsDebuff = false,
                        MaxStacks = 5,
                        DefaultTurns = 3,
                        TickPhase = BuffTickPhase.TurnStart
                    }
                },
                {
                    StatusType.Evasion,
                    new BuffDefinition
                    {
                        Type = StatusType.Evasion,
                        DisplayName = "闪避",
                        Description = "每层抵挡一次伤害，触发后消耗。",
                        IsDebuff = false,
                        MaxStacks = 3,
                        DefaultTurns = 3,
                        StackMode = BuffStackMode.Charges,
                        TickPhase = BuffTickPhase.TurnEnd
                    }
                }
            };

        public static bool TryGet(StatusType type, out BuffDefinition definition)
        {
            return Definitions.TryGetValue(type, out definition);
        }

        public static BuffDefinition Get(StatusType type)
        {
            BuffDefinition definition;
            return Definitions.TryGetValue(type, out definition) ? definition : null;
        }

        public static string Name(StatusType type)
        {
            BuffDefinition definition = Get(type);
            return definition == null ? "状态" : definition.DisplayName;
        }

        public static string Description(StatusType type)
        {
            BuffDefinition definition = Get(type);
            return definition == null ? string.Empty : definition.Description;
        }

        public static bool IsDebuff(StatusType type)
        {
            BuffDefinition definition = Get(type);
            return definition != null && definition.IsDebuff;
        }

        public static int MaxStacks(StatusType type)
        {
            BuffDefinition definition = Get(type);
            return definition == null ? 1 : definition.MaxStacks;
        }

        public static bool Decays(StatusType type)
        {
            BuffDefinition definition = Get(type);
            return definition == null || definition.Decays;
        }
    }
}
