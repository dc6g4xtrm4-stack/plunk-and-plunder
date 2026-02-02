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
            // DUMB AI - No pathfinding, just random movement for fast testing
            List<IOrder> orders = new List<IOrder>();
            Player player = playerManager.GetPlayer(playerId);

            if (player == null || structureManager == null)
                return orders;

            // Get units and shipyards
            List<Unit> myUnits = unitManager.GetUnitsForPlayer(playerId);
            List<Structure> myShipyards = structureManager.GetStructuresForPlayer(playerId)
                .FindAll(s => s.type == StructureType.SHIPYARD);

            // Occasionally build a ship (20% chance per shipyard if we have gold)
            foreach (Structure shipyard in myShipyards)
            {
                if (player.gold >= BuildingConfig.BUILD_SHIP_COST && random.NextDouble() < 0.2)
                {
                    orders.Add(new BuildShipOrder(playerId, shipyard.id, shipyard.position));
                }
            }

            // Move each unit randomly
            foreach (Unit unit in myUnits)
            {
                // Get navigable neighbors
                List<HexCoord> neighbors = grid.GetNavigableNeighbors(unit.position);

                if (neighbors.Count > 0)
                {
                    // Pick random neighbor and move there
                    HexCoord randomNeighbor = neighbors[random.Next(neighbors.Count)];
                    List<HexCoord> path = new List<HexCoord> { unit.position, randomNeighbor };
                    orders.Add(new MoveOrder(unit.id, playerId, path));
                }
            }

            return orders;
        }

        // FindNearestTarget removed - using dumb random AI for fast testing
    }
}
