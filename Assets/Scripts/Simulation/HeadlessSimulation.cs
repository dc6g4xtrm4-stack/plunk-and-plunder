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
        private GameState state;
        private TurnResolver turnResolver;
        private Pathfinding pathfinding;
        private AIController aiController;
        private ConstructionManager constructionManager;

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
            // PHASE 1: Initialize game state (no UI)
            Debug.Log("[HeadlessSimulation] Phase 1: Initializing game...");

            bool initSuccess = false;
            yield return InitializeHeadlessGame(numPlayers);

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
                    LogGameOver(winner);
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

        private IEnumerator InitializeHeadlessGame(int numPlayers)
        {
            Debug.Log("[HeadlessSimulation] Initializing game state...");

            // Create game state
            state = new GameState();
            state.playerManager = new PlayerManager();
            state.unitManager = new UnitManager();
            state.structureManager = new StructureManager();
            state.turnNumber = 0;
            state.phase = GamePhase.Planning;

            // Create players (all AI)
            for (int i = 0; i < numPlayers; i++)
            {
                Player player = state.playerManager.AddPlayer($"AI {i}", PlayerType.AI);
                gameLog.AppendLine($"[INIT] Player {i}: {player.name} (AI) - Gold: {player.gold}");
            }

            gameLog.AppendLine();

            // Generate map
            int mapSeed = UnityEngine.Random.Range(0, int.MaxValue);
            state.mapSeed = mapSeed;

            // Set Unity's random seed for deterministic generation
            UnityEngine.Random.InitState(mapSeed);

            MapGenerator mapGen = new MapGenerator();
            // Use reasonable map parameters: 500 sea tiles, 25 islands, 4-8 size range, 4 players
            state.grid = mapGen.GenerateMap(
                numSeaTiles: 500,
                numIslands: 25,
                minIslandSize: 4,
                maxIslandSize: 8,
                numPlayers: numPlayers
            );

            int seaTiles = state.grid.GetAllTiles().Count(t => t.type == TileType.SEA);
            int harborTiles = state.grid.GetAllTiles().Count(t => t.type == TileType.HARBOR);
            int totalTiles = state.grid.GetAllTiles().Count;

            gameLog.AppendLine($"[INIT] Map generated - Seed: {mapSeed}");
            gameLog.AppendLine($"[INIT] Total tiles: {totalTiles} (Sea: {seaTiles}, Harbors: {harborTiles})");
            gameLog.AppendLine();

            // Place starting shipyards and units for each player
            var harbors = state.grid.GetAllTiles().Where(t => t.type == TileType.HARBOR).ToList();

            for (int i = 0; i < numPlayers && i < harbors.Count; i++)
            {
                HexCoord harborPos = harbors[i].coord;

                // Create shipyard
                Structure shipyard = state.structureManager.CreateStructure(i, harborPos, StructureType.SHIPYARD);
                gameLog.AppendLine($"[INIT] Player {i} starting shipyard at {harborPos}");

                // Create starting ship next to shipyard
                var neighborCoords = harborPos.GetNeighbors();
                Tile seaTile = null;
                foreach (var neighborCoord in neighborCoords)
                {
                    var tile = state.grid.GetTile(neighborCoord);
                    if (tile != null && tile.type == TileType.SEA)
                    {
                        seaTile = tile;
                        break;
                    }
                }

                if (seaTile != null)
                {
                    Unit ship = state.unitManager.CreateUnit(i, seaTile.coord, UnitType.SHIP);
                    gameLog.AppendLine($"[INIT] Player {i} starting ship '{ship.id}' at {seaTile.coord}");
                }
            }

            gameLog.AppendLine();

            // Initialize systems
            pathfinding = new Pathfinding(state.grid);
            turnResolver = new TurnResolver(
                state.grid,
                state.unitManager,
                state.playerManager,
                state.structureManager,
                enableLogging: false,
                deferUnitRemoval: false  // CRITICAL: Remove dead units immediately (no TurnAnimator in headless mode)
            );
            aiController = new AIController(
                state.grid,
                state.unitManager,
                state.playerManager,
                state.structureManager,
                pathfinding
            );

            // Initialize ConstructionManager
            if (ConstructionManager.Instance == null)
            {
                GameObject cmObj = new GameObject("ConstructionManager");
                constructionManager = cmObj.AddComponent<ConstructionManager>();
            }
            else
            {
                constructionManager = ConstructionManager.Instance;
            }

            constructionManager.Reset();

            // CRITICAL: Inject GameState so ConstructionManager uses the correct state
            constructionManager.Initialize(state);

            // Register shipyards with construction manager
            foreach (var shipyard in state.structureManager.GetAllStructures().Where(s => s.type == StructureType.SHIPYARD))
            {
                constructionManager.RegisterShipyard(shipyard.id);
            }

            Debug.Log("[HeadlessSimulation] Game initialized successfully");
            gameLog.AppendLine("[INIT] Game initialization complete");
            gameLog.AppendLine();

            yield return null;
        }

        private IEnumerator ProcessTurnHeadless()
        {
            gameLog.AppendLine($"--- TURN {turnNumber} ---");

            // Validate state
            if (state == null || state.playerManager == null || state.unitManager == null)
            {
                Debug.LogError($"[HeadlessSimulation] Game state is null at turn {turnNumber}");
                gameLog.AppendLine("[ERROR] Game state is null - stopping simulation");
                yield break;
            }

            // Log turn start state
            LogTurnState();

            // PLANNING PHASE: AI generates orders
            List<IOrder> allOrders = new List<IOrder>();

            foreach (var player in state.playerManager.players.Where(p => !p.isEliminated))
            {
                List<IOrder> playerOrders = aiController.PlanTurn(player.id);
                allOrders.AddRange(playerOrders);

                // Log orders
                foreach (var order in playerOrders)
                {
                    gameLog.AppendLine($"[ORDER] Player {player.id}: {GetOrderDescription(order)}");
                }
            }

            if (allOrders.Count == 0)
            {
                gameLog.AppendLine("[ORDER] No orders submitted this turn");
            }

            gameLog.AppendLine();

            // RESOLUTION PHASE: Process orders
            List<GameEvent> events = turnResolver.ResolveTurn(allOrders, turnNumber);

            // CRITICAL FIX: Handle collision resolution
            // ResolveTurn() returns early when collisions are detected
            // We must collect yield decisions and resolve collisions manually
            bool hasCollisions = events.Any(e => e.type == GameEventType.CollisionNeedsResolution);
            if (hasCollisions)
            {
                gameLog.AppendLine("[COLLISION_RESOLUTION] Collisions detected, resolving...");

                List<CollisionInfo> collisions = new List<CollisionInfo>();
                Dictionary<string, bool> yieldDecisions = new Dictionary<string, bool>();

                // Extract collision info from events
                foreach (var evt in events)
                {
                    if (evt is CollisionNeedsResolutionEvent collisionEvent)
                    {
                        collisions.Add(collisionEvent.collision);
                        gameLog.AppendLine($"[COLLISION_RESOLUTION] Collision at {collisionEvent.collision.destination} with {collisionEvent.collision.unitIds.Count} units");
                    }
                }

                // AI makes yield decisions automatically (based on health)
                foreach (var collision in collisions)
                {
                    foreach (string unitId in collision.unitIds)
                    {
                        Unit unit = state.unitManager.GetUnit(unitId);
                        if (unit != null)
                        {
                            // Yield if health below 50%
                            bool shouldYield = unit.health < (unit.maxHealth * 0.5f);
                            yieldDecisions[unitId] = shouldYield;
                            gameLog.AppendLine($"[COLLISION_RESOLUTION] Unit {unitId} (P{unit.ownerId}): health={unit.health}/{unit.maxHealth}, yield={shouldYield}");
                        }
                    }
                }

                // Resolve collisions with AI decisions
                List<GameEvent> collisionEvents = turnResolver.ResolveCollisionsWithYieldDecisions(
                    collisions,
                    yieldDecisions
                );
                events.AddRange(collisionEvents);
                gameLog.AppendLine($"[COLLISION_RESOLUTION] Generated {collisionEvents.Count} collision resolution events");

                // Resolve combat after movement
                List<GameEvent> combatEvents = turnResolver.ResolveCombatAfterMovement();
                events.AddRange(combatEvents);
                gameLog.AppendLine($"[COLLISION_RESOLUTION] Generated {combatEvents.Count} combat events");
            }

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

            // NOTE: Construction is processed by TurnResolver.ResolveTurn() at the start of turn
            // No need to call ProcessTurn() again here - it would be a duplicate

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
                var queue = constructionManager?.GetShipyardQueue(structure.id);
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

        private void LogGameOver(Player winner)
        {
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
