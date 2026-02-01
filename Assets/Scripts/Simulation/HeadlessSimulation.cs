using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PlunkAndPlunder.AI;
using PlunkAndPlunder.Construction;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Orders;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Resolution;
using PlunkAndPlunder.Structures;
using PlunkAndPlunder.Units;
using UnityEngine;

namespace PlunkAndPlunder.Simulation
{
    /// <summary>
    /// Runs a COMPLETE headless game simulation with NO UI
    /// Generates timestamped log file with ALL game events for replay
    /// </summary>
    public class HeadlessSimulation : MonoBehaviour
    {
        private GameEngine engine;
        private GameState state;  // Convenience reference to engine.State

        private StringBuilder gameLog;
        private string logFilePath;
        private int targetTurns = 100;
        private int turnNumber = 0;

        // Callback when simulation completes
        public System.Action<string> OnSimulationComplete;

        /// <summary>
        /// Start a headless 100-turn simulation with 4 AI players
        /// </summary>
        public void RunSimulation(int numPlayers = 4, int maxTurns = 100)
        {
            targetTurns = maxTurns;

            // Create timestamped log file
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            logFilePath = Path.Combine(Application.dataPath, "..", $"simulation_{timestamp}.txt");

            Debug.Log($"[HeadlessSimulation] Starting {maxTurns}-turn simulation");
            Debug.Log($"[HeadlessSimulation] Log file: {logFilePath}");

            gameLog = new StringBuilder();
            LogHeader(numPlayers, maxTurns);

            StartCoroutine(RunSimulationCoroutine(numPlayers));
        }

        private void LogHeader(int numPlayers, int maxTurns)
        {
            gameLog.AppendLine("=".PadRight(100, '='));
            gameLog.AppendLine("PLUNK & PLUNDER - GAME SIMULATION LOG");
            gameLog.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            gameLog.AppendLine($"Players: {numPlayers} (All AI)");
            gameLog.AppendLine($"Max Turns: {maxTurns}");
            gameLog.AppendLine("Format: Complete event log for game replay");
            gameLog.AppendLine("=".PadRight(100, '='));
            gameLog.AppendLine();
        }

        private IEnumerator RunSimulationCoroutine(int numPlayers)
        {
            // PHASE 1: Initialize game state using GameEngine
            Debug.Log("[HeadlessSimulation] Phase 1: Initializing game...");

            InitializeWithGameEngine(numPlayers);
            yield return null;

            // Check if initialization succeeded
            if (state == null || state.playerManager == null)
            {
                Debug.LogError("[HeadlessSimulation] Initialization failed!");
                yield break;
            }

            Debug.Log("[HeadlessSimulation] Initialization complete");

            // PHASE 2: Run turns until game over or max turns
            Debug.Log($"[HeadlessSimulation] Phase 2: Running {targetTurns} turns...");

            int maxIterations = targetTurns + 10; // Safety limit
            int iterations = 0;

            while (turnNumber < targetTurns && iterations < maxIterations)
            {
                iterations++;

                // Check for winner
                Player winner = state.playerManager.GetWinner();
                if (winner != null)
                {
                    LogGameOver(winner.id);
                    break;
                }

                // Process turn
                yield return ProcessTurnHeadless();
                turnNumber++;

                if (turnNumber % 10 == 0)
                {
                    Debug.Log($"[HeadlessSimulation] Turn {turnNumber}/{targetTurns} complete");
                }

                // Safety check
                if (iterations >= maxIterations)
                {
                    Debug.LogError("[HeadlessSimulation] Safety limit reached - stopping simulation");
                    gameLog.AppendLine("\n[ERROR] Simulation stopped - safety limit reached");
                    break;
                }
            }

            // PHASE 3: Write log file
            Debug.Log("[HeadlessSimulation] Phase 3: Writing log file...");
            FinishSimulation();
        }

        private void InitializeWithGameEngine(int numPlayers)
        {
            Debug.Log("[HeadlessSimulation] Initializing game with GameEngine...");

            // Create GameEngine with config
            GameConfig config = new GameConfig
            {
                numSeaTiles = 500,
                numIslands = 25,
                minIslandSize = 4,
                maxIslandSize = 8
            };
            engine = new GameEngine(config);

            // Subscribe to engine events for logging
            engine.OnStateChanged += LogStateSnapshot;
            engine.OnEventsGenerated += LogEvents;
            engine.OnGameWon += LogGameOver;

            // Create player configs (all AI)
            List<PlayerConfig> players = new List<PlayerConfig>();
            for (int i = 0; i < numPlayers; i++)
            {
                players.Add(new PlayerConfig($"AI {i}", PlayerType.AI));
            }

            // Initialize game
            engine.InitializeGame(players);
            state = engine.State;  // Keep reference for convenience

            // Log initialization
            gameLog.AppendLine($"[INIT] Map generated - Seed: {state.mapSeed}");
            int seaTiles = state.grid.GetAllTiles().Count(t => t.type == TileType.SEA);
            int harborTiles = state.grid.GetAllTiles().Count(t => t.type == TileType.HARBOR);
            gameLog.AppendLine($"[INIT] Total tiles: {state.grid.GetAllTiles().Count} (Sea: {seaTiles}, Harbors: {harborTiles})");
            gameLog.AppendLine();

            // Log players
            foreach (var player in state.playerManager.players)
            {
                gameLog.AppendLine($"[INIT] Player {player.id}: {player.name} (AI) - Gold: {player.gold}");
            }
            gameLog.AppendLine();

            // Log starting positions
            foreach (var player in state.playerManager.players)
            {
                var playerShipyards = state.structureManager.GetStructuresForPlayer(player.id)
                    .Where(s => s.type == StructureType.SHIPYARD).ToList();
                var playerShips = state.unitManager.GetUnitsForPlayer(player.id);

                foreach (var shipyard in playerShipyards)
                {
                    gameLog.AppendLine($"[INIT] Player {player.id} starting shipyard at {shipyard.position}");
                }

                foreach (var ship in playerShips)
                {
                    gameLog.AppendLine($"[INIT] Player {player.id} starting ship '{ship.id}' at {ship.position}");
                }
            }

            gameLog.AppendLine();
            gameLog.AppendLine("[INIT] Game initialization complete");
            gameLog.AppendLine();

            Debug.Log("[HeadlessSimulation] Game initialized successfully using GameEngine");
        }

        private IEnumerator ProcessTurnHeadless()
        {
            gameLog.AppendLine($"--- TURN {turnNumber} ---");

            // Log turn start state
            LogTurnState();

            // Process turn using GameEngine (handles income, AI orders, resolution)
            TurnResult result = engine.ProcessTurn();

            // Log AI orders (these were already generated and processed)
            foreach (var player in state.playerManager.players.Where(p => !p.isEliminated && p.type == PlayerType.AI))
            {
                // Orders already processed, just note that AI acted
                gameLog.AppendLine($"[AI] Player {player.id} orders processed");
            }

            // NEW: Handle encounters if any (new encounter system)
            if (result.encounters.Count > 0)
            {
                gameLog.AppendLine($"[ENCOUNTER_RESOLUTION] {result.encounters.Count} encounters detected, resolving...");

                foreach (var encounter in result.encounters)
                {
                    string location = encounter.Type == Combat.EncounterType.ENTRY
                        ? $"tile {encounter.TileCoord}"
                        : $"edge {encounter.EdgeCoords?.Item1} <-> {encounter.EdgeCoords?.Item2}";
                    gameLog.AppendLine($"[ENCOUNTER_RESOLUTION] {encounter.Type} encounter at {location} with {encounter.InvolvedUnitIds.Count} units");
                }

                // AI auto-decides for all encounters
                engine.MakeAIEncounterDecisions(result.encounters);

                // Log AI decisions
                foreach (var encounter in result.encounters)
                {
                    gameLog.AppendLine($"[ENCOUNTER_RESOLUTION] {encounter.Type} encounter decisions:");
                    foreach (string unitId in encounter.InvolvedUnitIds)
                    {
                        Unit unit = state.unitManager.GetUnit(unitId);
                        if (unit != null)
                        {
                            string decision = "";
                            if (encounter.Type == Combat.EncounterType.PASSING)
                            {
                                decision = encounter.PassingDecisions[unitId].ToString();
                            }
                            else if (encounter.Type == Combat.EncounterType.ENTRY)
                            {
                                decision = encounter.EntryDecisions[unitId].ToString();
                            }
                            gameLog.AppendLine($"[ENCOUNTER_RESOLUTION]   Unit {unitId} (P{unit.ownerId}): health={unit.health}/{unit.maxHealth}, decision={decision}");
                        }
                    }
                }

                // Resolve encounters
                var encounterEvents = engine.ResolveEncounters(result.encounters);
                gameLog.AppendLine($"[ENCOUNTER_RESOLUTION] Generated {encounterEvents.Count} encounter resolution events");
            }
            // OLD: Backward compatibility for old collision system
            else if (result.collisions.Count > 0)
            {
                gameLog.AppendLine("[COLLISION_RESOLUTION] Collisions detected, resolving...");

                foreach (var collision in result.collisions)
                {
                    gameLog.AppendLine($"[COLLISION_RESOLUTION] Collision at {collision.destination} with {collision.unitIds.Count} units");
                }

                // Get AI yield decisions
                var yieldDecisions = engine.GetAIYieldDecisions(result.collisions);

                foreach (var decision in yieldDecisions)
                {
                    Unit unit = state.unitManager.GetUnit(decision.Key);
                    if (unit != null)
                    {
                        gameLog.AppendLine($"[COLLISION_RESOLUTION] Unit {decision.Key} (P{unit.ownerId}): health={unit.health}/{unit.maxHealth}, yield={decision.Value}");
                    }
                }

                // Resolve collisions
                var collisionEvents = engine.ResolveCollisions(yieldDecisions);
                gameLog.AppendLine($"[COLLISION_RESOLUTION] Generated {collisionEvents.Count} collision resolution events");
            }

            // Events are already logged via OnEventsGenerated event handler

            // Log turn end state
            LogTurnEndState();
            gameLog.AppendLine();

            yield return null;
        }

        private void LogTurnState()
        {
            gameLog.AppendLine("[STATE_START]");

            foreach (var player in state.playerManager.players)
            {
                if (player.isEliminated)
                {
                    gameLog.AppendLine($"  Player {player.id}: ELIMINATED");
                    continue;
                }

                int ships = state.unitManager.GetUnitsForPlayer(player.id).Count;
                int shipyards = state.structureManager.GetStructuresForPlayer(player.id)
                    .Count(s => s.type == StructureType.SHIPYARD);

                gameLog.AppendLine($"  Player {player.id}: {ships} ships, {shipyards} shipyards, {player.gold}g");
            }
        }

        private void LogTurnEndState()
        {
            gameLog.AppendLine("[STATE_END]");

            // Log detailed unit positions
            foreach (var unit in state.unitManager.GetAllUnits())
            {
                gameLog.AppendLine($"  Unit {unit.id} (P{unit.ownerId}): pos={unit.position}, hp={unit.health}/{unit.maxHealth}, sails={unit.sails}, cannons={unit.cannons}");
            }

            // Log structure states
            foreach (var structure in state.structureManager.GetAllStructures())
            {
                var queue = ConstructionManager.Instance?.GetShipyardQueue(structure.id);
                int queueCount = queue?.Count ?? 0;
                gameLog.AppendLine($"  Structure {structure.id} (P{structure.ownerId}): type={structure.type}, pos={structure.position}, queue={queueCount}");
            }
        }

        private string GetOrderDescription(IOrder order)
        {
            switch (order)
            {
                case MoveOrder move:
                    return $"MOVE {move.unitId} to {move.path.Last()}";
                case BuildShipOrder build:
                    return $"BUILD_SHIP at {build.shipyardId}";
                case DeployShipyardOrder deploy:
                    return $"DEPLOY_SHIPYARD {deploy.unitId} at {deploy.position}";
                case RepairShipOrder repair:
                    return $"REPAIR {repair.unitId}";
                case UpgradeSailsOrder sails:
                    return $"UPGRADE_SAILS {sails.unitId}";
                case UpgradeCannonsOrder cannons:
                    return $"UPGRADE_CANNONS {cannons.unitId}";
                case UpgradeMaxLifeOrder maxLife:
                    return $"UPGRADE_MAX_LIFE {maxLife.unitId}";
                case AttackShipyardOrder attack:
                    return $"ATTACK_SHIPYARD at {attack.targetPosition}";
                default:
                    return order.GetType().Name;
            }
        }

        private void LogGameOver(int winnerId)
        {
            Player winner = state.playerManager.GetPlayer(winnerId);
            if (winner == null) return;

            gameLog.AppendLine();
            gameLog.AppendLine("=".PadRight(100, '='));
            gameLog.AppendLine("GAME OVER");
            gameLog.AppendLine($"Winner: Player {winner.id} ({winner.name})");
            gameLog.AppendLine($"Total Turns: {turnNumber}");
            gameLog.AppendLine("=".PadRight(100, '='));
            gameLog.AppendLine();

            // Log final statistics
            LogFinalStatistics();
        }

        // Event handlers for GameEngine
        private void LogStateSnapshot(GameState updatedState)
        {
            // State is updated - nothing to log here, handled elsewhere
        }

        private void LogEvents(List<GameEvent> events)
        {
            // Log all events
            foreach (var evt in events)
            {
                gameLog.AppendLine($"[EVENT] {evt.type}: {evt.message}");
            }

            if (events.Count == 0)
            {
                gameLog.AppendLine("[EVENT] No events this turn");
            }

            gameLog.AppendLine();
        }

        private void LogFinalStatistics()
        {
            gameLog.AppendLine("FINAL STATISTICS:");
            gameLog.AppendLine();

            foreach (var player in state.playerManager.players.OrderBy(p => p.id))
            {
                gameLog.AppendLine($"Player {player.id} ({player.name}):");
                gameLog.AppendLine($"  Status: {(player.isEliminated ? "Eliminated" : "Survived")}");
                gameLog.AppendLine($"  Final Gold: {player.gold}");

                int ships = state.unitManager.GetUnitsForPlayer(player.id).Count;
                int shipyards = state.structureManager.GetStructuresForPlayer(player.id)
                    .Count(s => s.type == StructureType.SHIPYARD);

                gameLog.AppendLine($"  Final Units: {ships} ships");
                gameLog.AppendLine($"  Final Structures: {shipyards} shipyards");
                gameLog.AppendLine();
            }
        }

        private void FinishSimulation()
        {
            // Write complete log to file
            File.WriteAllText(logFilePath, gameLog.ToString());

            Debug.Log($"[HeadlessSimulation] Simulation complete!");
            Debug.Log($"[HeadlessSimulation] Total turns: {turnNumber}");
            Debug.Log($"[HeadlessSimulation] Log written to: {logFilePath}");

            // Invoke callback
            OnSimulationComplete?.Invoke(logFilePath);
        }
    }
}
