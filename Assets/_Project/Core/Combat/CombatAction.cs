namespace BitRunner.Core
{
    public enum CombatActionType
    {
        UseItem,
        EndTurn
    }

    public readonly struct CombatAction
    {
        public CombatAction(CombatActionType type, int itemInstanceId, string targetActorId)
        {
            Type = type;
            ItemInstanceId = itemInstanceId;
            TargetActorId = targetActorId;
        }

        public CombatActionType Type { get; }

        public int ItemInstanceId { get; }

        public string TargetActorId { get; }

        public static CombatAction UseItem(int itemInstanceId, string targetActorId)
        {
            return new CombatAction(CombatActionType.UseItem, itemInstanceId, targetActorId);
        }

        public static CombatAction EndTurn()
        {
            return new CombatAction(CombatActionType.EndTurn, 0, null);
        }
    }

    public interface ICombatBrain
    {
        CombatAction Decide(CombatEngine engine, CombatActor self);
    }
}
