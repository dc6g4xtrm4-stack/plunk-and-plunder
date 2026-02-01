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
        private Dictionary<string, GameObject> selectionIndicators = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> healthBars = new Dictionary<string, GameObject>();
        private Dictionary<string, int> unitUpgradeLevels = new Dictionary<string, int>(); // Track upgrade level for each unit
        private GameObject unitsContainer;

        private void Awake()
        {
            unitsContainer = new GameObject("Units");
            unitsContainer.transform.SetParent(transform);
        }

        public void RenderUnits(UnitManager unitManager)
        {
            // Verbose logging disabled for performance (called every animation frame)
            // Debug.Log($"[UnitRenderer] RenderUnits called with {unitManager.GetAllUnits().Count} units");

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
                unitUpgradeLevels.Remove(unitId);

                // Clean up health bar
                if (healthBars.ContainsKey(unitId))
                {
                    Destroy(healthBars[unitId]);
                    healthBars.Remove(unitId);
                }
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
                    UpdateUnitVisual(unit);
                    updated++;
                }
            }

            // Apply stacking offsets for units on the same tile
            ApplyStackingOffsets(unitManager);

            // Verbose logging disabled for performance (called every animation frame)
            // if (created > 0 || updated > 0)
            // {
            //     Debug.Log($"[UnitRenderer] Rendered units: {created} created, {updated} updated, {unitObjects.Count} total");
            // }
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

            // Create ship model with dynamic visual tiers
            CreateShipModel(unitObj, unit);
            unitUpgradeLevels[unit.id] = unit.maxHealth;

            // Create health bar
            CreateHealthBar(unit, unitObj);

            unitObjects[unit.id] = unitObj;
        }

        private void CreateShipModel(GameObject parent, Unit unit)
        {
            Color playerColor = GetPlayerColor(unit.ownerId);
            Color brownHull = new Color(0.4f, 0.25f, 0.1f); // Brown wood color
            Color darkHull = new Color(0.3f, 0.2f, 0.08f); // Darker hull for cannons
            Color sailColor = playerColor; // Sails use player color

            // Calculate visual tiers based on upgrades
            int sailTier = GetSailVisualTier(unit.sails);
            int cannonTier = GetCannonVisualTier(unit.cannons);
            float scaleMultiplier = GetShipScale(unit);

            // Determine health tier for mast count (1-3 masts)
            int healthTier = 1;
            if (unit.maxHealth >= 21) healthTier = 3;
            else if (unit.maxHealth >= 11) healthTier = 2;

            // 1. Create Hull (brown boat body) - stretched cube, scales with upgrade
            GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Hull";
            hull.transform.SetParent(parent.transform);
            hull.transform.localPosition = new Vector3(0, 0, 0);
            hull.transform.localScale = new Vector3(0.5f * scaleMultiplier, 0.15f, 0.3f * scaleMultiplier);
            hull.transform.localRotation = Quaternion.identity;
            SetMaterialColor(hull, brownHull);

            // 2. Create Bow (front pointed part)
            GameObject bow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bow.name = "Bow";
            bow.transform.SetParent(parent.transform);
            bow.transform.localPosition = new Vector3(0.35f * scaleMultiplier, 0, 0);
            bow.transform.localScale = new Vector3(0.2f * scaleMultiplier, 0.12f, 0.2f * scaleMultiplier);
            bow.transform.localRotation = Quaternion.Euler(0, 45, 0);
            SetMaterialColor(bow, brownHull);

            // 3. Create Mast(s) - number depends on health tier, visual depends on sail tier
            if (healthTier == 1)
            {
                // Tier 1: Single mast
                CreateMastWithSail(parent, new Vector3(0, 0.35f, 0), 0.35f, sailColor, playerColor, brownHull, scaleMultiplier, sailTier);
            }
            else if (healthTier == 2)
            {
                // Tier 2: Two masts
                CreateMastWithSail(parent, new Vector3(-0.15f * scaleMultiplier, 0.35f, 0), 0.35f, sailColor, playerColor, brownHull, scaleMultiplier, sailTier);
                CreateMastWithSail(parent, new Vector3(0.15f * scaleMultiplier, 0.35f, 0), 0.35f, sailColor, playerColor, brownHull, scaleMultiplier, sailTier);

                // Add cannons based on cannon tier
                int cannonsToRender = GetCannonsPerSide(cannonTier);
                CreateCannons(parent, cannonsToRender, scaleMultiplier, darkHull);
            }
            else if (healthTier >= 3)
            {
                // Tier 3: Three masts
                CreateMastWithSail(parent, new Vector3(-0.25f * scaleMultiplier, 0.35f, 0), 0.35f, sailColor, playerColor, brownHull, scaleMultiplier, sailTier);
                CreateMastWithSail(parent, new Vector3(0, 0.4f, 0), 0.4f, sailColor, playerColor, brownHull, scaleMultiplier, sailTier);
                CreateMastWithSail(parent, new Vector3(0.25f * scaleMultiplier, 0.35f, 0), 0.35f, sailColor, playerColor, brownHull, scaleMultiplier, sailTier);

                // Add cannons based on cannon tier
                int cannonsToRender = GetCannonsPerSide(cannonTier);
                CreateCannons(parent, cannonsToRender, scaleMultiplier, darkHull);

                // Add decorative elements - additional flags
                CreateExtraFlags(parent, 2, scaleMultiplier, playerColor);
            }
        }

        private void CreateMastWithSail(GameObject parent, Vector3 position, float mastHeight, Color sailColor, Color flagColor, Color mastColor, float scale, int sailTier)
        {
            // Mast (vertical pole)
            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Mast";
            mast.transform.SetParent(parent.transform);
            mast.transform.localPosition = position;
            mast.transform.localScale = new Vector3(0.04f, mastHeight, 0.04f);
            SetMaterialColor(mast, mastColor);

            // Sail - size and shape varies by tier
            GameObject sail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sail.name = "Sail";
            sail.transform.SetParent(parent.transform);
            sail.transform.localPosition = position;

            // Tier-based sail dimensions
            switch (sailTier)
            {
                case 1: // Small square sails (0-1 sail upgrades)
                    sail.transform.localScale = new Vector3(0.3f * scale, 0.35f, 0.05f);
                    break;
                case 2: // Larger triangular-style sails (2-3 sail upgrades)
                    sail.transform.localScale = new Vector3(0.4f * scale, 0.5f, 0.06f);
                    // Make sail slightly transparent for "billowing" effect
                    break;
                case 3: // Massive billowing sails (4-5 sail upgrades)
                    sail.transform.localScale = new Vector3(0.5f * scale, 0.65f, 0.08f);
                    // Add secondary sail layer for depth
                    GameObject sailLayer2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    sailLayer2.name = "Sail_Layer2";
                    sailLayer2.transform.SetParent(parent.transform);
                    sailLayer2.transform.localPosition = position + new Vector3(0, 0, -0.06f);
                    sailLayer2.transform.localScale = new Vector3(0.45f * scale, 0.6f, 0.06f);
                    Color layer2Color = new Color(sailColor.r * 0.9f, sailColor.g * 0.9f, sailColor.b * 0.9f, 0.8f);
                    SetMaterialColor(sailLayer2, layer2Color);
                    break;
            }
            SetMaterialColor(sail, sailColor);

            // Flag at top
            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Flag";
            flag.transform.SetParent(parent.transform);
            flag.transform.localPosition = position + new Vector3(0, mastHeight + 0.25f, 0);
            flag.transform.localScale = new Vector3(0.08f, 0.06f, 0.02f);
            SetMaterialColor(flag, flagColor);

            // Flag pole
            GameObject flagPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flagPole.name = "FlagPole";
            flagPole.transform.SetParent(parent.transform);
            flagPole.transform.localPosition = position + new Vector3(0, mastHeight + 0.2f, 0);
            flagPole.transform.localScale = new Vector3(0.02f, 0.08f, 0.02f);
            SetMaterialColor(flagPole, mastColor);
        }

        private void CreateCannons(GameObject parent, int count, float scale, Color cannonColor)
        {
            // Create small dark cylinders protruding from sides as cannons
            float spacing = 0.3f * scale / Mathf.Max(1, count - 1);
            float startZ = -0.15f * scale;

            for (int i = 0; i < count; i++)
            {
                float zPos = startZ + (i * spacing);

                // Left side cannon
                GameObject cannonLeft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cannonLeft.name = $"Cannon_L_{i}";
                cannonLeft.transform.SetParent(parent.transform);
                cannonLeft.transform.localPosition = new Vector3(-0.25f * scale, 0.05f, zPos);
                cannonLeft.transform.localRotation = Quaternion.Euler(0, 0, 90); // Rotate to point sideways
                cannonLeft.transform.localScale = new Vector3(0.03f, 0.08f, 0.03f);
                SetMaterialColor(cannonLeft, cannonColor);

                // Right side cannon
                GameObject cannonRight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cannonRight.name = $"Cannon_R_{i}";
                cannonRight.transform.SetParent(parent.transform);
                cannonRight.transform.localPosition = new Vector3(0.25f * scale, 0.05f, zPos);
                cannonRight.transform.localRotation = Quaternion.Euler(0, 0, 90);
                cannonRight.transform.localScale = new Vector3(0.03f, 0.08f, 0.03f);
                SetMaterialColor(cannonRight, cannonColor);
            }
        }

        private void CreateExtraFlags(GameObject parent, int count, float scale, Color flagColor)
        {
            // Add decorative flags on the hull sides
            for (int i = 0; i < count; i++)
            {
                float xPos = -0.2f * scale + (i * 0.4f * scale);

                GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
                flag.name = $"DecorFlag_{i}";
                flag.transform.SetParent(parent.transform);
                flag.transform.localPosition = new Vector3(xPos, 0.25f, -0.18f * scale);
                flag.transform.localScale = new Vector3(0.06f, 0.05f, 0.02f);
                SetMaterialColor(flag, flagColor);
            }
        }

        private void SetMaterialColor(GameObject obj, Color color)
        {
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse") ?? Shader.Find("Unlit/Color");
                Material mat = new Material(shader);
                mat.color = color;
                renderer.material = mat;
            }
        }

        private void UpdateUnitVisual(Unit unit)
        {
            if (unitObjects.TryGetValue(unit.id, out GameObject unitObj))
            {
                Vector3 worldPos = unit.position.ToWorldPosition(hexSize);
                worldPos.y = unitHeight;
                unitObj.transform.position = worldPos;
                // Verbose logging disabled for performance (called every animation frame)
                // Debug.Log($"[UnitRenderer] Updated unit {unit.id} position to {unit.position} (world: {worldPos})");

                // Update rotation based on facing direction
                unitObj.transform.rotation = Quaternion.Euler(0, unit.facingAngle, 0);

                // Check if upgrade level changed (ship was upgraded)
                int currentUpgradeLevel = unit.maxHealth;
                if (unitUpgradeLevels.TryGetValue(unit.id, out int previousUpgradeLevel))
                {
                    if (currentUpgradeLevel != previousUpgradeLevel)
                    {
                        // Ship was upgraded! Rebuild the visual
                        Debug.Log($"[UnitRenderer] Ship {unit.id} upgraded from tier {previousUpgradeLevel} to tier {currentUpgradeLevel}");

                        // Destroy old ship parts
                        foreach (Transform child in unitObj.transform)
                        {
                            Destroy(child.gameObject);
                        }

                        // Recreate ship with new upgrade level
                        CreateShipModel(unitObj, unit.ownerId, currentUpgradeLevel);
                        unitUpgradeLevels[unit.id] = currentUpgradeLevel;
                    }
                }

                // Update health bar
                UpdateHealthBar(unit);

                // Update selection indicator position if it exists
                if (selectionIndicators.TryGetValue(unit.id, out GameObject indicator))
                {
                    indicator.transform.position = new Vector3(
                        unitObj.transform.position.x,
                        0.05f,
                        unitObj.transform.position.z
                    );
                }
            }
        }

        /// <summary>
        /// Apply visual offsets to ships that occupy the same tile
        /// Ships from the same player get stacked with visible offsets
        /// </summary>
        private void ApplyStackingOffsets(UnitManager unitManager)
        {
            // Group units by their hex position
            Dictionary<HexCoord, List<Unit>> unitsByPosition = new Dictionary<HexCoord, List<Unit>>();

            foreach (Unit unit in unitManager.GetAllUnits())
            {
                if (!unitsByPosition.ContainsKey(unit.position))
                {
                    unitsByPosition[unit.position] = new List<Unit>();
                }
                unitsByPosition[unit.position].Add(unit);
            }

            // Apply offsets to stacked units
            foreach (var kvp in unitsByPosition)
            {
                HexCoord position = kvp.Key;
                List<Unit> unitsAtPosition = kvp.Value;

                // Only apply offsets if multiple units at same position
                if (unitsAtPosition.Count > 1)
                {
                    // Calculate offset pattern based on number of units
                    float offsetRadius = 0.3f; // How far from center to offset

                    for (int i = 0; i < unitsAtPosition.Count; i++)
                    {
                        Unit unit = unitsAtPosition[i];
                        if (!unitObjects.TryGetValue(unit.id, out GameObject unitObj))
                            continue;

                        Vector3 basePos = position.ToWorldPosition(hexSize);
                        basePos.y = unitHeight;

                        // Calculate offset in a circular pattern
                        if (unitsAtPosition.Count == 2)
                        {
                            // For 2 ships: offset left and right
                            float offsetX = (i == 0) ? -offsetRadius : offsetRadius;
                            basePos.x += offsetX;
                        }
                        else if (unitsAtPosition.Count == 3)
                        {
                            // For 3 ships: triangle formation
                            float angle = (i * 120f) * Mathf.Deg2Rad;
                            basePos.x += Mathf.Cos(angle) * offsetRadius;
                            basePos.z += Mathf.Sin(angle) * offsetRadius;
                        }
                        else
                        {
                            // For 4+ ships: circular formation
                            float angle = (i * 360f / unitsAtPosition.Count) * Mathf.Deg2Rad;
                            basePos.x += Mathf.Cos(angle) * offsetRadius;
                            basePos.z += Mathf.Sin(angle) * offsetRadius;
                        }

                        unitObj.transform.position = basePos;

                        // Also update selection indicator position if it exists
                        if (selectionIndicators.TryGetValue(unit.id, out GameObject indicator))
                        {
                            indicator.transform.position = new Vector3(
                                basePos.x,
                                0.05f,
                                basePos.z
                            );
                        }
                    }
                }
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

                        // Recreate ship with correct colors and upgrade level
                        int upgradeLevel = unit.maxHealth;
                        CreateShipModel(unitObj, unit.ownerId, upgradeLevel);
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

        public void AddTemporaryHighlight(string unitId, float duration = 4f)
        {
            if (unitObjects.TryGetValue(unitId, out GameObject unitObj))
            {
                // Add or get the highlight effect component
                TemporaryHighlight highlight = unitObj.GetComponent<TemporaryHighlight>();
                if (highlight == null)
                {
                    highlight = unitObj.AddComponent<TemporaryHighlight>();
                }
                highlight.StartHighlight(duration);
            }
        }

        /// <summary>
        /// Show a selection indicator under the specified unit
        /// </summary>
        public void ShowSelectionIndicator(string unitId)
        {
            if (!unitObjects.TryGetValue(unitId, out GameObject unitObj))
                return;

            // Check if indicator already exists for this unit
            if (selectionIndicators.ContainsKey(unitId))
                return;

            // Create selection indicator - a glowing ring at the base
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            indicator.name = $"SelectionIndicator_{unitId}";

            // Position at base of unit
            indicator.transform.position = new Vector3(
                unitObj.transform.position.x,
                0.05f, // Just above water
                unitObj.transform.position.z
            );

            // Make it a flat ring shape
            indicator.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);

            // Set bright cyan/white color
            MeshRenderer renderer = indicator.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse") ?? Shader.Find("Unlit/Color");
                Material mat = new Material(shader);
                mat.color = new Color(0f, 1f, 1f, 0.8f); // Bright cyan

                // Enable emission if Standard shader
                if (shader.name == "Standard")
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0f, 0.5f, 0.5f, 1f));
                }

                renderer.material = mat;
            }

            // Remove collider
            Collider collider = indicator.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            selectionIndicators[unitId] = indicator;

            // Add pulsing animation
            SelectionPulse pulse = indicator.AddComponent<SelectionPulse>();
            pulse.Initialize();
        }

        /// <summary>
        /// Hide the current selection indicator
        /// </summary>
        public void HideSelectionIndicator()
        {
            foreach (var kvp in selectionIndicators)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            selectionIndicators.Clear();
        }

        private void CreateHealthBar(Unit unit, GameObject parent)
        {
            // Create health bar container
            GameObject healthBarContainer = new GameObject($"HealthBar_{unit.id}");
            healthBarContainer.transform.SetParent(parent.transform);
            healthBarContainer.transform.localPosition = new Vector3(0, 0.8f, 0); // Above ship

            // Background (red bar)
            GameObject background = GameObject.CreatePrimitive(PrimitiveType.Cube);
            background.name = "HealthBarBG";
            background.transform.SetParent(healthBarContainer.transform);
            background.transform.localPosition = Vector3.zero;
            background.transform.localScale = new Vector3(0.6f, 0.08f, 0.02f);
            SetMaterialColor(background, Color.red);
            Destroy(background.GetComponent<Collider>()); // Remove collider

            // Foreground (green bar)
            GameObject foreground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foreground.name = "HealthBarFG";
            foreground.transform.SetParent(healthBarContainer.transform);
            foreground.transform.localPosition = Vector3.zero;
            foreground.transform.localScale = new Vector3(0.6f, 0.09f, 0.01f);
            SetMaterialColor(foreground, Color.green);
            Destroy(foreground.GetComponent<Collider>()); // Remove collider

            // Make health bar always face camera
            BillboardHealthBar billboard = healthBarContainer.AddComponent<BillboardHealthBar>();

            healthBars[unit.id] = healthBarContainer;

            // Initial update
            UpdateHealthBar(unit);
        }

        private void UpdateHealthBar(Unit unit)
        {
            if (healthBars.TryGetValue(unit.id, out GameObject healthBarContainer))
            {
                Transform foreground = healthBarContainer.transform.Find("HealthBarFG");
                if (foreground != null)
                {
                    // Calculate health percentage
                    float healthPercent = (float)unit.health / unit.maxHealth;

                    // Update foreground scale
                    Vector3 scale = foreground.localScale;
                    scale.x = 0.6f * healthPercent;
                    foreground.localScale = scale;

                    // Update position to align left
                    Vector3 pos = foreground.localPosition;
                    pos.x = -0.3f + (scale.x / 2f);
                    foreground.localPosition = pos;

                    // Update color based on health
                    Color healthColor = Color.Lerp(Color.red, Color.green, healthPercent);
                    SetMaterialColor(foreground.gameObject, healthColor);

                    // Hide health bar if at full health
                    healthBarContainer.SetActive(unit.health < unit.maxHealth);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (GameObject unitObj in unitObjects.Values)
            {
                Destroy(unitObj);
            }
            unitObjects.Clear();

            foreach (GameObject healthBar in healthBars.Values)
            {
                Destroy(healthBar);
            }
            healthBars.Clear();

            HideSelectionIndicator();
        }
    }

    // ====================
    // VISUAL TIER CALCULATION SYSTEM
    // ====================

    /// <summary>
    /// Calculate sail visual tier (3 tiers from 0-5 upgrades)
    /// Tier 1: 0-1 sail upgrades (small square sails)
    /// Tier 2: 2-3 sail upgrades (larger triangular sails)
    /// Tier 3: 4-5 sail upgrades (massive billowing sails)
    /// </summary>
    private int GetSailVisualTier(int sailUpgrades)
    {
        if (sailUpgrades <= 1) return 1;
        if (sailUpgrades <= 3) return 2;
        return 3; // 4-5 upgrades
    }

    /// <summary>
    /// Calculate cannon visual tier (3 tiers from 2-7 total cannons)
    /// Tier 1: 2-3 cannons (2 per side, small)
    /// Tier 2: 4-5 cannons (4 per side, medium)
    /// Tier 3: 6-7 cannons (6+ per side, large/varied)
    /// </summary>
    private int GetCannonVisualTier(int totalCannons)
    {
        if (totalCannons <= 3) return 1;
        if (totalCannons <= 5) return 2;
        return 3; // 6-7 cannons
    }

    /// <summary>
    /// Calculate ship scale based on all upgrades (0.5 → 0.95 of tile size)
    /// Combines maxHealth, sails, and cannons to determine overall size
    /// </summary>
    private float GetShipScale(Unit unit)
    {
        // Normalize each upgrade type to 0-1 range
        float healthProgress = Mathf.Clamp01((unit.maxHealth - 10f) / 20f); // 10-30 → 0-1
        float sailProgress = Mathf.Clamp01(unit.sails / 5f); // 0-5 → 0-1
        float cannonProgress = Mathf.Clamp01((unit.cannons - 2f) / 5f); // 2-7 → 0-1

        // Combined progress (weighted average: health 40%, sails 30%, cannons 30%)
        float overallProgress = (healthProgress * 0.4f) + (sailProgress * 0.3f) + (cannonProgress * 0.3f);

        // Scale from 0.5 to 0.95 (nearly filling tile when fully upgraded)
        return Mathf.Lerp(0.5f, 0.95f, overallProgress);
    }

    /// <summary>
    /// Get number of cannons to render per side based on visual tier
    /// </summary>
    private int GetCannonsPerSide(int cannonVisualTier)
    {
        switch (cannonVisualTier)
        {
            case 1: return 2;  // 2 per side (4 total visible, but ship has 2-3)
            case 2: return 4;  // 4 per side (8 total visible)
            case 3: return 6;  // 6 per side (12 total visible)
            default: return 2;
        }
    }

    /// <summary>
    /// Makes the health bar always face the camera
    /// </summary>
    public class BillboardHealthBar : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (Camera.main != null)
            {
                transform.LookAt(Camera.main.transform);
                transform.Rotate(0, 180, 0); // Flip to face camera correctly
            }
        }
    }
}
