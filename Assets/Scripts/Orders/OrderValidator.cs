using System.Collections.Generic;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Structures;
using PlunkAndPlunder.Units;

namespace PlunkAndPlunder.Orders
{
    public class OrderValidator
    {
        private HexGrid grid;
        private UnitManager unitManager;
        private StructureManager structureManager;
        private PlayerManager playerManager;

        public OrderValidator(HexGrid grid, UnitManager unitManager, StructureManager structureManager = null, PlayerManager playerManager = null)
        {
            this.grid = grid;
            this.unitManager = unitManager;
            this.structureManager = structureManager;
            this.playerManager = playerManager;
        }

        public bool ValidateMoveOrder(MoveOrder order, out string error)
        {
            error = null;

            // Check unit exists
            Unit unit = unitManager.GetUnit(order.unitId);
            if (unit == null)
            {
                error = "Unit does not exist";
                return false;
            }

            // Check ownership
            if (unit.ownerId != order.playerId)
            {
                error = "Player does not own this unit";
                return false;
            }

            // Check path validity
            if (order.path == null || order.path.Count == 0)
            {
                error = "Path is empty";
                return false;
            }

            // Check path starts from unit position
            if (!order.path[0].Equals(unit.position))
            {
                error = "Path does not start at unit position";
                return false;
            }

            // Check all tiles in path are navigable and adjacent
            for (int i = 0; i < order.path.Count; i++)
            {
                HexCoord coord = order.path[i];

                if (!grid.IsNavigable(coord))
                {
                    error = $"Path contains non-navigable tile at {coord}";
                    return false;
                }

                if (i > 0)
                {
                    HexCoord prev = order.path[i - 1];
                    if (coord.Distance(prev) != 1)
                    {
                        error = $"Path is not continuous between {prev} and {coord}";
                        return false;
                    }
                }
            }

            // Check destination tile for enemy shipyard
            HexCoord destination = order.path[order.path.Count - 1];
            Structure structureAtDest = structureManager?.GetStructureAtPosition(destination);

            if (structureAtDest != null &&
                structureAtDest.type == StructureType.SHIPYARD &&
                structureAtDest.ownerId != order.playerId)
            {
                error = "Cannot move into a harbor occupied by an enemy shipyard";
                return false;
            }

            return true;
        }

        public bool ValidateDeployShipyardOrder(DeployShipyardOrder order, out string error)
        {
            error = null;

            // Check unit exists
            Unit unit = unitManager.GetUnit(order.unitId);
            if (unit == null)
            {
                error = "Unit does not exist";
                return false;
            }

            // Check ownership
            if (unit.ownerId != order.playerId)
            {
                error = "Player does not own this unit";
                return false;
            }

            // Check unit is a ship
            if (unit.type != UnitType.SHIP)
            {
                error = "Only ships can be deployed as shipyards";
                return false;
            }

            // Check position matches unit position
            if (!unit.position.Equals(order.position))
            {
                error = "Position does not match unit position";
                return false;
            }

            // Check tile is a harbor
            Tile tile = grid.GetTile(order.position);
            if (tile == null || tile.type != TileType.HARBOR)
            {
                error = "Ship must be on a harbor tile to deploy a shipyard";
                return false;
            }

            // Check if there's already a shipyard at this location
            if (structureManager != null)
            {
                Structure existingStructure = structureManager.GetStructureAtPosition(order.position);
                if (existingStructure != null && existingStructure.type == StructureType.SHIPYARD)
                {
                    error = "A shipyard already exists at this location";
                    return false;
                }
            }

            // Check player has enough currency
            if (playerManager != null)
            {
                Player player = playerManager.GetPlayer(order.playerId);
                if (player == null)
                {
                    error = "Player does not exist";
                    return false;
                }

                if (player.gold < BuildingConfig.DEPLOY_SHIPYARD_COST)
                {
                    error = $"Not enough gold. Need {BuildingConfig.DEPLOY_SHIPYARD_COST}, have {player.gold}";
                    return false;
                }
            }

            return true;
        }

        public bool ValidateBuildShipOrder(BuildShipOrder order, out string error)
        {
            error = null;

            // Check structure manager is available
            if (structureManager == null)
            {
                error = "Structure manager not available";
                return false;
            }

            // Check player manager is available
            if (playerManager == null)
            {
                error = "Player manager not available";
                return false;
            }

            // Check shipyard exists
            Structure shipyard = structureManager.GetStructure(order.shipyardId);
            if (shipyard == null)
            {
                error = "Shipyard does not exist";
                return false;
            }

            // Check shipyard type
            if (shipyard.type != StructureType.SHIPYARD)
            {
                error = "Structure is not a shipyard";
                return false;
            }

            // Check ownership
            if (shipyard.ownerId != order.playerId)
            {
                error = "Player does not own this shipyard";
                return false;
            }

            // Check position matches shipyard position
            if (!shipyard.position.Equals(order.shipyardPosition))
            {
                error = "Position does not match shipyard position";
                return false;
            }

            // Check player has enough currency
            Player player = playerManager.GetPlayer(order.playerId);
            if (player == null)
            {
                error = "Player does not exist";
                return false;
            }

            if (player.gold < BuildingConfig.BUILD_SHIP_COST)
            {
                error = $"Not enough gold. Need {BuildingConfig.BUILD_SHIP_COST}, have {player.gold}";
                return false;
            }

            return true;
        }

        public bool ValidateRepairShipOrder(RepairShipOrder order, out string error)
        {
            error = null;

            // Check unit exists
            Unit unit = unitManager.GetUnit(order.unitId);
            if (unit == null)
            {
                error = "Unit does not exist";
                return false;
            }

            // Check ownership
            if (unit.ownerId != order.playerId)
            {
                error = "Player does not own this unit";
                return false;
            }

            // Check unit is a ship
            if (unit.type != UnitType.SHIP)
            {
                error = "Only ships can be repaired";
                return false;
            }

            // Check structure manager is available
            if (structureManager == null)
            {
                error = "Structure manager not available";
                return false;
            }

            // Check shipyard exists
            Structure shipyard = structureManager.GetStructure(order.shipyardId);
            if (shipyard == null)
            {
                error = "Shipyard does not exist";
                return false;
            }

            // Check shipyard type
            if (shipyard.type != StructureType.SHIPYARD)
            {
                error = "Structure is not a shipyard";
                return false;
            }

            // Check shipyard ownership
            if (shipyard.ownerId != order.playerId)
            {
                error = "Player does not own this shipyard";
                return false;
            }

            // Check unit is at shipyard location
            if (!unit.position.Equals(shipyard.position))
            {
                error = "Unit must be at shipyard location to repair";
                return false;
            }

            // Check unit needs repair
            if (unit.health >= unit.maxHealth)
            {
                error = "Unit is already at full health";
                return false;
            }

            // Check player has enough currency
            if (playerManager == null)
            {
                error = "Player manager not available";
                return false;
            }

            Player player = playerManager.GetPlayer(order.playerId);
            if (player == null)
            {
                error = "Player does not exist";
                return false;
            }

            if (player.gold < BuildingConfig.REPAIR_SHIP_COST)
            {
                error = $"Not enough gold. Need {BuildingConfig.REPAIR_SHIP_COST}, have {player.gold}";
                return false;
            }

            return true;
        }

        public bool ValidateUpgradeShipOrder(UpgradeShipOrder order, out string error)
        {
            error = null;

            // Check unit exists
            Unit unit = unitManager.GetUnit(order.unitId);
            if (unit == null)
            {
                error = "Unit does not exist";
                return false;
            }

            // Check ownership
            if (unit.ownerId != order.playerId)
            {
                error = "Player does not own this unit";
                return false;
            }

            // Check unit is a ship
            if (unit.type != UnitType.SHIP)
            {
                error = "Only ships can be upgraded";
                return false;
            }

            // Check structure manager is available
            if (structureManager == null)
            {
                error = "Structure manager not available";
                return false;
            }

            // Check shipyard exists
            Structure shipyard = structureManager.GetStructure(order.shipyardId);
            if (shipyard == null)
            {
                error = "Shipyard does not exist";
                return false;
            }

            // Check shipyard type
            if (shipyard.type != StructureType.SHIPYARD)
            {
                error = "Structure is not a shipyard";
                return false;
            }

            // Check shipyard ownership
            if (shipyard.ownerId != order.playerId)
            {
                error = "Player does not own this shipyard";
                return false;
            }

            // Check unit is at shipyard location
            if (!unit.position.Equals(shipyard.position))
            {
                error = "Unit must be at shipyard location to upgrade";
                return false;
            }

            // Check unit is not already at max tier
            if (unit.maxHealth >= BuildingConfig.UPGRADED_SHIP_TIER_3_MAX_HEALTH)
            {
                error = "Unit is already at maximum tier";
                return false;
            }

            // Check player has enough currency
            if (playerManager == null)
            {
                error = "Player manager not available";
                return false;
            }

            Player player = playerManager.GetPlayer(order.playerId);
            if (player == null)
            {
                error = "Player does not exist";
                return false;
            }

            if (player.gold < BuildingConfig.UPGRADE_SHIP_COST)
            {
                error = $"Not enough gold. Need {BuildingConfig.UPGRADE_SHIP_COST}, have {player.gold}";
                return false;
            }

            return true;
        }

        public bool ValidateAttackShipyardOrder(AttackShipyardOrder order, out string error)
        {
            error = null;

            // Check unit exists
            Unit unit = unitManager.GetUnit(order.unitId);
            if (unit == null)
            {
                error = "Unit does not exist";
                return false;
            }

            // Check ownership
            if (unit.ownerId != order.playerId)
            {
                error = "Player does not own this unit";
                return false;
            }

            // Check unit is a ship
            if (unit.type != UnitType.SHIP)
            {
                error = "Only ships can attack shipyards";
                return false;
            }

            // Check structure manager is available
            if (structureManager == null)
            {
                error = "Structure manager not available";
                return false;
            }

            // Check target shipyard exists
            Structure targetShipyard = structureManager.GetStructure(order.targetShipyardId);
            if (targetShipyard == null)
            {
                error = "Target shipyard does not exist";
                return false;
            }

            // Check target is a shipyard
            if (targetShipyard.type != StructureType.SHIPYARD)
            {
                error = "Target structure is not a shipyard";
                return false;
            }

            // Check target is not owned by the player (can't attack your own shipyard)
            if (targetShipyard.ownerId == order.playerId)
            {
                error = "Cannot attack your own shipyard";
                return false;
            }

            // Check target position matches shipyard position
            if (!targetShipyard.position.Equals(order.targetPosition))
            {
                error = "Target position does not match shipyard position";
                return false;
            }

            // Check path validity (if provided)
            if (order.path != null && order.path.Count > 0)
            {
                // Check path starts from unit position
                if (!order.path[0].Equals(unit.position))
                {
                    error = "Path does not start at unit position";
                    return false;
                }

                // Check path ends at or adjacent to target
                HexCoord pathEnd = order.path[order.path.Count - 1];
                int distanceToTarget = pathEnd.Distance(order.targetPosition);
                if (distanceToTarget > 1)
                {
                    error = "Path does not end adjacent to target shipyard";
                    return false;
                }

                // Check all tiles in path are navigable and adjacent
                for (int i = 0; i < order.path.Count; i++)
                {
                    HexCoord coord = order.path[i];

                    if (!grid.IsNavigable(coord))
                    {
                        error = $"Path contains non-navigable tile at {coord}";
                        return false;
                    }

                    if (i > 0)
                    {
                        HexCoord prev = order.path[i - 1];
                        if (coord.Distance(prev) != 1)
                        {
                            error = $"Path is not continuous between {prev} and {coord}";
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}
