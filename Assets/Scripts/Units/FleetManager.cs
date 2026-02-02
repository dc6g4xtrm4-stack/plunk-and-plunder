using System;
using System.Collections.Generic;
using System.Linq;
using PlunkAndPlunder.Map;
using UnityEngine;

namespace PlunkAndPlunder.Units
{
    /// <summary>
    /// Manages all fleets in the game.
    /// Handles fleet creation, disbanding, and validation.
    /// </summary>
    [Serializable]
    public class FleetManager
    {
        private Dictionary<string, Fleet> fleets = new Dictionary<string, Fleet>();
        private int nextFleetId = 0;

        /// <summary>
        /// Create a new fleet from units at the same position
        /// </summary>
        public Fleet CreateFleet(int ownerId, List<string> unitIds, UnitManager unitManager)
        {
            if (unitIds.Count < 2)
            {
                Debug.LogWarning("[FleetManager] Cannot create fleet with less than 2 units");
                return null;
            }

            // Verify all units exist and are at the same position
            Unit firstUnit = unitManager.GetUnit(unitIds[0]);
            if (firstUnit == null)
            {
                Debug.LogWarning("[FleetManager] First unit does not exist");
                return null;
            }

            HexCoord position = firstUnit.position;
            foreach (string unitId in unitIds)
            {
                Unit unit = unitManager.GetUnit(unitId);
                if (unit == null)
                {
                    Debug.LogWarning($"[FleetManager] Unit {unitId} does not exist");
                    return null;
                }

                if (!unit.position.Equals(position))
                {
                    Debug.LogWarning($"[FleetManager] Unit {unitId} is not at the same position as other units");
                    return null;
                }

                if (unit.ownerId != ownerId)
                {
                    Debug.LogWarning($"[FleetManager] Unit {unitId} is not owned by player {ownerId}");
                    return null;
                }
            }

            // Remove units from any existing fleets
            foreach (string unitId in unitIds)
            {
                RemoveUnitFromAllFleets(unitId);
            }

            // Create new fleet
            string fleetId = $"fleet_{nextFleetId++}";
            Fleet fleet = new Fleet(fleetId, ownerId, unitIds);
            fleets[fleetId] = fleet;

            Debug.Log($"[FleetManager] Created fleet {fleetId} with {unitIds.Count} ships at {position}");
            return fleet;
        }

        /// <summary>
        /// Disband a fleet, turning it back into individual units
        /// </summary>
        public void DisbandFleet(string fleetId)
        {
            if (fleets.ContainsKey(fleetId))
            {
                Fleet fleet = fleets[fleetId];
                Debug.Log($"[FleetManager] Disbanded fleet {fleetId} ({fleet.unitIds.Count} ships)");
                fleets.Remove(fleetId);
            }
        }

        /// <summary>
        /// Get a fleet by ID
        /// </summary>
        public Fleet GetFleet(string fleetId)
        {
            fleets.TryGetValue(fleetId, out Fleet fleet);
            return fleet;
        }

        /// <summary>
        /// Get all fleets
        /// </summary>
        public List<Fleet> GetAllFleets()
        {
            return new List<Fleet>(fleets.Values);
        }

        /// <summary>
        /// Get all fleets owned by a player
        /// </summary>
        public List<Fleet> GetFleetsForPlayer(int playerId)
        {
            return fleets.Values.Where(f => f.ownerId == playerId).ToList();
        }

        /// <summary>
        /// Find the fleet that contains a specific unit
        /// </summary>
        public Fleet GetFleetContainingUnit(string unitId)
        {
            foreach (Fleet fleet in fleets.Values)
            {
                if (fleet.unitIds.Contains(unitId))
                {
                    return fleet;
                }
            }
            return null;
        }

        /// <summary>
        /// Check if a unit is part of any fleet
        /// </summary>
        public bool IsUnitInFleet(string unitId)
        {
            return GetFleetContainingUnit(unitId) != null;
        }

        /// <summary>
        /// Remove a unit from all fleets (e.g., when unit is destroyed)
        /// </summary>
        public void RemoveUnitFromAllFleets(string unitId)
        {
            List<string> fleetsToRemove = new List<string>();

            foreach (var kvp in fleets)
            {
                Fleet fleet = kvp.Value;
                if (fleet.unitIds.Contains(unitId))
                {
                    fleet.RemoveUnit(unitId);
                    Debug.Log($"[FleetManager] Removed unit {unitId} from fleet {fleet.id}");

                    // Mark fleet for removal if it has less than 2 units
                    if (fleet.unitIds.Count < 2)
                    {
                        fleetsToRemove.Add(fleet.id);
                    }
                }
            }

            // Remove invalid fleets
            foreach (string fleetId in fleetsToRemove)
            {
                Debug.Log($"[FleetManager] Auto-disbanded fleet {fleetId} (less than 2 units remaining)");
                fleets.Remove(fleetId);
            }
        }

        /// <summary>
        /// Validate all fleets and auto-disband invalid ones
        /// Call this after movement/combat resolution
        /// </summary>
        public void ValidateFleets(UnitManager unitManager)
        {
            List<string> fleetsToRemove = new List<string>();

            foreach (var kvp in fleets)
            {
                Fleet fleet = kvp.Value;

                // Remove destroyed units from fleet
                List<string> unitsToRemove = new List<string>();
                foreach (string unitId in fleet.unitIds)
                {
                    if (unitManager.GetUnit(unitId) == null)
                    {
                        unitsToRemove.Add(unitId);
                    }
                }

                foreach (string unitId in unitsToRemove)
                {
                    fleet.RemoveUnit(unitId);
                }

                // Check if fleet is still valid
                if (!fleet.IsValid(unitManager))
                {
                    fleetsToRemove.Add(fleet.id);
                }
            }

            // Remove invalid fleets
            foreach (string fleetId in fleetsToRemove)
            {
                Fleet fleet = fleets[fleetId];
                Debug.Log($"[FleetManager] Auto-disbanded fleet {fleetId} (units separated or destroyed)");
                fleets.Remove(fleetId);
            }
        }

        /// <summary>
        /// Clear all fleets (for game reset)
        /// </summary>
        public void Clear()
        {
            fleets.Clear();
            nextFleetId = 0;
        }

        /// <summary>
        /// Get units at a position that can form a fleet
        /// </summary>
        public List<Unit> GetFleetableUnitsAtPosition(HexCoord position, int playerId, UnitManager unitManager)
        {
            List<Unit> unitsAtPosition = unitManager.GetUnitsAtPosition(position);

            // Filter to only units owned by the player that are not already in a fleet
            List<Unit> fleetableUnits = new List<Unit>();
            foreach (Unit unit in unitsAtPosition)
            {
                if (unit.ownerId == playerId && !IsUnitInFleet(unit.id))
                {
                    fleetableUnits.Add(unit);
                }
            }

            return fleetableUnits;
        }
    }
}
