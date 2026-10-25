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
        CombatEnded
    }

    public sealed class CombatEvent
    {
        public CombatEventType Type;
        public string ActorId;
        public string TargetId;
        public string ItemId;
        public int Amount;
        public int Shielded;
        public string Message;
    }
}
