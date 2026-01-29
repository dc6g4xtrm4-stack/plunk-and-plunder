using System;
using System.Collections.Generic;
using System.Linq;
using PlunkAndPlunder.Map;

namespace PlunkAndPlunder.Units
{
    [Serializable]
    public class UnitManager
    {
        public Dictionary<string, Unit> units = new Dictionary<string, Unit>();
        private int nextUnitId = 0;

        public Unit CreateUnit(int ownerId, HexCoord position, UnitType type)
        {
            string id = $"unit_{nextUnitId++}";
            Unit unit = new Unit(id, ownerId, position, type);
            units[id] = unit;
            return unit;
        }

        public Unit GetUnit(string id)
        {
            units.TryGetValue(id, out Unit unit);
            return unit;
        }

        public void RemoveUnit(string id)
        {
            units.Remove(id);
        }

        public List<Unit> GetUnitsAtPosition(HexCoord position)
        {
            return units.Values.Where(u => u.position.Equals(position)).ToList();
        }

        public List<Unit> GetUnitsForPlayer(int playerId)
        {
            return units.Values.Where(u => u.ownerId == playerId).ToList();
        }

        public List<Unit> GetAllUnits()
        {
            return new List<Unit>(units.Values);
        }

        public void MoveUnit(string unitId, HexCoord newPosition)
        {
            Unit unit = GetUnit(unitId);
            if (unit != null)
            {
                HexCoord oldPosition = unit.position;
                unit.position = newPosition;
                unit.UpdateFacing(oldPosition, newPosition);
            }
        }
    }
}
