using System;
using System.Collections.Generic;
using PlunkAndPlunder.Map;
using UnityEngine;

namespace PlunkAndPlunder.Units
{
    [Serializable]
    public class Unit
    {
        public string id;
        public int ownerId;
        public HexCoord position;
        public UnitType type;
        public int health;
        public int maxHealth;
        public float facingAngle; // Rotation angle in degrees (0 = facing right/east)
        public int movementRemaining; // Movement remaining this turn
        public List<HexCoord> queuedPath; // Path queued from previous turn (for multi-turn moves)

        public Unit(string id, int ownerId, HexCoord position, UnitType type)
        {
            this.id = id;
            this.ownerId = ownerId;
            this.position = position;
            this.type = type;
            this.health = 10; // Ships start with 10 HP
            this.maxHealth = 10;
            this.facingAngle = 0f; // Default facing east
            this.movementRemaining = GetMovementCapacity(); // Initialize with full movement
        }

        /// <summary>
        /// Apply damage to this unit
        /// </summary>
        public void TakeDamage(int damage)
        {
            health = Mathf.Max(0, health - damage);
        }

        /// <summary>
        /// Check if unit is dead
        /// </summary>
        public bool IsDead()
        {
            return health <= 0;
        }

        /// <summary>
        /// Get the maximum movement capacity for this unit based on its tier
        /// Tier is determined by maxHealth thresholds:
        /// Tier 1 (maxHealth 1-10): 3 movement
        /// Tier 2 (maxHealth 11-20): 4 movement
        /// Tier 3 (maxHealth 21+): 5 movement
        /// </summary>
        public int GetMovementCapacity()
        {
            // Determine tier based on maxHealth thresholds
            int tier = 1;
            if (maxHealth >= 21)
                tier = 3;
            else if (maxHealth >= 11)
                tier = 2;

            // Base movement of 3, plus bonus based on tier
            return 3 + (tier - 1);
        }

        /// <summary>
        /// Reset movement to full capacity at start of turn
        /// </summary>
        public void ResetMovement()
        {
            movementRemaining = GetMovementCapacity();
        }

        /// <summary>
        /// Update facing direction based on movement from oldPos to newPos
        /// </summary>
        public void UpdateFacing(HexCoord oldPos, HexCoord newPos)
        {
            if (oldPos.Equals(newPos))
                return;

            // Calculate direction vector
            int dq = newPos.q - oldPos.q;
            int dr = newPos.r - oldPos.r;

            // Convert hex direction to angle
            // Hex directions (flat-top orientation):
            // East (1,0), West (-1,0), NE (1,-1), NW (0,-1), SE (0,1), SW (-1,1)
            if (dq == 1 && dr == 0) facingAngle = 0f;      // East
            else if (dq == 1 && dr == -1) facingAngle = 60f;   // Northeast
            else if (dq == 0 && dr == -1) facingAngle = 120f;  // Northwest
            else if (dq == -1 && dr == 0) facingAngle = 180f;  // West
            else if (dq == -1 && dr == 1) facingAngle = 240f;  // Southwest
            else if (dq == 0 && dr == 1) facingAngle = 300f;   // Southeast
        }
    }

    public enum UnitType
    {
        SHIP
    }
}
