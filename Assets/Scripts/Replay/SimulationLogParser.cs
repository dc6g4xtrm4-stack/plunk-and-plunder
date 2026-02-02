using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using UnityEngine;

namespace PlunkAndPlunder.Replay
{
    /// <summary>
    /// Parses simulation_*.txt log files into structured ReplayData
    /// </summary>
    public class SimulationLogParser
    {
        public ReplayData ParseLog(string filePath)
        {
            Debug.Log($"[SimulationLogParser] Parsing log file: {filePath}");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Simulation log not found: {filePath}");
            }

            ReplayData replayData = new ReplayData();
            replayData.logFilePath = filePath;
            replayData.initialization = new InitializationData();
            replayData.turns = new List<TurnData>();

            string[] lines = File.ReadAllLines(filePath);
            int currentTurn = -1;
            TurnData currentTurnData = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                try
                {
                    // Parse header information
                    if (line.StartsWith("Timestamp:"))
                    {
                        string timestampStr = line.Substring("Timestamp:".Length).Trim();
                        DateTime.TryParse(timestampStr, out replayData.timestamp);
                    }
                    else if (line.StartsWith("Players:"))
                    {
                        Match match = Regex.Match(line, @"Players:\s*(\d+)");
                        if (match.Success)
                        {
                            replayData.numPlayers = int.Parse(match.Groups[1].Value);
                        }
                    }
                    else if (line.StartsWith("Max Turns:"))
                    {
                        Match match = Regex.Match(line, @"Max Turns:\s*(\d+)");
                        if (match.Success)
                        {
                            replayData.maxTurns = int.Parse(match.Groups[1].Value);
                        }
                    }
                    // Parse initialization data
                    else if (line.StartsWith("[INIT]"))
                    {
                        ParseInitLine(line, replayData.initialization);
                    }
                    // Parse turn markers
                    else if (line.StartsWith("--- TURN"))
                    {
                        Match match = Regex.Match(line, @"--- TURN (\d+) ---");
                        if (match.Success)
                        {
                            currentTurn = int.Parse(match.Groups[1].Value);
                            currentTurnData = new TurnData();
                            currentTurnData.turnNumber = currentTurn;
                            currentTurnData.events = new List<GameEvent>();
                            replayData.turns.Add(currentTurnData);
                            Debug.Log($"[SimulationLogParser] Starting turn {currentTurn}");
                        }
                    }
                    // Parse events (only EVENT lines matter for animation)
                    else if (line.StartsWith("[EVENT]") && currentTurnData != null)
                    {
                        GameEvent gameEvent = ParseEventLine(line, currentTurn);
                        if (gameEvent != null)
                        {
                            currentTurnData.events.Add(gameEvent);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SimulationLogParser] Error parsing line {i}: {line}\n{ex.Message}");
                }
            }

            Debug.Log($"[SimulationLogParser] Parsing complete:");
            Debug.Log($"  - Players: {replayData.numPlayers}");
            Debug.Log($"  - Max Turns: {replayData.maxTurns}");
            Debug.Log($"  - Map Seed: {replayData.initialization.mapSeed}");
            Debug.Log($"  - Turns parsed: {replayData.turns.Count}");

            return replayData;
        }

        private void ParseInitLine(string line, InitializationData initData)
        {
            string content = line.Substring("[INIT]".Length).Trim();

            // Map seed
            Match seedMatch = Regex.Match(content, @"Map generated - Seed:\s*(\d+)");
            if (seedMatch.Success)
            {
                initData.mapSeed = int.Parse(seedMatch.Groups[1].Value);
                Debug.Log($"[SimulationLogParser] Map seed: {initData.mapSeed}");
                return;
            }

            // Player initialization
            Match playerMatch = Regex.Match(content, @"Player (\d+):\s*(.+?)\s*\((.+?)\)\s*-\s*Gold:\s*(\d+)");
            if (playerMatch.Success)
            {
                PlayerInitData playerData = new PlayerInitData();
                playerData.playerId = int.Parse(playerMatch.Groups[1].Value);
                playerData.name = playerMatch.Groups[2].Value;
                playerData.type = playerMatch.Groups[3].Value;
                playerData.startingGold = int.Parse(playerMatch.Groups[4].Value);
                initData.players.Add(playerData);
                Debug.Log($"[SimulationLogParser] Player {playerData.playerId}: {playerData.name}");
                return;
            }

            // Starting shipyard
            Match shipyardMatch = Regex.Match(content, @"Player (\d+) starting shipyard at \((-?\d+),\s*(-?\d+)\)");
            if (shipyardMatch.Success)
            {
                StructureInitData structData = new StructureInitData();
                structData.ownerId = int.Parse(shipyardMatch.Groups[1].Value);
                structData.position = new HexCoord(
                    int.Parse(shipyardMatch.Groups[2].Value),
                    int.Parse(shipyardMatch.Groups[3].Value)
                );
                structData.type = "SHIPYARD";
                structData.structureId = $"structure_{structData.ownerId}"; // Match GameSimulator's ID pattern
                initData.structures.Add(structData);
                Debug.Log($"[SimulationLogParser] Player {structData.ownerId} shipyard at {structData.position}");
                return;
            }

            // Starting ship
            Match shipMatch = Regex.Match(content, @"Player (\d+) starting ship '(.+?)' at \((-?\d+),\s*(-?\d+)\)");
            if (shipMatch.Success)
            {
                UnitInitData unitData = new UnitInitData();
                unitData.ownerId = int.Parse(shipMatch.Groups[1].Value);
                unitData.unitId = shipMatch.Groups[2].Value;
                unitData.position = new HexCoord(
                    int.Parse(shipMatch.Groups[3].Value),
                    int.Parse(shipMatch.Groups[4].Value)
                );
                unitData.type = "SHIP";
                initData.units.Add(unitData);
                Debug.Log($"[SimulationLogParser] Player {unitData.ownerId} ship '{unitData.unitId}' at {unitData.position}");
                return;
            }
        }

        private GameEvent ParseEventLine(string line, int turnNumber)
        {
            string content = line.Substring("[EVENT]".Length).Trim();

            // CollisionDetected
            Match collisionMatch = Regex.Match(content, @"(\d+) units colliding at \((-?\d+),\s*(-?\d+)\)");
            if (collisionMatch.Success)
            {
                // For now, we'll skip collision events as they don't directly animate
                // The TurnAnimator logs them but doesn't animate them
                return null;
            }

            // CollisionNeedsResolution
            if (content.Contains("needs yield decisions"))
            {
                // Skip - these are for interactive gameplay, not replay
                return null;
            }

            // If we find other event types in logs, we'll add them here
            // For now, the simulation logs only contain collision events

            return null;
        }
    }

    // Data structures for parsed replay data

    [Serializable]
    public class ReplayData
    {
        public string logFilePath;
        public DateTime timestamp;
        public int numPlayers;
        public int maxTurns;
        public InitializationData initialization;
        public List<TurnData> turns;
    }

    [Serializable]
    public class InitializationData
    {
        public int mapSeed;
        public List<PlayerInitData> players = new List<PlayerInitData>();
        public List<StructureInitData> structures = new List<StructureInitData>();
        public List<UnitInitData> units = new List<UnitInitData>();
    }

    [Serializable]
    public class PlayerInitData
    {
        public int playerId;
        public string name;
        public string type;
        public int startingGold;
    }

    [Serializable]
    public class StructureInitData
    {
        public string structureId;
        public int ownerId;
        public HexCoord position;
        public string type;
    }

    [Serializable]
    public class UnitInitData
    {
        public string unitId;
        public int ownerId;
        public HexCoord position;
        public string type;
    }

    [Serializable]
    public class TurnData
    {
        public int turnNumber;
        public List<GameEvent> events;
    }
}
