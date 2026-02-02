using System.Collections.Generic;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Structures;
using PlunkAndPlunder.Units;
using UnityEngine;

namespace PlunkAndPlunder.AI
{
    /// <summary>
    /// Manages pirate ship spawning from Pirate Coves
    /// Pirates spawn periodically and roam the map attacking players
    /// </summary>
    public static class PirateSpawner
    {
        private const int PIRATE_SPAWN_INTERVAL = 5; // Spawn a pirate every 5 turns
        private const int PIRATE_PLAYER_ID = -1; // Pirates are neutral/hostile (-1)

        /// <summary>
        /// Process pirate spawning for the current turn
        /// Called by TurnResolver at the start of each turn
        /// </summary>
        public static List<GameEvent> ProcessPirateSpawning(GameState gameState, int turnNumber)
        {
            List<GameEvent> events = new List<GameEvent>();

            // Only spawn on spawn interval
            if (turnNumber % PIRATE_SPAWN_INTERVAL != 0)
            {
                return events;
            }

            // Find all Pirate Coves
            List<Structure> pirateCoves = new List<Structure>();
            foreach (var structure in gameState.structureManager.GetAllStructures())
            {
                if (structure.type == StructureType.PIRATE_COVE)
                {
                    pirateCoves.Add(structure);
                }
            }

            // Spawn a pirate from each cove
            foreach (var cove in pirateCoves)
            {
                // Check if cove position is occupied by a unit
                bool positionOccupied = gameState.unitManager.GetUnitsAtPosition(cove.position).Count > 0;

                if (positionOccupied)
                {
                    Debug.Log($"[PirateSpawner] Pirate Cove at {cove.position} blocked - cannot spawn");
                    continue;
                }

                // Spawn pirate ship at cove position
                Unit pirateShip = gameState.unitManager.CreateUnit(
                    PIRATE_PLAYER_ID,
                    cove.position,
                    UnitType.PIRATE_SHIP
                );

                events.Add(new GameEvent(turnNumber, GameEventType.ShipBuilt,
                    $"Pirate ship {pirateShip.id} spawned from Pirate Cove at {cove.position}"));

                Debug.Log($"[PirateSpawner] Spawned pirate ship {pirateShip.id} at {cove.position}");
            }

            return events;
        }

        /// <summary>
        /// Award gold to player who destroyed a pirate
        /// </summary>
        public static List<GameEvent> AwardPirateKillGold(GameState gameState, Unit destroyedUnit, Unit attackerUnit, int turnNumber)
        {
            List<GameEvent> events = new List<GameEvent>();

            // Only award gold if a pirate was killed by a player unit
            if (destroyedUnit.type != UnitType.PIRATE_SHIP || destroyedUnit.ownerId != PIRATE_PLAYER_ID)
            {
                return events; // Not a pirate kill
            }

            if (attackerUnit == null || attackerUnit.ownerId == PIRATE_PLAYER_ID)
            {
                return events; // Attacker not valid or is also a pirate
            }

            // Award random gold between min and max
            int goldReward = Random.Range(
                BuildingConfig.PIRATE_GOLD_REWARD_MIN,
                BuildingConfig.PIRATE_GOLD_REWARD_MAX + 1
            );

            Players.Player player = gameState.playerManager.GetPlayer(attackerUnit.ownerId);
            if (player != null)
            {
                player.gold += goldReward;

                events.Add(new GameEvent(turnNumber, GameEventType.GoldEarned,
                    $"Player {player.name} earned {goldReward} gold for destroying a pirate ship!"));

                Debug.Log($"[PirateSpawner] Player {player.name} earned {goldReward} gold for killing pirate {destroyedUnit.id}");
            }

            return events;
        }

        /// <summary>
        /// Check if a unit is a pirate
        /// </summary>
        public static bool IsPirate(Unit unit)
        {
            return unit != null && unit.type == UnitType.PIRATE_SHIP && unit.ownerId == PIRATE_PLAYER_ID;
        }
    }
}
