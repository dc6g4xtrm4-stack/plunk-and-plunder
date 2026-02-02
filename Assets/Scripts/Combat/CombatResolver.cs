using System;
using PlunkAndPlunder.Units;
using UnityEngine;

namespace PlunkAndPlunder.Combat
{
    /// <summary>
    /// Resolves combat deterministically - no dice, pure strategy.
    /// Each ship deals damage equal to its cannons.
    /// </summary>
    public class CombatResolver
    {
        private UnitManager unitManager;

        public CombatResolver(UnitManager unitManager)
        {
            this.unitManager = unitManager;
        }

        /// <summary>
        /// Resolves combat deterministically between two units.
        /// Each ship shoots its cannons, dealing that much damage.
        /// NO RANDOMNESS - what you see is what you get.
        /// </summary>
        /// <param name="attackerId">First unit ID</param>
        /// <param name="defenderId">Second unit ID</param>
        /// <param name="attackerCannons">DEPRECATED - now uses unit.cannons directly</param>
        /// <param name="defenderCannons">DEPRECATED - now uses unit.cannons directly</param>
        /// <returns>Combat result with damage dealt</returns>
        public CombatResult ResolveCombat(string attackerId, string defenderId, int attackerCannons = 0, int defenderCannons = 0)
        {
            Unit attacker = unitManager.GetUnit(attackerId);
            Unit defender = unitManager.GetUnit(defenderId);

            if (attacker == null || defender == null)
            {
                Debug.LogError($"[CombatResolver] Cannot resolve combat - unit not found (attacker: {attackerId}, defender: {defenderId})");
                return new CombatResult
                {
                    attackerId = attackerId,
                    defenderId = defenderId,
                    damageToAttacker = 0,
                    damageToDefender = 0
                };
            }

            // Deterministic damage: each ship shoots its cannons
            int damageToDefender = attacker.cannons;
            int damageToAttacker = defender.cannons;

            // Apply pirate damage multiplier if applicable
            if (attacker.type == UnitType.PIRATE_SHIP)
            {
                damageToDefender *= BuildingConfig.PIRATE_DAMAGE_MULTIPLIER;
                Debug.Log($"[CombatResolver] Pirate damage multiplier applied: {attacker.cannons} × {BuildingConfig.PIRATE_DAMAGE_MULTIPLIER} = {damageToDefender}");
            }

            if (defender.type == UnitType.PIRATE_SHIP)
            {
                damageToAttacker *= BuildingConfig.PIRATE_DAMAGE_MULTIPLIER;
                Debug.Log($"[CombatResolver] Pirate damage multiplier applied: {defender.cannons} × {BuildingConfig.PIRATE_DAMAGE_MULTIPLIER} = {damageToAttacker}");
            }

            Debug.Log($"[CombatResolver] {attacker.id} ({attacker.cannons} cannons) vs " +
                      $"{defender.id} ({defender.cannons} cannons) → " +
                      $"Damage: {damageToDefender} to defender, {damageToAttacker} to attacker");

            return new CombatResult
            {
                attackerId = attackerId,
                defenderId = defenderId,
                damageToAttacker = damageToAttacker,
                damageToDefender = damageToDefender
            };
        }

        /// <summary>
        /// Preview combat outcome without applying damage.
        /// Useful for tactical UI showing potential outcomes.
        /// </summary>
        public CombatResult PreviewCombat(string attackerId, string defenderId)
        {
            return ResolveCombat(attackerId, defenderId);
        }
    }

    /// <summary>
    /// Result of combat between two units.
    /// SIMPLIFIED - no dice data, just damage numbers.
    /// </summary>
    [Serializable]
    public class CombatResult
    {
        public string attackerId;
        public string defenderId;
        public int damageToAttacker;
        public int damageToDefender;

        public override string ToString()
        {
            return $"Combat: {attackerId} vs {defenderId} - " +
                   $"Damage: {damageToAttacker} to attacker, {damageToDefender} to defender";
        }
    }
}
