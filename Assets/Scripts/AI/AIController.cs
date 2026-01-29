using System.Collections.Generic;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Orders;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Units;

namespace PlunkAndPlunder.AI
{
    /// <summary>
    /// Coordinates AI player decision-making
    /// </summary>
    public class AIController
    {
        private HexGrid grid;
        private UnitManager unitManager;
        private PlayerManager playerManager;
        private Pathfinding pathfinding;
        private SimpleAI simpleAI;

        public AIController(HexGrid grid, UnitManager unitManager, PlayerManager playerManager, Pathfinding pathfinding)
        {
            this.grid = grid;
            this.unitManager = unitManager;
            this.playerManager = playerManager;
            this.pathfinding = pathfinding;
            this.simpleAI = new SimpleAI(grid, unitManager, playerManager, pathfinding);
        }

        public List<IOrder> PlanTurn(int playerId)
        {
            Player player = playerManager.GetPlayer(playerId);
            if (player == null || player.isEliminated || player.type != PlayerType.AI)
                return new List<IOrder>();

            // Use simple AI for MVP
            return simpleAI.GenerateOrders(playerId);
        }
    }
}
