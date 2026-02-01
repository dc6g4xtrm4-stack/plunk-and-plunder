using System.Collections.Generic;
using PlunkAndPlunder.Construction;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Structures;
using PlunkAndPlunder.Units;
using UnityEngine;

namespace PlunkAndPlunder.Core
{
    public static class GameInitializer
    {
        public static void PlaceStartingShipyards(GameState state)
        {
            Debug.Log("[GameInitializer] Placing starting shipyards");
            int shipyardsPlaced = 0;

            for (int i = 0; i < state.playerManager.players.Count; i++)
            {
                if (i >= state.grid.playerStartIslands.Count)
                {
                    Debug.LogError($"[GameInitializer] No start island for player {i}");
                    continue;
                }

                PlayerStartIsland startIsland = state.grid.playerStartIslands[i];
                HexCoord shipyardPos = startIsland.shipyardPosition;

                // Create shipyard directly - no need to check for existing harbor structures
                Structure shipyard = state.structureManager.CreateStructure(i, shipyardPos, StructureType.SHIPYARD);
                Debug.Log($"[GameInitializer] Created starting shipyard for Player {i} at {shipyardPos}");

                if (shipyard != null && ConstructionManager.Instance != null)
                {
                    ConstructionManager.Instance.RegisterShipyard(shipyard.id);
                }

                shipyardsPlaced++;
            }

            Debug.Log($"[GameInitializer] Placed {shipyardsPlaced} starting shipyards");
        }

        public static void PlaceStartingUnits(GameState state)
        {
            Debug.Log("[GameInitializer] Placing starting units (3 ships per player on harbor tiles)");

            for (int i = 0; i < state.playerManager.players.Count; i++)
            {
                Player player = state.playerManager.players[i];

                if (i >= state.grid.playerStartIslands.Count)
                {
                    Debug.LogError($"[GameInitializer] No start island for player {player.id}");
                    continue;
                }

                PlayerStartIsland startIsland = state.grid.playerStartIslands[i];
                HexCoord shipyardPos = startIsland.shipyardPosition;

                Debug.Log($"[GameInitializer] Player {player.id} ({player.name}) shipyard at {shipyardPos}");

                Unit ship1 = state.unitManager.CreateUnit(player.id, shipyardPos, UnitType.SHIP);
                Debug.Log($"[GameInitializer]   Ship {ship1.id} spawned ON SHIPYARD at {shipyardPos}");

                List<HexCoord> nearbyHarbors = new List<HexCoord>();
                foreach (HexCoord neighborPos in shipyardPos.GetNeighbors())
                {
                    Tile tile = state.grid.GetTile(neighborPos);
                    if (tile != null && tile.type == TileType.HARBOR)
                    {
                        Structure existing = state.structureManager.GetStructureAtPosition(neighborPos);
                        // Only add if no shipyard exists (only structure type)
                        if (existing == null)
                        {
                            nearbyHarbors.Add(neighborPos);
                        }
                    }
                }

                int shipsPlaced = 1;
                for (int j = 0; j < Mathf.Min(2, nearbyHarbors.Count); j++)
                {
                    Unit ship = state.unitManager.CreateUnit(player.id, nearbyHarbors[j], UnitType.SHIP);
                    Debug.Log($"[GameInitializer]   Ship {ship.id} spawned on harbor at {nearbyHarbors[j]}");
                    shipsPlaced++;
                }

                Debug.Log($"[GameInitializer] Player {player.id} starts with {shipsPlaced} ships");
            }

            Debug.Log($"[GameInitializer] Placed {state.unitManager.units.Count} total starting units");
        }
    }
}
