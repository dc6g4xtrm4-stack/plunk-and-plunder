using System;
using System.Collections.Generic;
using PlunkAndPlunder.Construction.Commands;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using UnityEngine;

namespace PlunkAndPlunder.Construction
{
    /// <summary>
    /// Central authority for all construction operations
    /// Manages queue state, validation, and command execution
    /// Singleton service pattern
    /// </summary>
    public class ConstructionManager : MonoBehaviour
    {
        public static ConstructionManager Instance { get; private set; }

        // Current construction state (read-only from outside)
        private ConstructionState state;

        // CRITICAL: GameState reference for construction operations
        // Must be injected via Initialize() before use
        private GameState gameState;

        // Events for UI updates
        public event Action<ConstructionQueuedEvent> OnConstructionQueued;
        public event Action<ConstructionProgressedEvent> OnConstructionProgressed;
        public event Action<ConstructionCompletedEvent> OnConstructionCompleted;
        public event Action<ConstructionCancelledEvent> OnConstructionCancelled;

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[ConstructionManager] Destroying duplicate instance");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize state
            state = new ConstructionState();

            Debug.Log("[ConstructionManager] Initialized");
        }

        /// <summary>
        /// Initialize with a GameState reference
        /// MUST be called before any construction operations
        /// </summary>
        public void Initialize(GameState gameState)
        {
            this.gameState = gameState;
            Debug.Log("[ConstructionManager] GameState injected");
        }

        /// <summary>
        /// Get a read-only copy of the current state
        /// </summary>
        public ConstructionState GetState()
        {
            return state.Clone();
        }

        /// <summary>
        /// Queue a ship for construction at a shipyard
        /// Returns: Success + job ID, or Failure + reason
        /// </summary>
        public ConstructionResult QueueShip(int playerId, string shipyardId)
        {
            // 1. Validate request
            var validation = ConstructionValidator.ValidateQueueShip(
                playerId, shipyardId, state, gameState
            );

            if (!validation.isValid)
            {
                Debug.LogWarning($"[ConstructionManager] QueueShip validation failed: {validation.reason}");
                return ConstructionResult.Failure(validation.reason);
            }

            // 2. Create and execute command
            var command = new QueueShipCommand(playerId, shipyardId);
            var result = command.Execute(state, gameState);

            if (result.success)
            {
                // 3. Emit event
                var job = state.GetJob(result.jobId);
                OnConstructionQueued?.Invoke(new ConstructionQueuedEvent
                {
                    jobId = result.jobId,
                    shipyardId = shipyardId,
                    itemType = "Ship",
                    turnsRequired = job.turnsTotal,
                    cost = job.costPaid
                });

                Debug.Log($"[ConstructionManager] Successfully queued ship at {shipyardId}");
            }

            return result;
        }

        /// <summary>
        /// Queue a Galleon for construction at a Naval Fortress
        /// </summary>
        public ConstructionResult QueueGalleon(int playerId, string navalFortressId)
        {
            // 1. Validate request
            var validation = ConstructionValidator.ValidateQueueGalleon(
                playerId, navalFortressId, state, gameState
            );

            if (!validation.isValid)
            {
                Debug.LogWarning($"[ConstructionManager] QueueGalleon validation failed: {validation.reason}");
                return ConstructionResult.Failure(validation.reason);
            }

            // 2. Create and execute command
            var command = new QueueGalleonCommand(playerId, navalFortressId);
            var result = command.Execute(state, gameState);

            if (result.success)
            {
                // 3. Emit event
                var job = state.GetJob(result.jobId);
                OnConstructionQueued?.Invoke(new ConstructionQueuedEvent
                {
                    jobId = result.jobId,
                    shipyardId = navalFortressId,
                    itemType = "Galleon",
                    turnsRequired = job.turnsTotal,
                    cost = job.costPaid
                });

                Debug.Log($"[ConstructionManager] Successfully queued Galleon at {navalFortressId}");
            }

            return result;
        }

        /// <summary>
        /// Deploy a ship as a shipyard (consumes ship)
        /// </summary>
        public ConstructionResult DeployShipyard(int playerId, string shipId, HexCoord position)
        {
            // 1. Validate request
            var validation = ConstructionValidator.ValidateDeployShipyard(
                playerId, shipId, position, gameState
            );

            if (!validation.isValid)
            {
                Debug.LogWarning($"[ConstructionManager] DeployShipyard validation failed: {validation.reason}");
                return ConstructionResult.Failure(validation.reason);
            }

            // 2. Create and execute command
            var command = new DeployShipyardCommand(playerId, shipId, position);
            var result = command.Execute(state, gameState);

            if (result.success)
            {
                Debug.Log($"[ConstructionManager] Successfully deployed shipyard at {position}");
            }

            return result;
        }

        /// <summary>
        /// Cancel a construction job
        /// </summary>
        public ConstructionResult CancelJob(int playerId, string jobId, float refundPercent = 0.75f)
        {
            // 1. Validate request
            var validation = ConstructionValidator.ValidateCancelJob(
                playerId, jobId, state
            );

            if (!validation.isValid)
            {
                Debug.LogWarning($"[ConstructionManager] CancelJob validation failed: {validation.reason}");
                return ConstructionResult.Failure(validation.reason);
            }

            // Get job info before cancellation
            var job = state.GetJob(jobId);
            int refundAmount = Mathf.RoundToInt(job.costPaid * refundPercent);

            // 2. Create and execute command
            var command = new CancelConstructionCommand(playerId, jobId, refundPercent);
            var result = command.Execute(state, gameState);

            if (result.success)
            {
                // 3. Emit event
                OnConstructionCancelled?.Invoke(new ConstructionCancelledEvent
                {
                    jobId = jobId,
                    shipyardId = job.shipyardId,
                    refundAmount = refundAmount
                });

                Debug.Log($"[ConstructionManager] Successfully cancelled job {jobId}");
            }

            return result;
        }

        /// <summary>
        /// Process all construction queues for turn advancement
        /// Called by TurnResolver
        /// </summary>
        public List<GameEvent> ProcessTurn(int turnNumber)
        {
            var events = ConstructionProcessor.ProcessAllQueues(state, gameState, turnNumber);

            // Fire progress events
            foreach (var evt in events)
            {
                if (evt is ShipBuiltEvent shipBuilt)
                {
                    OnConstructionCompleted?.Invoke(new ConstructionCompletedEvent
                    {
                        jobId = "completed", // Job was already removed
                        shipyardId = shipBuilt.shipyardId,
                        itemType = "Ship",
                        spawnedUnitId = shipBuilt.shipId,
                        spawnPosition = shipBuilt.position
                    });
                }
                else if (evt.type == GameEventType.ConstructionProgressed)
                {
                    // Note: We could enhance this to include job details if needed
                }
            }

            return events;
        }

        /// <summary>
        /// Initialize a new shipyard's construction queue
        /// Called when a new shipyard is created
        /// </summary>
        public void RegisterShipyard(string shipyardId)
        {
            state.InitializeShipyard(shipyardId);
            Debug.Log($"[ConstructionManager] Registered new shipyard {shipyardId}");
        }

        /// <summary>
        /// Get all jobs for a specific shipyard
        /// </summary>
        public List<ConstructionJob> GetShipyardQueue(string shipyardId)
        {
            return state.GetQueueForShipyard(shipyardId);
        }

        /// <summary>
        /// Check if a shipyard's queue is full
        /// </summary>
        public bool IsShipyardQueueFull(string shipyardId)
        {
            return state.IsQueueFull(shipyardId);
        }

        /// <summary>
        /// Reset all construction state (for new game)
        /// </summary>
        public void Reset()
        {
            state = new ConstructionState();
            Debug.Log("[ConstructionManager] State reset");
        }
    }
}
