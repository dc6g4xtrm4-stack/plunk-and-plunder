using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlunkAndPlunder.Combat
{
    /// <summary>
    /// Resolves combat between units using dice rolls
    /// </summary>
    public class CombatResolver
    {
        private System.Random rng;

        public CombatResolver(int seed)
        {
            this.rng = new System.Random(seed);
        }

        /// <summary>
        /// Resolve combat between attacker and defender using dice rolls
        /// </summary>
        public CombatResult ResolveCombat(string attackerId, string defenderId)
        {
            // Attacker rolls 3 dice
            List<int> attackerRolls = RollDice(3);

            // Defender rolls 2 dice
            List<int> defenderRolls = RollDice(2);

            // Sort both in descending order to get highest values
            attackerRolls.Sort((a, b) => b.CompareTo(a));
            defenderRolls.Sort((a, b) => b.CompareTo(a));

            // Take highest 2 from attacker
            List<int> attackerTop2 = attackerRolls.Take(2).ToList();
            List<int> defenderTop2 = defenderRolls.Take(2).ToList();

            int attackerDamage = 0;
            int defenderDamage = 0;

            // Compare pairwise (highest vs highest, second vs second)
            // Defender wins ties
            for (int i = 0; i < 2; i++)
            {
                if (attackerTop2[i] > defenderTop2[i])
                {
                    // Attacker wins this comparison - defender takes 2 damage
                    defenderDamage += 2;
                }
                else
                {
                    // Defender wins this comparison (including ties) - attacker takes 2 damage
                    attackerDamage += 2;
                }
            }

            return new CombatResult
            {
                attackerId = attackerId,
                defenderId = defenderId,
                attackerRolls = attackerRolls,
                defenderRolls = defenderRolls,
                attackerTop2 = attackerTop2,
                defenderTop2 = defenderTop2,
                damageToAttacker = attackerDamage,
                damageToDefender = defenderDamage
            };
        }

        /// <summary>
        /// Roll N dice (1-6)
        /// </summary>
        private List<int> RollDice(int count)
        {
            List<int> rolls = new List<int>();
            for (int i = 0; i < count; i++)
            {
                rolls.Add(rng.Next(1, 7)); // 1-6 inclusive
            }
            return rolls;
        }
    }

    [Serializable]
    public class CombatResult
    {
        public string attackerId;
        public string defenderId;
        public List<int> attackerRolls;
        public List<int> defenderRolls;
        public List<int> attackerTop2;
        public List<int> defenderTop2;
        public int damageToAttacker;
        public int damageToDefender;

        public override string ToString()
        {
            return $"Combat: {attackerId} vs {defenderId}\n" +
                   $"Attacker rolls: [{string.Join(", ", attackerRolls)}] -> top 2: [{string.Join(", ", attackerTop2)}]\n" +
                   $"Defender rolls: [{string.Join(", ", defenderRolls)}] -> top 2: [{string.Join(", ", defenderTop2)}]\n" +
                   $"Damage: {damageToAttacker} to attacker, {damageToDefender} to defender";
        }
    }
}
