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
                if (structure == null)
                {
                    toRemove.Add(structureId);
                }
            }

            foreach (string structureId in toRemove)
            {
                Destroy(buildingObjects[structureId]);
                buildingObjects.Remove(structureId);
            }

            // Create/update all structures
            int created = 0;
            int updated = 0;
            foreach (Structure structure in structureManager.GetAllStructures())
            {
                // Render all structure types
                // (SHIPYARD, NAVAL_YARD, NAVAL_FORTRESS, PIRATE_COVE)

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
                // Verbose logging disabled for performance (called every animation frame)
            // Debug.Log($"[BuildingRenderer] Rendered buildings: {created} created, {updated} updated, {buildingObjects.Count} total");
            }
        }

        private void CreateBuildingObject(Structure structure)
        {
            GameObject buildingObj = new GameObject($"Building_{structure.id}");
            buildingObj.transform.SetParent(buildingsContainer.transform);

            // Create structure based on type
            switch (structure.type)
            {
                case StructureType.SHIPYARD:
                    CreateShipyardVisual(buildingObj, structure);
                    break;
                case StructureType.NAVAL_YARD:
                    CreateNavalYardVisual(buildingObj, structure);
                    break;
                case StructureType.NAVAL_FORTRESS:
                    CreateNavalFortressVisual(buildingObj, structure);
                    break;
                case StructureType.PIRATE_COVE:
                    CreatePirateCoveVisual(buildingObj, structure);
                    break;
                default:
                    CreateShipyardVisual(buildingObj, structure); // Fallback to shipyard
                    break;
            }

            // Position the building
            Vector3 worldPos = structure.position.ToWorldPosition(hexSize);
            worldPos.y = buildingHeight;
            buildingObj.transform.position = worldPos;

            buildingObjects[structure.id] = buildingObj;

            Debug.Log($"[BuildingRenderer] Created {structure.type} {structure.id} (Player {structure.ownerId}) at {structure.position}");
        }

        private void CreateShipyardVisual(GameObject buildingObj, Structure structure)
        {
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
        }

        private void CreateNavalYardVisual(GameObject buildingObj, Structure structure)
        {
            // Naval Yard: Larger, more fortified than shipyard
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse") ?? Shader.Find("Unlit/Color");
            Color stoneColor = new Color(0.5f, 0.5f, 0.55f); // Stone grey

            // Larger base
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObj.name = "Base";
            baseObj.transform.SetParent(buildingObj.transform);
            baseObj.transform.localScale = new Vector3(1.0f, 0.3f, 1.0f);
            baseObj.transform.localPosition = Vector3.zero;
            Material baseMat = new Material(shader);
            baseMat.color = stoneColor;
            baseObj.GetComponent<MeshRenderer>().material = baseMat;

            // Two towers
            for (int i = 0; i < 2; i++)
            {
                GameObject towerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                towerObj.name = $"Tower{i}";
                towerObj.transform.SetParent(buildingObj.transform);
                towerObj.transform.localScale = new Vector3(0.3f, 0.5f, 0.3f);
                float xOffset = (i == 0) ? -0.3f : 0.3f;
                towerObj.transform.localPosition = new Vector3(xOffset, 0.4f, 0);
                Material towerMat = new Material(shader);
                towerMat.color = stoneColor;
                towerObj.GetComponent<MeshRenderer>().material = towerMat;
            }

            // Flag
            GameObject flagObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flagObj.name = "Flag";
            flagObj.transform.SetParent(buildingObj.transform);
            flagObj.transform.localScale = new Vector3(0.5f, 0.25f, 0.05f);
            flagObj.transform.localPosition = new Vector3(0, 0.85f, 0);
            Material flagMat = new Material(shader);
            flagMat.color = GetPlayerColor(structure.ownerId);
            flagObj.GetComponent<MeshRenderer>().material = flagMat;
        }

        private void CreateNavalFortressVisual(GameObject buildingObj, Structure structure)
        {
            // Naval Fortress: Massive, castle-like structure
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse") ?? Shader.Find("Unlit/Color");
            Color fortressColor = new Color(0.4f, 0.4f, 0.45f); // Dark stone grey

            // Large base
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObj.name = "Base";
            baseObj.transform.SetParent(buildingObj.transform);
            baseObj.transform.localScale = new Vector3(1.2f, 0.4f, 1.2f);
            baseObj.transform.localPosition = Vector3.zero;
            Material baseMat = new Material(shader);
            baseMat.color = fortressColor;
            baseObj.GetComponent<MeshRenderer>().material = baseMat;

            // Four corner towers
            for (int i = 0; i < 4; i++)
            {
                GameObject towerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                towerObj.name = $"Tower{i}";
                towerObj.transform.SetParent(buildingObj.transform);
                towerObj.transform.localScale = new Vector3(0.25f, 0.7f, 0.25f);
                float xOffset = (i % 2 == 0) ? -0.5f : 0.5f;
                float zOffset = (i < 2) ? -0.5f : 0.5f;
                towerObj.transform.localPosition = new Vector3(xOffset, 0.55f, zOffset);
                Material towerMat = new Material(shader);
                towerMat.color = fortressColor;
                towerObj.GetComponent<MeshRenderer>().material = towerMat;
            }

            // Central keep
            GameObject keepObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            keepObj.name = "Keep";
            keepObj.transform.SetParent(buildingObj.transform);
            keepObj.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
            keepObj.transform.localPosition = new Vector3(0, 0.5f, 0);
            Material keepMat = new Material(shader);
            keepMat.color = fortressColor;
            keepObj.GetComponent<MeshRenderer>().material = keepMat;

            // Flag
            GameObject flagObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flagObj.name = "Flag";
            flagObj.transform.SetParent(buildingObj.transform);
            flagObj.transform.localScale = new Vector3(0.6f, 0.3f, 0.05f);
            flagObj.transform.localPosition = new Vector3(0, 1.2f, 0);
            Material flagMat = new Material(shader);
            flagMat.color = GetPlayerColor(structure.ownerId);
            flagObj.GetComponent<MeshRenderer>().material = flagMat;
        }

        private void CreatePirateCoveVisual(GameObject buildingObj, Structure structure)
        {
            // Pirate Cove: Dark, menacing structure
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse") ?? Shader.Find("Unlit/Color");
            Color darkColor = new Color(0.15f, 0.1f, 0.1f); // Very dark brown/black
            Color skullColor = Color.white;

            // Dark base
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObj.name = "Base";
            baseObj.transform.SetParent(buildingObj.transform);
            baseObj.transform.localScale = new Vector3(0.9f, 0.25f, 0.9f);
            baseObj.transform.localPosition = Vector3.zero;
            Material baseMat = new Material(shader);
            baseMat.color = darkColor;
            baseObj.GetComponent<MeshRenderer>().material = baseMat;

            // Tower with skull
            GameObject towerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            towerObj.name = "Tower";
            towerObj.transform.SetParent(buildingObj.transform);
            towerObj.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
            towerObj.transform.localPosition = new Vector3(0, 0.35f, 0);
            Material towerMat = new Material(shader);
            towerMat.color = darkColor;
            towerObj.GetComponent<MeshRenderer>().material = towerMat;

            // Skull symbol (sphere as simplified skull)
            GameObject skullObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            skullObj.name = "Skull";
            skullObj.transform.SetParent(buildingObj.transform);
            skullObj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            skullObj.transform.localPosition = new Vector3(0, 0.75f, 0);
            Material skullMat = new Material(shader);
            skullMat.color = skullColor;
            skullObj.GetComponent<MeshRenderer>().material = skullMat;

            // Black flag (pirate colors)
            GameObject flagObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flagObj.name = "Flag";
            flagObj.transform.SetParent(buildingObj.transform);
            flagObj.transform.localScale = new Vector3(0.45f, 0.25f, 0.05f);
            flagObj.transform.localPosition = new Vector3(0.25f, 0.9f, 0);
            Material flagMat = new Material(shader);
            flagMat.color = Color.black;
            flagObj.GetComponent<MeshRenderer>().material = flagMat;
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
