using System;
using System.Collections.Generic;
using System.Linq;
using PlunkAndPlunder.Construction;
using PlunkAndPlunder.Map;

namespace PlunkAndPlunder.Structures
{
    [Serializable]
    public class StructureManager
    {
        public Dictionary<string, Structure> structures = new Dictionary<string, Structure>();
        private int nextStructureId = 0;

        public Structure CreateStructure(int ownerId, HexCoord position, StructureType type)
        {
            string id = $"structure_{nextStructureId++}";
            Structure structure = new Structure(id, ownerId, position, type);
            structures[id] = structure;

            // Register shipyards with ConstructionManager
            if (type == StructureType.SHIPYARD && ConstructionManager.Instance != null)
            {
                ConstructionManager.Instance.RegisterShipyard(id);
            }

            return structure;
        }

        public Structure GetStructure(string id)
        {
            structures.TryGetValue(id, out Structure structure);
            return structure;
        }

        public Structure GetStructureAtPosition(HexCoord position)
        {
            return structures.Values.FirstOrDefault(s => s.position.Equals(position));
        }

        public void RemoveStructure(string id)
        {
            structures.Remove(id);
        }

        public List<Structure> GetStructuresForPlayer(int playerId)
        {
            return structures.Values.Where(s => s.ownerId == playerId).ToList();
        }

        public List<Structure> GetAllStructures()
        {
            return new List<Structure>(structures.Values);
        }

        public void ChangeOwner(string structureId, int newOwnerId)
        {
            Structure structure = GetStructure(structureId);
            if (structure != null)
            {
                structure.ownerId = newOwnerId;
            }
        }
    }
}
