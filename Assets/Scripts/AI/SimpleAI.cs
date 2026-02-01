using System.Collections.Generic;
using System.Linq;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Orders;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Structures;
using PlunkAndPlunder.Units;
using UnityEngine;

namespace PlunkAndPlunder.AI
{
    /// <summary>
    /// Simple AI that moves ships toward nearest enemy or harbor
    /// </summary>
    public class SimpleAI
    {
        private HexGrid grid;
        private UnitManager unitManager;
        private PlayerManager playerManager;
        private StructureManager structureManager;
        private Pathfinding pathfinding;
        private System.Random random;

        public SimpleAI(HexGrid grid, UnitManager unitManager, PlayerManager playerManager, Pathfinding pathfinding)
        {
            this.grid = grid;
            this.unitManager = unitManager;
            this.playerManager = playerManager;
            this.pathfinding = pathfinding;
            this.random = new System.Random();
        }

        public void SetStructureManager(StructureManager structureManager)
        {
            this.structureManager = structureManager;
        }

        public List<IOrder> GenerateOrders(int playerId)
        {
            List<IOrder> orders = new List<IOrder>();
            Player player = playerManager.GetPlayer(playerId);

            if (player == null || structureManager == null)
            {
                Debug.LogWarning($"[AI Player {playerId}] Cannot generate orders: player or structureManager is null");
                return orders;
            }

            Debug.Log($"[AI Player {playerId}] === STARTING ORDER GENERATION === Gold: {player.gold}");

            // LOCAL gold tracking - don't modify player.gold during planning!
            int availableGold = player.gold;

            // Get all units for this player
            List<Unit> myUnits = unitManager.GetUnitsForPlayer(playerId);
            Debug.Log($"[AI Player {playerId}] Has {myUnits.Count} units");

            // Get player's shipyards
            List<Structure> myShipyards = structureManager.GetStructuresForPlayer(playerId)
                .FindAll(s => s.type == StructureType.SHIPYARD);
            Debug.Log($"[AI Player {playerId}] Has {myShipyards.Count} shipyards");

            // Get all enemy units
            List<Unit> enemyUnits = new List<Unit>();
            foreach (Player otherPlayer in playerManager.GetActivePlayers())
            {
                if (otherPlayer.id != playerId)
                {
                    List<Unit> otherUnits = unitManager.GetUnitsForPlayer(otherPlayer.id);
                    enemyUnits.AddRange(otherUnits);
                    Debug.Log($"[AI Player {playerId}] Enemy Player {otherPlayer.id} has {otherUnits.Count} units");
                }
            }
            Debug.Log($"[AI Player {playerId}] Total enemy units: {enemyUnits.Count}");

            // Get all harbors (for expansion)
            List<Tile> harbors = grid.GetTilesOfType(TileType.HARBOR);

            // Strategy 1: PRIORITIZE deploying shipyards (expansion before production)
            // Check units on harbors FIRST before spending gold on building ships
            List<Unit> unitsToMove = new List<Unit>(); // Track units that don't deploy

            foreach (Unit unit in myUnits)
            {
                Tile currentTile = grid.GetTile(unit.position);

                // Check if unit is on harbor without shipyard - deploy one!
                if (currentTile != null && currentTile.type == TileType.HARBOR)
                {
                    Structure existingStructure = structureManager.GetStructureAtPosition(unit.position);

                    // Check if there's already a friendly shipyard (only structure type)
                    bool hasFriendlyShipyard = existingStructure != null && existingStructure.ownerId == playerId;

                    // Deploy if no friendly shipyard and we have enough gold
                    if (!hasFriendlyShipyard && availableGold >= BuildingConfig.DEPLOY_SHIPYARD_COST)
                    {
                        DeployShipyardOrder deployOrder = new DeployShipyardOrder(unit.id, playerId, unit.position);
                        orders.Add(deployOrder);
                        availableGold -= BuildingConfig.DEPLOY_SHIPYARD_COST; // Track LOCAL gold spending
                        Debug.Log($"[AI Player {playerId}] DEPLOYING shipyard at {unit.position} (Gold: {availableGold + BuildingConfig.DEPLOY_SHIPYARD_COST} -> {availableGold})");
                        continue; // Don't move this unit
                    }
                    else if (!hasFriendlyShipyard)
                    {
                        Debug.Log($"[AI Player {playerId}] Unit {unit.id} on harbor at {unit.position} but not enough gold to deploy (have {availableGold}, need {BuildingConfig.DEPLOY_SHIPYARD_COST})");
                    }
                }

                // Unit didn't deploy, so it can move
                unitsToMove.Add(unit);
            }

            // Strategy 2: Build ships at shipyards if we have gold left
            // Build more aggressively - up to 10 ships total
            foreach (Structure shipyard in myShipyards)
            {
                int queueCount = PlunkAndPlunder.Construction.ConstructionManager.Instance?.GetShipyardQueue(shipyard.id)?.Count ?? 0;
                if (availableGold >= BuildingConfig.BUILD_SHIP_COST && queueCount < BuildingConfig.MAX_QUEUE_SIZE)
                {
                    // Build ships up to 10 total (more aggressive expansion)
                    if (myUnits.Count < 10)
                    {
                        BuildShipOrder buildOrder = new BuildShipOrder(playerId, shipyard.id, shipyard.position);
                        orders.Add(buildOrder);
                        availableGold -= BuildingConfig.BUILD_SHIP_COST; // Track LOCAL gold spending
                        Debug.Log($"[AI Player {playerId}] BUILDING ship at shipyard {shipyard.id} (Gold: {availableGold + BuildingConfig.BUILD_SHIP_COST} -> {availableGold})");
                    }
                    else
                    {
                        Debug.Log($"[AI Player {playerId}] Not building - already have {myUnits.Count} ships (max 10)");
                    }
                }
                else if (availableGold < BuildingConfig.BUILD_SHIP_COST)
                {
                    Debug.Log($"[AI Player {playerId}] Not enough gold to build ship (have {availableGold}, need {BuildingConfig.BUILD_SHIP_COST})");
                }
            }

            // Strategy 3: Move remaining units (units that didn't deploy)
            foreach (Unit unit in unitsToMove)
            {
                // Find nearest target (enemy or unclaimed harbor)
                HexCoord targetPos = FindNearestTarget(unit.position, enemyUnits, harbors, structureManager);

                if (!targetPos.Equals(default(HexCoord)))
                {
                    // Try to move toward target - use much longer pathfinding range (50 tiles)
                    // AI should be able to plan long-distance moves
                    List<HexCoord> path = pathfinding.FindPath(unit.position, targetPos, 50);

                    if (path != null && path.Count > 1)
                    {
                        // Move as far as movement allows this turn
                        int maxMoveDistance = Mathf.Min(unit.GetMovementCapacity(), path.Count - 1);
                        if (maxMoveDistance > 0)
                        {
                            List<HexCoord> movePath = new List<HexCoord>();
                            for (int i = 0; i <= maxMoveDistance; i++)
                            {
                                movePath.Add(path[i]);
                            }

                            MoveOrder order = new MoveOrder(unit.id, playerId, movePath);
                            orders.Add(order);
                            Debug.Log($"[AI Player {playerId}] MOVING unit {unit.id} from {unit.position} toward target {targetPos} (distance: {unit.position.Distance(targetPos)}, moving {maxMoveDistance} tiles)");
                        }
                        else
                        {
                            Debug.Log($"[AI Player {playerId}] Unit {unit.id} cannot move (maxMoveDistance = 0)");
                        }
                    }
                    else
                    {
                        Debug.Log($"[AI Player {playerId}] Unit {unit.id} at {unit.position} - no path found to target {targetPos}");
                    }
                }
                else
                {
                    Debug.Log($"[AI Player {playerId}] Unit {unit.id} at {unit.position} - no target found");
                }
            }

            Debug.Log($"[AI Player {playerId}] === COMPLETED ORDER GENERATION === Generated {orders.Count} orders");
            return orders;
        }

        private HexCoord FindNearestTarget(HexCoord position, List<Unit> enemyUnits, List<Tile> harbors, StructureManager structureManager)
        {
            HexCoord nearest = default(HexCoord);
            int minDistance = int.MaxValue;
            string targetType = "none";

            // Priority 1: Check ALL enemies (removed 10-tile limit for more aggressive AI)
            foreach (Unit enemy in enemyUnits)
            {
                int dist = position.Distance(enemy.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = enemy.position;
                    targetType = $"enemy_unit_{enemy.id}";
                }
            }

            // Priority 2: Check unclaimed harbors (for expansion) - only if no nearby enemy
            if (minDistance > 15) // Only expand if no enemy within 15 tiles
            {
                foreach (Tile harbor in harbors)
                {
                    Structure existingStructure = structureManager.GetStructureAtPosition(harbor.coord);
                    // Harbor is unclaimed if no structure exists (SHIPYARD is only structure type)
                    if (existingStructure == null)
                    {
                        int dist = position.Distance(harbor.coord);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            nearest = harbor.coord;
                            targetType = "unclaimed_harbor";
                        }
                    }
                }
            }

            // Priority 3: Check enemy shipyards (for conquest) - if no closer targets
            // SHIPYARD is the only structure type, so all structures are shipyards
            List<Structure> allStructures = structureManager.GetAllStructures();
            foreach (Structure structure in allStructures)
            {
                if (structure.ownerId != -1)
                {
                    // Don't target own shipyards
                    Unit anyUnit = enemyUnits.FirstOrDefault();
                    if (anyUnit != null && structure.ownerId != anyUnit.ownerId)
                        continue;

                    int dist = position.Distance(structure.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = structure.position;
                        targetType = $"enemy_shipyard_{structure.id}";
                    }
                }
            }

            if (!nearest.Equals(default(HexCoord)))
            {
                Debug.Log($"[AI] FindNearestTarget from {position}: Found {targetType} at {nearest} (distance: {minDistance})");
            }

            return nearest;
        }
    }
}
