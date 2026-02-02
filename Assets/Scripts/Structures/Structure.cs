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
        public int tier; // Structure tier/level (1 = basic, 2 = upgraded, 3 = advanced)
        public int health;
        public int maxHealth;

        /// <summary>
        /// DEPRECATED: Use ConstructionManager.GetShipyardQueue(id) instead
        /// This field is kept for save compatibility but should not be accessed directly
        /// </summary>
        [System.Obsolete("Use ConstructionManager.GetShipyardQueue() instead of accessing buildQueue directly")]
        public List<BuildQueueItem> buildQueue = new List<BuildQueueItem>();

        public Structure(string id, int ownerId, HexCoord position, StructureType type, int tier = 1)
        {
            this.id = id;
            this.ownerId = ownerId;
            this.position = position;
            this.type = type;
            this.tier = tier;

            // Initialize health based on structure type and tier
            switch (type)
            {
                case StructureType.SHIPYARD:
                    this.health = 3;
                    this.maxHealth = 3;
                    break;
                case StructureType.NAVAL_YARD:
                    this.health = 5;      // Naval Yards are tougher
                    this.maxHealth = 5;
                    break;
                case StructureType.NAVAL_FORTRESS:
                    this.health = 10;     // Fortresses are very tough
                    this.maxHealth = 10;
                    break;
                case StructureType.PIRATE_COVE:
                    this.health = 5;      // Pirate Coves are tough
                    this.maxHealth = 5;
                    break;
                default:
                    this.health = 1;
                    this.maxHealth = 1;
                    break;
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
        SHIPYARD,
        NAVAL_YARD,        // Upgraded Shipyard (Tier 2)
        NAVAL_FORTRESS,    // Advanced Shipyard (Tier 3) - Can build Galleons
        PIRATE_COVE        // Pirate structure - Spawns pirate ships
    }
}
