using System;
using System.Collections.Generic;

namespace BitRunner.Core
{
    public sealed class EnemyBrain : ICombatBrain
    {
        public CombatAction Decide(CombatEngine engine, CombatActor self)
        {
            return DecideInternal(engine, self, true);
        }

        public CombatAction PreviewNextAction(CombatEngine engine, CombatActor self)
        {
            return DecideInternal(engine, self, false);
        }

        private CombatAction DecideInternal(CombatEngine engine, CombatActor self, bool requireTurn)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            CombatActor target = engine.Player;
            if (target == null || !target.IsAlive)
            {
                return CombatAction.EndTurn();
            }

            PlacedItem bestItem = null;
            int bestScore = 0;

            IReadOnlyList<PlacedItem> items = self.Bag.Items;
            for (int i = 0; i < items.Count; i++)
            {
                PlacedItem item = items[i];
                ItemDefinition definition = engine.GetDefinition(item);
                if (definition == null || (!definition.IsActive && definition.Shield <= 0))
                {
                    continue;
                }

                string error;
                bool canUse = requireTurn
                    ? engine.CanUseItem(self, item, out error)
                    : engine.CanUseItemPreview(self, item, out error);
                if (!canUse)
                {
                    continue;
                }

                int score = Score(engine, self, target, item, definition);
                if (score <= 0)
                {
                    continue;
                }

                if (bestItem == null || score > bestScore || (score == bestScore && item.InstanceId < bestItem.InstanceId))
                {
                    bestItem = item;
                    bestScore = score;
                }
            }

            if (bestItem == null)
            {
                return CombatAction.EndTurn();
            }

            ItemDefinition chosen = engine.GetDefinition(bestItem);
            string targetId = chosen.Target == ItemTarget.Enemy ? target.Id : self.Id;
            return CombatAction.UseItem(bestItem.InstanceId, targetId);
        }

        private static int Score(CombatEngine engine, CombatActor self, CombatActor target, PlacedItem item, ItemDefinition definition)
        {
            int damage = engine.GetDamageAfterShieldAndArmor(target, engine.GetDamage(self, item), definition.Type);
            int effectiveDamage = damage;

            if (damage > 0 && effectiveDamage >= target.Hp)
            {
                return 100000 + damage;
            }

            if (definition.Heal > 0)
            {
                if (self.Hp <= self.MaxHp * 0.35)
                {
                    return 50000 + definition.Heal;
                }

                return 0;
            }

            if (effectiveDamage > 0)
            {
                return 1000 + effectiveDamage * 10;
            }

            int block = engine.GetShield(self, item);
            if (block > 0)
            {
                return 100 + block;
            }

            StatusType appliedStatus;
            int statusStacks;
            int statusTurns;
            if (engine.Traits.TryGetOnUseStatus(definition, out appliedStatus, out statusStacks, out statusTurns))
            {
                BuffDefinition buff = BuffLibrary.Get(appliedStatus);
                if (buff != null && buff.IsDebuff)
                {
                    return 50 + statusStacks * 10
                           + target.GetStatusStacks(appliedStatus) * 5;
                }

                return 40 + statusStacks * 5;
            }

            return 0;
        }
    }
}
