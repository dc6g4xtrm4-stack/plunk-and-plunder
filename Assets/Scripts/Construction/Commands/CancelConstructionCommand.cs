using PlunkAndPlunder.Core;
using PlunkAndPlunder.Players;
using UnityEngine;

namespace PlunkAndPlunder.Construction.Commands
{
    /// <summary>
    /// Atomic command to cancel a construction job
    /// Removes from queue and refunds a portion of the cost
    /// </summary>
    public class CancelConstructionCommand : ConstructionCommand
    {
        private int playerId;
        private string jobId;
        private float refundPercent;

        public CancelConstructionCommand(int playerId, string jobId, float refundPercent = 0.75f)
        {
            this.playerId = playerId;
            this.jobId = jobId;
            this.refundPercent = refundPercent; // 75% refund by default
        }

        public override ConstructionResult Execute(ConstructionState constructionState, GameState gameState)
        {
            // Get the job
            ConstructionJob job = constructionState.GetJob(jobId);
            if (job == null)
                return ConstructionResult.Failure("Job not found");

            // Calculate refund
            int refundAmount = Mathf.RoundToInt(job.costPaid * refundPercent);

            // Mark job as cancelled
            job.status = ConstructionStatus.Cancelled;

            // Remove from shipyard queue
            if (constructionState.shipyardQueues.TryGetValue(job.shipyardId, out var queue))
            {
                queue.Remove(jobId);

                // If we cancelled the first job, promote the next one to Building status
                if (queue.Count > 0)
                {
                    string nextJobId = queue[0];
                    ConstructionJob nextJob = constructionState.GetJob(nextJobId);
                    if (nextJob != null)
                    {
                        nextJob.status = ConstructionStatus.Building;
                    }
                }
            }

            // Refund gold
            Player player = gameState.playerManager.GetPlayer(playerId);
            if (player != null)
            {
                player.gold += refundAmount;
            }

            Debug.Log($"[CancelConstructionCommand] Cancelled job {jobId} for player {playerId}, refunded {refundAmount}g ({refundPercent * 100}%)");

            return ConstructionResult.Success(jobId);
        }
    }
}
