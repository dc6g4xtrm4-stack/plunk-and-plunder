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
        public Color pathColor = new Color(1f, 1f, 0f, 0.8f); // Yellow
        public float lineWidth = 0.15f;

        private GameObject pathContainer;
        private GameObject currentPathObject;
        private LineRenderer lineRenderer;

        private void Awake()
        {
            pathContainer = new GameObject("PathVisualizations");
            pathContainer.transform.SetParent(transform);
        }

        /// <summary>
        /// Visualize a path for a unit
        /// </summary>
        public void ShowPath(List<HexCoord> path)
        {
            ClearPath();

            if (path == null || path.Count < 2)
                return;

            // Create path object
            currentPathObject = new GameObject("Path");
            currentPathObject.transform.SetParent(pathContainer.transform);

            // Create line renderer
            lineRenderer = currentPathObject.AddComponent<LineRenderer>();

            // Find a suitable shader
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material lineMaterial = new Material(shader);
            lineMaterial.color = pathColor;
            lineRenderer.material = lineMaterial;

            // Configure line renderer
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.positionCount = path.Count;
            lineRenderer.useWorldSpace = true;

            // Set line positions from path
            for (int i = 0; i < path.Count; i++)
            {
                Vector3 worldPos = path[i].ToWorldPosition(hexSize);
                worldPos.y = pathHeight;
                lineRenderer.SetPosition(i, worldPos);
            }

            // Add direction arrows along the path
            AddDirectionArrows(path);

            Debug.Log($"[PathVisualizer] Showing path with {path.Count} waypoints");
        }

        /// <summary>
        /// Add small arrow indicators to show movement direction
        /// </summary>
        private void AddDirectionArrows(List<HexCoord> path)
        {
            // Add arrows every other waypoint (skip first and last)
            for (int i = 1; i < path.Count - 1; i += 2)
            {
                Vector3 currentPos = path[i].ToWorldPosition(hexSize);
                Vector3 nextPos = path[i + 1].ToWorldPosition(hexSize);

                // Calculate direction
                Vector3 direction = (nextPos - currentPos).normalized;

                // Create small arrow
                CreateArrow(currentPos, direction);
            }

            // Always add arrow at the end
            if (path.Count >= 2)
            {
                Vector3 lastPos = path[path.Count - 1].ToWorldPosition(hexSize);
                Vector3 prevPos = path[path.Count - 2].ToWorldPosition(hexSize);
                Vector3 direction = (lastPos - prevPos).normalized;
                CreateArrow(lastPos, direction);
            }
        }

        /// <summary>
        /// Create a simple arrow indicator
        /// </summary>
        private void CreateArrow(Vector3 position, Vector3 direction)
        {
            GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.name = "PathArrow";
            arrow.transform.SetParent(currentPathObject.transform);

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
                mat.color = pathColor;
                renderer.material = mat;
            }

            // Remove collider (we don't need it for visuals)
            Collider collider = arrow.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        /// <summary>
        /// Clear the current path visualization
        /// </summary>
        public void ClearPath()
        {
            if (currentPathObject != null)
            {
                Destroy(currentPathObject);
                currentPathObject = null;
                lineRenderer = null;
            }
        }

        private void OnDestroy()
        {
            ClearPath();
        }
    }
}
