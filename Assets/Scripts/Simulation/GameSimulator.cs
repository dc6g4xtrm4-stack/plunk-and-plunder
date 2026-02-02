using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Players;
using PlunkAndPlunder.Units;
using PlunkAndPlunder.Structures;
using UnityEngine;

namespace PlunkAndPlunder.Simulation
{
    /// <summary>
    /// Runs headless AI simulations for testing game mechanics
    /// Logs comprehensive turn-by-turn data for replay analysis
    /// </summary>
    public class GameSimulator : MonoBehaviour
    {
        [Header("Simulation Settings")]
        [SerializeField] private int targetTurns = 100;
        [SerializeField] private int numPlayers = 4;
        [SerializeField] private bool autoStart = false;
        [SerializeField] private float turnsPerSecond = 10f; // Simulation speed

        [Header("Logging")]
        [SerializeField] private bool verboseLogging = true;
        [SerializeField] private string logFilePath = "simulation_log.txt";

        // Public setters for programmatic configuration
        public void Configure(int turns = 100, int players = 4, float speed = 10f, bool verbose = true)
        {
            targetTurns = turns;
            numPlayers = players;
            turnsPerSecond = speed;
            verboseLogging = verbose;
        }

        private GameManager gameManager;
        private StringBuilder logBuilder;
        private bool isSimulating = false;
        private int turnsProcessed = 0;
        private float lastTurnTime = 0f;

        // Statistics tracking
        private Dictionary<int, PlayerStats> playerStats = new Dictionary<int, PlayerStats>();

        private class PlayerStats
        {
            public int playerId;
            public string playerName;
            public int shipsBuilt;
            public int shipyardsDeployed;
            public int combatsWon;
            public int combatsLost;
            public int goldSpent;
            public int goldEarned;
            public int turnsAlive;
            public bool eliminated;
            public int eliminatedOnTurn;
        }

        private void Start()
        {
            if (autoStart)
            {
                StartSimulation();
            }
        }

        /// <summary>
        /// Start a new 100-turn simulation
        /// </summary>
        public void StartSimulation()
        {
            if (isSimulating)
            {
                Debug.LogWarning("[GameSimulator] Simulation already running");
                return;
            }

            Debug.Log($"[GameSimulator] Starting {targetTurns}-turn simulation with {numPlayers} AI players");

            logBuilder = new StringBuilder();
            logBuilder.AppendLine("=".PadRight(80, '='));
            logBuilder.AppendLine($"PLUNK & PLUNDER - {targetTurns}-TURN AI SIMULATION");
            logBuilder.AppendLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logBuilder.AppendLine($"Players: {numPlayers} AI");
            logBuilder.AppendLine("=".PadRight(80, '='));
            logBuilder.AppendLine();

            InitializeSimulation();
            StartCoroutine(RunSimulationCoroutine());
        }

        private void InitializeSimulation()
        {
            gameManager = GameManager.Instance;

            if (gameManager == null)
            {
                Debug.LogError("[GameSimulator] GameManager not found!");
                return;
            }

            // Configure GameManager for headless simulation
            gameManager.skipAnimation = true; // Will add this flag

            // Initialize game with all AI players
            gameManager.StartOfflineGame(numPlayers);

            // Initialize player statistics
            playerStats.Clear();
            foreach (var player in gameManager.state.playerManager.players)
            {
                playerStats[player.id] = new PlayerStats
                {
                    playerId = player.id,
                    playerName = player.name,
                    eliminated = false
                };
            }

            isSimulating = true;
            turnsProcessed = 0;
            lastTurnTime = Time.time;

            LogTurnState(0); // Log initial state
        }

        private IEnumerator RunSimulationCoroutine()
        {
            while (isSimulating && turnsProcessed < targetTurns)
            {
                // Check for game over
                Player winner = gameManager.state.playerManager.GetWinner();
                if (winner != null)
                {
                    LogGameOver(winner);
                    break;
                }

                // Process one turn
                yield return ProcessTurn();

                // Rate limiting
                float targetDelay = 1f / turnsPerSecond;
                float elapsed = Time.time - lastTurnTime;
                if (elapsed < targetDelay)
                {
                    yield return new WaitForSeconds(targetDelay - elapsed);
                }

                lastTurnTime = Time.time;
            }

            // Simulation complete
            FinishSimulation();
        }

        private IEnumerator ProcessTurn()
        {
            GameState state = gameManager.state;

            // Wait for Planning phase
            while (state.phase != GamePhase.Planning)
            {
                yield return null;
            }

            // Auto-resolve turn (submits orders for all players)
            gameManager.AutoResolveTurn();

            // Wait for turn to complete (back to Planning phase)
            int currentTurn = state.turnNumber;
            while (state.turnNumber == currentTurn)
            {
                yield return null;
            }

            turnsProcessed++;

            // Log turn results
            LogTurnState(state.turnNumber);
            UpdatePlayerStatistics(state);

            if (verboseLogging && turnsProcessed % 10 == 0)
            {
                Debug.Log($"[GameSimulator] Turn {turnsProcessed}/{targetTurns} complete");
            }
        }

        private void LogTurnState(int turnNumber)
        {
            GameState state = gameManager.state;

            logBuilder.AppendLine($"\n--- TURN {turnNumber} ---");

            // Player summary
            foreach (var player in state.playerManager.players)
            {
                if (player.isEliminated)
                {
                    logBuilder.AppendLine($"Player {player.id} ({player.name}): ELIMINATED");
                    continue;
                }

                int shipCount = state.unitManager.GetUnitsForPlayer(player.id).Count;
                int shipyardCount = state.structureManager.GetStructuresForPlayer(player.id)
                    .Count(s => s.type == StructureType.SHIPYARD);

                logBuilder.AppendLine($"Player {player.id} ({player.name}): " +
                    $"{shipCount} ships, {shipyardCount} shipyards, {player.gold}g");
            }

            // Recent events (last 10)
            if (state.eventHistory.Count > 0)
            {
                var recentEvents = state.eventHistory
                    .Where(e => e.turnNumber == turnNumber)
                    .Take(10)
                    .ToList();

                if (recentEvents.Count > 0)
                {
                    logBuilder.AppendLine("\nKey Events:");
                    foreach (var evt in recentEvents)
                    {
                        logBuilder.AppendLine($"  - {evt.message}");
                    }
                }
            }
        }

        private void UpdatePlayerStatistics(GameState state)
        {
            // Update turn counters
            foreach (var player in state.playerManager.players)
            {
                if (!player.isEliminated && playerStats.ContainsKey(player.id))
                {
                    playerStats[player.id].turnsAlive++;
                }
            }

            // Parse events for statistics
            var turnEvents = state.eventHistory
                .Where(e => e.turnNumber == state.turnNumber)
                .ToList();

            foreach (var evt in turnEvents)
            {
                switch (evt.type)
                {
                    case GameEventType.ShipBuilt:
                        if (evt is ShipBuiltEvent shipBuilt && playerStats.ContainsKey(shipBuilt.playerId))
                        {
                            playerStats[shipBuilt.playerId].shipsBuilt++;
                            playerStats[shipBuilt.playerId].goldSpent += shipBuilt.cost;
                        }
                        break;

                    case GameEventType.ShipyardDeployed:
                        if (evt is ShipyardDeployedEvent shipyardDeployed && playerStats.ContainsKey(shipyardDeployed.playerId))
                        {
                            playerStats[shipyardDeployed.playerId].shipyardsDeployed++;
                        }
                        break;

                    case GameEventType.UnitDestroyed:
                        if (evt is UnitDestroyedEvent destroyed)
                        {
                            // Track combat losses
                            if (playerStats.ContainsKey(destroyed.ownerId))
                            {
                                playerStats[destroyed.ownerId].combatsLost++;
                            }
                        }
                        break;

                    case GameEventType.PlayerEliminated:
                        if (evt is PlayerEliminatedEvent eliminated && playerStats.ContainsKey(eliminated.playerId))
                        {
                            playerStats[eliminated.playerId].eliminated = true;
                            playerStats[eliminated.playerId].eliminatedOnTurn = state.turnNumber;
                        }
                        break;
                }
            }
        }

        private void LogGameOver(Player winner)
        {
            logBuilder.AppendLine("\n");
            logBuilder.AppendLine("=".PadRight(80, '='));
            logBuilder.AppendLine("GAME OVER");
            logBuilder.AppendLine($"Winner: Player {winner.id} ({winner.name})");
            logBuilder.AppendLine($"Turns Played: {gameManager.state.turnNumber}");
            logBuilder.AppendLine("=".PadRight(80, '='));

            Debug.Log($"[GameSimulator] Game Over! Winner: {winner.name} on turn {gameManager.state.turnNumber}");
        }

        private void FinishSimulation()
        {
            isSimulating = false;

            // Final statistics
            logBuilder.AppendLine("\n");
            logBuilder.AppendLine("=".PadRight(80, '='));
            logBuilder.AppendLine("SIMULATION STATISTICS");
            logBuilder.AppendLine("=".PadRight(80, '='));

            foreach (var stats in playerStats.Values.OrderBy(s => s.playerId))
            {
                logBuilder.AppendLine($"\nPlayer {stats.playerId} ({stats.playerName}):");
                logBuilder.AppendLine($"  Status: {(stats.eliminated ? $"Eliminated (Turn {stats.eliminatedOnTurn})" : "Survived")}");
                logBuilder.AppendLine($"  Turns Alive: {stats.turnsAlive}");
                logBuilder.AppendLine($"  Ships Built: {stats.shipsBuilt}");
                logBuilder.AppendLine($"  Shipyards Deployed: {stats.shipyardsDeployed}");
                logBuilder.AppendLine($"  Combats Won: {stats.combatsWon}");
                logBuilder.AppendLine($"  Combats Lost: {stats.combatsLost}");
                logBuilder.AppendLine($"  Gold Spent: {stats.goldSpent}");
            }

            logBuilder.AppendLine("\n");
            logBuilder.AppendLine($"Simulation Completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            logBuilder.AppendLine($"Total Turns: {turnsProcessed}");

            // Write to file
            string fullPath = System.IO.Path.Combine(Application.dataPath, "..", logFilePath);
            System.IO.File.WriteAllText(fullPath, logBuilder.ToString());

            Debug.Log($"[GameSimulator] Simulation complete! Log written to: {fullPath}");
            Debug.Log($"[GameSimulator] Turns processed: {turnsProcessed}/{targetTurns}");
        }

        private void OnDestroy()
        {
            if (isSimulating)
            {
                StopAllCoroutines();
                FinishSimulation();
            }
        }
    }
}
