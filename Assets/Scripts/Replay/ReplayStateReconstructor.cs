using System.Collections.Generic;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Structures;
using PlunkAndPlunder.Units;
using UnityEngine;

namespace PlunkAndPlunder.Replay
{
    /// <summary>
    /// Reconstructs initial GameState from parsed initialization data
    /// </summary>
    public class ReplayStateReconstructor
    {
        public GameState ReconstructInitialState(InitializationData initData)
        {
            Debug.Log("[ReplayStateReconstructor] Reconstructing initial game state...");

            GameState state = new GameState();

            // 1. Create players
            state.playerManager = new PlayerManager();
            foreach (var playerData in initData.players)
            {
                Player player = state.playerManager.AddPlayer(playerData.name, PlayerType.AI);
                player.gold = playerData.startingGold;
                Debug.Log($"[ReplayStateReconstructor] Created player {player.id}: {player.name} with {player.gold}g");
            }

            // 2. Generate map from seed (deterministic)
            Debug.Log($"[ReplayStateReconstructor] Generating map with seed {initData.mapSeed}");
            MapGenerator mapGen = new MapGenerator(initData.mapSeed);
            state.grid = mapGen.GenerateMap(500, 25, 4, 8, initData.players.Count);
            state.mapSeed = initData.mapSeed;

            // 3. Create structure manager and structures
            state.structureManager = new StructureManager();
            foreach (var structData in initData.structures)
            {
                Structure structure = state.structureManager.CreateStructure(
                    structData.ownerId,
                    structData.position,
                    StructureType.SHIPYARD
                );
                // Override ID to match log (critical for event matching)
                structure.id = structData.structureId;
                Debug.Log($"[ReplayStateReconstructor] Created structure {structure.id} at {structData.position}");
            }

            // 4. Create unit manager and units
            state.unitManager = new UnitManager();
            foreach (var unitData in initData.units)
            {
                Unit unit = state.unitManager.CreateUnit(
                    unitData.ownerId,
                    unitData.position,
                    UnitType.SHIP
                );
                // Override ID to match log
                unit.id = unitData.unitId;
                Debug.Log($"[ReplayStateReconstructor] Created unit {unit.id} at {unitData.position}");
            }

            // 5. Set initial phase
            state.currentPhase = GamePhase.Replay;
            state.currentTurn = 0;

            Debug.Log($"[ReplayStateReconstructor] State reconstruction complete:");
            Debug.Log($"  - Players: {state.playerManager.players.Count}");
            Debug.Log($"  - Structures: {state.structureManager.GetAllStructures().Count}");
            Debug.Log($"  - Units: {state.unitManager.GetAllUnits().Count}");

            return state;
        }
    }
}
