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
    /// Renders semi-transparent ghost ships along movement paths
    /// Phase 1.1: Ghost Trail System
    /// </summary>
    public class GhostTrailRenderer : MonoBehaviour
    {
        [Header("Ghost Trail Settings")]
        public int maxGhostsPerUnit = 5;
        public float ghostFadeDuration = 2.5f;
        public float ghostAlpha = 0.3f;
        public Color playerGhostTint = new Color(0.5f, 0.8f, 1f); // Light blue
        public Color aiGhostTint = new Color(1f, 0.5f, 0.5f); // Light red

        private Dictionary<string, Queue<GhostInstance>> unitGhosts = new Dictionary<string, Queue<GhostInstance>>();
        private GameObject ghostContainer;
        private UnitRenderer unitRenderer;

        private class GhostInstance
        {
            public GameObject ghostObject;
            public float spawnTime;
            public int ownerId;
        }

        private void Awake()
        {
            ghostContainer = new GameObject("GhostTrails");
            ghostContainer.transform.SetParent(transform);
        }

        public void Initialize(TurnAnimator animator, UnitRenderer renderer)
        {
            unitRenderer = renderer;

            if (animator != null)
            {
                animator.OnAnimationStep += HandleAnimationStep;
                Debug.Log("[GhostTrailRenderer] Subscribed to OnAnimationStep event");
            }
        }

        private void HandleAnimationStep(GameState state)
        {
            if (state?.unitManager == null) return;

            // For each unit that has moved, create a ghost at its previous position
            List<Unit> allUnits = state.unitManager.GetAllUnits();

            foreach (Unit unit in allUnits)
            {
                // Check if unit has a queued path (is moving)
                if (unit.queuedPath != null && unit.queuedPath.Count > 1)
                {
                    // Add ghost at current position (before next move)
                    AddGhostAtPosition(unit.id, unit.position, unit.ownerId);
                }
            }

            // Update and fade existing ghosts
            UpdateGhosts();
        }

        private void AddGhostAtPosition(string unitId, HexCoord position, int ownerId)
        {
            // Initialize ghost queue for this unit if needed
            if (!unitGhosts.ContainsKey(unitId))
            {
                unitGhosts[unitId] = new Queue<GhostInstance>();
            }

            Queue<GhostInstance> ghosts = unitGhosts[unitId];

            // Remove oldest ghost if at max capacity
            if (ghosts.Count >= maxGhostsPerUnit)
            {
                GhostInstance oldest = ghosts.Dequeue();
                if (oldest.ghostObject != null)
                {
                    Destroy(oldest.ghostObject);
                }
            }

            // Create ghost ship at position
            GameObject ghost = CreateGhostShip(position, ownerId);

            GhostInstance instance = new GhostInstance
            {
                ghostObject = ghost,
                spawnTime = Time.time,
                ownerId = ownerId
            };

            ghosts.Enqueue(instance);
        }

        private GameObject CreateGhostShip(HexCoord position, int ownerId)
        {
            GameObject ghost = new GameObject($"Ghost_{position}");
            ghost.transform.SetParent(ghostContainer.transform);
            ghost.transform.position = position.ToWorldPosition() + Vector3.up * 0.1f; // Slightly elevated

            // Create simple cube as ghost (we don't have ship models to clone easily)
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(ghost.transform);
            cube.transform.localPosition = Vector3.zero;
            cube.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);

            // Make it semi-transparent with player color tint
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material ghostMat = new Material(Shader.Find("Standard"));
                ghostMat.SetFloat("_Mode", 3); // Transparent mode
                ghostMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ghostMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                ghostMat.SetInt("_ZWrite", 0);
                ghostMat.DisableKeyword("_ALPHATEST_ON");
                ghostMat.EnableKeyword("_ALPHABLEND_ON");
                ghostMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                ghostMat.renderQueue = 3000;

                Color tint = ownerId == 0 ? playerGhostTint : aiGhostTint;
                tint.a = ghostAlpha;
                ghostMat.color = tint;

                renderer.material = ghostMat;
            }

            return ghost;
        }

        private void UpdateGhosts()
        {
            float currentTime = Time.time;
            List<string> unitsToRemove = new List<string>();

            foreach (var kvp in unitGhosts)
            {
                Queue<GhostInstance> ghosts = kvp.Value;
                List<GhostInstance> toRemove = new List<GhostInstance>();

                foreach (GhostInstance ghost in ghosts)
                {
                    if (ghost.ghostObject == null)
                    {
                        toRemove.Add(ghost);
                        continue;
                    }

                    // Calculate fade based on age
                    float age = currentTime - ghost.spawnTime;
                    if (age >= ghostFadeDuration)
                    {
                        // Ghost expired, mark for removal
                        Destroy(ghost.ghostObject);
                        toRemove.Add(ghost);
                    }
                    else
                    {
                        // Fade out over time
                        float fadeProgress = age / ghostFadeDuration;
                        float alpha = ghostAlpha * (1f - fadeProgress);

                        Renderer renderer = ghost.ghostObject.GetComponentInChildren<Renderer>();
                        if (renderer != null && renderer.material != null)
                        {
                            Color color = renderer.material.color;
                            color.a = alpha;
                            renderer.material.color = color;
                        }
                    }
                }

                // Remove expired ghosts
                foreach (var ghost in toRemove)
                {
                    // Can't remove from queue while iterating, will handle cleanup next frame
                }

                if (ghosts.Count == 0)
                {
                    unitsToRemove.Add(kvp.Key);
                }
            }

            // Clean up empty unit entries
            foreach (string unitId in unitsToRemove)
            {
                unitGhosts.Remove(unitId);
            }
        }

        public void ClearAllGhosts()
        {
            foreach (var kvp in unitGhosts)
            {
                foreach (GhostInstance ghost in kvp.Value)
                {
                    if (ghost.ghostObject != null)
                    {
                        Destroy(ghost.ghostObject);
                    }
                }
            }
            unitGhosts.Clear();
        }

        private void OnDestroy()
        {
            ClearAllGhosts();
        }
    }
}
