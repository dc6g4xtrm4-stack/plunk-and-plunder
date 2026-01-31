using PlunkAndPlunder.Core;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Structures;
using UnityEngine;

namespace PlunkAndPlunder.Construction.Commands
{
    /// <summary>
    /// Atomic command to queue a ship for construction
    /// Either succeeds completely or fails with no side effects
    /// </summary>
    public class QueueShipCommand : ConstructionCommand
    {
        private int playerId;
        private string shipyardId;

        public QueueShipCommand(int playerId, string shipyardId)
        {
            this.playerId = playerId;
            this.shipyardId = shipyardId;
        }

        public override ConstructionResult Execute(ConstructionState constructionState, GameState gameState)
        {
            // Create new job
            string jobId = GenerateJobId();
            bool isFirstInQueue = constructionState.GetQueueLength(shipyardId) == 0;

            var job = new ConstructionJob
            {
                jobId = jobId,
                shipyardId = shipyardId,
                playerId = playerId,
                itemType = "Ship",
                turnsRemaining = BuildingConfig.SHIP_BUILD_TIME,
                turnsTotal = BuildingConfig.SHIP_BUILD_TIME,
                costPaid = BuildingConfig.BUILD_SHIP_COST,
                status = isFirstInQueue ? ConstructionStatus.Building : ConstructionStatus.Queued
            };

            // Initialize shipyard queue if needed
            constructionState.InitializeShipyard(shipyardId);

            // Atomic state updates
            constructionState.activeJobs[jobId] = job;
            constructionState.shipyardQueues[shipyardId].Add(jobId);

            // Deduct gold (only after state update succeeds)
            Player player = gameState.playerManager.GetPlayer(playerId);
            player.gold -= BuildingConfig.BUILD_SHIP_COST;

            Debug.Log($"[QueueShipCommand] Queued ship at {shipyardId} for player {playerId}, job {jobId} ({job.status})");

            return ConstructionResult.Success(jobId);
        }
    }
}
