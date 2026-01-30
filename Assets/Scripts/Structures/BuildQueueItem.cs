using System;

namespace PlunkAndPlunder.Structures
{
    [Serializable]
    public class BuildQueueItem
    {
        public string itemType; // "Ship"
        public int turnsRemaining; // 3, 2, 1
        public int cost;

        public BuildQueueItem(string itemType, int turnsRemaining, int cost)
        {
            this.itemType = itemType;
            this.turnsRemaining = turnsRemaining;
            this.cost = cost;
        }
    }
}
