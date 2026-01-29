using System;
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
        HARBOR,
        SHIPYARD // Future-ready placeholder
    }
}
