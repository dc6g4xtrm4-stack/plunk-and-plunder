using System;
using System.Collections.Generic;
using System.Linq;
using PlunkAndPlunder.Combat;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Orders;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Structures;
using PlunkAndPlunder.Units;
using UnityEngine;

namespace PlunkAndPlunder.Resolution
{
    public class TurnResolver
    {
        private HexGrid grid;
        private UnitManager unitManager;
        private PlayerManager playerManager;
        private StructureManager structureManager;
        private CombatResolver combatResolver;
        private int turnNumber;
        private bool enableLogging;

        public TurnResolver(HexGrid grid, UnitManager unitManager, PlayerManager playerManager, StructureManager structureManager, bool enableLogging = false)
        {
            this.grid = grid;
            this.unitManager = unitManager;
            this.playerManager = playerManager;
            this.structureManager = structureManager;
            this.enableLogging = enableLogging;
            // Initialize combat resolver with a seed based on system time
            this.combatResolver = new CombatResolver(UnityEngine.Random.Range(0, int.MaxValue));
        }

        /// <summary>
        /// Resolve all orders deterministically
        /// </summary>
        public List<GameEvent> ResolveTurn(List<IOrder> orders, int currentTurn)
        {
            turnNumber = currentTurn;
            List<GameEvent> events = new List<GameEvent>();

            if (enableLogging)
            {
                Debug.Log($"[TurnResolver] Resolving turn {turnNumber} with {orders.Count} orders");
            }

            // Reset movement for all units at start of turn
            ResetAllUnitMovement();

            // Process build queues at start of turn
            events.AddRange(ProcessBuildQueues());

            // Sort orders by type priority and then by unit ID for determinism
            // Priority: DeployShipyard > BuildShip > RepairShip > UpgradeShip > UpgradeSails > UpgradeCannons > UpgradeMaxLife > Move
            List<IOrder> sortedOrders = orders
                .OrderBy(o => GetOrderPriority(o))
                .ThenBy(o => o.unitId)
                .ToList();

            // Process deploy shipyard orders first
            List<DeployShipyardOrder> deployOrders = sortedOrders.OfType<DeployShipyardOrder>().ToList();
            events.AddRange(ResolveDeployShipyardOrders(deployOrders));

            // Process build ship orders
            List<BuildShipOrder> buildOrders = sortedOrders.OfType<BuildShipOrder>().ToList();
            events.AddRange(ResolveBuildShipOrders(buildOrders));

            // Process repair ship orders
            List<RepairShipOrder> repairOrders = sortedOrders.OfType<RepairShipOrder>().ToList();
            events.AddRange(ResolveRepairShipOrders(repairOrders));

            // Process upgrade ship orders (legacy)
            List<UpgradeShipOrder> upgradeOrders = sortedOrders.OfType<UpgradeShipOrder>().ToList();
            events.AddRange(ResolveUpgradeShipOrders(upgradeOrders));

            // Process new upgrade orders
            List<UpgradeSailsOrder> upgradeSailsOrders = sortedOrders.OfType<UpgradeSailsOrder>().ToList();
            events.AddRange(ResolveUpgradeSailsOrders(upgradeSailsOrders));

            List<UpgradeCannonsOrder> upgradeCannonsOrders = sortedOrders.OfType<UpgradeCannonsOrder>().ToList();
            events.AddRange(ResolveUpgradeCannonsOrders(upgradeCannonsOrders));

            List<UpgradeMaxLifeOrder> upgradeMaxLifeOrders = sortedOrders.OfType<UpgradeMaxLifeOrder>().ToList();
            events.AddRange(ResolveUpgradeMaxLifeOrders(upgradeMaxLifeOrders));

            // Process move orders
            List<MoveOrder> moveOrders = sortedOrders.OfType<MoveOrder>().ToList();
            events.AddRange(ResolveMoveOrders(moveOrders));

            // If collisions were detected, return early - GameManager will handle collision resolution
            // and call ResolveCombatAfterMovement() after collisions are resolved
            bool hasCollisionEvents = events.Any(e => e.type == GameEventType.CollisionNeedsResolution);
            if (hasCollisionEvents)
            {
                return events;
            }

            // Check for combat (adjacent enemies) - only if no collisions to resolve
            events.AddRange(ResolveCombat());

            // Check for player elimination
            events.AddRange(CheckPlayerElimination());

            return events;
        }

        /// <summary>
        /// Resolve combat after movement (called after collision resolution)
        /// </summary>
        public List<GameEvent> ResolveCombatAfterMovement()
        {
            List<GameEvent> events = new List<GameEvent>();

            // Check for combat (adjacent enemies)
            events.AddRange(ResolveCombat());

            // Check for player elimination
            events.AddRange(CheckPlayerElimination());

            return events;
        }

        private int GetOrderPriority(IOrder order)
        {
            switch (order.GetOrderType())
            {
                case OrderType.DeployShipyard:
                    return 0;
                case OrderType.BuildShip:
                    return 1;
                case OrderType.RepairShip:
                    return 2;
                case OrderType.UpgradeShip:
                    return 3;
                case OrderType.UpgradeSails:
                    return 4;
                case OrderType.UpgradeCannons:
                    return 5;
                case OrderType.UpgradeMaxLife:
                    return 6;
                case OrderType.Move:
                    return 7;
                default:
                    return 999;
            }
        }

        private List<GameEvent> ResolveMoveOrders(List<MoveOrder> moveOrders)
        {
            List<GameEvent> events = new List<GameEvent>();

            // Build a map of intended destinations (this turn only) and store paths
            Dictionary<string, HexCoord> intendedMoves = new Dictionary<string, HexCoord>();
            Dictionary<string, List<HexCoord>> movePaths = new Dictionary<string, List<HexCoord>>();
            Dictionary<string, List<HexCoord>> remainingPaths = new Dictionary<string, List<HexCoord>>();

            foreach (MoveOrder order in moveOrders)
            {
                if (order.path != null && order.path.Count > 1)
                {
                    Unit unit = unitManager.GetUnit(order.unitId);
                    if (unit == null)
                        continue;

                    // Get unit's movement capacity
                    int movementCapacity = unit.GetMovementCapacity();

                    // Calculate how much of the path can be executed this turn
                    // Path includes starting position, so we need pathLength = path.Count - 1 moves
                    int pathLength = order.path.Count - 1;
                    int movesThisTurn = Mathf.Min(movementCapacity, pathLength);

                    // Extract this turn's path (includes starting position + movesThisTurn hexes)
                    List<HexCoord> thisTurnPath = order.path.GetRange(0, movesThisTurn + 1);
                    HexCoord thisTurnDestination = thisTurnPath[thisTurnPath.Count - 1];

                    intendedMoves[order.unitId] = thisTurnDestination;
                    movePaths[order.unitId] = thisTurnPath;

                    // If there's remaining path, store it
                    if (movesThisTurn < pathLength)
                    {
                        // Remaining path starts at the destination of this turn (to maintain continuity)
                        List<HexCoord> remaining = order.path.GetRange(movesThisTurn, pathLength - movesThisTurn + 1);
                        remainingPaths[order.unitId] = remaining;
                    }
                }
            }

            // Detect collisions (multiple units trying to move to same destination)
            Dictionary<HexCoord, List<string>> destinationMap = new Dictionary<HexCoord, List<string>>();
            foreach (var kvp in intendedMoves)
            {
                HexCoord dest = kvp.Value;
                if (!destinationMap.ContainsKey(dest))
                {
                    destinationMap[dest] = new List<string>();
                }
                destinationMap[dest].Add(kvp.Key);
            }

            // Find collisions and store them
            HashSet<string> blockedUnits = new HashSet<string>();
            List<CollisionInfo> detectedCollisions = new List<CollisionInfo>();
            HashSet<string> alreadyInCollision = new HashSet<string>();

            // Type 1: Same destination collisions
            foreach (var kvp in destinationMap)
            {
                HexCoord destination = kvp.Key;
                List<string> unitIds = kvp.Value;

                // Collision detected: multiple units trying to move to same hex
                if (unitIds.Count > 1)
                {
                    CollisionInfo collision = new CollisionInfo(unitIds, destination);

                    // Store paths for each unit involved in collision
                    foreach (string unitId in unitIds)
                    {
                        if (movePaths.ContainsKey(unitId))
                        {
                            collision.unitPaths[unitId] = movePaths[unitId];
                        }
                        if (remainingPaths.ContainsKey(unitId))
                        {
                            collision.unitRemainingPaths[unitId] = remainingPaths[unitId];
                        }
                        alreadyInCollision.Add(unitId);
                    }

                    detectedCollisions.Add(collision);
                    events.Add(new CollisionDetectedEvent(turnNumber, unitIds, destination));

                    if (enableLogging)
                    {
                        Debug.Log($"[TurnResolver] Same-destination collision at {destination}: {unitIds.Count} units");
                    }
                }
            }

            // Type 2: Swapping positions collision (ships moving into each other)
            List<MoveOrder> moveOrdersList = moveOrders.ToList();
            for (int i = 0; i < moveOrdersList.Count; i++)
            {
                for (int j = i + 1; j < moveOrdersList.Count; j++)
                {
                    MoveOrder order1 = moveOrdersList[i];
                    MoveOrder order2 = moveOrdersList[j];

                    // Skip if either unit is already in a collision
                    if (alreadyInCollision.Contains(order1.unitId) || alreadyInCollision.Contains(order2.unitId))
                        continue;

                    // Get units and their start/end positions
                    Unit unit1 = unitManager.GetUnit(order1.unitId);
                    Unit unit2 = unitManager.GetUnit(order2.unitId);

                    if (unit1 == null || unit2 == null)
                        continue;

                    // Skip if same owner (friendly ships can pass through each other, no collision)
                    // Friendly ships from the same player NEVER trigger combat or collision
                    if (unit1.ownerId == unit2.ownerId)
                        continue;

                    if (!intendedMoves.ContainsKey(order1.unitId) || !intendedMoves.ContainsKey(order2.unitId))
                        continue;

                    HexCoord unit1Start = unit1.position;
                    HexCoord unit1End = intendedMoves[order1.unitId];
                    HexCoord unit2Start = unit2.position;
                    HexCoord unit2End = intendedMoves[order2.unitId];

                    // Check if they're swapping positions (A->B and B->A)
                    if (unit1End.Equals(unit2Start) && unit2End.Equals(unit1Start))
                    {
                        // Ships are moving into each other's positions - create collision
                        List<string> swapUnits = new List<string> { order1.unitId, order2.unitId };

                        // Use the midpoint or first ship's destination for collision location
                        HexCoord collisionLocation = unit1End;

                        CollisionInfo collision = new CollisionInfo(swapUnits, collisionLocation);

                        // Store paths
                        if (movePaths.ContainsKey(order1.unitId))
                        {
                            collision.unitPaths[order1.unitId] = movePaths[order1.unitId];
                        }
                        if (remainingPaths.ContainsKey(order1.unitId))
                        {
                            collision.unitRemainingPaths[order1.unitId] = remainingPaths[order1.unitId];
                        }
                        if (movePaths.ContainsKey(order2.unitId))
                        {
                            collision.unitPaths[order2.unitId] = movePaths[order2.unitId];
                        }
                        if (remainingPaths.ContainsKey(order2.unitId))
                        {
                            collision.unitRemainingPaths[order2.unitId] = remainingPaths[order2.unitId];
                        }

                        detectedCollisions.Add(collision);
                        events.Add(new CollisionDetectedEvent(turnNumber, swapUnits, collisionLocation));

                        alreadyInCollision.Add(order1.unitId);
                        alreadyInCollision.Add(order2.unitId);

                        if (enableLogging)
                        {
                            Debug.Log($"[TurnResolver] Swap collision: {order1.unitId} ({unit1Start}->{unit1End}) and {order2.unitId} ({unit2Start}->{unit2End})");
                        }
                    }
                }
            }

            // If collisions detected, return early with collision events
            // GameManager will handle requesting yield decisions
            if (detectedCollisions.Count > 0)
            {
                // Store collisions in event for GameManager to handle
                foreach (var collision in detectedCollisions)
                {
                    events.Add(new CollisionNeedsResolutionEvent(turnNumber, collision));
                }
                return events;
            }

            // Execute non-blocked moves
            foreach (var kvp in intendedMoves)
            {
                string unitId = kvp.Key;
                HexCoord destination = kvp.Value;

                if (!blockedUnits.Contains(unitId))
                {
                    Unit unit = unitManager.GetUnit(unitId);
                    if (unit != null)
                    {
                        HexCoord from = unit.position;
                        List<HexCoord> thisTurnPath = movePaths.ContainsKey(unitId) ? movePaths[unitId] : null;
                        List<HexCoord> remaining = remainingPaths.ContainsKey(unitId) ? remainingPaths[unitId] : null;

                        // Calculate movement used
                        int movementUsed = thisTurnPath != null ? thisTurnPath.Count - 1 : 0;
                        int movementCapacity = unit.GetMovementCapacity();
                        int movementRemaining = movementCapacity - movementUsed;

                        // Update unit's movement remaining
                        unit.movementRemaining = movementRemaining;

                        // Store remaining path in the unit for next turn
                        unit.queuedPath = remaining;

                        // Create move event with partial movement info
                        bool isPartial = remaining != null && remaining.Count > 1;
                        events.Add(new UnitMovedEvent(
                            turnNumber, unitId, from, destination, thisTurnPath,
                            isPartial, remaining, movementUsed, movementRemaining
                        ));

                        if (enableLogging)
                        {
                            if (isPartial)
                            {
                                Debug.Log($"[TurnResolver] Unit {unitId} moved {movementUsed}/{movementCapacity} tiles (partial move, {remaining.Count - 1} tiles remain)");
                            }
                            else
                            {
                                Debug.Log($"[TurnResolver] Unit {unitId} moved from {from} to {destination} ({movementUsed} tiles)");
                            }
                        }
                    }
                }
            }

            return events;
        }

        /// <summary>
        /// Resolve collisions based on yield decisions
        /// </summary>
        public List<GameEvent> ResolveCollisionsWithYieldDecisions(List<CollisionInfo> collisions, Dictionary<string, bool> yieldDecisions)
        {
            List<GameEvent> events = new List<GameEvent>();

            foreach (CollisionInfo collision in collisions)
            {
                // Count how many units are yielding
                List<string> yieldingUnits = new List<string>();
                List<string> notYieldingUnits = new List<string>();

                foreach (string unitId in collision.unitIds)
                {
                    bool isYielding = yieldDecisions.ContainsKey(unitId) && yieldDecisions[unitId];
                    if (isYielding)
                    {
                        yieldingUnits.Add(unitId);
                    }
                    else
                    {
                        notYieldingUnits.Add(unitId);
                    }
                }

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Resolving collision at {collision.destination}: {yieldingUnits.Count} yielding, {notYieldingUnits.Count} not yielding");
                }

                // Case 1: All units yield (both choose PROCEED) - both move, no combat
                if (notYieldingUnits.Count == 0)
                {
                    // All units are proceeding peacefully - execute all moves
                    foreach (string unitId in collision.unitIds)
                    {
                        ExecuteUnitMove(unitId, collision, events);
                    }

                    events.Add(new CollisionResolvedEvent(turnNumber, collision.unitIds, collision.destination, "All units proceeded, no combat"));

                    if (enableLogging)
                    {
                        Debug.Log($"[TurnResolver] All units proceeded peacefully at {collision.destination}");
                    }
                }
                // Case 2: Some units yield - non-yielding units move (or fight if multiple)
                else if (yieldingUnits.Count > 0)
                {
                    // If only one unit not yielding, it moves
                    if (notYieldingUnits.Count == 1)
                    {
                        string movingUnitId = notYieldingUnits[0];
                        ExecuteUnitMove(movingUnitId, collision, events);

                        events.Add(new CollisionResolvedEvent(turnNumber, collision.unitIds, collision.destination,
                            $"{movingUnitId} moved, others yielded"));
                    }
                    // Multiple units not yielding - they fight
                    else
                    {
                        // Move all non-yielding units to the collision point
                        foreach (string unitId in notYieldingUnits)
                        {
                            ExecuteUnitMove(unitId, collision, events);
                        }

                        // Trigger combat between non-yielding units
                        events.AddRange(ResolveCombatAtLocation(notYieldingUnits, collision.destination));

                        events.Add(new CollisionResolvedEvent(turnNumber, collision.unitIds, collision.destination,
                            $"Combat triggered between {notYieldingUnits.Count} non-yielding units"));
                    }
                }
                // Case 3: No units yield - ONE ROUND of combat happens, ships stay in place
                else
                {
                    // DO NOT move units - ships stay in their current positions during combat
                    // Get units involved
                    List<Unit> combatUnits = new List<Unit>();
                    foreach (string unitId in collision.unitIds)
                    {
                        Unit unit = unitManager.GetUnit(unitId);
                        if (unit != null)
                        {
                            combatUnits.Add(unit);
                        }
                    }

                    // Trigger ONE ROUND of combat between enemy ships
                    // Ships remain at their starting positions
                    if (combatUnits.Count == 2 && combatUnits[0].ownerId != combatUnits[1].ownerId)
                    {
                        // Use collision destination as the "location" for the combat event
                        events.AddRange(ResolveOneRoundOfCombat(combatUnits[0], combatUnits[1], collision.destination));
                    }
                    else if (combatUnits.Count > 2)
                    {
                        // Multiple units - do pairwise one-round combat
                        for (int i = 0; i < combatUnits.Count; i++)
                        {
                            for (int j = i + 1; j < combatUnits.Count; j++)
                            {
                                Unit unit1 = combatUnits[i];
                                Unit unit2 = combatUnits[j];

                                // Only fight if different players and both still alive
                                if (unit1.ownerId != unit2.ownerId && !unit1.IsDead() && !unit2.IsDead())
                                {
                                    events.AddRange(ResolveOneRoundOfCombat(unit1, unit2, collision.destination));
                                }
                            }
                        }
                    }

                    events.Add(new CollisionResolvedEvent(turnNumber, collision.unitIds, collision.destination,
                        $"No units yielded, combat round triggered (ships remain in position)"));
                }
            }

            return events;
        }

        private void ExecuteUnitMove(string unitId, CollisionInfo collision, List<GameEvent> events)
        {
            Unit unit = unitManager.GetUnit(unitId);
            if (unit == null) return;

            HexCoord from = unit.position;
            HexCoord destination = collision.destination;
            List<HexCoord> thisTurnPath = collision.unitPaths.ContainsKey(unitId) ? collision.unitPaths[unitId] : null;
            List<HexCoord> remaining = collision.unitRemainingPaths.ContainsKey(unitId) ? collision.unitRemainingPaths[unitId] : null;

            // Calculate movement
            int movementUsed = thisTurnPath != null ? thisTurnPath.Count - 1 : 0;
            int movementCapacity = unit.GetMovementCapacity();
            int movementRemaining = movementCapacity - movementUsed;

            // Update unit position
            unit.position = destination;
            unit.movementRemaining = movementRemaining;
            unit.queuedPath = remaining;

            // Create move event
            bool isPartial = remaining != null && remaining.Count > 1;
            events.Add(new UnitMovedEvent(
                turnNumber, unitId, from, destination, thisTurnPath,
                isPartial, remaining, movementUsed, movementRemaining
            ));

            if (enableLogging)
            {
                Debug.Log($"[TurnResolver] Unit {unitId} moved from {from} to {destination}");
            }
        }

        /// <summary>
        /// Resolve ONE ROUND of combat between two units (for multi-turn combat)
        /// Ships stay in their positions and fight one round per turn
        /// </summary>
        private List<GameEvent> ResolveOneRoundOfCombat(Unit unit1, Unit unit2, HexCoord location)
        {
            List<GameEvent> events = new List<GameEvent>();

            // Fight ONE round only, with cannon bonuses
            CombatResult result = combatResolver.ResolveCombat(unit1.id, unit2.id, unit1.cannons, unit2.cannons);

            // Apply damage
            unit1.TakeDamage(result.damageToAttacker);
            unit2.TakeDamage(result.damageToDefender);

            bool attackerDestroyed = unit1.IsDead();
            bool defenderDestroyed = unit2.IsDead();

            // Mark as in combat if both alive
            if (!attackerDestroyed && !defenderDestroyed)
            {
                unit1.isInCombat = true;
                unit1.combatOpponentId = unit2.id;
                unit2.isInCombat = true;
                unit2.combatOpponentId = unit1.id;
            }
            else
            {
                // Combat ended, clear flags
                unit1.isInCombat = false;
                unit1.combatOpponentId = null;
                unit2.isInCombat = false;
                unit2.combatOpponentId = null;
            }

            // Create combat event for this round
            events.Add(new CombatOccurredEvent(
                turnNumber,
                unit1.id,
                unit2.id,
                result.damageToAttacker,
                result.damageToDefender,
                result.attackerRolls,
                result.defenderRolls,
                attackerDestroyed,
                defenderDestroyed
            ));

            // Log combat to file
            GameLogger.LogCombat(unit1.id, unit2.id, result.damageToAttacker, result.damageToDefender, result.attackerRolls, result.defenderRolls);

            if (enableLogging)
            {
                Debug.Log($"[TurnResolver] One round of combat: {unit1.id} ({unit1.health}HP) vs {unit2.id} ({unit2.health}HP)");
            }

            // Create destruction events (units will be removed by TurnAnimator)
            if (attackerDestroyed)
            {
                events.Add(new UnitDestroyedEvent(turnNumber, unit1.id, unit1.ownerId, location));
                // unitManager.RemoveUnit(unit1.id); // Deferred to TurnAnimator

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] {unit1.id} destroyed");
                }
            }

            if (defenderDestroyed)
            {
                events.Add(new UnitDestroyedEvent(turnNumber, unit2.id, unit2.ownerId, location));
                // unitManager.RemoveUnit(unit2.id); // Deferred to TurnAnimator

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] {unit2.id} destroyed");
                }
            }

            return events;
        }

        private List<GameEvent> ResolveCombatAtLocation(List<string> unitIds, HexCoord location)
        {
            List<GameEvent> events = new List<GameEvent>();

            // Get all units involved
            List<Unit> units = unitIds.Select(id => unitManager.GetUnit(id)).Where(u => u != null).ToList();
            if (units.Count < 2) return events;

            // Group by player
            Dictionary<int, List<Unit>> unitsByPlayer = new Dictionary<int, List<Unit>>();
            foreach (Unit unit in units)
            {
                if (!unitsByPlayer.ContainsKey(unit.ownerId))
                {
                    unitsByPlayer[unit.ownerId] = new List<Unit>();
                }
                unitsByPlayer[unit.ownerId].Add(unit);
            }

            // If all units belong to same player, no combat
            // RULE: Friendly ships from the same player can occupy the same square peacefully
            if (unitsByPlayer.Count == 1) return events;

            // Combat between different players - fight to the death!
            // RULE: Enemy ships CANNOT occupy the same square - they must fight until one is destroyed
            if (units.Count == 2 && units[0].ownerId != units[1].ownerId)
            {
                // Direct 1v1 combat - fight until one dies
                events.AddRange(ResolveCombatToTheDeath(units[0], units[1], location));
            }
            else
            {
                // Multiple units - do pairwise combat (could happen with 3+ ships)
                for (int i = 0; i < units.Count; i++)
                {
                    for (int j = i + 1; j < units.Count; j++)
                    {
                        Unit unit1 = units[i];
                        Unit unit2 = units[j];

                        // Only fight if different players and both still alive
                        if (unit1.ownerId != unit2.ownerId && !unit1.IsDead() && !unit2.IsDead())
                        {
                            events.AddRange(ResolveCombatToTheDeath(unit1, unit2, location));
                        }
                    }
                }
            }

            return events;
        }

        /// <summary>
        /// Resolve combat between two units until one dies
        /// Creates multiple combat events (rounds) until one ship is destroyed
        /// </summary>
        private List<GameEvent> ResolveCombatToTheDeath(Unit unit1, Unit unit2, HexCoord location)
        {
            List<GameEvent> events = new List<GameEvent>();

            int roundNumber = 1;
            const int MAX_ROUNDS = 50; // Safety limit to prevent infinite loops

            if (enableLogging)
            {
                Debug.Log($"[TurnResolver] Combat to the death: {unit1.id} ({unit1.health}HP) vs {unit2.id} ({unit2.health}HP)");
            }

            // Keep fighting until one dies
            while (!unit1.IsDead() && !unit2.IsDead() && roundNumber <= MAX_ROUNDS)
            {
                // Resolve one round of combat, with cannon bonuses
                CombatResult result = combatResolver.ResolveCombat(unit1.id, unit2.id, unit1.cannons, unit2.cannons);

                // Apply damage
                unit1.TakeDamage(result.damageToAttacker);
                unit2.TakeDamage(result.damageToDefender);

                bool attackerDestroyed = unit1.IsDead();
                bool defenderDestroyed = unit2.IsDead();

                // Create combat event for this round
                events.Add(new CombatOccurredEvent(
                    turnNumber,
                    unit1.id,
                    unit2.id,
                    result.damageToAttacker,
                    result.damageToDefender,
                    result.attackerRolls,
                    result.defenderRolls,
                    attackerDestroyed,
                    defenderDestroyed
                ));

                // Log combat to file
                GameLogger.LogCombat(unit1.id, unit2.id, result.damageToAttacker, result.damageToDefender, result.attackerRolls, result.defenderRolls);

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Round {roundNumber}: {unit1.id} ({unit1.health}HP) vs {unit2.id} ({unit2.health}HP) - Damage: {result.damageToAttacker} to attacker, {result.damageToDefender} to defender");
                }

                roundNumber++;
            }

            // Remove destroyed units
            if (unit1.IsDead())
            {
                events.Add(new UnitDestroyedEvent(turnNumber, unit1.id, unit1.ownerId, location));
                unitManager.RemoveUnit(unit1.id);

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] {unit1.id} destroyed after {roundNumber - 1} rounds");
                }
            }

            if (unit2.IsDead())
            {
                events.Add(new UnitDestroyedEvent(turnNumber, unit2.id, unit2.ownerId, location));
                unitManager.RemoveUnit(unit2.id);

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] {unit2.id} destroyed after {roundNumber - 1} rounds");
                }
            }

            return events;
        }


        private List<GameEvent> ResolveCombat()
        {
            List<GameEvent> events = new List<GameEvent>();

            // Find all units with adjacent enemies
            List<Unit> allUnits = unitManager.GetAllUnits();
            HashSet<string> unitsProcessed = new HashSet<string>();
            HashSet<string> unitsToDestroy = new HashSet<string>();

            if (enableLogging)
            {
                Debug.Log($"[TurnResolver] Checking combat for {allUnits.Count} units");
            }

            // Check each pair of units for combat
            for (int i = 0; i < allUnits.Count; i++)
            {
                for (int j = i + 1; j < allUnits.Count; j++)
                {
                    Unit unitA = allUnits[i];
                    Unit unitB = allUnits[j];

                    // Skip if already processed in combat this turn
                    if (unitsProcessed.Contains(unitA.id) || unitsProcessed.Contains(unitB.id))
                        continue;

                    // Skip if same owner (friendly ships never fight each other)
                    // RULE: Ships from the same player do NOT trigger combat/collision
                    if (unitA.ownerId == unitB.ownerId)
                        continue;

                    // Check if adjacent
                    int distance = unitA.position.Distance(unitB.position);

                    if (enableLogging && distance <= 2)
                    {
                        Debug.Log($"[TurnResolver] Units {unitA.id} (Player {unitA.ownerId}) at {unitA.position} and {unitB.id} (Player {unitB.ownerId}) at {unitB.position} are distance {distance} apart");
                    }

                    if (distance == 1)
                    {
                        // Resolve ONE ROUND of combat (multi-turn combat system)
                        if (enableLogging)
                        {
                            Debug.Log($"[TurnResolver] Adjacent combat triggered: {unitA.id} vs {unitB.id}");
                        }

                        // Use location as midpoint for the combat event
                        HexCoord combatLocation = unitA.position;
                        events.AddRange(ResolveOneRoundOfCombat(unitA, unitB, combatLocation));

                        // Mark units as processed
                        unitsProcessed.Add(unitA.id);
                        unitsProcessed.Add(unitB.id);

                        // Mark for destruction if dead (already handled in ResolveOneRoundOfCombat)
                        if (unitA.IsDead())
                        {
                            unitsToDestroy.Add(unitA.id);
                        }
                        if (unitB.IsDead())
                        {
                            unitsToDestroy.Add(unitB.id);
                        }
                    }
                }
            }

            if (enableLogging)
            {
                Debug.Log($"[TurnResolver] Combat resolution complete: {unitsProcessed.Count / 2} combats occurred, {unitsToDestroy.Count} units destroyed");
            }

            // Destruction events are already created by ResolveOneRoundOfCombat
            // No need to create them again here

            return events;
        }

        private List<GameEvent> CheckPlayerElimination()
        {
            List<GameEvent> events = new List<GameEvent>();

            foreach (Player player in playerManager.GetActivePlayers())
            {
                // Player eliminated if they have no units
                List<Unit> playerUnits = unitManager.GetUnitsForPlayer(player.id);
                if (playerUnits.Count == 0)
                {
                    playerManager.EliminatePlayer(player.id);
                    events.Add(new PlayerEliminatedEvent(turnNumber, player.id, player.name));

                    if (enableLogging)
                    {
                        Debug.Log($"[TurnResolver] Player {player.name} eliminated");
                    }
                }
            }

            // Check for winner
            Player winner = playerManager.GetWinner();
            if (winner != null)
            {
                events.Add(new GameEvent(turnNumber, GameEventType.GameWon,
                    $"Player {winner.name} wins!"));

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Player {winner.name} wins the game!");
                }
            }

            return events;
        }

        private List<GameEvent> ResolveDeployShipyardOrders(List<DeployShipyardOrder> orders)
        {
            List<GameEvent> events = new List<GameEvent>();

            foreach (DeployShipyardOrder order in orders)
            {
                Unit ship = unitManager.GetUnit(order.unitId);
                if (ship == null)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} not found for shipyard deployment");
                    }
                    continue;
                }

                // Check if tile is still a harbor
                Tile tile = grid.GetTile(order.position);
                if (tile == null || tile.type != TileType.HARBOR)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Position {order.position} is not a harbor");
                    }
                    continue;
                }

                // Check for existing shipyard
                Structure existingStructure = structureManager.GetStructureAtPosition(order.position);
                if (existingStructure != null && existingStructure.type == StructureType.SHIPYARD)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Shipyard already exists at {order.position}");
                    }
                    continue;
                }

                // Check player currency
                Player player = playerManager.GetPlayer(order.playerId);
                if (player == null || player.gold < BuildingConfig.DEPLOY_SHIPYARD_COST)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not have enough gold to deploy shipyard");
                    }
                    continue;
                }

                // Deduct currency
                player.gold -= BuildingConfig.DEPLOY_SHIPYARD_COST;

                // Create shipyard - if there's a harbor structure, convert it, otherwise create new
                Structure shipyard;
                if (existingStructure != null && existingStructure.type == StructureType.HARBOR)
                {
                    // Convert harbor to shipyard
                    existingStructure.type = StructureType.SHIPYARD;
                    existingStructure.ownerId = order.playerId;
                    shipyard = existingStructure;
                }
                else
                {
                    // Create new shipyard
                    shipyard = structureManager.CreateStructure(order.playerId, order.position, StructureType.SHIPYARD);
                }

                // NOTE: We don't remove the ship here - TurnAnimator will do it
                // unitManager.RemoveUnit(order.unitId); // Deferred to TurnAnimator

                events.Add(new ShipyardDeployedEvent(turnNumber, order.unitId, shipyard.id, order.playerId, order.position));

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Ship {order.unitId} deployed as shipyard {shipyard.id} at {order.position} for {BuildingConfig.DEPLOY_SHIPYARD_COST} gold");
                }
            }

            return events;
        }

        /// <summary>
        /// Process all shipyard build queues, advancing progress and spawning completed ships
        /// </summary>
        private List<GameEvent> ProcessBuildQueues()
        {
            List<GameEvent> events = new List<GameEvent>();

            // Get all shipyards
            List<Structure> allStructures = structureManager.GetAllStructures();
            List<Structure> shipyards = allStructures.FindAll(s => s.type == StructureType.SHIPYARD);

            foreach (Structure shipyard in shipyards)
            {
                if (shipyard.buildQueue.Count == 0)
                    continue;

                // Process first item in queue (the one being built)
                BuildQueueItem currentItem = shipyard.buildQueue[0];
                currentItem.turnsRemaining--;

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Shipyard {shipyard.id} building {currentItem.itemType}: {currentItem.turnsRemaining} turns remaining");
                }

                // Check if item is complete
                if (currentItem.turnsRemaining <= 0)
                {
                    // Spawn the ship
                    Unit newShip = unitManager.CreateUnit(shipyard.ownerId, shipyard.position, UnitType.SHIP);

                    // Create event for completed build
                    events.Add(new ShipBuiltEvent(turnNumber, newShip.id, shipyard.id, shipyard.ownerId, shipyard.position, currentItem.cost));

                    // Remove completed item from queue
                    shipyard.buildQueue.RemoveAt(0);

                    if (enableLogging)
                    {
                        Debug.Log($"[TurnResolver] Shipyard {shipyard.id} completed {currentItem.itemType}, spawned ship {newShip.id}");
                    }
                }
            }

            return events;
        }

        private List<GameEvent> ResolveBuildShipOrders(List<BuildShipOrder> orders)
        {
            List<GameEvent> events = new List<GameEvent>();

            foreach (BuildShipOrder order in orders)
            {
                Structure shipyard = structureManager.GetStructure(order.shipyardId);
                if (shipyard == null || shipyard.type != StructureType.SHIPYARD)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Shipyard {order.shipyardId} not found");
                    }
                    continue;
                }

                // Check ownership
                if (shipyard.ownerId != order.playerId)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not own shipyard {order.shipyardId}");
                    }
                    continue;
                }

                // Check queue space
                if (shipyard.buildQueue.Count >= BuildingConfig.MAX_QUEUE_SIZE)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Shipyard {order.shipyardId} build queue is full");
                    }
                    continue;
                }

                // Check player currency
                Player player = playerManager.GetPlayer(order.playerId);
                if (player == null || player.gold < BuildingConfig.BUILD_SHIP_COST)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not have enough gold to build ship");
                    }
                    continue;
                }

                // Deduct currency immediately
                player.gold -= BuildingConfig.BUILD_SHIP_COST;

                // Add to build queue instead of instant spawn
                BuildQueueItem queueItem = new BuildQueueItem("Ship", BuildingConfig.SHIP_BUILD_TIME, BuildingConfig.BUILD_SHIP_COST);
                shipyard.buildQueue.Add(queueItem);

                // Create event for queued build
                events.Add(new GameEvent(turnNumber, GameEventType.ShipQueued,
                    $"Player {order.playerId} queued ship at shipyard {shipyard.id} ({shipyard.buildQueue.Count}/{BuildingConfig.MAX_QUEUE_SIZE} slots)"));

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Player {order.playerId} queued ship at shipyard {shipyard.id} for {BuildingConfig.BUILD_SHIP_COST} gold (queue: {shipyard.buildQueue.Count}/{BuildingConfig.MAX_QUEUE_SIZE})");
                }
            }

            return events;
        }

        private List<GameEvent> ResolveRepairShipOrders(List<RepairShipOrder> orders)
        {
            List<GameEvent> events = new List<GameEvent>();

            foreach (RepairShipOrder order in orders)
            {
                Unit ship = unitManager.GetUnit(order.unitId);
                if (ship == null)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} not found for repair");
                    }
                    continue;
                }

                // Check ownership
                if (ship.ownerId != order.playerId)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not own ship {order.unitId}");
                    }
                    continue;
                }

                Structure shipyard = structureManager.GetStructure(order.shipyardId);
                if (shipyard == null || shipyard.type != StructureType.SHIPYARD)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Shipyard {order.shipyardId} not found");
                    }
                    continue;
                }

                // Check shipyard ownership
                if (shipyard.ownerId != order.playerId)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not own shipyard {order.shipyardId}");
                    }
                    continue;
                }

                // Check ship is at shipyard
                if (!ship.position.Equals(shipyard.position))
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} is not at shipyard {order.shipyardId}");
                    }
                    continue;
                }

                // Check if ship needs repair
                if (ship.health >= ship.maxHealth)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} is already at full health");
                    }
                    continue;
                }

                // Check player currency
                Player player = playerManager.GetPlayer(order.playerId);
                if (player == null || player.gold < BuildingConfig.REPAIR_SHIP_COST)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not have enough gold to repair ship");
                    }
                    continue;
                }

                // Deduct currency
                player.gold -= BuildingConfig.REPAIR_SHIP_COST;

                // Repair ship to full health
                int oldHealth = ship.health;
                ship.health = ship.maxHealth;

                events.Add(new ShipRepairedEvent(turnNumber, order.unitId, shipyard.id, order.playerId, oldHealth, ship.health, BuildingConfig.REPAIR_SHIP_COST));

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Player {order.playerId} repaired ship {order.unitId} at shipyard {shipyard.id} for {BuildingConfig.REPAIR_SHIP_COST} gold");
                }
            }

            return events;
        }

        private List<GameEvent> ResolveUpgradeShipOrders(List<UpgradeShipOrder> orders)
        {
            List<GameEvent> events = new List<GameEvent>();

            foreach (UpgradeShipOrder order in orders)
            {
                Unit ship = unitManager.GetUnit(order.unitId);
                if (ship == null)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} not found for upgrade");
                    }
                    continue;
                }

                // Check ownership
                if (ship.ownerId != order.playerId)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not own ship {order.unitId}");
                    }
                    continue;
                }

                Structure shipyard = structureManager.GetStructure(order.shipyardId);
                if (shipyard == null || shipyard.type != StructureType.SHIPYARD)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Shipyard {order.shipyardId} not found");
                    }
                    continue;
                }

                // Check shipyard ownership
                if (shipyard.ownerId != order.playerId)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not own shipyard {order.shipyardId}");
                    }
                    continue;
                }

                // Check ship is at shipyard
                if (!ship.position.Equals(shipyard.position))
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} is not at shipyard {order.shipyardId}");
                    }
                    continue;
                }

                // Check if ship is already at max tier
                if (ship.maxHealth >= BuildingConfig.MAX_SHIP_TIER)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} is already at max upgrade tier");
                    }
                    continue;
                }

                // Check player currency
                Player player = playerManager.GetPlayer(order.playerId);
                if (player == null || player.gold < BuildingConfig.UPGRADE_SHIP_COST)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not have enough gold to upgrade ship");
                    }
                    continue;
                }

                // Deduct currency
                player.gold -= BuildingConfig.UPGRADE_SHIP_COST;

                // Upgrade ship to next tier (add 10 HP per upgrade)
                int oldMaxHealth = ship.maxHealth;
                ship.maxHealth = Mathf.Min(ship.maxHealth + 10, BuildingConfig.MAX_SHIP_TIER);
                ship.health = ship.maxHealth; // Fully heal on upgrade

                events.Add(new ShipUpgradedEvent(turnNumber, order.unitId, shipyard.id, order.playerId, oldMaxHealth, ship.maxHealth, BuildingConfig.UPGRADE_SHIP_COST));

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Player {order.playerId} upgraded ship {order.unitId} at shipyard {shipyard.id} for {BuildingConfig.UPGRADE_SHIP_COST} gold");
                }
            }

            return events;
        }

        private List<GameEvent> ResolveUpgradeSailsOrders(List<UpgradeSailsOrder> orders)
        {
            List<GameEvent> events = new List<GameEvent>();

            foreach (UpgradeSailsOrder order in orders)
            {
                Unit ship = unitManager.GetUnit(order.unitId);
                if (ship == null)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} not found for sails upgrade");
                    }
                    continue;
                }

                // Check ownership
                if (ship.ownerId != order.playerId)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not own ship {order.unitId}");
                    }
                    continue;
                }

                Structure shipyard = structureManager.GetStructure(order.shipyardId);
                if (shipyard == null || shipyard.type != StructureType.SHIPYARD)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Shipyard {order.shipyardId} not found");
                    }
                    continue;
                }

                // Check shipyard ownership
                if (shipyard.ownerId != order.playerId)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not own shipyard {order.shipyardId}");
                    }
                    continue;
                }

                // Check ship is at shipyard
                if (!ship.position.Equals(shipyard.position))
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} is not at shipyard {order.shipyardId}");
                    }
                    continue;
                }

                // Check if ship is already at max sails upgrades
                if (ship.sails >= BuildingConfig.MAX_SAILS_UPGRADES)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} already has maximum sails upgrades");
                    }
                    continue;
                }

                // Check player currency
                Player player = playerManager.GetPlayer(order.playerId);
                if (player == null || player.gold < BuildingConfig.UPGRADE_SAILS_COST)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not have enough gold to upgrade sails");
                    }
                    continue;
                }

                // Deduct currency
                player.gold -= BuildingConfig.UPGRADE_SAILS_COST;

                // Upgrade sails
                int oldSails = ship.sails;
                ship.sails++;

                events.Add(new GameEvent(turnNumber, GameEventType.ShipUpgraded,
                    $"Player {order.playerId} upgraded sails on ship {order.unitId} (Level {ship.sails})"));

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Player {order.playerId} upgraded sails on ship {order.unitId} at shipyard {shipyard.id} for {BuildingConfig.UPGRADE_SAILS_COST} gold");
                }
            }

            return events;
        }

        private List<GameEvent> ResolveUpgradeCannonsOrders(List<UpgradeCannonsOrder> orders)
        {
            List<GameEvent> events = new List<GameEvent>();

            foreach (UpgradeCannonsOrder order in orders)
            {
                Unit ship = unitManager.GetUnit(order.unitId);
                if (ship == null)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} not found for cannons upgrade");
                    }
                    continue;
                }

                // Check ownership
                if (ship.ownerId != order.playerId)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not own ship {order.unitId}");
                    }
                    continue;
                }

                Structure shipyard = structureManager.GetStructure(order.shipyardId);
                if (shipyard == null || shipyard.type != StructureType.SHIPYARD)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Shipyard {order.shipyardId} not found");
                    }
                    continue;
                }

                // Check shipyard ownership
                if (shipyard.ownerId != order.playerId)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not own shipyard {order.shipyardId}");
                    }
                    continue;
                }

                // Check ship is at shipyard
                if (!ship.position.Equals(shipyard.position))
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} is not at shipyard {order.shipyardId}");
                    }
                    continue;
                }

                // Check if ship is already at max cannons upgrades
                if (ship.cannons >= BuildingConfig.MAX_CANNONS_UPGRADES)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} already has maximum cannons upgrades");
                    }
                    continue;
                }

                // Check player currency
                Player player = playerManager.GetPlayer(order.playerId);
                if (player == null || player.gold < BuildingConfig.UPGRADE_CANNONS_COST)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not have enough gold to upgrade cannons");
                    }
                    continue;
                }

                // Deduct currency
                player.gold -= BuildingConfig.UPGRADE_CANNONS_COST;

                // Upgrade cannons
                int oldCannons = ship.cannons;
                ship.cannons++;

                events.Add(new GameEvent(turnNumber, GameEventType.ShipUpgraded,
                    $"Player {order.playerId} upgraded cannons on ship {order.unitId} (Level {ship.cannons})"));

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Player {order.playerId} upgraded cannons on ship {order.unitId} at shipyard {shipyard.id} for {BuildingConfig.UPGRADE_CANNONS_COST} gold");
                }
            }

            return events;
        }

        private List<GameEvent> ResolveUpgradeMaxLifeOrders(List<UpgradeMaxLifeOrder> orders)
        {
            List<GameEvent> events = new List<GameEvent>();

            foreach (UpgradeMaxLifeOrder order in orders)
            {
                Unit ship = unitManager.GetUnit(order.unitId);
                if (ship == null)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} not found for max life upgrade");
                    }
                    continue;
                }

                // Check ownership
                if (ship.ownerId != order.playerId)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not own ship {order.unitId}");
                    }
                    continue;
                }

                Structure shipyard = structureManager.GetStructure(order.shipyardId);
                if (shipyard == null || shipyard.type != StructureType.SHIPYARD)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Shipyard {order.shipyardId} not found");
                    }
                    continue;
                }

                // Check shipyard ownership
                if (shipyard.ownerId != order.playerId)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not own shipyard {order.shipyardId}");
                    }
                    continue;
                }

                // Check ship is at shipyard
                if (!ship.position.Equals(shipyard.position))
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} is not at shipyard {order.shipyardId}");
                    }
                    continue;
                }

                // Check if ship is already at max tier
                if (ship.maxHealth >= BuildingConfig.MAX_SHIP_TIER)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} is already at max health tier");
                    }
                    continue;
                }

                // Check player currency
                Player player = playerManager.GetPlayer(order.playerId);
                if (player == null || player.gold < BuildingConfig.UPGRADE_MAX_LIFE_COST)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} does not have enough gold to upgrade max life");
                    }
                    continue;
                }

                // Deduct currency
                player.gold -= BuildingConfig.UPGRADE_MAX_LIFE_COST;

                // Upgrade max life (add 10 HP per upgrade)
                int oldMaxHealth = ship.maxHealth;
                ship.maxHealth = Mathf.Min(ship.maxHealth + 10, BuildingConfig.MAX_SHIP_TIER);
                ship.health = ship.maxHealth; // Fully heal on upgrade

                events.Add(new ShipUpgradedEvent(turnNumber, order.unitId, shipyard.id, order.playerId, oldMaxHealth, ship.maxHealth, BuildingConfig.UPGRADE_MAX_LIFE_COST));

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Player {order.playerId} upgraded max life on ship {order.unitId} at shipyard {shipyard.id} for {BuildingConfig.UPGRADE_MAX_LIFE_COST} gold");
                }
            }

            return events;
        }

        /// <summary>
        /// Reset movement for all units at the start of a turn
        /// </summary>
        private void ResetAllUnitMovement()
        {
            List<Unit> allUnits = unitManager.GetAllUnits();
            foreach (Unit unit in allUnits)
            {
                unit.ResetMovement();
            }

            if (enableLogging)
            {
                Debug.Log($"[TurnResolver] Reset movement for {allUnits.Count} units");
            }
        }
    }
}
