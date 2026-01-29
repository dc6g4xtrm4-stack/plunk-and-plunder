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

            // Sort orders by type priority and then by unit ID for determinism
            // Priority: DeployShipyard > BuildShip > RepairShip > UpgradeShip > Move
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

            // Process upgrade ship orders
            List<UpgradeShipOrder> upgradeOrders = sortedOrders.OfType<UpgradeShipOrder>().ToList();
            events.AddRange(ResolveUpgradeShipOrders(upgradeOrders));

            // Process move orders
            List<MoveOrder> moveOrders = sortedOrders.OfType<MoveOrder>().ToList();
            events.AddRange(ResolveMoveOrders(moveOrders));

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
                case OrderType.Move:
                    return 4;
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

            // Resolve moves
            HashSet<string> blockedUnits = new HashSet<string>();

            foreach (var kvp in destinationMap)
            {
                HexCoord destination = kvp.Key;
                List<string> unitIds = kvp.Value;

                // Collision rule: if multiple units try to move to same hex, none move (bounce back)
                if (unitIds.Count > 1)
                {
                    events.Add(new UnitsCollidedEvent(turnNumber, unitIds, destination));
                    foreach (string unitId in unitIds)
                    {
                        blockedUnits.Add(unitId);
                    }

                    if (enableLogging)
                    {
                        Debug.Log($"[TurnResolver] Collision at {destination}: {unitIds.Count} units bounced back");
                    }
                }
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

                    // Skip if same owner
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
                        // Resolve dice-based combat
                        CombatResult result = combatResolver.ResolveCombat(unitA.id, unitB.id);

                        if (enableLogging)
                        {
                            Debug.Log($"[TurnResolver] Combat triggered! {result}");
                        }

                        // Apply damage
                        unitA.TakeDamage(result.damageToAttacker);
                        unitB.TakeDamage(result.damageToDefender);

                        // Create combat event
                        events.Add(new CombatOccurredEvent(
                            turnNumber,
                            unitA.id,
                            unitB.id,
                            result.damageToAttacker,
                            result.damageToDefender,
                            result.attackerRolls,
                            result.defenderRolls,
                            unitA.IsDead(),
                            unitB.IsDead()
                        ));

                        // Mark units as processed
                        unitsProcessed.Add(unitA.id);
                        unitsProcessed.Add(unitB.id);

                        // Mark for destruction if dead
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
                Debug.Log($"[TurnResolver] Combat resolution complete: {events.Count / 2} combats occurred, {unitsToDestroy.Count} units destroyed");
            }

            // Create destruction events (sort IDs for determinism)
            // NOTE: We don't actually destroy units here - TurnAnimator will do it
            foreach (string unitId in unitsToDestroy.OrderBy(id => id))
            {
                Unit unit = unitManager.GetUnit(unitId);
                if (unit != null)
                {
                    events.Add(new UnitDestroyedEvent(turnNumber, unitId, unit.ownerId, unit.position));
                    // unitManager.RemoveUnit(unitId); // Deferred to TurnAnimator
                }
            }

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

                // Deduct currency
                player.gold -= BuildingConfig.BUILD_SHIP_COST;

                // Create new ship at shipyard location
                Unit newShip = unitManager.CreateUnit(order.playerId, shipyard.position, UnitType.SHIP);

                events.Add(new ShipBuiltEvent(turnNumber, newShip.id, shipyard.id, order.playerId, shipyard.position, BuildingConfig.BUILD_SHIP_COST));

                if (enableLogging)
                {
                    Debug.Log($"[TurnResolver] Player {order.playerId} built ship {newShip.id} at shipyard {shipyard.id} for {BuildingConfig.BUILD_SHIP_COST} gold");
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
