using PlunkAndPlunder.Map;

namespace PlunkAndPlunder.Construction
{
    /// <summary>
    /// Event fired when construction is queued
    /// </summary>
    public class ConstructionQueuedEvent
    {
        public string jobId;
        public string shipyardId;
        public string itemType;
        public int turnsRequired;
        public int cost;
    }

    /// <summary>
    /// Event fired when construction progresses
    /// </summary>
    public class ConstructionProgressedEvent
    {
        public string jobId;
        public string shipyardId;
        public int turnsRemaining;
        public float progressPercent;
    }

    /// <summary>
    /// Event fired when construction completes
    /// </summary>
    public class ConstructionCompletedEvent
    {
        public string jobId;
        public string shipyardId;
        public string itemType;
        public string spawnedUnitId;
        public HexCoord spawnPosition;
    }

    /// <summary>
    /// Event fired when construction is cancelled
    /// </summary>
    public class ConstructionCancelledEvent
    {
        public string jobId;
        public string shipyardId;
        public int refundAmount;
    }
}
