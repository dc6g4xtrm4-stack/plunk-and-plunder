using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Structures;
using PlunkAndPlunder.Units;
using UnityEngine;

namespace PlunkAndPlunder.Construction.Commands
{
    /// <summary>
    /// Atomic command to deploy a ship as a shipyard
    /// Consumes the ship and creates a new shipyard structure
    /// </summary>
    public class DeployShipyardCommand : ConstructionCommand
    {
        private int playerId;
        private string shipId;
        private HexCoord position;

        public DeployShipyardCommand(int playerId, string shipId, HexCoord position)
        {
            this.playerId = playerId;
            this.shipId = shipId;
            this.position = position;
        }

        public override ConstructionResult Execute(ConstructionState constructionState, GameState gameState)
        {
            // Get the ship
            Unit ship = gameState.unitManager.GetUnit(shipId);
            if (ship == null)
                return ConstructionResult.Failure("Ship not found");

            // Deduct gold
            Player player = gameState.playerManager.GetPlayer(playerId);
            player.gold -= BuildingConfig.DEPLOY_SHIPYARD_COST;

            // Create shipyard
            Structure shipyard = gameState.structureManager.CreateStructure(
                playerId,
                position,
                StructureType.SHIPYARD
            );

            // Initialize construction queue for new shipyard
            constructionState.InitializeShipyard(shipyard.id);

            // Destroy the ship
            gameState.unitManager.RemoveUnit(shipId);

            Debug.Log($"[DeployShipyardCommand] Player {playerId} deployed shipyard {shipyard.id} at {position}, consumed ship {shipId}");

            return ConstructionResult.Success();
        }
    }
}
