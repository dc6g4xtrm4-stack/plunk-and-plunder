using System.Collections.Generic;
using System.Linq;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Orders;
using PlunkAndPlunder.Players;
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

        public List<IOrder> GenerateOrders(int playerId)
        {
            List<IOrder> orders = new List<IOrder>();

            // Get all units for this player
            List<Unit> myUnits = unitManager.GetUnitsForPlayer(playerId);

            // Get all enemy units
            List<Unit> enemyUnits = new List<Unit>();
            foreach (Player player in playerManager.GetActivePlayers())
            {
                if (player.id != playerId)
                {
                    enemyUnits.AddRange(unitManager.GetUnitsForPlayer(player.id));
                }
            }

            // Get all harbors
            List<Tile> harbors = grid.GetTilesOfType(TileType.HARBOR);

            foreach (Unit unit in myUnits)
            {
                // Find nearest target (enemy or harbor)
                HexCoord targetPos = FindNearestTarget(unit.position, enemyUnits, harbors);

                if (!targetPos.Equals(default(HexCoord)))
                {
                    // Try to move toward target
                    List<HexCoord> path = pathfinding.FindPath(unit.position, targetPos, 5); // Max 5 steps per turn

                    if (path != null && path.Count > 1)
                    {
                        // Move as far as possible (up to 3 tiles for MVP)
                        int maxMoveDistance = Mathf.Min(3, path.Count);
                        List<HexCoord> movePath = path.Take(maxMoveDistance).ToList();

                        MoveOrder order = new MoveOrder(unit.id, playerId, movePath);
                        orders.Add(order);
                    }
                }
            }

            return orders;
        }

        private HexCoord FindNearestTarget(HexCoord position, List<Unit> enemyUnits, List<Tile> harbors)
        {
            HexCoord nearest = default(HexCoord);
            int minDistance = int.MaxValue;

            // Check enemies
            foreach (Unit enemy in enemyUnits)
            {
                int dist = position.Distance(enemy.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = enemy.position;
                }
            }

            // Check harbors (lower priority)
            foreach (Tile harbor in harbors)
            {
                int dist = position.Distance(harbor.coord);
                if (dist < minDistance - 2) // Prefer enemies over harbors
                {
                    minDistance = dist;
                    nearest = harbor.coord;
                }
            }

            return nearest;
        }
    }
}
