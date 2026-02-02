using PlunkAndPlunder.Core;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Structures;
using UnityEngine;

namespace PlunkAndPlunder.Construction.Commands
{
    /// <summary>
    /// Atomic command to queue a Galleon for construction at a Naval Fortress
    /// Either succeeds completely or fails with no side effects
    /// </summary>
    public class QueueGalleonCommand : ConstructionCommand
    {
        private int playerId;
        private string navalFortressId;

        public QueueGalleonCommand(int playerId, string navalFortressId)
        {
            this.playerId = playerId;
            this.navalFortressId = navalFortressId;
        }

        public override ConstructionResult Execute(ConstructionState constructionState, GameState gameState)
        {
            // Create new job
            string jobId = GenerateJobId();
            bool isFirstInQueue = constructionState.GetQueueLength(navalFortressId) == 0;

            var job = new ConstructionJob
            {
                jobId = jobId,
                shipyardId = navalFortressId, // Use shipyardId field for Naval Fortress ID
                playerId = playerId,
                itemType = "Galleon",
                turnsRemaining = BuildingConfig.GALLEON_BUILD_TIME,
                turnsTotal = BuildingConfig.GALLEON_BUILD_TIME,
                costPaid = BuildingConfig.BUILD_GALLEON_COST,
                status = isFirstInQueue ? ConstructionStatus.Building : ConstructionStatus.Queued
            };

            // Initialize naval fortress queue if needed
            constructionState.InitializeShipyard(navalFortressId);

            // Atomic state updates
            constructionState.activeJobs[jobId] = job;
            constructionState.shipyardQueues[navalFortressId].Add(jobId);

            // Deduct gold (only after state update succeeds)
            Player player = gameState.playerManager.GetPlayer(playerId);
            player.gold -= BuildingConfig.BUILD_GALLEON_COST;

            Debug.Log($"[QueueGalleonCommand] Queued Galleon at {navalFortressId} for player {playerId}, job {jobId} ({job.status})");

            return ConstructionResult.Success(jobId);
        }
    }
}
