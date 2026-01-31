using System;
using System.Collections.Generic;
using System.Linq;

namespace PlunkAndPlunder.Construction
{
    /// <summary>
    /// Centralized state for all construction activity
    /// Replaces scattered Structure.buildQueue fields
    /// </summary>
    [Serializable]
    public class ConstructionState
    {
        // All active construction jobs
        public Dictionary<string, ConstructionJob> activeJobs = new Dictionary<string, ConstructionJob>();

        // Queue per shipyard (ordered list of job IDs)
        public Dictionary<string, List<string>> shipyardQueues = new Dictionary<string, List<string>>();

        /// <summary>
        /// Get a job by ID
        /// </summary>
        public ConstructionJob GetJob(string jobId)
        {
            return activeJobs.TryGetValue(jobId, out var job) ? job : null;
        }

        /// <summary>
        /// Get all jobs for a specific shipyard, in queue order
        /// </summary>
        public List<ConstructionJob> GetQueueForShipyard(string shipyardId)
        {
            if (!shipyardQueues.TryGetValue(shipyardId, out var queue))
                return new List<ConstructionJob>();

            return queue
                .Select(jobId => GetJob(jobId))
                .Where(job => job != null)
                .ToList();
        }

        /// <summary>
        /// Get the number of jobs in a shipyard's queue
        /// </summary>
        public int GetQueueLength(string shipyardId)
        {
            return shipyardQueues.TryGetValue(shipyardId, out var queue) ? queue.Count : 0;
        }

        /// <summary>
        /// Check if a shipyard's queue is full
        /// </summary>
        public bool IsQueueFull(string shipyardId)
        {
            return GetQueueLength(shipyardId) >= Structures.BuildingConfig.MAX_QUEUE_SIZE;
        }

        /// <summary>
        /// Get the currently building job for a shipyard (first in queue)
        /// </summary>
        public ConstructionJob GetActiveJob(string shipyardId)
        {
            if (!shipyardQueues.TryGetValue(shipyardId, out var queue) || queue.Count == 0)
                return null;

            return GetJob(queue[0]);
        }

        /// <summary>
        /// Create a deep copy of this state
        /// </summary>
        public ConstructionState Clone()
        {
            var clone = new ConstructionState();

            // Clone jobs
            foreach (var kvp in activeJobs)
            {
                clone.activeJobs[kvp.Key] = kvp.Value.Clone();
            }

            // Clone queues
            foreach (var kvp in shipyardQueues)
            {
                clone.shipyardQueues[kvp.Key] = new List<string>(kvp.Value);
            }

            return clone;
        }

        /// <summary>
        /// Initialize queue for a new shipyard
        /// </summary>
        public void InitializeShipyard(string shipyardId)
        {
            if (!shipyardQueues.ContainsKey(shipyardId))
            {
                shipyardQueues[shipyardId] = new List<string>();
            }
        }
    }
}
