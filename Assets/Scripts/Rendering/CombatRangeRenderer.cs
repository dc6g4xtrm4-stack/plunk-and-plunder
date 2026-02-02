using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using PlunkAndPlunder.Resolution;
using PlunkAndPlunder.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlunkAndPlunder.Rendering
{
    /// <summary>
    /// Shows range circles around ships to indicate firing range
    /// Phase 5.1: Range Indicators for edge combat distinction
    /// </summary>
    public class CombatRangeRenderer : MonoBehaviour
    {
        [Header("Range Circle Settings")]
        public float rangeRadius = 1.73f; // Hex radius for adjacent hexes
        public Color inRangeColor = new Color(1f, 0.5f, 0f, 0.6f); // Orange
        public Color escapingColor = new Color(0f, 1f, 0f, 0.8f); // Green
        public float lineWidth = 0.1f;
        public int circleSegments = 36;
        public float pulseSpeed = 2f;
        public float pulseAmount = 0.2f;

        private Dictionary<string, GameObject> activeRangeCircles = new Dictionary<string, GameObject>();
        private UnitManager unitManager;

        public void Initialize(TurnAnimator animator, UnitManager manager)
        {
            unitManager = manager;

            if (animator != null)
            {
                animator.OnAnimationStep += HandleAnimationStep;
                Debug.Log("[CombatRangeRenderer] Subscribed to OnAnimationStep event");
            }
        }

        private void HandleAnimationStep(GameState state)
        {
            if (state?.unitManager == null) return;

            // Clear old circles
            ClearRangeCircles();

            // For each unit, check if adjacent to enemies
            List<Unit> allUnits = state.unitManager.GetAllUnits();

            foreach (Unit unit in allUnits)
            {
                // Find adjacent enemy units
                List<Unit> adjacentEnemies = GetAdjacentEnemies(unit, state);

                if (adjacentEnemies.Count > 0)
                {
                    // Show range circle around this unit
                    ShowRangeCircle(unit, adjacentEnemies);
                }
            }
        }

        private List<Unit> GetAdjacentEnemies(Unit unit, GameState state)
        {
            List<Unit> enemies = new List<Unit>();

            // Get all adjacent hex positions
            List<HexCoord> neighbors = state.grid.GetNavigableNeighbors(unit.position);

            foreach (HexCoord neighbor in neighbors)
            {
                List<Unit> unitsAtPos = state.unitManager.GetUnitsAtPosition(neighbor);
                foreach (Unit other in unitsAtPos)
                {
                    if (other.ownerId != unit.ownerId)
                    {
                        enemies.Add(other);
                    }
                }
            }

            return enemies;
        }

        private void ShowRangeCircle(Unit unit, List<Unit> targets)
        {
            string key = $"{unit.id}_range";

            GameObject circleObj = new GameObject($"RangeCircle_{unit.id}");
            circleObj.transform.SetParent(transform);
            circleObj.transform.position = unit.position.ToWorldPosition() + Vector3.up * 0.3f;

            LineRenderer lineRenderer = circleObj.AddComponent<LineRenderer>();
            lineRenderer.positionCount = circleSegments + 1;
            lineRenderer.useWorldSpace = false;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

            // Create circle points
            for (int i = 0; i <= circleSegments; i++)
            {
                float angle = (i / (float)circleSegments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * rangeRadius;
                float z = Mathf.Sin(angle) * rangeRadius;
                lineRenderer.SetPosition(i, new Vector3(x, 0, z));
            }

            // Set color
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = inRangeColor;
            lineRenderer.material = mat;

            // Add pulse animation
            RangePulse pulse = circleObj.AddComponent<RangePulse>();
            pulse.Initialize(lineRenderer, pulseSpeed, pulseAmount);

            // Show indicators for each target in range
            foreach (Unit target in targets)
            {
                ShowTargetIndicator(unit, target);
            }

            activeRangeCircles[key] = circleObj;
        }

        private void ShowTargetIndicator(Unit shooter, Unit target)
        {
            // Create small indicator above target showing they're in range
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.transform.SetParent(transform);
            indicator.transform.position = target.position.ToWorldPosition() + Vector3.up * 2f;
            indicator.transform.localScale = Vector3.one * 0.3f;

            Renderer renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(1f, 0f, 0f, 0.8f); // Red warning
                renderer.material = mat;
            }

            // Auto-destroy after a short time
            Destroy(indicator, 0.5f);
        }

        public void ShowEscapeIndicator(Vector3 position)
        {
            // Create "ESCAPED!" floating text
            var floatingTextRenderer = FindFirstObjectByType<FloatingTextRenderer>();
            if (floatingTextRenderer != null)
            {
                floatingTextRenderer.SpawnText(position + Vector3.up * 1f, "ESCAPED!", escapingColor);
            }
        }

        private void ClearRangeCircles()
        {
            foreach (var kvp in activeRangeCircles)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            activeRangeCircles.Clear();
        }

        private void OnDestroy()
        {
            ClearRangeCircles();
        }

        /// <summary>
        /// Pulse animation for range circles
        /// </summary>
        private class RangePulse : MonoBehaviour
        {
            private LineRenderer lineRenderer;
            private float speed;
            private float amount;
            private float baseWidth;

            public void Initialize(LineRenderer lr, float pulseSpeed, float pulseAmount)
            {
                lineRenderer = lr;
                speed = pulseSpeed;
                amount = pulseAmount;
                baseWidth = lr.startWidth;
            }

            private void Update()
            {
                if (lineRenderer == null) return;

                float pulse = Mathf.Sin(Time.time * speed) * amount;
                float width = baseWidth + pulse;
                lineRenderer.startWidth = width;
                lineRenderer.endWidth = width;
            }
        }
    }
}
