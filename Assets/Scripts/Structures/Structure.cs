using System;
using System.Collections.Generic;
using PlunkAndPlunder.Map;

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
            this.health = 1; // MVP: simple health
            this.maxHealth = 1;
        }
    }

    public enum StructureType
    {
        SHIPYARD
    }
}
