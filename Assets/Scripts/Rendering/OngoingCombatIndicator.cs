using PlunkAndPlunder.Core;
using PlunkAndPlunder.Resolution;
using PlunkAndPlunder.Units;
using System.Collections.Generic;
using UnityEngine;

namespace PlunkAndPlunder.Rendering
{
    /// <summary>
    /// Shows persistent visual indicators for ships engaged in multi-turn combat
    /// Displays crossed swords icon and connecting lines between combatants
    /// </summary>
    public class OngoingCombatIndicator : MonoBehaviour
    {
        [Header("Combat Icon Settings")]
        public float iconHeight = 2.5f;
        public float iconSize = 0.4f;
        public Color iconColor = new Color(1f, 0.3f, 0.3f, 0.9f); // Red
        public float pulseSpeed = 3f;
        public float pulseAmount = 0.15f;

        [Header("Connection Line Settings")]
        public float lineHeight = 1.2f;
        public float lineWidth = 0.08f;
        public Color lineColor = new Color(1f, 0.4f, 0.4f, 0.6f); // Semi-transparent red
        public int lineDashSegments = 10;

        private Dictionary<string, CombatIndicatorInstance> activeCombatIndicators = new Dictionary<string, CombatIndicatorInstance>();
        private UnitManager unitManager;
        private Material lineMaterial;

        public void Initialize(TurnAnimator animator, UnitManager manager)
        {
            unitManager = manager;

            // Create material for lines
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            lineMaterial.color = lineColor;

            if (animator != null)
            {
                animator.OnAnimationStep += HandleAnimationStep;
                Debug.Log("[OngoingCombatIndicator] Subscribed to OnAnimationStep event");
            }
        }

        private void HandleAnimationStep(GameState state)
        {
            if (state?.unitManager == null) return;

            // Clear old indicators
            ClearIndicators();

            // Find all units in combat
            List<Unit> allUnits = state.unitManager.GetAllUnits();
            HashSet<string> processedPairs = new HashSet<string>();

            foreach (Unit unit in allUnits)
            {
                if (unit.isInCombat && !string.IsNullOrEmpty(unit.combatOpponentId))
                {
                    // Get opponent
                    Unit opponent = state.unitManager.GetUnit(unit.combatOpponentId);
                    if (opponent != null && opponent.isInCombat)
                    {
                        // Create unique key for this combat pair (sorted to avoid duplicates)
                        string key1 = $"{unit.id}_{opponent.id}";
                        string key2 = $"{opponent.id}_{unit.id}";

                        if (!processedPairs.Contains(key1) && !processedPairs.Contains(key2))
                        {
                            ShowCombatIndicator(unit, opponent);
                            processedPairs.Add(key1);
                        }
                    }
                }
            }
        }

        private void ShowCombatIndicator(Unit unit1, Unit unit2)
        {
            string key = $"{unit1.id}_{unit2.id}";

            // Create parent object for this combat pair
            GameObject indicatorObj = new GameObject($"CombatIndicator_{key}");
            indicatorObj.transform.SetParent(transform);

            CombatIndicatorInstance instance = new CombatIndicatorInstance
            {
                gameObject = indicatorObj,
                unit1Id = unit1.id,
                unit2Id = unit2.id
            };

            // Create icons above each unit
            CreateCombatIcon(indicatorObj, unit1.position.ToWorldPosition());
            CreateCombatIcon(indicatorObj, unit2.position.ToWorldPosition());

            // Create connecting line
            CreateConnectionLine(indicatorObj, unit1.position.ToWorldPosition(), unit2.position.ToWorldPosition());

            activeCombatIndicators[key] = instance;
        }

        private void CreateCombatIcon(GameObject parent, Vector3 position)
        {
            // Create crossed swords icon using two intersecting planes
            GameObject iconObj = new GameObject("CombatIcon");
            iconObj.transform.SetParent(parent.transform);
            iconObj.transform.position = position + Vector3.up * iconHeight;

            // Create two crossed planes for the swords
            CreateSwordPlane(iconObj, Quaternion.Euler(0, 45, 0));
            CreateSwordPlane(iconObj, Quaternion.Euler(0, -45, 0));

            // Add pulse animation
            CombatIconPulse pulse = iconObj.AddComponent<CombatIconPulse>();
            pulse.Initialize(pulseSpeed, pulseAmount, iconSize);
        }

        private void CreateSwordPlane(GameObject parent, Quaternion rotation)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plane.transform.SetParent(parent.transform, false);
            plane.transform.localRotation = rotation;
            plane.transform.localScale = Vector3.one * iconSize;

            // Remove collider
            Destroy(plane.GetComponent<Collider>());

            // Set material
            Renderer renderer = plane.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Sprites/Default"));
                mat.color = iconColor;
                renderer.material = mat;
            }
        }

        private void CreateConnectionLine(GameObject parent, Vector3 pos1, Vector3 pos2)
        {
            GameObject lineObj = new GameObject("ConnectionLine");
            lineObj.transform.SetParent(parent.transform);

            LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
            lineRenderer.material = lineMaterial;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;

            Vector3 start = pos1 + Vector3.up * lineHeight;
            Vector3 end = pos2 + Vector3.up * lineHeight;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);

            // Make it dashed
            lineRenderer.textureMode = LineTextureMode.Tile;
            lineRenderer.material.mainTextureScale = new Vector2(lineDashSegments, 1f);

            // Add animated dash scroll
            CombatLineScroll scroll = lineObj.AddComponent<CombatLineScroll>();
            scroll.Initialize(lineRenderer);
        }

        private void ClearIndicators()
        {
            foreach (var kvp in activeCombatIndicators)
            {
                if (kvp.Value.gameObject != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            activeCombatIndicators.Clear();
        }

        private void OnDestroy()
        {
            ClearIndicators();
        }

        /// <summary>
        /// Pulse animation for combat icons
        /// </summary>
        private class CombatIconPulse : MonoBehaviour
        {
            private float speed;
            private float amount;
            private float baseSize;

            public void Initialize(float pulseSpeed, float pulseAmount, float iconSize)
            {
                speed = pulseSpeed;
                amount = pulseAmount;
                baseSize = iconSize;
            }

            private void Update()
            {
                float pulse = Mathf.Sin(Time.time * speed) * amount;
                float size = baseSize + pulse;
                transform.localScale = Vector3.one * size;

                // Billboard effect - face camera
                if (Camera.main != null)
                {
                    transform.rotation = Camera.main.transform.rotation;
                }
            }
        }

        /// <summary>
        /// Animated dash scroll for combat lines
        /// </summary>
        private class CombatLineScroll : MonoBehaviour
        {
            private LineRenderer lineRenderer;
            private float scrollSpeed = 0.5f;

            public void Initialize(LineRenderer lr)
            {
                lineRenderer = lr;
            }

            private void Update()
            {
                if (lineRenderer != null && lineRenderer.material != null)
                {
                    float offset = Time.time * scrollSpeed;
                    lineRenderer.material.mainTextureOffset = new Vector2(offset, 0);
                }
            }
        }

        private class CombatIndicatorInstance
        {
            public GameObject gameObject;
            public string unit1Id;
            public string unit2Id;
        }
    }
}
