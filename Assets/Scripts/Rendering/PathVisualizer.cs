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
        public Color futurePathColor = new Color(1f, 1f, 0f, 0.3f); // Faint yellow for future turns
        public float lineWidth = 0.15f;

        private GameObject pathContainer;
        private Dictionary<string, PathData> paths = new Dictionary<string, PathData>();

        private class PathData
        {
            public GameObject pathObject;
            public LineRenderer lineRenderer;
            public LineRenderer futureLineRenderer; // For future turns segment
            public List<GameObject> arrows;
            public List<GameObject> futureArrows; // Arrows for future segment
            public bool isPrimary;
            public int movementCapacity; // How many tiles can be moved this turn
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
        /// <param name="movementCapacity">How many tiles the unit can move this turn (default 3)</param>
        public void AddPath(string unitId, List<HexCoord> path, bool isPrimary, int movementCapacity = 3)
        {
            Debug.Log($"[PathVisualizer] AddPath called for {unitId}: path={path?.Count ?? 0} coords, isPrimary={isPrimary}, capacity={movementCapacity}");

            if (path == null || path.Count < 2)
            {
                Debug.LogWarning($"[PathVisualizer] Path is null or too short ({path?.Count ?? 0} coords), clearing path for {unitId}");
                // Remove path if it exists
                ClearPath(unitId);
                return;
            }

            // Validate movement capacity
            if (movementCapacity <= 0)
            {
                Debug.LogError($"[PathVisualizer] Invalid movementCapacity={movementCapacity} for {unitId}, defaulting to 3");
                movementCapacity = 3;
            }

            // Remove existing path for this unit if it exists
            ClearPath(unitId);

            // Create new path data
            PathData pathData = new PathData
            {
                isPrimary = isPrimary,
                arrows = new List<GameObject>(),
                futureArrows = new List<GameObject>(),
                movementCapacity = movementCapacity
            };

            // Create path object
            pathData.pathObject = new GameObject($"Path_{unitId}");
            pathData.pathObject.transform.SetParent(pathContainer.transform);

            // Calculate split point: first segment is up to movementCapacity moves
            // Path includes starting position, so movementCapacity+1 points for "this turn"
            int splitIndex = Mathf.Min(movementCapacity + 1, path.Count);
            Debug.Log($"[PathVisualizer] Splitting path at index {splitIndex} (capacity={movementCapacity}, pathCount={path.Count})");

            // Validate split index bounds
            if (splitIndex < 0 || splitIndex > path.Count)
            {
                Debug.LogError($"[PathVisualizer] BOUNDS ERROR: splitIndex={splitIndex} out of range [0, {path.Count}]");
                ClearPath(unitId);
                return;
            }

            List<HexCoord> thisTurnPath = path.GetRange(0, splitIndex);
            List<HexCoord> futurePath = splitIndex < path.Count ? path.GetRange(splitIndex - 1, path.Count - splitIndex + 1) : null;

            Debug.Log($"[PathVisualizer] Split result: thisTurnPath={thisTurnPath.Count} coords, futurePath={futurePath?.Count ?? 0} coords");

            // Create "this turn" line renderer
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

            // Configure "this turn" line renderer
            pathData.lineRenderer.startWidth = lineWidth;
            pathData.lineRenderer.endWidth = lineWidth;
            pathData.lineRenderer.positionCount = thisTurnPath.Count;
            pathData.lineRenderer.useWorldSpace = true;

            // Set "this turn" line positions
            for (int i = 0; i < thisTurnPath.Count; i++)
            {
                Vector3 worldPos = thisTurnPath[i].ToWorldPosition(hexSize);
                worldPos.y = pathHeight;
                pathData.lineRenderer.SetPosition(i, worldPos);
            }

            // Add direction arrows for "this turn" segment (full arrows for primary)
            if (isPrimary)
            {
                AddDirectionArrows(thisTurnPath, pathData, pathColor);
            }

            // Create "future turns" line renderer if path extends beyond this turn
            if (futurePath != null && futurePath.Count > 1)
            {
                Debug.Log($"[PathVisualizer] Creating dotted path for future segment: {futurePath.Count} coords");
                try
                {
                    // Create dotted line segments with gaps instead of solid line
                    // We'll create multiple short LineRenderers with gaps between them
                    CreateDottedPath(futurePath, pathData, isPrimary);

                    // Add fewer arrows for future path, and they're smaller/dimmer
                    if (isPrimary)
                    {
                        Color futureColor = isPrimary ? futurePathColor : new Color(futurePathColor.r, futurePathColor.g, futurePathColor.b, futurePathColor.a * 0.5f);
                        AddFutureDirectionArrows(futurePath, pathData, futureColor);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[PathVisualizer] EXCEPTION in CreateDottedPath for {unitId}:");
                    Debug.LogError($"  Future path count: {futurePath.Count}");
                    Debug.LogError($"  Exception: {ex.Message}");
                    Debug.LogError($"  Stack trace: {ex.StackTrace}");
                    // Continue without future path visualization
                }
            }
            else
            {
                Debug.Log($"[PathVisualizer] No future path segment (futurePath={(futurePath != null ? futurePath.Count.ToString() : "null")} coords)");
            }

            paths[unitId] = pathData;

            Debug.Log($"[PathVisualizer] Added {(isPrimary ? "primary" : "secondary")} path for {unitId} with {path.Count} waypoints. Total paths now: {paths.Count}");
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

            foreach (var arrow in pathData.futureArrows)
            {
                if (arrow != null)
                    Destroy(arrow);
            }
            pathData.futureArrows.Clear();

            // Update future line renderer color
            if (pathData.futureLineRenderer != null)
            {
                Color futureColor = isPrimary ? futurePathColor : new Color(futurePathColor.r, futurePathColor.g, futurePathColor.b, futurePathColor.a * 0.5f);
                if (pathData.futureLineRenderer.material != null)
                {
                    pathData.futureLineRenderer.material.color = futureColor;
                }
            }

            // Add arrows only for primary paths
            if (isPrimary && pathData.lineRenderer != null && pathData.lineRenderer.positionCount > 1)
            {
                List<HexCoord> thisTurnPath = new List<HexCoord>();
                for (int i = 0; i < pathData.lineRenderer.positionCount; i++)
                {
                    Vector3 pos = pathData.lineRenderer.GetPosition(i);
                    thisTurnPath.Add(HexCoord.FromWorldPosition(pos, hexSize));
                }
                AddDirectionArrows(thisTurnPath, pathData, pathColor);

                // Add future arrows if there's a future path
                if (pathData.futureLineRenderer != null && pathData.futureLineRenderer.positionCount > 1)
                {
                    List<HexCoord> futurePath = new List<HexCoord>();
                    for (int i = 0; i < pathData.futureLineRenderer.positionCount; i++)
                    {
                        Vector3 pos = pathData.futureLineRenderer.GetPosition(i);
                        futurePath.Add(HexCoord.FromWorldPosition(pos, hexSize));
                    }
                    Color futureColor = isPrimary ? futurePathColor : new Color(futurePathColor.r, futurePathColor.g, futurePathColor.b, futurePathColor.a * 0.5f);
                    AddFutureDirectionArrows(futurePath, pathData, futureColor);
                }
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
        /// Create a dotted path by making multiple short line segments with gaps
        /// </summary>
        private void CreateDottedPath(List<HexCoord> path, PathData pathData, bool isPrimary)
        {
            Debug.Log($"[PathVisualizer] CreateDottedPath called with {path.Count} coords, isPrimary={isPrimary}");

            // Validate path has at least 2 elements
            if (path == null || path.Count < 2)
            {
                Debug.LogError($"[PathVisualizer] CreateDottedPath: Invalid path (count={path?.Count ?? 0})");
                return;
            }

            // Create a container for dotted line segments
            GameObject dottedContainer = new GameObject("DottedPath");
            dottedContainer.transform.SetParent(pathData.pathObject.transform);

            // Build cumulative distance array for each waypoint
            List<float> cumulativeDistances = new List<float>();
            List<Vector3> worldPositions = new List<Vector3>();

            cumulativeDistances.Add(0f);
            worldPositions.Add(path[0].ToWorldPosition(hexSize));

            float totalPathLength = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 prevPos = path[i - 1].ToWorldPosition(hexSize);
                Vector3 currPos = path[i].ToWorldPosition(hexSize);
                float segmentLength = Vector3.Distance(prevPos, currPos);
                totalPathLength += segmentLength;
                cumulativeDistances.Add(totalPathLength);
                worldPositions.Add(currPos);
            }

            Debug.Log($"[PathVisualizer] Calculated total path length={totalPathLength}");

            // Dotted pattern: dash length and gap length
            float dashLength = 0.3f;
            float gapLength = 0.2f;
            float patternLength = dashLength + gapLength;

            // Find shader
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Standard");

            Color futureColor = isPrimary ? futurePathColor : new Color(futurePathColor.r, futurePathColor.g, futurePathColor.b, futurePathColor.a * 0.5f);

            // Calculate number of complete patterns we can fit
            int numDashes = Mathf.CeilToInt(totalPathLength / patternLength);
            Debug.Log($"[PathVisualizer] Creating {numDashes} dashes along path");

            // Create each dash by calculating its start and end positions
            for (int dashIndex = 0; dashIndex < numDashes; dashIndex++)
            {
                float dashStartDistance = dashIndex * patternLength;
                float dashEndDistance = Mathf.Min(dashStartDistance + dashLength, totalPathLength);

                // Skip if dash is beyond path length
                if (dashStartDistance >= totalPathLength)
                    break;

                // Find the world positions for dash start and end
                Vector3 dashStartPos = GetPositionAtDistance(dashStartDistance, worldPositions, cumulativeDistances);
                Vector3 dashEndPos = GetPositionAtDistance(dashEndDistance, worldPositions, cumulativeDistances);

                // Create line renderer for this dash
                GameObject dashObj = new GameObject($"Dash_{dashIndex}");
                dashObj.transform.SetParent(dottedContainer.transform);
                LineRenderer dashRenderer = dashObj.AddComponent<LineRenderer>();

                Material dashMaterial = new Material(shader);
                dashMaterial.color = futureColor;
                dashRenderer.material = dashMaterial;

                dashRenderer.startWidth = lineWidth * 0.8f;
                dashRenderer.endWidth = lineWidth * 0.8f;
                dashRenderer.positionCount = 2;
                dashRenderer.useWorldSpace = true;

                dashStartPos.y = pathHeight + 0.01f;
                dashEndPos.y = pathHeight + 0.01f;

                dashRenderer.SetPosition(0, dashStartPos);
                dashRenderer.SetPosition(1, dashEndPos);
            }

            Debug.Log($"[PathVisualizer] Completed dotted path generation");

            // Store reference to dotted container as futureLineRenderer (for cleanup)
            // We'll use a dummy LineRenderer just to hold the reference for cleanup
            pathData.futureLineRenderer = dottedContainer.AddComponent<LineRenderer>();
            pathData.futureLineRenderer.enabled = false; // Disable it since we don't actually use it
        }

        /// <summary>
        /// Get world position at a specific distance along the path
        /// </summary>
        private Vector3 GetPositionAtDistance(float distance, List<Vector3> worldPositions, List<float> cumulativeDistances)
        {
            // Handle edge cases
            if (distance <= 0f)
                return worldPositions[0];
            if (distance >= cumulativeDistances[cumulativeDistances.Count - 1])
                return worldPositions[worldPositions.Count - 1];

            // Find which segment the distance falls into
            for (int i = 1; i < cumulativeDistances.Count; i++)
            {
                if (distance <= cumulativeDistances[i])
                {
                    // Interpolate between waypoint i-1 and i
                    float segmentStartDist = cumulativeDistances[i - 1];
                    float segmentEndDist = cumulativeDistances[i];
                    float segmentLength = segmentEndDist - segmentStartDist;

                    if (segmentLength < 0.0001f)
                        return worldPositions[i - 1];

                    float t = (distance - segmentStartDist) / segmentLength;
                    return Vector3.Lerp(worldPositions[i - 1], worldPositions[i], t);
                }
            }

            // Should never reach here, but return last position as fallback
            return worldPositions[worldPositions.Count - 1];
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
                GameObject arrow = CreateArrow(currentPos, direction, pathData, color, 1.0f);
                pathData.arrows.Add(arrow);
            }

            // Always add arrow at the end
            if (path.Count >= 2)
            {
                Vector3 lastPos = path[path.Count - 1].ToWorldPosition(hexSize);
                Vector3 prevPos = path[path.Count - 2].ToWorldPosition(hexSize);
                Vector3 direction = (lastPos - prevPos).normalized;
                GameObject arrow = CreateArrow(lastPos, direction, pathData, color, 1.0f);
                pathData.arrows.Add(arrow);
            }
        }

        /// <summary>
        /// Add small arrow indicators for future turns (fewer, smaller arrows)
        /// </summary>
        private void AddFutureDirectionArrows(List<HexCoord> path, PathData pathData, Color color)
        {
            // Add arrows every 3rd waypoint for future path
            for (int i = 2; i < path.Count - 1; i += 3)
            {
                Vector3 currentPos = path[i].ToWorldPosition(hexSize);
                Vector3 nextPos = path[i + 1].ToWorldPosition(hexSize);

                // Calculate direction
                Vector3 direction = (nextPos - currentPos).normalized;

                // Create smaller, dimmer arrow
                GameObject arrow = CreateArrow(currentPos, direction, pathData, color, 0.7f);
                pathData.futureArrows.Add(arrow);
            }

            // Add arrow at the end
            if (path.Count >= 2)
            {
                Vector3 lastPos = path[path.Count - 1].ToWorldPosition(hexSize);
                Vector3 prevPos = path[path.Count - 2].ToWorldPosition(hexSize);
                Vector3 direction = (lastPos - prevPos).normalized;
                GameObject arrow = CreateArrow(lastPos, direction, pathData, color, 0.7f);
                pathData.futureArrows.Add(arrow);
            }
        }

        /// <summary>
        /// Create a simple arrow indicator
        /// </summary>
        private GameObject CreateArrow(Vector3 position, Vector3 direction, PathData pathData, Color color, float scale = 1.0f)
        {
            GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.name = "PathArrow";
            arrow.transform.SetParent(pathData.pathObject.transform);

            position.y = pathHeight + 0.05f; // Slightly above the line
            arrow.transform.position = position;
            arrow.transform.localScale = new Vector3(0.2f * scale, 0.05f * scale, 0.3f * scale);

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
