using System;
using System.Collections.Generic;
using PlunkAndPlunder.Map;

namespace PlunkAndPlunder.Units
{
    /// <summary>
    /// Represents a fleet - a group of units that can be commanded together.
    /// Fleets are purely a QoL feature for issuing orders to multiple ships at once.
    /// Each ship in a fleet still maintains its own stats, health, and acts independently in combat.
    /// </summary>
    [Serializable]
    public class Fleet
    {
        public string id;
        public int ownerId;
        public List<string> unitIds;
        public string name; // Optional fleet name (e.g., "Fleet Alpha")

        public Fleet(string id, int ownerId, List<string> unitIds)
        {
            this.id = id;
            this.ownerId = ownerId;
            this.unitIds = new List<string>(unitIds);
            this.name = $"Fleet {id.Substring(0, Math.Min(4, id.Length))}";
        }

        /// <summary>
        /// Get the position of the fleet (position of first unit, or null if no units)
        /// </summary>
        public HexCoord? GetPosition(UnitManager unitManager)
        {
            if (unitIds.Count == 0) return null;

            Unit firstUnit = unitManager.GetUnit(unitIds[0]);
            return firstUnit?.position;
        }

        /// <summary>
        /// Get all units in this fleet
        /// </summary>
        public List<Unit> GetUnits(UnitManager unitManager)
        {
            List<Unit> units = new List<Unit>();
            foreach (string unitId in unitIds)
            {
                Unit unit = unitManager.GetUnit(unitId);
                if (unit != null)
                {
                    units.Add(unit);
                }
            }
            return units;
        }

        /// <summary>
        /// Add a unit to the fleet
        /// </summary>
        public void AddUnit(string unitId)
        {
            if (!unitIds.Contains(unitId))
            {
                unitIds.Add(unitId);
            }
        }

        /// <summary>
        /// Remove a unit from the fleet
        /// </summary>
        public void RemoveUnit(string unitId)
        {
            unitIds.Remove(unitId);
        }

        /// <summary>
        /// Check if fleet is valid (has at least 2 units at same position)
        /// </summary>
        public bool IsValid(UnitManager unitManager)
        {
            if (unitIds.Count < 2) return false;

            // Get position of first unit
            Unit firstUnit = unitManager.GetUnit(unitIds[0]);
            if (firstUnit == null) return false;

            HexCoord fleetPosition = firstUnit.position;

            // Verify all units exist and are at the same position
            foreach (string unitId in unitIds)
            {
                Unit unit = unitManager.GetUnit(unitId);
                if (unit == null || !unit.position.Equals(fleetPosition))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get total ship count in fleet
        /// </summary>
        public int GetShipCount(UnitManager unitManager)
        {
            int count = 0;
            foreach (string unitId in unitIds)
            {
                if (unitManager.GetUnit(unitId) != null)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
