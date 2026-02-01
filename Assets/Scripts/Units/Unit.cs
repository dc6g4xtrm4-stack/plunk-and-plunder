using System;
using System.Collections.Generic;
using PlunkAndPlunder.Map;
using UnityEngine;

namespace PlunkAndPlunder.Units
{
    [Serializable]
    public class Unit
    {
        public string id;
        public string name; // Ship name for display (e.g., "Theodore", "Sailfast")
        public int ownerId;
        public HexCoord position;
        public UnitType type;
        public int health;
        public int maxHealth;
        public float facingAngle; // Rotation angle in degrees (0 = facing right/east)
        public int movementRemaining; // Movement remaining this turn
        public List<HexCoord> queuedPath; // Path queued from previous turn (for multi-turn moves)

        // Ship upgrade stats
        public int sails; // Affects movement range
        public int cannons; // Affects combat effectiveness

        // Multi-turn combat tracking
        public bool isInCombat;
        public string combatOpponentId;

        // Ship name lists for random generation
        private static readonly string[] SHIP_NAMES = new string[]
        {
            "Theodore", "Sailfast", "Rusty", "Stanley", "Poseidon", "Neptune",
            "Kraken", "Leviathan", "Tempest", "Hurricane", "Typhoon", "Cyclone",
            "Seawolf", "Barracuda", "Hammerhead", "Mako", "Tigershark", "Orca",
            "Revenge", "Victory", "Glory", "Valor", "Pride", "Honor",
            "Thunder", "Lightning", "Storm", "Gale", "Squall", "Tornado",
            "Phantom", "Shadow", "Ghost", "Specter", "Wraith", "Banshee",
            "Cutlass", "Scimitar", "Rapier", "Sabre", "Blade", "Dagger",
            "Falcon", "Hawk", "Eagle", "Raven", "Crow", "Vulture",
            "Serpent", "Dragon", "Wyvern", "Drake", "Hydra", "Basilisk",
            "Fortune", "Destiny", "Fate", "Chance", "Luck", "Providence",
            "Marauder", "Corsair", "Buccaneer", "Privateer", "Raider", "Reaver",
            "Black Pearl", "Flying Dutchman", "Queen Anne", "Golden Hind", "Jolly Roger",
            "Sea Dog", "Salt Spray", "Wave Rider", "Tide Turner", "Wind Chaser",
            "Iron Maiden", "Steel Heart", "Copper Crown", "Silver Sword", "Gold Gull",
            "Davy Jones", "Captain Morgan", "Red Beard", "Blackbeard", "Bluebeard",
            "Stormbringer", "Wavecutter", "Seafarer", "Ocean Rider", "Deep Diver"
        };

        private static System.Random nameRandom = new System.Random();

        public Unit(string id, int ownerId, HexCoord position, UnitType type)
        {
            this.id = id;
            this.ownerId = ownerId;
            this.position = position;
            this.type = type;
            this.health = 10; // Ships start with 10 HP
            this.maxHealth = 10;
            this.facingAngle = 0f; // Default facing east
            this.movementRemaining = GetMovementCapacity(); // Initialize with full movement
            this.sails = 0; // Base ships have no sail upgrades
            this.cannons = 2; // Base ships start with 2 cannons (Sloop equivalent)
            this.isInCombat = false;
            this.combatOpponentId = null;

            // Generate a random ship name
            this.name = GenerateShipName();
        }

        /// <summary>
        /// Generate a random ship name from the predefined list
        /// </summary>
        private static string GenerateShipName()
        {
            return SHIP_NAMES[nameRandom.Next(SHIP_NAMES.Length)];
        }

        /// <summary>
        /// Get the full display name with owner information
        /// e.g., "Theodore (Player 1)" or "Sailfast (AI #2)"
        /// </summary>
        public string GetDisplayName(Players.PlayerManager playerManager = null)
        {
            if (playerManager != null)
            {
                Players.Player owner = playerManager.GetPlayer(ownerId);
                if (owner != null)
                {
                    return $"{name} ({owner.name})";
                }
            }
            return $"{name} (Player {ownerId})";
        }

        /// <summary>
        /// Apply damage to this unit
        /// </summary>
        public void TakeDamage(int damage)
        {
            health = Mathf.Max(0, health - damage);
        }

        /// <summary>
        /// Check if unit is dead
        /// </summary>
        public bool IsDead()
        {
            return health <= 0;
        }

        /// <summary>
        /// Get the maximum movement capacity for this unit based on its tier and sails upgrades
        /// Tier is determined by maxHealth thresholds:
        /// Tier 1 (maxHealth 1-10): 3 movement
        /// Tier 2 (maxHealth 11-20): 4 movement
        /// Tier 3 (maxHealth 21+): 5 movement
        /// Sails upgrades add +1 movement per upgrade
        /// </summary>
        public int GetMovementCapacity()
        {
            // Determine tier based on maxHealth thresholds
            int tier = 1;
            if (maxHealth >= 21)
                tier = 3;
            else if (maxHealth >= 11)
                tier = 2;

            // Base movement of 3, plus bonus based on tier, plus sails upgrades
            return 3 + (tier - 1) + sails;
        }

        /// <summary>
        /// Reset movement to full capacity at start of turn
        /// </summary>
        public void ResetMovement()
        {
            movementRemaining = GetMovementCapacity();
        }

        /// <summary>
        /// Update facing direction based on movement from oldPos to newPos
        /// </summary>
        public void UpdateFacing(HexCoord oldPos, HexCoord newPos)
        {
            if (oldPos.Equals(newPos))
                return;

            // Calculate direction vector
            int dq = newPos.q - oldPos.q;
            int dr = newPos.r - oldPos.r;

            // Convert hex direction to angle
            // Hex directions (flat-top orientation):
            // East (1,0), West (-1,0), NE (1,-1), NW (0,-1), SE (0,1), SW (-1,1)
            if (dq == 1 && dr == 0) facingAngle = 0f;      // East
            else if (dq == 1 && dr == -1) facingAngle = 60f;   // Northeast
            else if (dq == 0 && dr == -1) facingAngle = 120f;  // Northwest
            else if (dq == -1 && dr == 0) facingAngle = 180f;  // West
            else if (dq == -1 && dr == 1) facingAngle = 240f;  // Southwest
            else if (dq == 0 && dr == 1) facingAngle = 300f;   // Southeast
        }
    }

    public enum UnitType
    {
        SHIP
    }
}
