using System.Collections.Generic;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Structures;
using UnityEngine;

namespace PlunkAndPlunder.Rendering
{
    /// <summary>
    /// Renders buildings (shipyards) as distinct 3D structures
    /// </summary>
    public class BuildingRenderer : MonoBehaviour
    {
        [Header("Settings")]
        public float hexSize = 1f;
        public float buildingHeight = 0.6f;

        [Header("Player Colors")]
        public Color[] playerColors = new Color[]
        {
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow
        };

        private Dictionary<string, GameObject> buildingObjects = new Dictionary<string, GameObject>();
        private GameObject buildingsContainer;

        private void Awake()
        {
            buildingsContainer = new GameObject("Buildings");
            buildingsContainer.transform.SetParent(transform);
        }

        public void RenderBuildings(StructureManager structureManager)
        {
            // Remove destroyed buildings
            List<string> toRemove = new List<string>();
            foreach (string structureId in buildingObjects.Keys)
            {
                Structure structure = structureManager.GetStructure(structureId);
                if (structure == null || structure.type != StructureType.SHIPYARD)
                {
                    toRemove.Add(structureId);
                }
            }

            foreach (string structureId in toRemove)
            {
                Destroy(buildingObjects[structureId]);
                buildingObjects.Remove(structureId);
            }

            // Create/update shipyards
            int created = 0;
            int updated = 0;
            foreach (Structure structure in structureManager.GetAllStructures())
            {
                // Only render shipyards
                if (structure.type != StructureType.SHIPYARD)
                    continue;

                if (!buildingObjects.ContainsKey(structure.id))
                {
                    CreateBuildingObject(structure);
                    created++;
                }
                else
                {
                    UpdateBuildingOwner(structure);
                    updated++;
                }
            }

            if (created > 0 || updated > 0)
            {
                Debug.Log($"[BuildingRenderer] Rendered buildings: {created} created, {updated} updated, {buildingObjects.Count} total");
            }
        }

        private void CreateBuildingObject(Structure structure)
        {
            GameObject buildingObj = new GameObject($"Building_{structure.id}");
            buildingObj.transform.SetParent(buildingsContainer.transform);

            // Create shipyard as a composite structure: base + tower + flag
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObj.name = "Base";
            baseObj.transform.SetParent(buildingObj.transform);
            baseObj.transform.localScale = new Vector3(0.8f, 0.2f, 0.8f);
            baseObj.transform.localPosition = Vector3.zero;

            GameObject towerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            towerObj.name = "Tower";
            towerObj.transform.SetParent(buildingObj.transform);
            towerObj.transform.localScale = new Vector3(0.3f, 0.4f, 0.3f);
            towerObj.transform.localPosition = new Vector3(0, 0.3f, 0);

            GameObject flagObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flagObj.name = "Flag";
            flagObj.transform.SetParent(buildingObj.transform);
            flagObj.transform.localScale = new Vector3(0.4f, 0.2f, 0.05f);
            flagObj.transform.localPosition = new Vector3(0.2f, 0.65f, 0);

            // Position the building
            Vector3 worldPos = structure.position.ToWorldPosition(hexSize);
            worldPos.y = buildingHeight;
            buildingObj.transform.position = worldPos;

            // Color by player (brown base, player-colored flag)
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse") ?? Shader.Find("Unlit/Color");
            Color brownColor = new Color(0.6f, 0.4f, 0.2f);

            Material baseMat = new Material(shader);
            baseMat.color = brownColor;
            baseObj.GetComponent<MeshRenderer>().material = baseMat;

            Material towerMat = new Material(shader);
            towerMat.color = brownColor;
            towerObj.GetComponent<MeshRenderer>().material = towerMat;

            Material flagMat = new Material(shader);
            flagMat.color = GetPlayerColor(structure.ownerId);
            flagObj.GetComponent<MeshRenderer>().material = flagMat;

            buildingObjects[structure.id] = buildingObj;

            Debug.Log($"[BuildingRenderer] Created shipyard {structure.id} (Player {structure.ownerId}) at {structure.position}");
        }

        private void UpdateBuildingOwner(Structure structure)
        {
            if (buildingObjects.TryGetValue(structure.id, out GameObject buildingObj))
            {
                // Update flag color when owner changes
                Transform flagTransform = buildingObj.transform.Find("Flag");
                if (flagTransform != null)
                {
                    MeshRenderer renderer = flagTransform.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = GetPlayerColor(structure.ownerId);
                    }
                }
            }
        }

        public void HighlightBuilding(string structureId, bool highlight)
        {
            if (buildingObjects.TryGetValue(structureId, out GameObject buildingObj))
            {
                Transform flagTransform = buildingObj.transform.Find("Flag");
                if (flagTransform != null)
                {
                    MeshRenderer renderer = flagTransform.GetComponent<MeshRenderer>();
                    if (renderer != null)
                    {
                        if (highlight)
                        {
                            renderer.material.color = Color.white;
                        }
                        else
                        {
                            // Reset to player color
                            Structure structure = GameManager.Instance?.state?.structureManager?.GetStructure(structureId);
                            if (structure != null)
                            {
                                renderer.material.color = GetPlayerColor(structure.ownerId);
                            }
                        }
                    }
                }
            }
        }

        private Color GetPlayerColor(int playerId)
        {
            if (playerId >= 0 && playerId < playerColors.Length)
            {
                return playerColors[playerId];
            }
            return Color.gray; // Neutral color
        }

        public void AddTemporaryHighlight(string structureId, float duration = 4f)
        {
            if (buildingObjects.TryGetValue(structureId, out GameObject buildingObj))
            {
                // Add or get the highlight effect component
                TemporaryHighlight highlight = buildingObj.GetComponent<TemporaryHighlight>();
                if (highlight == null)
                {
                    highlight = buildingObj.AddComponent<TemporaryHighlight>();
                }
                highlight.StartHighlight(duration);
            }
        }

        private void OnDestroy()
        {
            foreach (GameObject buildingObj in buildingObjects.Values)
            {
                Destroy(buildingObj);
            }
            buildingObjects.Clear();
        }
    }
}
