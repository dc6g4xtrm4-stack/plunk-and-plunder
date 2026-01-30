using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PlunkAndPlunder.Core
{
    /// <summary>
    /// File-based logging system for gameplay events and game logic
    /// </summary>
    public static class GameLogger
    {
        private static string logFilePath;
        private static StreamWriter logWriter;
        private static bool isInitialized = false;

        public static void Initialize()
        {
            if (isInitialized)
                return;

            string logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            logFilePath = Path.Combine(logDirectory, $"gameplay_{timestamp}.log");

            try
            {
                logWriter = new StreamWriter(logFilePath, true);
                logWriter.AutoFlush = true;
                isInitialized = true;

                LogHeader();
                Debug.Log($"[GameLogger] Initialized. Log file: {logFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameLogger] Failed to initialize log file: {ex.Message}");
            }
        }

        private static void LogHeader()
        {
            Log("====================================");
            Log($"PLUNDERPUNK GAMEPLAY LOG");
            Log($"Session Start: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Log($"Unity Version: {Application.unityVersion}");
            Log("====================================");
            Log("");
        }

        public static void Log(string message)
        {
            if (!isInitialized)
                Initialize();

            if (logWriter != null)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    logWriter.WriteLine($"[{timestamp}] {message}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameLogger] Failed to write log: {ex.Message}");
                }
            }
        }

        public static void LogTurnStart(int turnNumber)
        {
            Log("");
            Log("====================================");
            Log($"TURN {turnNumber} START");
            Log("====================================");
        }

        public static void LogPhaseChange(GamePhase fromPhase, GamePhase toPhase)
        {
            Log($"PHASE CHANGE: {fromPhase} -> {toPhase}");
        }

        public static void LogPlayerAction(int playerId, string action)
        {
            Log($"[Player {playerId}] {action}");
        }

        public static void LogUnitAction(string unitId, int ownerId, string action)
        {
            Log($"[Unit {unitId} (Player {ownerId})] {action}");
        }

        public static void LogCombat(string attackerId, string defenderId, int damageToAttacker, int damageToDefender, List<int> attackerRolls, List<int> defenderRolls)
        {
            Log($"COMBAT: {attackerId} vs {defenderId}");
            Log($"  Attacker rolls: [{string.Join(", ", attackerRolls)}] = {SumList(attackerRolls)}");
            Log($"  Defender rolls: [{string.Join(", ", defenderRolls)}] = {SumList(defenderRolls)}");
            Log($"  Damage: Attacker -{damageToAttacker} HP, Defender -{damageToDefender} HP");
        }

        public static void LogCollision(List<string> unitIds, HexCoord destination)
        {
            Log($"COLLISION at {destination}: Units [{string.Join(", ", unitIds)}]");
        }

        public static void LogYieldDecision(string unitId, bool yielding)
        {
            string decision = yielding ? "PROCEED (yield)" : "ATTACK";
            Log($"  Unit {unitId} chose: {decision}");
        }

        public static void LogGameState(GameState state)
        {
            Log("GAME STATE SNAPSHOT:");
            Log($"  Turn: {state.turnNumber}");
            Log($"  Phase: {state.phase}");
            Log($"  Active Players: {state.playerManager.GetActivePlayers().Count}");
            Log($"  Total Units: {state.unitManager.GetAllUnits().Count}");
            Log($"  Total Structures: {state.structureManager.GetAllStructures().Count}");
            Log($"  Ongoing Combats: {state.ongoingCombats.Count}");

            // Log player details
            foreach (var player in state.playerManager.players)
            {
                if (!player.isEliminated)
                {
                    int ships = state.unitManager.GetUnitsForPlayer(player.id).Count;
                    int shipyards = state.structureManager.GetStructuresForPlayer(player.id).Count;
                    Log($"    Player {player.id}: {player.gold} gold, {ships} ships, {shipyards} shipyards");
                }
            }
        }

        public static void LogError(string context, string error)
        {
            Log($"ERROR in {context}: {error}");
            Debug.LogError($"[GameLogger] {context}: {error}");
        }

        private static int SumList(List<int> list)
        {
            int sum = 0;
            foreach (int val in list)
                sum += val;
            return sum;
        }

        public static void Shutdown()
        {
            if (logWriter != null)
            {
                Log("");
                Log("====================================");
                Log($"Session End: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Log("====================================");

                logWriter.Close();
                logWriter = null;
                isInitialized = false;

                Debug.Log($"[GameLogger] Log file closed: {logFilePath}");
            }
        }

        public static string GetLogFilePath()
        {
            return logFilePath;
        }
    }
}
