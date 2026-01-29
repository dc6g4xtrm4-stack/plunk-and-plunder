using System;
using System.Collections;
using System.Collections.Generic;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Units;
using UnityEngine;

namespace PlunkAndPlunder.Resolution
{
    /// <summary>
    /// Animates turn resolution events step-by-step
    /// </summary>
    public class TurnAnimator : MonoBehaviour
    {
        private UnitManager unitManager;
        private bool isAnimating = false;
        private bool isPaused = false;

        public float hexStepDelay = 0.25f; // Time to move one hex
        public float combatPauseDelay = 0.5f; // Time to pause for combat
        public float eventPauseDelay = 0.3f; // Time to pause for other events

        public event Action<GameState> OnAnimationStep; // Fired after each animation step
        public event Action OnAnimationComplete; // Fired when all animations done
        public event Action<ConflictDetectedEvent> OnConflictDetected; // Fired when conflict is detected during animation

        private void Awake()
        {
            // This component will be added to GameManager
        }

        public void Initialize(UnitManager unitManager)
        {
            this.unitManager = unitManager;
        }

        public bool IsAnimating => isAnimating;

        public void PauseAnimation()
        {
            isPaused = true;
        }

        public void ResumeAnimation()
        {
            isPaused = false;
        }

        /// <summary>
        /// Start animating a list of game events
        /// </summary>
        public void AnimateEvents(List<GameEvent> events, GameState state)
        {
            if (isAnimating)
            {
                Debug.LogWarning("[TurnAnimator] Already animating, cannot start new animation");
                return;
            }

            StartCoroutine(AnimateEventsCoroutine(events, state));
        }

        private IEnumerator AnimateEventsCoroutine(List<GameEvent> events, GameState state)
        {
            isAnimating = true;

            Debug.Log($"[TurnAnimator] Starting animation of {events.Count} events");

            foreach (GameEvent gameEvent in events)
            {
                switch (gameEvent)
                {
                    case UnitMovedEvent moveEvent:
                        yield return AnimateMovement(moveEvent, state);
                        break;

                    case UnitDestroyedEvent destroyEvent:
                        yield return AnimateDestruction(destroyEvent, state);
                        break;

                    case UnitsCollidedEvent collisionEvent:
                        yield return AnimateCollision(collisionEvent, state);
                        break;

                    case ShipBuiltEvent buildEvent:
                        yield return AnimateShipBuilt(buildEvent, state);
                        break;

                    case ShipyardDeployedEvent deployEvent:
                        yield return AnimateShipyardDeployed(deployEvent, state);
                        break;

                    case ShipRepairedEvent repairEvent:
                        yield return AnimateShipRepaired(repairEvent, state);
                        break;

                    case ShipUpgradedEvent upgradeEvent:
                        yield return AnimateShipUpgraded(upgradeEvent, state);
                        break;

                    case CombatOccurredEvent combatEvent:
                        yield return AnimateCombat(combatEvent, state);
                        break;

                    case ConflictDetectedEvent conflictEvent:
                        yield return AnimateConflictDetected(conflictEvent, state);
                        break;

                    default:
                        // Instant events - just pause briefly
                        yield return new WaitForSeconds(eventPauseDelay);
                        break;
                }
            }

            Debug.Log("[TurnAnimator] Animation complete");

            isAnimating = false;
            OnAnimationComplete?.Invoke();
        }

        private IEnumerator AnimateMovement(UnitMovedEvent moveEvent, GameState state)
        {
            Unit unit = unitManager.GetUnit(moveEvent.unitId);
            if (unit == null)
            {
                Debug.LogWarning($"[TurnAnimator] Unit {moveEvent.unitId} not found for movement animation");
                yield break;
            }

            List<HexCoord> path = moveEvent.path;
            if (path == null || path.Count < 2)
            {
                // No path, just move instantly
                unitManager.MoveUnit(moveEvent.unitId, moveEvent.to);
                OnAnimationStep?.Invoke(state);
                yield break;
            }

            Debug.Log($"[TurnAnimator] Animating unit {moveEvent.unitId} along path with {path.Count} steps");

            // Animate step-by-step movement along the path
            HexCoord currentPos = moveEvent.from;
            for (int i = 0; i < path.Count; i++)
            {
                HexCoord nextPos = path[i];

                // Skip if we're already at this position (shouldn't happen, but safety check)
                if (currentPos.Equals(nextPos))
                    continue;

                // Move the unit one step
                unitManager.MoveUnit(moveEvent.unitId, nextPos);
                currentPos = nextPos;

                // Trigger visual update
                OnAnimationStep?.Invoke(state);

                // Wait before next step (except for the last step)
                if (i < path.Count - 1)
                {
                    yield return new WaitForSeconds(hexStepDelay);
                }
            }

            // Small pause at the end of movement
            yield return new WaitForSeconds(hexStepDelay * 0.5f);
        }

        private IEnumerator AnimateDestruction(UnitDestroyedEvent destroyEvent, GameState state)
        {
            Debug.Log($"[TurnAnimator] Animating destruction of unit {destroyEvent.unitId}");

            // Remove the unit
            unitManager.RemoveUnit(destroyEvent.unitId);

            // Trigger visual update
            OnAnimationStep?.Invoke(state);

            // Pause to show destruction
            yield return new WaitForSeconds(combatPauseDelay);
        }

        private IEnumerator AnimateCollision(UnitsCollidedEvent collisionEvent, GameState state)
        {
            Debug.Log($"[TurnAnimator] Animating collision of {collisionEvent.unitIds.Count} units at {collisionEvent.position}");

            // For collisions, units don't actually move (they bounce back)
            // Just pause to indicate something happened
            OnAnimationStep?.Invoke(state);

            yield return new WaitForSeconds(combatPauseDelay);
        }

        private IEnumerator AnimateShipBuilt(ShipBuiltEvent buildEvent, GameState state)
        {
            Debug.Log($"[TurnAnimator] Animating ship built: {buildEvent.shipId} at {buildEvent.position}");

            // Ship was already created by TurnResolver
            // Just trigger visual update
            OnAnimationStep?.Invoke(state);

            yield return new WaitForSeconds(eventPauseDelay);
        }

        private IEnumerator AnimateShipyardDeployed(ShipyardDeployedEvent deployEvent, GameState state)
        {
            Debug.Log($"[TurnAnimator] Animating shipyard deployed at {deployEvent.position}");

            // Remove the ship that was converted to a shipyard
            unitManager.RemoveUnit(deployEvent.shipId);

            // Trigger visual update
            OnAnimationStep?.Invoke(state);

            yield return new WaitForSeconds(eventPauseDelay);
        }

        private IEnumerator AnimateShipRepaired(ShipRepairedEvent repairEvent, GameState state)
        {
            Debug.Log($"[TurnAnimator] Animating ship repair: {repairEvent.shipId}");

            // Ship was already repaired by TurnResolver
            // Just trigger visual update
            OnAnimationStep?.Invoke(state);

            yield return new WaitForSeconds(eventPauseDelay);
        }

        private IEnumerator AnimateShipUpgraded(ShipUpgradedEvent upgradeEvent, GameState state)
        {
            Debug.Log($"[TurnAnimator] Animating ship upgrade: {upgradeEvent.shipId}");

            // Ship was already upgraded by TurnResolver
            // Just trigger visual update
            OnAnimationStep?.Invoke(state);

            yield return new WaitForSeconds(eventPauseDelay);
        }

        private IEnumerator AnimateCombat(CombatOccurredEvent combatEvent, GameState state)
        {
            Debug.Log($"[TurnAnimator] Animating combat: {combatEvent.attackerId} vs {combatEvent.defenderId}");

            // TODO: Show combat indicator (red flashing on both units)
            // TODO: Show dice roll results
            // TODO: Display damage numbers

            // Trigger visual update
            OnAnimationStep?.Invoke(state);

            // Pause to show combat
            yield return new WaitForSeconds(combatPauseDelay * 2);

            // If units were destroyed, they should be removed by subsequent UnitDestroyedEvent
        }

        private IEnumerator AnimateConflictDetected(ConflictDetectedEvent conflictEvent, GameState state)
        {
            Debug.Log($"[TurnAnimator] Conflict detected at {conflictEvent.position} with {conflictEvent.unitIds.Count} units");

            // Fire event to notify GameManager
            OnConflictDetected?.Invoke(conflictEvent);

            // Wait while paused (conflict resolution UI will be shown)
            while (isPaused)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
