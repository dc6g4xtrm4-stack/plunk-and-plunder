using System;

namespace PlunkAndPlunder.Construction
{
    /// <summary>
    /// Individual construction job
    /// Represents a single item being built in a queue
    /// </summary>
    [Serializable]
    public class ConstructionJob
    {
        public string jobId;              // Unique identifier
        public string shipyardId;         // Where it's being built
        public int playerId;              // Who ordered it
        public string itemType;           // "Ship", "Upgrade", etc.
        public int turnsRemaining;        // 3 -> 2 -> 1 -> 0
        public int turnsTotal;            // 3 (for progress %)
        public int costPaid;              // Gold already spent
        public ConstructionStatus status; // Queued, Building, Completed, Cancelled

        public float ProgressPercent => turnsTotal > 0 ? 1f - (turnsRemaining / (float)turnsTotal) : 1f;

        public ConstructionJob Clone()
        {
            return new ConstructionJob
            {
                jobId = this.jobId,
                shipyardId = this.shipyardId,
                playerId = this.playerId,
                itemType = this.itemType,
                turnsRemaining = this.turnsRemaining,
                turnsTotal = this.turnsTotal,
                costPaid = this.costPaid,
                status = this.status
            };
        }
    }

    public enum ConstructionStatus
    {
        Queued,    // Waiting in queue (not active yet)
        Building,  // First in queue, actively progressing
        Completed, // Finished, ship spawned
        Cancelled  // Cancelled by player
    }
}
