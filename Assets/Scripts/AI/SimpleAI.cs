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
                return orders;

            // Get all units for this player
            List<Unit> myUnits = unitManager.GetUnitsForPlayer(playerId);

            // Get player's shipyards
            List<Structure> myShipyards = structureManager.GetStructuresForPlayer(playerId)
                .FindAll(s => s.type == StructureType.SHIPYARD);

            // Get all enemy units
            List<Unit> enemyUnits = new List<Unit>();
            foreach (Player otherPlayer in playerManager.GetActivePlayers())
            {
                if (otherPlayer.id != playerId)
                {
                    enemyUnits.AddRange(unitManager.GetUnitsForPlayer(otherPlayer.id));
                }
            }

            // Get all harbors (for expansion)
            List<Tile> harbors = grid.GetTilesOfType(TileType.HARBOR);

            // Strategy 1: Build ships at shipyards if we have gold
            foreach (Structure shipyard in myShipyards)
            {
                if (player.gold >= BuildingConfig.BUILD_SHIP_COST && shipyard.buildQueue.Count < BuildingConfig.MAX_QUEUE_SIZE)
                {
                    // Only build if we have less than 5 ships
                    if (myUnits.Count < 5)
                    {
                        BuildShipOrder buildOrder = new BuildShipOrder(playerId, shipyard.id, shipyard.position);
                        orders.Add(buildOrder);
                        Debug.Log($"[AI Player {playerId}] Building ship at shipyard {shipyard.id}");
                    }
                }
            }

            // Strategy 2: Move units
            foreach (Unit unit in myUnits)
            {
                Tile currentTile = grid.GetTile(unit.position);

                // Check if unit is on harbor without shipyard - deploy one!
                if (currentTile != null && currentTile.type == TileType.HARBOR)
                {
                    Structure existingStructure = structureManager.GetStructureAtPosition(unit.position);
                    if (existingStructure == null && player.gold >= BuildingConfig.DEPLOY_SHIPYARD_COST)
                    {
                        DeployShipyardOrder deployOrder = new DeployShipyardOrder(unit.id, playerId, unit.position);
                        orders.Add(deployOrder);
                        Debug.Log($"[AI Player {playerId}] Deploying shipyard at {unit.position}");
                        continue; // Don't move this unit
                    }
                }

                // Find nearest target (enemy or unclaimed harbor)
                HexCoord targetPos = FindNearestTarget(unit.position, enemyUnits, harbors, structureManager);

                if (!targetPos.Equals(default(HexCoord)))
                {
                    // Try to move toward target
                    List<HexCoord> path = pathfinding.FindPath(unit.position, targetPos, unit.GetMovementCapacity() + 2);

                    if (path != null && path.Count > 1)
                    {
                        // Move as far as movement allows
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
                            Debug.Log($"[AI Player {playerId}] Moving unit {unit.id} toward target {targetPos}");
                        }
                    }
                }
            }

            Debug.Log($"[AI Player {playerId}] Generated {orders.Count} orders");
            return orders;
        }

        private HexCoord FindNearestTarget(HexCoord position, List<Unit> enemyUnits, List<Tile> harbors, StructureManager structureManager)
        {
            HexCoord nearest = default(HexCoord);
            int minDistance = int.MaxValue;

            // Priority 1: Check nearby enemies (within 10 tiles)
            foreach (Unit enemy in enemyUnits)
            {
                int dist = position.Distance(enemy.position);
                if (dist < minDistance && dist <= 10)
                {
                    minDistance = dist;
                    nearest = enemy.position;
                }
            }

            // Priority 2: Check unclaimed harbors (for expansion)
            foreach (Tile harbor in harbors)
            {
                Structure existingStructure = structureManager.GetStructureAtPosition(harbor.coord);
                if (existingStructure == null) // Harbor without shipyard
                {
                    int dist = position.Distance(harbor.coord);
                    if (dist < minDistance - 3) // Prefer enemies, but consider nearby unclaimed harbors
                    {
                        minDistance = dist;
                        nearest = harbor.coord;
                    }
                }
            }

            // Priority 3: If no nearby enemies or harbors, just pick the closest enemy
            if (nearest.Equals(default(HexCoord)) && enemyUnits.Count > 0)
            {
                foreach (Unit enemy in enemyUnits)
                {
                    int dist = position.Distance(enemy.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = enemy.position;
                    }
                }
            }

            return nearest;
        }
    }
}
