namespace BitRunner.Core
{
    public enum CombatEventType
    {
        CombatStarted,
        TurnStarted,
        TurnEnded,
        ItemUsed,
        Damage,
        ShieldGained,
        Healed,
        StatusApplied,
        StatusConsumed,
        TraitTriggered,
        ItemConsumed,
        AmmoConsumed,
        DurabilityLost,
        ItemBroken,
        ActorDied,
        ActorFled,
        PlayerRetreating,
        CombatEnded
    }

    public sealed class CombatEvent
    {
        public CombatEventType Type;
        public string ActorId;
        public string TargetId;
        public string ItemId;
        public int InstanceId;
        public int Amount;
        public int Shielded;
        public string Message;
    }
}
