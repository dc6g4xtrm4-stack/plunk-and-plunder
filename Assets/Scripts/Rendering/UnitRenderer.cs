using System.Collections.Generic;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Units;
using UnityEngine;

namespace PlunkAndPlunder.Rendering
{
    /// <summary>
    /// Renders units as simple 3D primitives
    /// </summary>
    public class UnitRenderer : MonoBehaviour
    {
        [Header("Settings")]
        public float hexSize = 1f;
        public float unitHeight = 0.3f;

        [Header("Player Colors")]
        public Color[] playerColors = new Color[]
        {
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow
        };

        private Dictionary<string, GameObject> unitObjects = new Dictionary<string, GameObject>();
        private GameObject unitsContainer;

        private void Awake()
        {
            unitsContainer = new GameObject("Units");
            unitsContainer.transform.SetParent(transform);
        }

        public void RenderUnits(UnitManager unitManager)
        {
            // Remove destroyed units
            List<string> toRemove = new List<string>();
            foreach (string unitId in unitObjects.Keys)
            {
                if (unitManager.GetUnit(unitId) == null)
                {
                    toRemove.Add(unitId);
                }
            }

            foreach (string unitId in toRemove)
            {
                Destroy(unitObjects[unitId]);
                unitObjects.Remove(unitId);
            }

            // Create/update units
            int created = 0;
            int updated = 0;
            foreach (Unit unit in unitManager.GetAllUnits())
            {
                if (!unitObjects.ContainsKey(unit.id))
                {
                    CreateUnitObject(unit);
                    created++;
                    Vector3 worldPos = unit.position.ToWorldPosition(hexSize);
                    Debug.Log($"[UnitRenderer] Created unit {unit.id} (Player {unit.ownerId}) at world pos {worldPos}");
                }
                else
                {
                    UpdateUnitPosition(unit);
                    updated++;
                }
            }

            if (created > 0 || updated > 0)
            {
                Debug.Log($"[UnitRenderer] Rendered units: {created} created, {updated} updated, {unitObjects.Count} total");
            }
        }

        private void CreateUnitObject(Unit unit)
        {
            // Create parent container for the ship
            GameObject unitObj = new GameObject($"Unit_{unit.id}");
            unitObj.transform.SetParent(unitsContainer.transform);

            // Position
            Vector3 worldPos = unit.position.ToWorldPosition(hexSize);
            worldPos.y = unitHeight;
            unitObj.transform.position = worldPos;

            // Rotation based on facing
            unitObj.transform.rotation = Quaternion.Euler(0, unit.facingAngle, 0);

            // Create ship model
            CreateShipModel(unitObj, unit.ownerId);

            unitObjects[unit.id] = unitObj;
        }

        private void CreateShipModel(GameObject parent, int ownerId)
        {
            Color playerColor = GetPlayerColor(ownerId);
            Color brownHull = new Color(0.4f, 0.25f, 0.1f); // Brown wood color
            Color sailColor = new Color(0.9f, 0.85f, 0.75f); // Off-white sail

            // 1. Create Hull (brown boat body) - stretched cube
            GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Hull";
            hull.transform.SetParent(parent.transform);
            hull.transform.localPosition = new Vector3(0, 0, 0);
            hull.transform.localScale = new Vector3(0.5f, 0.15f, 0.3f); // Wide and long
            hull.transform.localRotation = Quaternion.identity;
            SetMaterialColor(hull, brownHull);

            // 2. Create Bow (front pointed part) - scaled cube rotated to create point
            GameObject bow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bow.name = "Bow";
            bow.transform.SetParent(parent.transform);
            bow.transform.localPosition = new Vector3(0.35f, 0, 0);
            bow.transform.localScale = new Vector3(0.2f, 0.12f, 0.2f);
            bow.transform.localRotation = Quaternion.Euler(0, 45, 0); // Angled to create pointed bow
            SetMaterialColor(bow, brownHull);

            // 3. Create Mast (vertical pole) - thin cylinder
            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Mast";
            mast.transform.SetParent(parent.transform);
            mast.transform.localPosition = new Vector3(0, 0.35f, 0);
            mast.transform.localScale = new Vector3(0.04f, 0.35f, 0.04f); // Thin and tall
            SetMaterialColor(mast, brownHull);

            // 4. Create Sail (white/cream colored cube) - flat and wide
            GameObject sail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sail.name = "Sail";
            sail.transform.SetParent(parent.transform);
            sail.transform.localPosition = new Vector3(0, 0.35f, 0);
            sail.transform.localScale = new Vector3(0.3f, 0.4f, 0.05f); // Flat rectangle
            SetMaterialColor(sail, sailColor);

            // 5. Create Flag at top - small cube with player color
            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Flag";
            flag.transform.SetParent(parent.transform);
            flag.transform.localPosition = new Vector3(0, 0.65f, 0); // At top of mast
            flag.transform.localScale = new Vector3(0.15f, 0.1f, 0.02f); // Small flag
            SetMaterialColor(flag, playerColor);

            // 6. Create flag pole (tiny cylinder connecting mast to flag)
            GameObject flagPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flagPole.name = "FlagPole";
            flagPole.transform.SetParent(parent.transform);
            flagPole.transform.localPosition = new Vector3(0, 0.6f, 0);
            flagPole.transform.localScale = new Vector3(0.02f, 0.08f, 0.02f);
            SetMaterialColor(flagPole, brownHull);
        }

        private void SetMaterialColor(GameObject obj, Color color)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                renderer.material = mat;
            }
        }

        private void UpdateUnitPosition(Unit unit)
        {
            if (unitObjects.TryGetValue(unit.id, out GameObject unitObj))
            {
                Vector3 worldPos = unit.position.ToWorldPosition(hexSize);
                worldPos.y = unitHeight;
                unitObj.transform.position = worldPos;

                // Update rotation based on facing direction
                unitObj.transform.rotation = Quaternion.Euler(0, unit.facingAngle, 0);
            }
        }

        public void HighlightUnit(string unitId, bool highlight)
        {
            if (unitObjects.TryGetValue(unitId, out GameObject unitObj))
            {
                // Get all mesh renderers in the ship (hull, sail, flag, etc.)
                MeshRenderer[] renderers = unitObj.GetComponentsInChildren<MeshRenderer>();

                if (highlight)
                {
                    // Brighten all parts when highlighted
                    foreach (MeshRenderer renderer in renderers)
                    {
                        Color originalColor = renderer.material.color;
                        renderer.material.color = Color.Lerp(originalColor, Color.white, 0.5f);
                    }
                }
                else
                {
                    // Rebuild the ship to restore original colors
                    Unit unit = GameManager.Instance?.state?.unitManager?.GetUnit(unitId);
                    if (unit != null)
                    {
                        // Destroy old ship parts
                        foreach (Transform child in unitObj.transform)
                        {
                            Destroy(child.gameObject);
                        }

                        // Recreate ship with correct colors
                        CreateShipModel(unitObj, unit.ownerId);
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
            return Color.gray;
        }

        private void OnDestroy()
        {
            foreach (GameObject unitObj in unitObjects.Values)
            {
                Destroy(unitObj);
            }
            unitObjects.Clear();
        }
    }
}
