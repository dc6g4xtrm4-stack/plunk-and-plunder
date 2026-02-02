using System.Collections.Generic;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Structures;
using PlunkAndPlunder.Units;
using UnityEngine;

namespace PlunkAndPlunder.Construction
{
    /// <summary>
    /// Processes construction queue advancement during turn resolution
    /// </summary>
    public static class ConstructionProcessor
    {
        /// <summary>
        /// Process all construction queues, advancing progress and spawning completed units
        /// Called by TurnResolver at the start of each turn
        /// </summary>
        public static List<GameEvent> ProcessAllQueues(ConstructionState state, GameState gameState, int turnNumber)
        {
            List<GameEvent> events = new List<GameEvent>();

            // Process each shipyard's queue
            foreach (var kvp in state.shipyardQueues)
            {
                string shipyardId = kvp.Key;
                List<string> queue = kvp.Value;

                if (queue.Count == 0) continue;

                // Get first job (actively building)
                string activeJobId = queue[0];
                ConstructionJob job = state.GetJob(activeJobId);

                if (job == null || job.status != ConstructionStatus.Building)
                    continue;

                // Advance progress
                job.turnsRemaining--;

                Debug.Log($"[ConstructionProcessor] Shipyard {shipyardId} building {job.itemType}: {job.turnsRemaining} turns remaining");

                // Fire progress event
                events.Add(new GameEvent(turnNumber, GameEventType.ConstructionProgressed,
                    $"Shipyard {shipyardId} building {job.itemType} ({job.turnsTotal - job.turnsRemaining}/{job.turnsTotal})"));

                // Check completion
                if (job.turnsRemaining <= 0)
                {
                    events.AddRange(CompleteConstruction(job, state, gameState, turnNumber));
                }
            }

            return events;
        }

        /// <summary>
        /// Complete a construction job - spawn unit and update queue
        /// </summary>
        private static List<GameEvent> CompleteConstruction(ConstructionJob job, ConstructionState state, GameState gameState, int turnNumber)
        {
            List<GameEvent> events = new List<GameEvent>();

            // Get shipyard
            Structure shipyard = gameState.structureManager.GetStructure(job.shipyardId);
            if (shipyard == null)
            {
                Debug.LogError($"[ConstructionProcessor] Shipyard {job.shipyardId} not found for completed job {job.jobId}");
                return events;
            }

            // Determine unit type based on job item type
            UnitType unitType = UnitType.SHIP; // Default to regular ship
            if (job.itemType == "Galleon")
            {
                unitType = UnitType.GALLEON;
            }

            // Spawn ship or galleon
            Unit newShip = gameState.unitManager.CreateUnit(
                job.playerId,
                shipyard.position,
                unitType
            );

            // Update job status
            job.status = ConstructionStatus.Completed;

            // Remove from queue
            state.shipyardQueues[job.shipyardId].RemoveAt(0);

            // Promote next job to Building status
            if (state.shipyardQueues[job.shipyardId].Count > 0)
            {
                string nextJobId = state.shipyardQueues[job.shipyardId][0];
                ConstructionJob nextJob = state.GetJob(nextJobId);
                if (nextJob != null)
                {
                    nextJob.status = ConstructionStatus.Building;
                    Debug.Log($"[ConstructionProcessor] Promoted job {nextJobId} to Building status");
                }
            }

            // Create event
            events.Add(new ShipBuiltEvent(
                turnNumber: turnNumber,
                shipId: newShip.id,
                shipyardId: job.shipyardId,
                playerId: job.playerId,
                position: shipyard.position,
                cost: job.costPaid
            ));

            Debug.Log($"[ConstructionProcessor] Completed {job.itemType} at {job.shipyardId}, spawned {newShip.id}");

            return events;
        }
    }
}
