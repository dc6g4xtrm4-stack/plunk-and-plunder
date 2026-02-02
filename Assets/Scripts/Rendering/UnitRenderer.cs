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

        /// <summary>
        /// Flash health bar for a specific unit (Task #10: Visual feedback on damage)
        /// </summary>
        public void FlashHealthBar(string unitId)
        {
            if (healthBars.TryGetValue(unitId, out GameObject healthBarContainer))
            {
                if (healthBarContainer != null)
                {
                    HealthBarFlash flash = healthBarContainer.GetComponent<HealthBarFlash>();
                    if (flash != null)
                    {
                        flash.FlashDamage();
                    }
                }
            }
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

            // 1. Create Main Hull (brown boat body) - stretched cube, scales with upgrade
            GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.name = "Hull";
            hull.transform.SetParent(parent.transform);
            hull.transform.localPosition = new Vector3(0, 0, 0);
            hull.transform.localScale = new Vector3(0.5f * scaleMultiplier, 0.15f, 0.3f * scaleMultiplier);
            hull.transform.localRotation = Quaternion.identity;
            SetMaterialColor(hull, brownHull);

            // 1a. Add Hull Lower Section (darker, for depth)
            Color darkWood = new Color(0.3f, 0.18f, 0.06f); // Darker brown for lower hull
            GameObject hullLower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hullLower.name = "HullLower";
            hullLower.transform.SetParent(parent.transform);
            hullLower.transform.localPosition = new Vector3(0, -0.06f, 0);
            hullLower.transform.localScale = new Vector3(0.48f * scaleMultiplier, 0.08f, 0.28f * scaleMultiplier);
            hullLower.transform.localRotation = Quaternion.identity;
            SetMaterialColor(hullLower, darkWood);

            // 1b. Create Deck Surface (lighter wood on top)
            Color deckWood = new Color(0.5f, 0.32f, 0.15f); // Lighter wood for deck
            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Deck";
            deck.transform.SetParent(parent.transform);
            deck.transform.localPosition = new Vector3(0, 0.09f, 0);
            deck.transform.localScale = new Vector3(0.45f * scaleMultiplier, 0.02f, 0.28f * scaleMultiplier);
            deck.transform.localRotation = Quaternion.identity;
            SetMaterialColor(deck, deckWood);

            // 1c. Add Side Railings (left and right)
            Color railingWood = new Color(0.35f, 0.22f, 0.08f); // Medium brown for railings
            // Left railing
            GameObject railingLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railingLeft.name = "RailingLeft";
            railingLeft.transform.SetParent(parent.transform);
            railingLeft.transform.localPosition = new Vector3(-0.24f * scaleMultiplier, 0.12f, 0);
            railingLeft.transform.localScale = new Vector3(0.03f, 0.08f, 0.28f * scaleMultiplier);
            railingLeft.transform.localRotation = Quaternion.identity;
            SetMaterialColor(railingLeft, railingWood);

            // Right railing
            GameObject railingRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            railingRight.name = "RailingRight";
            railingRight.transform.SetParent(parent.transform);
            railingRight.transform.localPosition = new Vector3(0.24f * scaleMultiplier, 0.12f, 0);
            railingRight.transform.localScale = new Vector3(0.03f, 0.08f, 0.28f * scaleMultiplier);
            railingRight.transform.localRotation = Quaternion.identity;
            SetMaterialColor(railingRight, railingWood);

            // 2. Create Bow (front pointed part) - main prow
            GameObject bow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bow.name = "Bow";
            bow.transform.SetParent(parent.transform);
            bow.transform.localPosition = new Vector3(0.35f * scaleMultiplier, 0, 0);
            bow.transform.localScale = new Vector3(0.2f * scaleMultiplier, 0.12f, 0.2f * scaleMultiplier);
            bow.transform.localRotation = Quaternion.Euler(0, 45, 0);
            SetMaterialColor(bow, brownHull);

            // 2a. Add Prow Detail (decorative front piece)
            GameObject prow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prow.name = "Prow";
            prow.transform.SetParent(parent.transform);
            prow.transform.localPosition = new Vector3(0.42f * scaleMultiplier, 0.05f, 0);
            prow.transform.localScale = new Vector3(0.12f * scaleMultiplier, 0.06f, 0.08f * scaleMultiplier);
            prow.transform.localRotation = Quaternion.Euler(0, 45, 0);
            SetMaterialColor(prow, deckWood);

            // 2b. Add Stern (back of ship)
            GameObject stern = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stern.name = "Stern";
            stern.transform.SetParent(parent.transform);
            stern.transform.localPosition = new Vector3(-0.28f * scaleMultiplier, 0.03f, 0);
            stern.transform.localScale = new Vector3(0.1f * scaleMultiplier, 0.18f, 0.26f * scaleMultiplier);
            stern.transform.localRotation = Quaternion.identity;
            SetMaterialColor(stern, darkWood);

            // 3. Create Mast(s) - number depends on health tier, visual depends on sail tier
            if (healthTier == 1)
            {
                // Tier 1: Single mast
                CreateMastWithSail(parent, new Vector3(0, 0.35f, 0), 0.35f, sailColor, playerColor, brownHull, scaleMultiplier, sailTier);

                // Add cannons even for tier 1 (all ships have cannons!)
                int cannonsToRender = GetCannonsPerSide(cannonTier);
                CreateCannons(parent, cannonsToRender, scaleMultiplier, darkHull);
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

            // Add crow's nest platform at 70% height
            GameObject crowsNest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crowsNest.name = "CrowsNest";
            crowsNest.transform.SetParent(parent.transform);
            crowsNest.transform.localPosition = position + new Vector3(0, mastHeight * 0.7f, 0);
            crowsNest.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);
            SetMaterialColor(crowsNest, mastColor);

            // Tier-based sail dimensions and billowing effect
            float sailWidth, sailHeight, sailBillowDepth;
            int sailSegments; // Number of curved sail segments

            switch (sailTier)
            {
                case 1: // Small square sails (0-1 sail upgrades)
                    sailWidth = 0.3f * scale;
                    sailHeight = 0.35f;
                    sailBillowDepth = 0.04f;
                    sailSegments = 2;
                    break;
                case 2: // Larger billowing sails (2-3 sail upgrades)
                    sailWidth = 0.4f * scale;
                    sailHeight = 0.5f;
                    sailBillowDepth = 0.06f;
                    sailSegments = 3;
                    break;
                case 3: // Massive billowing sails (4-5 sail upgrades)
                    sailWidth = 0.5f * scale;
                    sailHeight = 0.65f;
                    sailBillowDepth = 0.08f;
                    sailSegments = 3;
                    break;
                default:
                    sailWidth = 0.3f * scale;
                    sailHeight = 0.35f;
                    sailBillowDepth = 0.04f;
                    sailSegments = 2;
                    break;
            }

            // Create billowing sail using multiple angled segments to simulate wind curve
            for (int i = 0; i < sailSegments; i++)
            {
                GameObject sailSegment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sailSegment.name = $"Sail_Segment_{i}";
                sailSegment.transform.SetParent(parent.transform);

                // Position segments vertically and curve them forward
                float segmentHeight = sailHeight / sailSegments;
                float yOffset = position.y + (i * segmentHeight) - (sailHeight / 2f) + (segmentHeight / 2f);

                // Curve each segment progressively forward and rotate for billowing effect
                float curveFactor = (float)i / (sailSegments - 1); // 0 to 1
                float zOffset = position.z - (curveFactor * sailBillowDepth);
                float rotation = curveFactor * 5f; // Slight rotation for wind effect

                sailSegment.transform.localPosition = new Vector3(position.x, yOffset, zOffset);
                sailSegment.transform.localRotation = Quaternion.Euler(rotation, 0, 0);
                sailSegment.transform.localScale = new Vector3(sailWidth, segmentHeight * 1.05f, 0.02f);

                // Gradient color for depth - darker at bottom, lighter at top
                float brightnessMultiplier = 0.85f + (curveFactor * 0.15f);
                Color segmentColor = new Color(
                    sailColor.r * brightnessMultiplier,
                    sailColor.g * brightnessMultiplier,
                    sailColor.b * brightnessMultiplier,
                    sailColor.a
                );
                SetMaterialColor(sailSegment, segmentColor);
            }

            // Add horizontal spar (crossbeam) at top of sail for rigging
            GameObject topSpar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            topSpar.name = "TopSpar";
            topSpar.transform.SetParent(parent.transform);
            topSpar.transform.localPosition = position + new Vector3(0, sailHeight / 2f, -sailBillowDepth * 0.3f);
            topSpar.transform.localRotation = Quaternion.Euler(0, 0, 90);
            topSpar.transform.localScale = new Vector3(0.02f, sailWidth * 0.6f, 0.02f);
            SetMaterialColor(topSpar, mastColor);

            // Add horizontal spar at bottom of sail
            GameObject bottomSpar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bottomSpar.name = "BottomSpar";
            bottomSpar.transform.SetParent(parent.transform);
            bottomSpar.transform.localPosition = position + new Vector3(0, -sailHeight / 2f, -sailBillowDepth * 0.7f);
            bottomSpar.transform.localRotation = Quaternion.Euler(0, 0, 90);
            bottomSpar.transform.localScale = new Vector3(0.02f, sailWidth * 0.6f, 0.02f);
            SetMaterialColor(bottomSpar, mastColor);

            // For tier 3, add a second smaller sail above for double-sail effect
            if (sailTier == 3)
            {
                float topSailHeight = sailHeight * 0.4f;
                float topSailWidth = sailWidth * 0.7f;

                GameObject topSail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                topSail.name = "TopSail";
                topSail.transform.SetParent(parent.transform);
                topSail.transform.localPosition = position + new Vector3(0, sailHeight * 0.7f, -sailBillowDepth * 0.5f);
                topSail.transform.localRotation = Quaternion.Euler(8, 0, 0);
                topSail.transform.localScale = new Vector3(topSailWidth, topSailHeight, 0.02f);
                Color topSailColor = new Color(sailColor.r * 0.95f, sailColor.g * 0.95f, sailColor.b * 0.95f);
                SetMaterialColor(topSail, topSailColor);
            }

            // Create triangular flag at top of mast
            GameObject flagPole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flagPole.name = "FlagPole";
            flagPole.transform.SetParent(parent.transform);
            flagPole.transform.localPosition = position + new Vector3(0, mastHeight + 0.15f, 0);
            flagPole.transform.localScale = new Vector3(0.02f, 0.1f, 0.02f);
            SetMaterialColor(flagPole, mastColor);

            // Triangular flag made from rotated cube (simulating pennant flag)
            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Flag";
            flag.transform.SetParent(parent.transform);
            flag.transform.localPosition = position + new Vector3(0.06f, mastHeight + 0.22f, 0);
            flag.transform.localRotation = Quaternion.Euler(0, 0, 45); // Rotate to look triangular
            flag.transform.localScale = new Vector3(0.12f, 0.08f, 0.01f);
            SetMaterialColor(flag, flagColor);

            // Add decorative flag streamer for motion effect
            GameObject flagStreamer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flagStreamer.name = "FlagStreamer";
            flagStreamer.transform.SetParent(parent.transform);
            flagStreamer.transform.localPosition = position + new Vector3(0.10f, mastHeight + 0.20f, 0);
            flagStreamer.transform.localRotation = Quaternion.Euler(0, 0, -10); // Angle for wind effect
            flagStreamer.transform.localScale = new Vector3(0.08f, 0.02f, 0.01f);
            Color streamerColor = new Color(flagColor.r * 0.9f, flagColor.g * 0.9f, flagColor.b * 0.9f);
            SetMaterialColor(flagStreamer, streamerColor);
        }

        private void CreateCannons(GameObject parent, int count, float scale, Color cannonColor)
        {
            // Enhanced cannon design with barrel, muzzle, and mount
            float spacing = 0.3f * scale / Mathf.Max(1, count - 1);
            float startZ = -0.15f * scale;

            // Define cannon colors
            Color ironBarrel = new Color(0.2f, 0.2f, 0.22f); // Dark gray iron
            Color muzzleInterior = new Color(0.08f, 0.08f, 0.08f); // Almost black interior
            Color woodMount = cannonColor; // Use the dark hull color for mount

            for (int i = 0; i < count; i++)
            {
                float zPos = startZ + (i * spacing);

                // Create left side cannon assembly
                CreateCannonAssembly(parent, new Vector3(-0.28f * scale, 0.05f, zPos), -90f, scale,
                    ironBarrel, muzzleInterior, woodMount, $"L_{i}");

                // Create right side cannon assembly
                CreateCannonAssembly(parent, new Vector3(0.28f * scale, 0.05f, zPos), 90f, scale,
                    ironBarrel, muzzleInterior, woodMount, $"R_{i}");
            }
        }

        private void CreateCannonAssembly(GameObject parent, Vector3 position, float yRotation, float scale,
            Color barrelColor, Color muzzleColor, Color mountColor, string suffix)
        {
            // 1. Cannon Mount/Carriage (wooden base connecting to hull)
            GameObject mount = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mount.name = $"CannonMount_{suffix}";
            mount.transform.SetParent(parent.transform);
            mount.transform.localPosition = position;
            mount.transform.localRotation = Quaternion.Euler(-5, yRotation, 0); // Angled slightly down
            mount.transform.localScale = new Vector3(0.04f, 0.03f, 0.05f);
            SetMaterialColor(mount, mountColor);

            // 2. Cannon Barrel (thicker, more prominent iron cylinder)
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = $"CannonBarrel_{suffix}";
            barrel.transform.SetParent(parent.transform);
            // Position barrel protruding outward from mount
            Vector3 barrelOffset = yRotation > 0 ? new Vector3(0.06f, 0, 0) : new Vector3(-0.06f, 0, 0);
            barrel.transform.localPosition = position + barrelOffset;
            barrel.transform.localRotation = Quaternion.Euler(-5, 0, 90); // Horizontal and angled down
            barrel.transform.localScale = new Vector3(0.045f, 0.12f, 0.045f); // Thicker and longer
            SetMaterialColor(barrel, barrelColor);

            // 3. Muzzle Detail (darker interior ring at end of barrel)
            GameObject muzzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            muzzle.name = $"CannonMuzzle_{suffix}";
            muzzle.transform.SetParent(parent.transform);
            // Position at the end of the barrel
            Vector3 muzzleOffset = yRotation > 0 ? new Vector3(0.12f, 0, 0) : new Vector3(-0.12f, 0, 0);
            muzzle.transform.localPosition = position + muzzleOffset;
            muzzle.transform.localRotation = Quaternion.Euler(-5, 0, 90);
            muzzle.transform.localScale = new Vector3(0.035f, 0.015f, 0.035f); // Thin ring
            SetMaterialColor(muzzle, muzzleColor);
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
                        CreateShipModel(unitObj, unit);
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
                        CreateShipModel(unitObj, unit);
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
            // Create health bar container (Task #10: Enhanced visibility)
            GameObject healthBarContainer = new GameObject($"HealthBar_{unit.id}");
            healthBarContainer.transform.SetParent(parent.transform);
            healthBarContainer.transform.localPosition = new Vector3(0, 1.0f, 0); // Raised higher for visibility

            // Outline/border (black for contrast)
            GameObject outline = GameObject.CreatePrimitive(PrimitiveType.Cube);
            outline.name = "HealthBarOutline";
            outline.transform.SetParent(healthBarContainer.transform);
            outline.transform.localPosition = Vector3.zero;
            outline.transform.localScale = new Vector3(0.82f, 0.14f, 0.01f); // Slightly larger than bar
            SetMaterialColor(outline, new Color(0.1f, 0.1f, 0.1f, 0.9f)); // Dark outline
            Destroy(outline.GetComponent<Collider>()); // Remove collider

            // Background (darker red bar)
            GameObject background = GameObject.CreatePrimitive(PrimitiveType.Cube);
            background.name = "HealthBarBG";
            background.transform.SetParent(healthBarContainer.transform);
            background.transform.localPosition = new Vector3(0, 0, -0.005f); // Slightly in front of outline
            background.transform.localScale = new Vector3(0.8f, 0.12f, 0.015f); // Larger for better visibility
            SetMaterialColor(background, new Color(0.6f, 0.1f, 0.1f, 0.9f)); // Dark red
            Destroy(background.GetComponent<Collider>()); // Remove collider

            // Foreground (vibrant health bar)
            GameObject foreground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foreground.name = "HealthBarFG";
            foreground.transform.SetParent(healthBarContainer.transform);
            foreground.transform.localPosition = new Vector3(0, 0, -0.01f); // In front of background
            foreground.transform.localScale = new Vector3(0.8f, 0.13f, 0.018f);
            SetMaterialColor(foreground, new Color(0.2f, 1f, 0.2f)); // Bright green
            Destroy(foreground.GetComponent<Collider>()); // Remove collider

            // Make health bar always face camera
            BillboardHealthBar billboard = healthBarContainer.AddComponent<BillboardHealthBar>();

            // Add damage flash component for visual feedback
            HealthBarFlash flash = healthBarContainer.AddComponent<HealthBarFlash>();
            flash.Initialize(foreground);

            healthBars[unit.id] = healthBarContainer;

            // Initial update
            UpdateHealthBar(unit);
        }

        private void UpdateHealthBar(Unit unit)
        {
            // Check if health bar exists and is still valid
            if (healthBars.TryGetValue(unit.id, out GameObject healthBarContainer))
            {
                // Check if the GameObject was destroyed (happens during ship upgrade)
                if (healthBarContainer == null)
                {
                    // Health bar was destroyed, remove from dictionary and recreate
                    healthBars.Remove(unit.id);

                    if (unitObjects.TryGetValue(unit.id, out GameObject unitObj))
                    {
                        CreateHealthBar(unit, unitObj);
                    }
                    return;
                }

                Transform foreground = healthBarContainer.transform.Find("HealthBarFG");
                if (foreground != null)
                {
                    // Calculate health percentage
                    float healthPercent = (float)unit.health / unit.maxHealth;

                    // Update foreground scale (adjusted for new size)
                    Vector3 scale = foreground.localScale;
                    scale.x = 0.8f * healthPercent;
                    foreground.localScale = scale;

                    // Update position to align left
                    Vector3 pos = foreground.localPosition;
                    pos.x = -0.4f + (scale.x / 2f);
                    foreground.localPosition = pos;

                    // Update color based on health (Task #10: More vibrant colors)
                    Color healthColor;
                    if (healthPercent > 0.6f)
                    {
                        healthColor = Color.Lerp(new Color(1f, 1f, 0.2f), new Color(0.2f, 1f, 0.2f), (healthPercent - 0.6f) / 0.4f); // Yellow to bright green
                    }
                    else if (healthPercent > 0.3f)
                    {
                        healthColor = Color.Lerp(new Color(1f, 0.5f, 0f), new Color(1f, 1f, 0.2f), (healthPercent - 0.3f) / 0.3f); // Orange to yellow
                    }
                    else
                    {
                        healthColor = Color.Lerp(new Color(1f, 0f, 0f), new Color(1f, 0.5f, 0f), healthPercent / 0.3f); // Red to orange
                    }
                    SetMaterialColor(foreground.gameObject, healthColor);

                    // Show health bar if damaged OR in combat (Task #10: Always show during combat)
                    bool showHealthBar = unit.health < unit.maxHealth || unit.isInCombat;
                    healthBarContainer.SetActive(showHealthBar);
                }
            }
            else
            {
                // Health bar doesn't exist, create it
                if (unitObjects.TryGetValue(unit.id, out GameObject unitObj))
                {
                    CreateHealthBar(unit, unitObj);
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
        /// Calculate ship scale based on all upgrades (1.0 → 1.9 of tile size)
        /// WoW-style dramatic scaling - ships should feel HUGE when fully upgraded
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

            // Scale from 1.0 (2x bigger base) to 1.9 (nearly bursting out of tile!)
            return Mathf.Lerp(1.0f, 1.9f, overallProgress);
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

    /// <summary>
    /// Flash effect for health bar when unit takes damage (Task #10)
    /// </summary>
    public class HealthBarFlash : MonoBehaviour
    {
        private GameObject foreground;
        private Material foregroundMaterial;
        private Color originalColor;
        private bool isFlashing = false;
        private float flashTimer = 0f;
        private const float flashDuration = 0.3f;

        public void Initialize(GameObject foregroundBar)
        {
            foreground = foregroundBar;
            if (foreground != null)
            {
                Renderer renderer = foreground.GetComponent<Renderer>();
                if (renderer != null)
                {
                    foregroundMaterial = renderer.material;
                    originalColor = foregroundMaterial.color;
                }
            }
        }

        /// <summary>
        /// Trigger damage flash effect
        /// </summary>
        public void FlashDamage()
        {
            if (!isFlashing)
            {
                isFlashing = true;
                flashTimer = 0f;
            }
        }

        private void Update()
        {
            if (isFlashing && foregroundMaterial != null)
            {
                flashTimer += Time.deltaTime;
                float t = flashTimer / flashDuration;

                if (t >= 1f)
                {
                    // Flash complete
                    isFlashing = false;
                    foregroundMaterial.color = originalColor;
                }
                else
                {
                    // Flash between white and original color
                    Color flashColor = Color.Lerp(Color.white, originalColor, t);
                    foregroundMaterial.color = flashColor;
                }
            }
        }
    }
}
