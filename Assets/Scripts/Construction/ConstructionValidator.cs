using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Structures;
using PlunkAndPlunder.Units;

namespace PlunkAndPlunder.Construction
{
    /// <summary>
    /// All validation rules for construction operations
    /// Pure functions - no side effects
    /// </summary>
    public static class ConstructionValidator
    {
        /// <summary>
        /// Validate queueing a ship at a shipyard
        /// </summary>
        public static ValidationResult ValidateQueueShip(
            int playerId,
            string shipyardId,
            ConstructionState constructionState,
            GameState gameState)
        {
            // 1. Shipyard exists
            Structure shipyard = gameState.structureManager.GetStructure(shipyardId);
            if (shipyard == null)
                return ValidationResult.Invalid("Shipyard not found");

            if (shipyard.type != StructureType.SHIPYARD)
                return ValidationResult.Invalid("Structure is not a shipyard");

            // 2. Player owns the shipyard
            if (shipyard.ownerId != playerId)
                return ValidationResult.Invalid("You don't own this shipyard");

            // 3. Queue has space
            if (constructionState.IsQueueFull(shipyardId))
            {
                int queueLength = constructionState.GetQueueLength(shipyardId);
                return ValidationResult.Invalid($"Queue is full ({queueLength}/{BuildingConfig.MAX_QUEUE_SIZE})");
            }

            // 4. Player has gold
            Player player = gameState.playerManager.GetPlayer(playerId);
            if (player == null || player.gold < BuildingConfig.BUILD_SHIP_COST)
            {
                int currentGold = player?.gold ?? 0;
                return ValidationResult.Invalid($"Insufficient gold (need {BuildingConfig.BUILD_SHIP_COST}g, have {currentGold}g)");
            }

            return ValidationResult.Valid();
        }

        /// <summary>
        /// Validate deploying a ship as a shipyard
        /// </summary>
        public static ValidationResult ValidateDeployShipyard(
            int playerId,
            string shipId,
            HexCoord position,
            GameState gameState)
        {
            // 1. Ship exists
            Unit ship = gameState.unitManager.GetUnit(shipId);
            if (ship == null)
                return ValidationResult.Invalid("Ship not found");

            // 2. Player owns the ship
            if (ship.ownerId != playerId)
                return ValidationResult.Invalid("You don't own this ship");

            // 3. Ship is at the target position
            if (!ship.position.Equals(position))
                return ValidationResult.Invalid("Ship must be at target position");

            // 4. Position is a harbor tile
            Tile tile = gameState.grid.GetTile(position);
            if (tile == null || tile.type != TileType.HARBOR)
                return ValidationResult.Invalid("Must deploy on harbor tile");

            // 5. No structure already exists at position (SHIPYARD is the only structure type)
            Structure existingStructure = gameState.structureManager.GetStructureAtPosition(position);
            if (existingStructure != null)
                return ValidationResult.Invalid("Shipyard already exists at this harbor");

            // 6. Player has gold for deployment
            Player player = gameState.playerManager.GetPlayer(playerId);
            if (player == null || player.gold < BuildingConfig.DEPLOY_SHIPYARD_COST)
            {
                int currentGold = player?.gold ?? 0;
                return ValidationResult.Invalid($"Insufficient gold for deployment (need {BuildingConfig.DEPLOY_SHIPYARD_COST}g, have {currentGold}g)");
            }

            return ValidationResult.Valid();
        }

        /// <summary>
        /// Validate cancelling a construction job
        /// </summary>
        public static ValidationResult ValidateCancelJob(
            int playerId,
            string jobId,
            ConstructionState constructionState)
        {
            // 1. Job exists
            ConstructionJob job = constructionState.GetJob(jobId);
            if (job == null)
                return ValidationResult.Invalid("Job not found");

            // 2. Player owns the job
            if (job.playerId != playerId)
                return ValidationResult.Invalid("You don't own this construction job");

            // 3. Job is not already completed or cancelled
            if (job.status == ConstructionStatus.Completed)
                return ValidationResult.Invalid("Job is already completed");

            if (job.status == ConstructionStatus.Cancelled)
                return ValidationResult.Invalid("Job is already cancelled");

            return ValidationResult.Valid();
        }
    }
}
