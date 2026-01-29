using System.Collections.Generic;
using PlunkAndPlunder.Map;
using UnityEngine;

namespace PlunkAndPlunder.Rendering
{
    /// <summary>
    /// Visualizes unit movement paths as lines with direction arrows
    /// </summary>
    public class PathVisualizer : MonoBehaviour
    {
        [Header("Path Settings")]
        public float hexSize = 1f;
        public float pathHeight = 0.2f;
        public Color primaryPathColor = new Color(1f, 1f, 0f, 0.8f); // Solid yellow
        public Color secondaryPathColor = new Color(1f, 1f, 0f, 0.5f); // Semi-transparent yellow
        public float lineWidth = 0.15f;

        private GameObject pathContainer;
        private Dictionary<string, PathData> paths = new Dictionary<string, PathData>();

        private class PathData
        {
            public GameObject pathObject;
            public LineRenderer lineRenderer;
            public List<GameObject> arrows;
            public bool isPrimary;
        }

        private void Awake()
        {
            pathContainer = new GameObject("PathVisualizations");
            pathContainer.transform.SetParent(transform);
        }

        /// <summary>
        /// Add or update a path for a specific unit
        /// </summary>
        /// <param name="unitId">Unique identifier for the unit</param>
        /// <param name="path">The path to visualize</param>
        /// <param name="isPrimary">Whether this is the primary (selected) unit's path</param>
        public void AddPath(string unitId, List<HexCoord> path, bool isPrimary)
        {
            if (path == null || path.Count < 2)
            {
                // Remove path if it exists
                ClearPath(unitId);
                return;
            }

            // Remove existing path for this unit if it exists
            ClearPath(unitId);

            // Create new path data
            PathData pathData = new PathData
            {
                isPrimary = isPrimary,
                arrows = new List<GameObject>()
            };

            // Create path object
            pathData.pathObject = new GameObject($"Path_{unitId}");
            pathData.pathObject.transform.SetParent(pathContainer.transform);

            // Create line renderer
            pathData.lineRenderer = pathData.pathObject.AddComponent<LineRenderer>();

            // Find a suitable shader
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material lineMaterial = new Material(shader);
            Color pathColor = isPrimary ? primaryPathColor : secondaryPathColor;
            lineMaterial.color = pathColor;
            pathData.lineRenderer.material = lineMaterial;

            // Configure line renderer
            pathData.lineRenderer.startWidth = lineWidth;
            pathData.lineRenderer.endWidth = lineWidth;
            pathData.lineRenderer.positionCount = path.Count;
            pathData.lineRenderer.useWorldSpace = true;

            // Set line positions from path
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 worldPos = path[i].ToWorldPosition(hexSize);
                worldPos.y = pathHeight;
                pathData.lineRenderer.SetPosition(i, worldPos);
            }

            // Add direction arrows along the path (full arrows for primary, fewer/none for secondary)
            if (isPrimary)
            {
                AddDirectionArrows(path, pathData, pathColor);
            }

            paths[unitId] = pathData;

            Debug.Log($"[PathVisualizer] Added {(isPrimary ? "primary" : "secondary")} path for {unitId} with {path.Count} waypoints");
        }

        /// <summary>
        /// Visualize a path for a unit (legacy method for backward compatibility)
        /// </summary>
        public void ShowPath(List<HexCoord> path)
        {
            AddPath("default", path, true);
        }

        /// <summary>
        /// Update whether a path is primary or secondary
        /// </summary>
        public void SetPathPrimary(string unitId, bool isPrimary)
        {
            if (!paths.ContainsKey(unitId))
                return;

            PathData pathData = paths[unitId];
            if (pathData.isPrimary == isPrimary)
                return; // No change needed

            pathData.isPrimary = isPrimary;

            // Update color
            Color pathColor = isPrimary ? primaryPathColor : secondaryPathColor;
            if (pathData.lineRenderer != null && pathData.lineRenderer.material != null)
            {
                pathData.lineRenderer.material.color = pathColor;
            }

            // Update arrows - remove old ones and add new ones if needed
            foreach (var arrow in pathData.arrows)
            {
                if (arrow != null)
                    Destroy(arrow);
            }
            pathData.arrows.Clear();

            // Add arrows only for primary paths
            if (isPrimary && pathData.lineRenderer != null && pathData.lineRenderer.positionCount > 1)
            {
                List<HexCoord> path = new List<HexCoord>();
                for (int i = 0; i < pathData.lineRenderer.positionCount; i++)
                {
                    Vector3 pos = pathData.lineRenderer.GetPosition(i);
                    path.Add(HexCoord.FromWorldPosition(pos, hexSize));
                }
                AddDirectionArrows(path, pathData, pathColor);
            }
            else
            {
                // Update arrow colors for existing arrows
                foreach (var arrow in pathData.arrows)
                {
                    if (arrow != null)
                    {
                        MeshRenderer renderer = arrow.GetComponent<MeshRenderer>();
                        if (renderer != null && renderer.material != null)
                        {
                            renderer.material.color = pathColor;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add small arrow indicators to show movement direction
        /// </summary>
        private void AddDirectionArrows(List<HexCoord> path, PathData pathData, Color color)
        {
            // Add arrows every other waypoint (skip first and last)
            for (int i = 1; i < path.Count - 1; i += 2)
            {
                Vector3 currentPos = path[i].ToWorldPosition(hexSize);
                Vector3 nextPos = path[i + 1].ToWorldPosition(hexSize);

                // Calculate direction
                Vector3 direction = (nextPos - currentPos).normalized;

                // Create small arrow
                GameObject arrow = CreateArrow(currentPos, direction, pathData, color);
                pathData.arrows.Add(arrow);
            }

            // Always add arrow at the end
            if (path.Count >= 2)
            {
                Vector3 lastPos = path[path.Count - 1].ToWorldPosition(hexSize);
                Vector3 prevPos = path[path.Count - 2].ToWorldPosition(hexSize);
                Vector3 direction = (lastPos - prevPos).normalized;
                GameObject arrow = CreateArrow(lastPos, direction, pathData, color);
                pathData.arrows.Add(arrow);
            }
        }

        /// <summary>
        /// Create a simple arrow indicator
        /// </summary>
        private GameObject CreateArrow(Vector3 position, Vector3 direction, PathData pathData, Color color)
        {
            GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.name = "PathArrow";
            arrow.transform.SetParent(pathData.pathObject.transform);

            position.y = pathHeight + 0.05f; // Slightly above the line
            arrow.transform.position = position;
            arrow.transform.localScale = new Vector3(0.2f, 0.05f, 0.3f);

            // Rotate to face direction
            if (direction != Vector3.zero)
            {
                float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                arrow.transform.rotation = Quaternion.Euler(0, angle, 0);
            }

            // Set color
            MeshRenderer renderer = arrow.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse") ?? Shader.Find("Unlit/Color");
                Material mat = new Material(shader);
                mat.color = color;
                renderer.material = mat;
            }

            // Remove collider (we don't need it for visuals)
            Collider collider = arrow.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            return arrow;
        }

        /// <summary>
        /// Clear the path visualization for a specific unit
        /// </summary>
        public void ClearPath(string unitId)
        {
            if (paths.ContainsKey(unitId))
            {
                PathData pathData = paths[unitId];
                if (pathData.pathObject != null)
                {
                    Destroy(pathData.pathObject);
                }
                paths.Remove(unitId);
                Debug.Log($"[PathVisualizer] Cleared path for {unitId}");
            }
        }

        /// <summary>
        /// Clear the current path visualization (legacy method for backward compatibility)
        /// </summary>
        public void ClearPath()
        {
            ClearPath("default");
        }

        /// <summary>
        /// Clear all path visualizations
        /// </summary>
        public void ClearAllPaths()
        {
            foreach (var kvp in paths)
            {
                if (kvp.Value.pathObject != null)
                {
                    Destroy(kvp.Value.pathObject);
                }
            }
            paths.Clear();
            Debug.Log("[PathVisualizer] Cleared all paths");
        }

        private void OnDestroy()
        {
            ClearAllPaths();
        }
    }
}
