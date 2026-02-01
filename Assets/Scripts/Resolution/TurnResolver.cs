using System;
using System.Collections.Generic;
using System.Linq;
using PlunkAndPlunder.Combat;
using PlunkAndPlunder.Construction;
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
        private bool deferUnitRemoval; // If true, TurnAnimator removes units; if false, remove immediately

        public TurnResolver(HexGrid grid, UnitManager unitManager, PlayerManager playerManager, StructureManager structureManager, bool enableLogging = false, bool deferUnitRemoval = true)
        {
            this.grid = grid;
            this.unitManager = unitManager;
            this.playerManager = playerManager;
            this.structureManager = structureManager;
            this.enableLogging = enableLogging;
            this.deferUnitRemoval = deferUnitRemoval;
            // Initialize combat resolver with unit manager (deterministic combat, no seed needed)
            this.combatResolver = new CombatResolver(unitManager);
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

            // Process income at start of turn (NEW ECONOMY SYSTEM)
            events.AddRange(ProcessIncome());

            // Process build queues at start of turn (NEW CONSTRUCTION SYSTEM)
            if (ConstructionManager.Instance != null)
            {
                events.AddRange(ConstructionManager.Instance.ProcessTurn(turnNumber));
                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Processed construction with new ConstructionManager");
                }
            }
            else
            {
                // CRITICAL ERROR: ConstructionManager should always be available
                Debug.LogError($"[TurnResolver] ConstructionManager not found! Construction will not process this turn.");
            }

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

            // Process attack shipyard orders
            List<AttackShipyardOrder> attackShipyardOrders = sortedOrders.OfType<AttackShipyardOrder>().ToList();
            events.AddRange(ResolveAttackShipyardOrders(attackShipyardOrders));

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
                case OrderType.AttackShipyard:
                    return 7; // Process attacks before moves
                case OrderType.Move:
                    return 8;
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

            // Find encounters and store them (NEW ENCOUNTER SYSTEM)
            HashSet<string> blockedUnits = new HashSet<string>();
            List<Combat.Encounter> detectedEncounters = new List<Combat.Encounter>();
            HashSet<string> alreadyInEncounter = new HashSet<string>();

            // Keep old collision tracking for backward compatibility (temporary)
            List<CollisionInfo> detectedCollisions = new List<CollisionInfo>();
            HashSet<string> alreadyInCollision = new HashSet<string>();

            // Type 1: ENTRY ENCOUNTER (multiple units moving to same destination)
            // RULE: Two enemy ships can NEVER occupy the same tile
            // When enemy ships attempt to ENTER the same tile, prompt each: YIELD or ATTACK
            //   - All YIELD → no movement
            //   - One ATTACK → attacker claims tile
            //   - Multiple ATTACK → contested tile (pairwise combat, all stay in place)
            //   - Friendly units from same player CAN stack peacefully (no encounter)
            foreach (var kvp in destinationMap)
            {
                HexCoord destination = kvp.Key;
                List<string> unitIds = kvp.Value;

                // Check if multiple units are moving to same hex
                if (unitIds.Count > 1)
                {
                    // Group units by owner
                    Dictionary<int, List<string>> unitsByOwner = new Dictionary<int, List<string>>();
                    foreach (string unitId in unitIds)
                    {
                        Unit unit = unitManager.GetUnit(unitId);
                        if (unit != null)
                        {
                            if (!unitsByOwner.ContainsKey(unit.ownerId))
                            {
                                unitsByOwner[unit.ownerId] = new List<string>();
                            }
                            unitsByOwner[unit.ownerId].Add(unitId);
                        }
                    }

                    // If all units belong to same player, NO encounter - friendly stacking is allowed
                    if (unitsByOwner.Count == 1)
                    {
                        if (enableLogging)
                        {
                            Debug.Log($"[TurnResolver] {unitIds.Count} friendly units moving to {destination} - allowing peaceful stacking");
                        }
                        continue; // Skip this, no encounter for friendly units
                    }

                    // ENEMY units detected trying to ENTER same tile - create ENTRY encounter
                    // Gather previous positions for all involved units
                    Dictionary<string, HexCoord> previousPositions = new Dictionary<string, HexCoord>();
                    foreach (string unitId in unitIds)
                    {
                        Unit unit = unitManager.GetUnit(unitId);
                        if (unit != null)
                        {
                            previousPositions[unitId] = unit.position;
                        }
                    }

                    Combat.Encounter encounter = Combat.Encounter.CreateEntryEncounter(
                        unitIds: unitIds,
                        targetTile: destination,
                        previousPositions: previousPositions,
                        turnNumber: turnNumber
                    );

                    detectedEncounters.Add(encounter);
                    events.Add(new EncounterDetectedEvent(turnNumber, encounter));

                    foreach (string unitId in unitIds)
                    {
                        alreadyInEncounter.Add(unitId);
                    }

                    if (enableLogging)
                    {
                        // Get ship names for narrative logging
                        List<string> shipNames = new List<string>();
                        foreach (string unitId in unitIds)
                        {
                            Unit u = unitManager.GetUnit(unitId);
                            if (u != null)
                            {
                                shipNames.Add(u.GetDisplayName(playerManager));
                            }
                        }

                        string tileId = $"#{Math.Abs(destination.GetHashCode()) % 10000}";
                        if (shipNames.Count == 2)
                        {
                            Debug.Log($"[TurnResolver] ENTRY ENCOUNTER: {shipNames[0]} and {shipNames[1]} contest tile {tileId}");
                        }
                        else
                        {
                            Debug.Log($"[TurnResolver] ENTRY ENCOUNTER at tile {tileId}: {string.Join(", ", shipNames)}");
                        }

                        // Log to file
                        GameLogger.LogCollision(shipNames, destination, "ENTRY");
                    }
                }
            }

            // Type 2: PASSING ENCOUNTER (ships swapping positions - moving into each other)
            // RULE: When two enemy ships are about to PASS each other (swap tiles), prompt each: PROCEED or ATTACK
            //   - Both PROCEED → peaceful swap
            //   - Any ATTACK → combat on edge, both stay in original positions
            //   - Friendly ships from same player CAN pass through each other freely (no encounter)
            List<MoveOrder> moveOrdersList = moveOrders.ToList();
            for (int i = 0; i < moveOrdersList.Count; i++)
            {
                for (int j = i + 1; j < moveOrdersList.Count; j++)
                {
                    MoveOrder order1 = moveOrdersList[i];
                    MoveOrder order2 = moveOrdersList[j];

                    // Skip if either unit is already in an encounter
                    if (alreadyInEncounter.Contains(order1.unitId) || alreadyInEncounter.Contains(order2.unitId))
                        continue;

                    // Get units and their start/end positions
                    Unit unit1 = unitManager.GetUnit(order1.unitId);
                    Unit unit2 = unitManager.GetUnit(order2.unitId);

                    if (unit1 == null || unit2 == null)
                        continue;

                    // Skip if same owner (friendly ships can pass through each other peacefully)
                    // RULE: Friendly ships from the same player NEVER trigger combat or encounters
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
                        // Enemy ships are PASSING each other - create PASSING encounter
                        Combat.Encounter encounter = Combat.Encounter.CreatePassingEncounter(
                            unitIdA: order1.unitId,
                            unitIdB: order2.unitId,
                            positionA: unit1Start,
                            positionB: unit2Start,
                            turnNumber: turnNumber
                        );

                        detectedEncounters.Add(encounter);
                        events.Add(new EncounterDetectedEvent(turnNumber, encounter));

                        alreadyInEncounter.Add(order1.unitId);
                        alreadyInEncounter.Add(order2.unitId);

                        if (enableLogging)
                        {
                            // Get ship names for narrative logging
                            string ship1Name = unit1.GetDisplayName(playerManager);
                            string ship2Name = unit2.GetDisplayName(playerManager);
                            string tile1Id = $"#{Math.Abs(unit1Start.GetHashCode()) % 10000}";
                            string tile2Id = $"#{Math.Abs(unit1End.GetHashCode()) % 10000}";

                            Debug.Log($"[TurnResolver] PASSING ENCOUNTER: {ship1Name} and {ship2Name} crossing between tiles {tile1Id} and {tile2Id}");

                            // Log to file
                            List<string> shipNames = new List<string> { ship1Name, ship2Name };
                            GameLogger.LogCollision(shipNames, unit1End, "PASSING");
                        }
                    }
                }
            }

            // If encounters detected, return early with encounter events
            // GameManager will handle requesting player decisions
            if (detectedEncounters.Count > 0)
            {
                // Wrap all encounters in a single EncounterNeedsResolutionEvent
                events.Add(new EncounterNeedsResolutionEvent(turnNumber, detectedEncounters));
                return events;
            }

            // OLD: Backward compatibility for collision system (will be removed)
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

                        // Update unit's movement remaining (position will be updated by animation)
                        unit.movementRemaining = movementRemaining;

                        // Store remaining path in the unit for next turn
                        unit.queuedPath = remaining;

                        // Create move event with partial movement info
                        bool isPartial = remaining != null && remaining.Count > 1;

                        // Get ship name for narrative logging
                        string shipName = unit.GetDisplayName(playerManager);
                        string fromTileId = $"#{Math.Abs(from.GetHashCode()) % 10000}";
                        string toTileId = $"#{Math.Abs(destination.GetHashCode()) % 10000}";

                        // Debug: Log path details
                        string pathDebug = thisTurnPath != null ? $"[{string.Join(", ", thisTurnPath)}]" : "null";
                        Debug.Log($"[TurnResolver] Creating UnitMovedEvent for {shipName}: from tile {fromTileId} to tile {toTileId}, pathCount={thisTurnPath?.Count ?? 0}");

                        events.Add(new UnitMovedEvent(
                            turnNumber, unitId, from, destination, thisTurnPath,
                            isPartial, remaining, movementUsed, movementRemaining
                        ));

                        if (enableLogging)
                        {
                            if (isPartial)
                            {
                                Debug.Log($"[TurnResolver] {shipName} moved {movementUsed}/{movementCapacity} tiles (partial move, {remaining.Count - 1} tiles remain)");
                            }
                            else
                            {
                                Debug.Log($"[TurnResolver] {shipName} moved from tile {fromTileId} to tile {toTileId} ({movementUsed} tiles)");
                            }
                        }
                    }
                }
            }

            return events;
        }

        /// <summary>
        /// Resolve collisions based on yield decisions
        ///
        /// Collision types:
        /// 1. ENTERING SAME TILE: Multiple ships trying to enter same destination
        ///    - Prompt: YIELD or ATTACK
        ///    - If one yields, other gets the tile
        ///    - If neither yields, CONTESTED TILE: fight one round, stay in original positions
        ///
        /// 2. PASSING (swapping positions): Two ships trying to swap tiles
        ///    - Prompt: PROCEED or ATTACK (proceed = yield, attack = don't yield)
        ///    - If both proceed, they swap peacefully
        ///    - If either attacks, fight one round, stay in original positions
        ///
        /// RULE: Two enemy ships can NEVER occupy the same tile. They must fight until one is destroyed or yields.
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
                    string tileId = $"#{Math.Abs(collision.destination.GetHashCode()) % 10000}";

                    // Get ship names for yielding and attacking units
                    List<string> yieldingNames = new List<string>();
                    foreach (string unitId in yieldingUnits)
                    {
                        Unit u = unitManager.GetUnit(unitId);
                        if (u != null) yieldingNames.Add(u.GetDisplayName(playerManager));
                    }

                    List<string> attackingNames = new List<string>();
                    foreach (string unitId in notYieldingUnits)
                    {
                        Unit u = unitManager.GetUnit(unitId);
                        if (u != null) attackingNames.Add(u.GetDisplayName(playerManager));
                    }

                    Debug.Log($"[TurnResolver] ===== RESOLVING COLLISION at tile {tileId} =====");
                    Debug.Log($"[TurnResolver]   Yielding: {string.Join(", ", yieldingNames)}");
                    Debug.Log($"[TurnResolver]   Attacking: {string.Join(", ", attackingNames)}");
                }

                // Case 1: All units yield (all choose PROCEED in PASSING, or all choose YIELD in ENTERING)
                // Result: Peaceful resolution - all moves execute, no combat
                if (notYieldingUnits.Count == 0)
                {
                    // All units are proceeding peacefully - execute all moves
                    foreach (string unitId in collision.unitIds)
                    {
                        ExecuteUnitMove(unitId, collision, events);
                    }

                    events.Add(new CollisionResolvedEvent(turnNumber, collision.unitIds, collision.destination,
                        "All units proceeded peacefully, no combat"));

                    if (enableLogging)
                    {
                        Debug.Log($"[TurnResolver] RESOLUTION: All units proceeded peacefully at {collision.destination}");
                    }
                }
                // Case 2: Some units yield, some don't
                // Result: Non-yielding units get the tile (or fight each other if multiple)
                else if (yieldingUnits.Count > 0)
                {
                    // If only one unit not yielding, it gets the tile
                    if (notYieldingUnits.Count == 1)
                    {
                        string movingUnitId = notYieldingUnits[0];
                        Unit movingUnit = unitManager.GetUnit(movingUnitId);
                        string movingUnitName = movingUnit != null ? movingUnit.GetDisplayName(playerManager) : movingUnitId;

                        ExecuteUnitMove(movingUnitId, collision, events);

                        events.Add(new CollisionResolvedEvent(turnNumber, collision.unitIds, collision.destination,
                            $"{movingUnitName} claimed the tile, others yielded"));

                        if (enableLogging)
                        {
                            string tileId = $"#{Math.Abs(collision.destination.GetHashCode()) % 10000}";
                            Debug.Log($"[TurnResolver] RESOLUTION: {movingUnitName} claims tile {tileId}, others yielded");
                        }
                    }
                    // Multiple units not yielding - they all want the tile, so they fight
                    else
                    {
                        // Move all non-yielding units to the collision point
                        foreach (string unitId in notYieldingUnits)
                        {
                            ExecuteUnitMove(unitId, collision, events);
                        }

                        // Trigger combat between non-yielding units (fight to the death - they can't share the tile)
                        events.AddRange(ResolveCombatAtLocation(notYieldingUnits, collision.destination));

                        events.Add(new CollisionResolvedEvent(turnNumber, collision.unitIds, collision.destination,
                            $"Combat to the death between {notYieldingUnits.Count} non-yielding units"));

                        if (enableLogging)
                        {
                            Debug.Log($"[TurnResolver] RESOLUTION: {notYieldingUnits.Count} units fight to the death for the tile");
                        }
                    }
                }
                // Case 3: NO units yield - all want to fight
                // Result: CONTESTED TILE - ONE ROUND of combat, ships STAY in their original positions
                // They will have another chance next turn to yield or continue attacking
                else
                {
                    // DO NOT move units - ships stay in their current positions during contested combat
                    // This creates a "standoff" where they can try again next turn
                    List<Unit> combatUnits = new List<Unit>();
                    foreach (string unitId in collision.unitIds)
                    {
                        Unit unit = unitManager.GetUnit(unitId);
                        if (unit != null)
                        {
                            combatUnits.Add(unit);
                        }
                    }

                    if (enableLogging)
                    {
                        // Get ship names for contested tile logging
                        List<string> contestingNames = new List<string>();
                        foreach (Unit u in combatUnits)
                        {
                            contestingNames.Add(u.GetDisplayName(playerManager));
                        }

                        string tileId = $"#{Math.Abs(collision.destination.GetHashCode()) % 10000}";
                        if (contestingNames.Count == 2)
                        {
                            Debug.Log($"[TurnResolver] CONTESTED TILE: {contestingNames[0]} and {contestingNames[1]} refuse to yield at tile {tileId} - fighting ONE round");
                        }
                        else
                        {
                            Debug.Log($"[TurnResolver] CONTESTED TILE: {string.Join(", ", contestingNames)} all attack at tile {tileId} - fighting ONE round");
                        }
                    }

                    // Trigger ONE ROUND of combat between enemy ships
                    // Ships remain at their starting positions (contested)
                    if (combatUnits.Count == 2 && combatUnits[0].ownerId != combatUnits[1].ownerId)
                    {
                        // Use collision destination as the "location" for the combat event (for visualization)
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
                        $"CONTESTED TILE: All units attacked - combat round (ships remain in original positions)"));

                    if (enableLogging)
                    {
                        Debug.Log($"[TurnResolver] Tile {collision.destination} remains CONTESTED - units can try again next turn");
                    }
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
                string shipName = unit.GetDisplayName(playerManager);
                string fromTileId = $"#{Math.Abs(from.GetHashCode()) % 10000}";
                string toTileId = $"#{Math.Abs(destination.GetHashCode()) % 10000}";
                Debug.Log($"[TurnResolver] {shipName} moved from tile {fromTileId} to tile {toTileId}");
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
            if (enableLogging)
            {
                Debug.Log($"[TurnResolver] BEFORE damage: {unit1.id} = {unit1.health} HP, {unit2.id} = {unit2.health} HP");
            }

            unit1.TakeDamage(result.damageToAttacker);
            unit2.TakeDamage(result.damageToDefender);

            if (enableLogging)
            {
                Debug.Log($"[TurnResolver] AFTER damage: {unit1.id} = {unit1.health} HP, {unit2.id} = {unit2.health} HP");
            }

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
                attackerDestroyed,
                defenderDestroyed
            ));

            // Get ship display names for narrative logging
            string unit1Name = unit1.GetDisplayName(playerManager);
            string unit2Name = unit2.GetDisplayName(playerManager);

            // Determine context based on combat situation
            string context = "adjacent ships";
            if (unit1.isInCombat && unit2.isInCombat)
            {
                context = "ongoing multi-turn combat";
            }

            // Log combat to file with narrative description
            GameLogger.LogCombat(unit1Name, unit2Name, location, result.damageToAttacker, result.damageToDefender, context);

            if (enableLogging)
            {
                string tileId = $"#{Math.Abs(location.GetHashCode()) % 10000}";
                Debug.Log($"[TurnResolver] {unit1Name} attacks {unit2Name} at tile {tileId}: {unit1.health}HP vs {unit2.health}HP");
            }

            // Create destruction events (units removed immediately or by TurnAnimator)
            if (attackerDestroyed)
            {
                events.Add(new UnitDestroyedEvent(turnNumber, unit1.id, unit1.ownerId, location));
                if (!deferUnitRemoval)
                {
                    unitManager.RemoveUnit(unit1.id);
                }

                if (enableLogging)
                {
                    string tileId = $"#{Math.Abs(location.GetHashCode()) % 10000}";
                    Debug.Log($"[TurnResolver] {unit1Name} destroyed at tile {tileId}");
                }
            }

            if (defenderDestroyed)
            {
                events.Add(new UnitDestroyedEvent(turnNumber, unit2.id, unit2.ownerId, location));
                if (!deferUnitRemoval)
                {
                    unitManager.RemoveUnit(unit2.id);
                }

                if (enableLogging)
                {
                    string tileId = $"#{Math.Abs(location.GetHashCode()) % 10000}";
                    Debug.Log($"[TurnResolver] {unit2Name} destroyed at tile {tileId}");
                }
            }

            return events;
        }

        /// <summary>
        /// Resolve combat at a specific location - when enemy units occupy the same tile
        /// CRITICAL RULE: Enemy ships CANNOT occupy the same tile - they must fight until one is destroyed
        /// Friendly ships from the same player CAN peacefully stack on the same tile
        /// </summary>
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

            // If all units belong to same player, no combat needed
            // RULE: Friendly ships from the same player can occupy the same square peacefully
            if (unitsByPlayer.Count == 1)
            {
                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] All {units.Count} units at {location} belong to same player - peaceful stacking allowed");
                }
                return events;
            }

            if (enableLogging)
            {
                // Get ship names for combat logging
                List<string> shipNames = new List<string>();
                foreach (Unit u in units)
                {
                    shipNames.Add(u.GetDisplayName(playerManager));
                }

                string tileId = $"#{Math.Abs(location.GetHashCode()) % 10000}";
                Debug.Log($"[TurnResolver] ===== COMBAT TO THE DEATH at tile {tileId} =====");
                Debug.Log($"[TurnResolver] RULE ENFORCEMENT: Enemy ships CANNOT occupy same tile - must fight until one is destroyed");
                Debug.Log($"[TurnResolver] Ships: {string.Join(", ", shipNames)} from {unitsByPlayer.Count} different players");
            }

            // Combat between different players - fight to the death!
            // RULE: Enemy ships CANNOT occupy the same square - they must fight until one is destroyed
            if (units.Count == 2 && units[0].ownerId != units[1].ownerId)
            {
                // Direct 1v1 combat - fight until one dies
                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] 1v1 combat to the death: {units[0].id} vs {units[1].id}");
                }
                events.AddRange(ResolveCombatToTheDeath(units[0], units[1], location));
            }
            else
            {
                // Multiple units - do pairwise combat (could happen with 3+ ships)
                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Multi-unit combat: {units.Count} ships fighting pairwise");
                }
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

            // Get ship display names for narrative logging
            string unit1Name = unit1.GetDisplayName(playerManager);
            string unit2Name = unit2.GetDisplayName(playerManager);
            string tileId = $"#{Math.Abs(location.GetHashCode()) % 10000}";

            if (enableLogging)
            {
                Debug.Log($"[TurnResolver] {unit1Name} and {unit2Name} battle to the death at tile {tileId}");
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
                    attackerDestroyed,
                    defenderDestroyed
                ));

                // Log combat to file with narrative description
                GameLogger.LogCombat(unit1Name, unit2Name, location, result.damageToAttacker, result.damageToDefender, $"battle to the death - round {roundNumber}");

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Round {roundNumber}: {unit1Name} ({unit1.health}HP) vs {unit2Name} ({unit2.health}HP) - Damage: {result.damageToAttacker} to {unit1Name}, {result.damageToDefender} to {unit2Name}");
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
                    Debug.Log($"[TurnResolver] {unit1Name} destroyed after {roundNumber - 1} rounds at tile {tileId}");
                }
            }

            if (unit2.IsDead())
            {
                events.Add(new UnitDestroyedEvent(turnNumber, unit2.id, unit2.ownerId, location));
                unitManager.RemoveUnit(unit2.id);

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] {unit2Name} destroyed after {roundNumber - 1} rounds at tile {tileId}");
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
                        string shipAName = unitA.GetDisplayName(playerManager);
                        string shipBName = unitB.GetDisplayName(playerManager);
                        string tileAId = $"#{Math.Abs(unitA.position.GetHashCode()) % 10000}";
                        string tileBId = $"#{Math.Abs(unitB.position.GetHashCode()) % 10000}";
                        Debug.Log($"[TurnResolver] {shipAName} at tile {tileAId} and {shipBName} at tile {tileBId} are distance {distance} apart");
                    }

                    if (distance == 1)
                    {
                        // Resolve ONE ROUND of combat (multi-turn combat system)
                        if (enableLogging)
                        {
                            string shipAName = unitA.GetDisplayName(playerManager);
                            string shipBName = unitB.GetDisplayName(playerManager);
                            Debug.Log($"[TurnResolver] Adjacent combat triggered: {shipAName} vs {shipBName}");
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
                // NEW SYSTEM: Use ConstructionManager for deployment
                if (ConstructionManager.Instance != null)
                {
                    var result = ConstructionManager.Instance.DeployShipyard(
                        order.playerId,
                        order.unitId,
                        order.position
                    );

                    if (result.success)
                    {
                        // Create event for successful deployment
                        events.Add(new GameEvent(turnNumber, GameEventType.ShipyardDeployed,
                            $"Player {order.playerId} deployed shipyard at {order.position}"));

                        if (enableLogging)
                        {
                            Debug.Log($"[TurnResolver] Player {order.playerId} deployed shipyard at {order.position} (NEW SYSTEM)");
                        }
                    }
                    else
                    {
                        if (enableLogging)
                        {
                            Debug.LogWarning($"[TurnResolver] Failed to deploy shipyard: {result.reason}");
                        }
                    }

                    continue; // Skip legacy code below
                }

                // LEGACY FALLBACK: Old deployment system
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

                // Create shipyard directly (no harbor structures to convert)
                Structure shipyard = structureManager.CreateStructure(order.playerId, order.position, StructureType.SHIPYARD);

                // Remove ship (immediately or deferred to TurnAnimator)
                if (!deferUnitRemoval)
                {
                    unitManager.RemoveUnit(order.unitId);
                }

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
        /// <summary>
        /// DEPRECATED: Legacy build queue processing - replaced by ConstructionManager
        /// This method is kept for reference but should not be used
        /// </summary>
        [System.Obsolete("Use ConstructionManager.ProcessTurn() instead")]
        private List<GameEvent> ProcessBuildQueues()
        {
            Debug.LogWarning("[TurnResolver] ProcessBuildQueues() is deprecated - use ConstructionManager.ProcessTurn() instead");
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
                    Debug.Log($"[TurnResolver] LEGACY: Shipyard {shipyard.id} building {currentItem.itemType}: {currentItem.turnsRemaining} turns remaining");
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
                        Debug.Log($"[TurnResolver] LEGACY: Shipyard {shipyard.id} completed {currentItem.itemType}, spawned ship {newShip.id}");
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
                // NEW SYSTEM: Use ConstructionManager for queueing ships
                if (ConstructionManager.Instance != null)
                {
                    var result = ConstructionManager.Instance.QueueShip(order.playerId, order.shipyardId);

                    if (result.success)
                    {
                        events.Add(new GameEvent(turnNumber, GameEventType.ShipQueued,
                            $"Player {order.playerId} queued ship at shipyard {order.shipyardId}"));

                        if (enableLogging)
                        {
                            Debug.Log($"[TurnResolver] Player {order.playerId} queued ship at {order.shipyardId}, job {result.jobId} (NEW SYSTEM)");
                        }
                    }
                    else
                    {
                        if (enableLogging)
                        {
                            Debug.LogWarning($"[TurnResolver] Failed to queue ship: {result.reason}");
                        }
                    }

                    continue; // Skip legacy code below
                }

                // LEGACY FALLBACK: Old build queue system
                Structure shipyard = structureManager.GetStructure(order.shipyardId);
                if (shipyard == null || shipyard.type != StructureType.SHIPYARD)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] LEGACY: Shipyard {order.shipyardId} not found");
                    }
                    continue;
                }

                if (shipyard.ownerId != order.playerId)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] LEGACY: Player {order.playerId} does not own shipyard {order.shipyardId}");
                    }
                    continue;
                }

                if (shipyard.buildQueue.Count >= BuildingConfig.MAX_QUEUE_SIZE)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] LEGACY: Shipyard {order.shipyardId} build queue is full");
                    }
                    continue;
                }

                Player player = playerManager.GetPlayer(order.playerId);
                if (player == null || player.gold < BuildingConfig.BUILD_SHIP_COST)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] LEGACY: Player {order.playerId} does not have enough gold to build ship");
                    }
                    continue;
                }

                // Legacy: Direct mutation
                player.gold -= BuildingConfig.BUILD_SHIP_COST;
                BuildQueueItem queueItem = new BuildQueueItem("Ship", BuildingConfig.SHIP_BUILD_TIME, BuildingConfig.BUILD_SHIP_COST);
                shipyard.buildQueue.Add(queueItem);

                events.Add(new GameEvent(turnNumber, GameEventType.ShipQueued,
                    $"Player {order.playerId} queued ship at shipyard {shipyard.id} (LEGACY)"));

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] LEGACY: Player {order.playerId} queued ship at shipyard {shipyard.id}");
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

        /// <summary>
        /// Process gold income for all players at start of turn
        /// Base income: 10g per turn
        /// Shipyard bonus: +5g per shipyard
        /// Ship bonus: +2g per ship
        /// </summary>
        private List<GameEvent> ProcessIncome()
        {
            List<GameEvent> events = new List<GameEvent>();

            const int BASE_INCOME = 10;
            const int SHIPYARD_INCOME = 5;
            const int SHIP_INCOME = 2;

            foreach (var player in playerManager.players)
            {
                if (player.isEliminated)
                    continue;

                // Calculate income components
                int baseIncome = BASE_INCOME;
                int shipyardCount = structureManager.GetStructuresForPlayer(player.id)
                    .Count(s => s.type == PlunkAndPlunder.Structures.StructureType.SHIPYARD);
                int shipCount = unitManager.GetUnitsForPlayer(player.id).Count;

                int shipyardBonus = shipyardCount * SHIPYARD_INCOME;
                int shipBonus = shipCount * SHIP_INCOME;
                int totalIncome = baseIncome + shipyardBonus + shipBonus;

                // Award income
                player.gold += totalIncome;

                // Create event
                events.Add(new GoldEarnedEvent(
                    turnNumber,
                    player.id,
                    totalIncome,
                    baseIncome,
                    shipyardBonus,
                    shipBonus,
                    player.gold
                ));

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Player {player.id} earned {totalIncome}g → {player.gold}g total");
                }
            }

            return events;
        }

        /// <summary>
        /// Resolve attack shipyard orders
        /// Ship attacks enemy shipyard: roll 1d6, on 5-6 destroy shipyard and move ship in, on 1-4 ship stays in place
        /// </summary>
        private List<GameEvent> ResolveAttackShipyardOrders(List<AttackShipyardOrder> orders)
        {
            List<GameEvent> events = new List<GameEvent>();

            foreach (AttackShipyardOrder order in orders)
            {
                Unit attackerShip = unitManager.GetUnit(order.unitId);
                if (attackerShip == null)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Ship {order.unitId} not found for shipyard attack");
                    }
                    continue;
                }

                // Get target shipyard
                Structure targetShipyard = structureManager.GetStructureAtPosition(order.targetPosition);
                if (targetShipyard == null || targetShipyard.type != StructureType.SHIPYARD)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] No shipyard found at {order.targetPosition} for attack");
                    }
                    continue;
                }

                // Verify attacker doesn't own the shipyard
                if (targetShipyard.ownerId == order.playerId)
                {
                    if (enableLogging)
                    {
                        Debug.LogWarning($"[TurnResolver] Player {order.playerId} cannot attack their own shipyard");
                    }
                    continue;
                }

                // Execute the move portion first (if there's a path)
                if (order.path != null && order.path.Count > 1)
                {
                    // Move ship along path to get adjacent to shipyard
                    int movementCapacity = attackerShip.GetMovementCapacity();
                    int pathLength = order.path.Count - 1; // Subtract starting position
                    int movesThisTurn = Mathf.Min(movementCapacity, pathLength);

                    // Move to final position before attack
                    HexCoord attackPosition = order.path[movesThisTurn];

                    // Verify ship is now adjacent to target
                    int distanceToTarget = attackPosition.Distance(order.targetPosition);
                    if (distanceToTarget > 1)
                    {
                        if (enableLogging)
                        {
                            Debug.LogWarning($"[TurnResolver] Ship {order.unitId} not adjacent to target shipyard after move (distance: {distanceToTarget})");
                        }
                        continue;
                    }

                    // Update ship position (but don't create move event, that will be implicit in attack)
                    attackerShip.position = attackPosition;
                    attackerShip.movementRemaining = movementCapacity - movesThisTurn;

                    if (enableLogging)
                    {
                        Debug.Log($"[TurnResolver] Ship {order.unitId} moved to {attackPosition} to attack shipyard at {order.targetPosition}");
                    }
                }

                // DETERMINISTIC ATTACK: Deal 1 damage to shipyard
                int oldHealth = targetShipyard.health;
                targetShipyard.TakeDamage(1);
                int newHealth = targetShipyard.health;

                // Create structure attacked event
                events.Add(new StructureAttackedEvent(
                    turnNumber,
                    order.unitId,
                    targetShipyard.id,
                    order.playerId,
                    targetShipyard.ownerId,
                    order.targetPosition,
                    oldHealth,
                    newHealth
                ));

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Player {order.playerId} ship {order.unitId} attacks shipyard {targetShipyard.id}: {oldHealth} HP → {newHealth} HP");
                }

                // Check if shipyard is captured (health reached 0)
                if (targetShipyard.IsDestroyed())
                {
                    // Capture the shipyard - change ownership
                    int oldOwnerId = targetShipyard.ownerId;
                    structureManager.ChangeOwner(targetShipyard.id, order.playerId);

                    // Reset health to full after capture
                    targetShipyard.health = targetShipyard.maxHealth;

                    // Create structure captured event
                    events.Add(new StructureCapturedEvent(
                        turnNumber,
                        targetShipyard.id,
                        oldOwnerId,
                        order.playerId,
                        order.targetPosition,
                        order.unitId
                    ));

                    if (enableLogging)
                    {
                        Debug.Log($"[TurnResolver] Shipyard {targetShipyard.id} CAPTURED by player {order.playerId}! Health reset to {targetShipyard.maxHealth}");
                    }

                    // Log to game logger
                    GameLogger.LogPlayerAction(order.playerId,
                        $"Ship {order.unitId} captured enemy shipyard at {order.targetPosition} after 3 attacks!");
                }
                else
                {
                    // Shipyard still has health remaining
                    if (enableLogging)
                    {
                        Debug.Log($"[TurnResolver] Shipyard {targetShipyard.id} damaged but still standing ({newHealth}/{targetShipyard.maxHealth} HP)");
                    }

                    // Log to game logger
                    GameLogger.LogPlayerAction(order.playerId,
                        $"Ship {order.unitId} attacked enemy shipyard at {order.targetPosition} ({newHealth}/{targetShipyard.maxHealth} HP remaining)");
                }
            }

            return events;
        }

        // ====================
        // NEW ENCOUNTER SYSTEM RESOLUTION METHODS
        // ====================

        /// <summary>
        /// Resolves all encounters with player decisions.
        /// Main entry point for encounter resolution.
        /// </summary>
        public List<GameEvent> ResolveEncountersWithDecisions(List<Combat.Encounter> encounters)
        {
            List<GameEvent> events = new List<GameEvent>();

            // Sort encounters deterministically for consistent resolution order
            var sortedEncounters = encounters.OrderBy(e => e.GetStableSortKey()).ToList();

            foreach (var encounter in sortedEncounters)
            {
                if (encounter.Type == Combat.EncounterType.PASSING)
                {
                    events.AddRange(ResolvePassingEncounter(encounter));
                }
                else if (encounter.Type == Combat.EncounterType.ENTRY)
                {
                    events.AddRange(ResolveEntryEncounter(encounter));
                }
            }

            return events;
        }

        /// <summary>
        /// Resolves a PASSING encounter (two ships swapping positions).
        /// Decision matrix:
        ///   - PROCEED + PROCEED → peaceful swap
        ///   - Any ATTACK → combat on edge, units stay in place
        /// </summary>
        private List<GameEvent> ResolvePassingEncounter(Combat.Encounter encounter)
        {
            List<GameEvent> events = new List<GameEvent>();

            if (encounter.InvolvedUnitIds.Count != 2)
            {
                Debug.LogError($"[TurnResolver] PASSING encounter must have exactly 2 units, got {encounter.InvolvedUnitIds.Count}");
                return events;
            }

            string unitIdA = encounter.InvolvedUnitIds[0];
            string unitIdB = encounter.InvolvedUnitIds[1];

            Unit unitA = unitManager.GetUnit(unitIdA);
            Unit unitB = unitManager.GetUnit(unitIdB);

            if (unitA == null || unitB == null)
            {
                Debug.LogError($"[TurnResolver] Cannot resolve PASSING encounter - units not found");
                return events;
            }

            var decisionA = encounter.PassingDecisions[unitIdA];
            var decisionB = encounter.PassingDecisions[unitIdB];

            bool bothProceed = (decisionA == Combat.PassingEncounterDecision.PROCEED &&
                                decisionB == Combat.PassingEncounterDecision.PROCEED);

            if (bothProceed)
            {
                // Peaceful swap - exchange positions
                HexCoord posA = unitA.position;
                HexCoord posB = unitB.position;

                unitA.position = posB;
                unitB.position = posA;

                events.Add(new UnitMovedEvent(turnNumber, unitIdA, posA, posB));
                events.Add(new UnitMovedEvent(turnNumber, unitIdB, posB, posA));

                encounter.MarkAsResolved();
                events.Add(new EncounterResolvedEvent(turnNumber, encounter,
                    $"PASSING encounter resolved peacefully - ships swapped positions"));

                if (enableLogging)
                {
                    string shipAName = unitA.GetDisplayName(playerManager);
                    string shipBName = unitB.GetDisplayName(playerManager);
                    Debug.Log($"[TurnResolver] PASSING PEACEFUL: {shipAName} and {shipBName} swapped positions");
                }
            }
            else
            {
                // At least one attacked - combat on edge, units stay in place
                HexCoord edgeLocation = encounter.EdgeCoords.HasValue ? encounter.EdgeCoords.Value.Item1 : unitA.position;

                events.AddRange(ResolveOneRoundOfCombat(unitA, unitB, edgeLocation));

                encounter.MarkAsResolved();
                events.Add(new EncounterResolvedEvent(turnNumber, encounter,
                    $"PASSING encounter resolved with combat - ships remain in original positions"));

                if (enableLogging)
                {
                    string shipAName = unitA.GetDisplayName(playerManager);
                    string shipBName = unitB.GetDisplayName(playerManager);
                    Debug.Log($"[TurnResolver] PASSING COMBAT: {shipAName} and {shipBName} fought - stayed in place");
                }
            }

            return events;
        }

        /// <summary>
        /// Resolves an ENTRY encounter (multiple ships entering same tile).
        /// Decision matrix:
        ///   - All YIELD → no movement
        ///   - Exactly one ATTACK → attacker claims tile
        ///   - Multiple ATTACK → contested tile (pairwise combat, all stay in place, persist)
        /// </summary>
        private List<GameEvent> ResolveEntryEncounter(Combat.Encounter encounter)
        {
            List<GameEvent> events = new List<GameEvent>();

            // Identify attackers and yielders
            List<string> attackerIds = new List<string>();
            List<string> yielderIds = new List<string>();

            foreach (var kvp in encounter.EntryDecisions)
            {
                if (kvp.Value == Combat.EntryEncounterDecision.ATTACK)
                {
                    attackerIds.Add(kvp.Key);
                }
                else if (kvp.Value == Combat.EntryEncounterDecision.YIELD)
                {
                    yielderIds.Add(kvp.Key);
                }
            }

            HexCoord targetTile = encounter.TileCoord.Value;

            if (attackerIds.Count == 0)
            {
                // All yielded - no movement
                encounter.MarkAsResolved();
                events.Add(new EncounterResolvedEvent(turnNumber, encounter,
                    $"ENTRY encounter resolved - all units yielded, no movement"));

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] ENTRY ALL YIELD: All units stayed in place at {targetTile}");
                }
            }
            else if (attackerIds.Count == 1)
            {
                // Exactly one attacker - claims the tile
                string attackerId = attackerIds[0];
                Unit attacker = unitManager.GetUnit(attackerId);

                if (attacker != null)
                {
                    HexCoord from = attacker.position;
                    attacker.position = targetTile;

                    events.Add(new UnitMovedEvent(turnNumber, attackerId, from, targetTile));

                    encounter.MarkAsResolved();
                    events.Add(new EncounterResolvedEvent(turnNumber, encounter,
                        $"ENTRY encounter resolved - unit {attackerId} claimed tile {targetTile}"));

                    if (enableLogging)
                    {
                        string shipName = attacker.GetDisplayName(playerManager);
                        Debug.Log($"[TurnResolver] ENTRY CLAIM: {shipName} claimed tile {targetTile}");
                    }
                }
            }
            else
            {
                // Multiple attackers - contested tile
                // Pairwise combat between all attackers
                List<Unit> attackers = attackerIds
                    .Select(id => unitManager.GetUnit(id))
                    .Where(u => u != null)
                    .ToList();

                // Conduct pairwise combat
                for (int i = 0; i < attackers.Count; i++)
                {
                    for (int j = i + 1; j < attackers.Count; j++)
                    {
                        events.AddRange(ResolveOneRoundOfCombat(attackers[i], attackers[j], targetTile));
                    }
                }

                // Mark as contested - this persists across turns
                encounter.MarkAsContested();
                events.Add(new ContestedTileCreatedEvent(turnNumber, targetTile, encounter, attackerIds));
                events.Add(new EncounterResolvedEvent(turnNumber, encounter,
                    $"ENTRY encounter created contested tile at {targetTile} with {attackerIds.Count} units"));

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] ENTRY CONTESTED: Tile {targetTile} contested by {attackerIds.Count} units - pairwise combat");
                }
            }

            return events;
        }

        /// <summary>
        /// Helper method to execute a unit movement to a tile (used by ENTRY encounter resolution).
        /// </summary>
        private void ExecuteUnitMovementToTile(Unit unit, HexCoord targetTile, List<GameEvent> events)
        {
            if (unit == null) return;

            HexCoord from = unit.position;
            unit.position = targetTile;

            events.Add(new UnitMovedEvent(turnNumber, unit.id, from, targetTile));

            if (enableLogging)
            {
                string shipName = unit.GetDisplayName(playerManager);
                Debug.Log($"[TurnResolver] {shipName} moved to {targetTile}");
            }
        }

        /// <summary>
        /// Helper method to execute a swap movement between two units (used by PASSING encounter resolution).
        /// </summary>
        private void ExecuteSwapMovement(Unit unitA, Unit unitB, List<GameEvent> events)
        {
            if (unitA == null || unitB == null) return;

            HexCoord posA = unitA.position;
            HexCoord posB = unitB.position;

            unitA.position = posB;
            unitB.position = posA;

            events.Add(new UnitMovedEvent(turnNumber, unitA.id, posA, posB));
            events.Add(new UnitMovedEvent(turnNumber, unitB.id, posB, posA));

            if (enableLogging)
            {
                string shipAName = unitA.GetDisplayName(playerManager);
                string shipBName = unitB.GetDisplayName(playerManager);
                Debug.Log($"[TurnResolver] {shipAName} and {shipBName} swapped positions");
            }
        }
    }
}
