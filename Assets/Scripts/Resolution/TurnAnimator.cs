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
        public event Action<CombatOccurredEvent> OnCombatOccurred; // Fired when combat occurs

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

            Debug.Log($"[TurnAnimator] AnimateEvents called with {events.Count} events");
            for (int i = 0; i < events.Count; i++)
            {
                Debug.Log($"[TurnAnimator]   Event {i}: {events[i].type}");
            }

            StartCoroutine(AnimateEventsCoroutine(events, state));
        }

        private IEnumerator AnimateEventsCoroutine(List<GameEvent> events, GameState state)
        {
            isAnimating = true;

            Debug.Log($"[TurnAnimator] ===== STARTING ANIMATION PHASE =====");
            Debug.Log($"[TurnAnimator] Starting animation of {events.Count} events");

            // Separate movement events from other events
            List<UnitMovedEvent> moveEvents = new List<UnitMovedEvent>();
            List<GameEvent> otherEvents = new List<GameEvent>();

            foreach (GameEvent gameEvent in events)
            {
                if (gameEvent is UnitMovedEvent moveEvent)
                {
                    moveEvents.Add(moveEvent);
                }
                else
                {
                    otherEvents.Add(gameEvent);
                }
            }

            Debug.Log($"[TurnAnimator] Event breakdown: {moveEvents.Count} movement events, {otherEvents.Count} other events");

            // Animate all movements simultaneously (step-by-step)
            if (moveEvents.Count > 0)
            {
                yield return AnimateSimultaneousMovement(moveEvents, state);
            }

            // Then animate other events sequentially
            Debug.Log($"[TurnAnimator] Movement complete, now animating {otherEvents.Count} other events");
            foreach (GameEvent gameEvent in otherEvents)
            {
                switch (gameEvent)
                {
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

                    case CollisionDetectedEvent collisionDetectedEvent:
                        // Collision detected - log it but don't animate (UI handles this)
                        Debug.Log($"[TurnAnimator] Collision detected at {collisionDetectedEvent.destination}");
                        yield return new WaitForSeconds(eventPauseDelay);
                        break;

                    case CollisionNeedsResolutionEvent collisionNeedsResolutionEvent:
                        // Skip animation - this is handled by the collision resolution UI
                        break;

                    case CollisionResolvedEvent collisionResolvedEvent:
                        Debug.Log($"[TurnAnimator] Collision resolved: {collisionResolvedEvent.resolution}");
                        yield return new WaitForSeconds(eventPauseDelay);
                        break;

                    default:
                        // Instant events - just pause briefly
                        yield return new WaitForSeconds(eventPauseDelay);
                        break;
                }
            }

            Debug.Log("[TurnAnimator] ===== ANIMATION PHASE COMPLETE =====");

            isAnimating = false;
            OnAnimationComplete?.Invoke();
        }

        /// <summary>
        /// Animate all unit movements simultaneously, step-by-step
        /// All units take their first step together, then second step, etc.
        /// </summary>
        private IEnumerator AnimateSimultaneousMovement(List<UnitMovedEvent> moveEvents, GameState state)
        {
            Debug.Log($"[TurnAnimator] Animating {moveEvents.Count} units simultaneously");

            // Build a dictionary of unit ID -> path
            Dictionary<string, List<HexCoord>> unitPaths = new Dictionary<string, List<HexCoord>>();
            Dictionary<string, int> unitCurrentStep = new Dictionary<string, int>();

            int maxPathLength = 0;

            foreach (UnitMovedEvent moveEvent in moveEvents)
            {
                if (moveEvent.path != null && moveEvent.path.Count > 1)
                {
                    unitPaths[moveEvent.unitId] = moveEvent.path;
                    unitCurrentStep[moveEvent.unitId] = 1; // Start at index 1 (skip starting position at index 0)
                    maxPathLength = Mathf.Max(maxPathLength, moveEvent.path.Count);
                    Debug.Log($"[TurnAnimator] Unit {moveEvent.unitId} has path with {moveEvent.path.Count} waypoints: [{string.Join(", ", moveEvent.path)}]");
                }
            }

            if (maxPathLength == 0)
            {
                Debug.Log("[TurnAnimator] No paths to animate");
                yield break;
            }

            Debug.Log($"[TurnAnimator] Max path length: {maxPathLength}, animating step-by-step");

            // Animate step-by-step: all units move together
            for (int step = 0; step < maxPathLength; step++)
            {
                bool anyUnitMoved = false;

                // Move all units that have a step at this index
                foreach (var kvp in unitPaths)
                {
                    string unitId = kvp.Key;
                    List<HexCoord> path = kvp.Value;
                    int currentStepIndex = unitCurrentStep[unitId];

                    // Check if this unit still has steps to take
                    if (currentStepIndex < path.Count)
                    {
                        HexCoord nextPos = path[currentStepIndex];

                        // Move the unit
                        unitManager.MoveUnit(unitId, nextPos);
                        unitCurrentStep[unitId]++;
                        anyUnitMoved = true;

                        if (step == 0 || currentStepIndex % 5 == 0)
                        {
                            Debug.Log($"[TurnAnimator] Step {step}: Unit {unitId} moved to {nextPos}");
                        }
                    }
                }

                // Fire animation step event so rendering can update
                if (anyUnitMoved)
                {
                    Debug.Log($"[TurnAnimator] Step {step} complete, firing OnAnimationStep event");
                    OnAnimationStep?.Invoke(state);

                    // Wait before next step
                    yield return new WaitForSeconds(hexStepDelay);
                }
            }

            Debug.Log("[TurnAnimator] Simultaneous movement animation complete");
        }

        /// <summary>
        /// Check if a unit at a position has any conflicts with enemy units
        /// Returns a ConflictDetectedEvent if conflicts are found, null otherwise
        /// Only detects same-hex collisions (not adjacent units - those are handled by combat resolution)
        /// </summary>
        private ConflictDetectedEvent CheckForConflicts(Unit unit, HexCoord position)
        {
            List<string> conflictingUnitIds = new List<string>();

            // Check for units at the same position ONLY
            // Adjacent combat is resolved automatically via ResolveCombat() in TurnResolver
            List<Unit> unitsAtPosition = unitManager.GetUnitsAtPosition(position);
            foreach (Unit otherUnit in unitsAtPosition)
            {
                // Skip self
                if (otherUnit.id == unit.id)
                    continue;

                // Check if enemy
                if (otherUnit.ownerId != unit.ownerId)
                {
                    conflictingUnitIds.Add(otherUnit.id);
                }
            }

            // If conflicts found, create event
            if (conflictingUnitIds.Count > 0)
            {
                // Add the moving unit itself
                conflictingUnitIds.Add(unit.id);

                Debug.Log($"[TurnAnimator] Conflict detected at {position}: {conflictingUnitIds.Count} units involved");

                // Use turn number 0 for conflicts detected during animation (actual turn number not available)
                return new ConflictDetectedEvent(0, conflictingUnitIds, position);
            }

            return null;
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

                // Check for conflicts at final position
                ConflictDetectedEvent conflict = CheckForConflicts(unit, moveEvent.to);
                if (conflict != null)
                {
                    yield return AnimateConflictDetected(conflict, state);
                }

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

                // Check for conflicts at this position
                ConflictDetectedEvent conflict = CheckForConflicts(unit, nextPos);
                if (conflict != null)
                {
                    yield return AnimateConflictDetected(conflict, state);
                }

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

            // Fire event to notify GameManager to show combat results UI
            OnCombatOccurred?.Invoke(combatEvent);

            // Trigger visual update
            OnAnimationStep?.Invoke(state);

            // Wait while paused (combat results UI will be shown)
            while (isPaused)
            {
                yield return new WaitForSeconds(0.1f);
            }

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
