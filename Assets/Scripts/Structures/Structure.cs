using System;
using System.Collections.Generic;
using PlunkAndPlunder.Map;
using UnityEngine;

namespace PlunkAndPlunder.Structures
{
    [Serializable]
    public class Structure
    {
        public string id;
        public int ownerId; // -1 for neutral
        public HexCoord position;
        public StructureType type;
        public int health;
        public int maxHealth;

        /// <summary>
        /// DEPRECATED: Use ConstructionManager.GetShipyardQueue(id) instead
        /// This field is kept for save compatibility but should not be accessed directly
        /// </summary>
        [System.Obsolete("Use ConstructionManager.GetShipyardQueue() instead of accessing buildQueue directly")]
        public List<BuildQueueItem> buildQueue = new List<BuildQueueItem>();

        public Structure(string id, int ownerId, HexCoord position, StructureType type)
        {
            this.id = id;
            this.ownerId = ownerId;
            this.position = position;
            this.type = type;

            // Initialize health based on structure type
            if (type == StructureType.SHIPYARD)
            {
                this.health = 3;      // Shipyards start with 3 HP
                this.maxHealth = 3;   // Takes 3 attacks to capture
            }
            else
            {
                this.health = 1;      // Other structures (if added later)
                this.maxHealth = 1;
            }
        }

        /// <summary>
        /// Apply damage to structure
        /// </summary>
        public void TakeDamage(int amount)
        {
            health = Mathf.Max(0, health - amount);
        }

        /// <summary>
        /// Check if structure is destroyed (0 HP)
        /// </summary>
        public bool IsDestroyed()
        {
            return health <= 0;
        }
    }

    public enum StructureType
    {
        SHIPYARD
    }
}
