using System;
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

        public Unit(string id, int ownerId, HexCoord position, UnitType type)
        {
            this.id = id;
            this.ownerId = ownerId;
            this.position = position;
            this.type = type;
            this.health = 1; // MVP: 1 HP ships
            this.maxHealth = 1;
            this.facingAngle = 0f; // Default facing east
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
