using System.Collections.Generic;
using PlunkAndPlunder.Core;
using PlunkAndPlunder.Map;
using UnityEngine;

namespace PlunkAndPlunder.Rendering
{
    /// <summary>
    /// Renders hex grid tiles as simple meshes
    /// </summary>
    public class HexRenderer : MonoBehaviour
    {
        [Header("Visuals")]
        public Material seaMaterial;
        public Material landMaterial;
        public Material harborMaterial;

        [Header("Settings")]
        public float hexSize = 1f;
        public float tileHeight = 0.2f;

        private Dictionary<HexCoord, GameObject> tileObjects = new Dictionary<HexCoord, GameObject>();
        private GameObject tilesContainer;

        private void Awake()
        {
            CreateMaterials();
            tilesContainer = new GameObject("Tiles");
            tilesContainer.transform.SetParent(transform);
        }

        private void CreateMaterials()
        {
            // Find shader - try Standard first, fall back to built-in shaders
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Diffuse");
                Debug.LogWarning("[HexRenderer] Standard shader not found, using Legacy Diffuse");
            }
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
                Debug.LogWarning("[HexRenderer] Diffuse shader not found, using Unlit/Color");
            }

            if (seaMaterial == null)
            {
                seaMaterial = new Material(shader);
                seaMaterial.color = new Color(0.1f, 0.3f, 0.7f); // Darker blue for water
            }

            if (landMaterial == null)
            {
                landMaterial = new Material(shader);
                landMaterial.color = new Color(0.3f, 0.7f, 0.2f); // Brighter green for land
            }

            if (harborMaterial == null)
            {
                harborMaterial = new Material(shader);
                harborMaterial.color = new Color(0.8f, 0.6f, 0.3f); // Brighter brown/tan for harbor
            }

            Debug.Log($"[HexRenderer] Materials created with shader: {shader.name}");
        }

        public void RenderGrid(HexGrid grid)
        {
            ClearTiles();

            int seaCount = 0, landCount = 0, harborCount = 0;
            Vector3 minPos = Vector3.positiveInfinity;
            Vector3 maxPos = Vector3.negativeInfinity;

            foreach (Tile tile in grid.GetAllTiles())
            {
                CreateTile(tile);
                Vector3 pos = tile.coord.ToWorldPosition(hexSize);
                minPos = Vector3.Min(minPos, pos);
                maxPos = Vector3.Max(maxPos, pos);

                if (tile.type == TileType.SEA) seaCount++;
                else if (tile.type == TileType.LAND) landCount++;
                else if (tile.type == TileType.HARBOR) harborCount++;
            }

            Vector3 center = (minPos + maxPos) / 2f;
            Vector3 size = maxPos - minPos;
            Debug.Log($"[HexRenderer] Rendered {tileObjects.Count} tiles ({seaCount} sea, {landCount} land, {harborCount} harbor)");
            Debug.Log($"[HexRenderer] Map bounds: center={center}, size={size}, min={minPos}, max={maxPos}");
        }

        private void CreateTile(Tile tile)
        {
            GameObject tileObj = new GameObject($"Tile_{tile.coord}");
            tileObj.transform.SetParent(tilesContainer.transform);
            tileObj.transform.position = tile.coord.ToWorldPosition(hexSize);

            // Create hex mesh
            MeshFilter meshFilter = tileObj.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = tileObj.AddComponent<MeshRenderer>();

            meshFilter.mesh = CreateHexMesh();

            // Set material based on tile type
            switch (tile.type)
            {
                case TileType.SEA:
                    meshRenderer.material = seaMaterial;
                    break;
                case TileType.LAND:
                    meshRenderer.material = landMaterial;
                    tileObj.transform.position += Vector3.up * tileHeight;
                    break;
                case TileType.HARBOR:
                    meshRenderer.material = harborMaterial;
                    tileObj.transform.position += Vector3.up * (tileHeight * 0.5f);
                    break;
            }

            // Add collider for mouse interaction
            MeshCollider collider = tileObj.AddComponent<MeshCollider>();
            collider.sharedMesh = meshFilter.mesh;

            // Note: Tag removed for MVP - not needed for basic functionality
            // tileObj.tag = "Tile";
            tileObj.layer = LayerMask.NameToLayer("Default");

            // Add black border around hex
            AddHexBorder(tileObj);

            // Store reference
            tileObjects[tile.coord] = tileObj;
        }

        private void AddHexBorder(GameObject tileObj)
        {
            // Create border line renderer
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(tileObj.transform, false);
            borderObj.transform.localPosition = Vector3.up * 0.01f; // Slightly above tile to prevent z-fighting

            LineRenderer lineRenderer = borderObj.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.black;
            lineRenderer.endColor = Color.black;
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.positionCount = 7; // 6 vertices + 1 to close the loop
            lineRenderer.loop = true;
            lineRenderer.useWorldSpace = false;

            // Set hex border vertices
            for (int i = 0; i < 6; i++)
            {
                float angle = (60f * i + 30f) * Mathf.Deg2Rad;
                Vector3 vertex = new Vector3(
                    hexSize * Mathf.Cos(angle),
                    0f,
                    hexSize * Mathf.Sin(angle)
                );
                lineRenderer.SetPosition(i, vertex);
            }

            // Close the loop
            float angle0 = 30f * Mathf.Deg2Rad;
            lineRenderer.SetPosition(6, new Vector3(
                hexSize * Mathf.Cos(angle0),
                0f,
                hexSize * Mathf.Sin(angle0)
            ));
        }

        private Mesh CreateHexMesh()
        {
            Mesh mesh = new Mesh();

            // Hex vertices (flat-top orientation - vertices at 30°, 90°, 150°, 210°, 270°, 330°)
            Vector3[] vertices = new Vector3[7];
            vertices[0] = Vector3.zero; // Center

            for (int i = 0; i < 6; i++)
            {
                float angle = (60f * i + 30f) * Mathf.Deg2Rad; // Offset by 30° for flat-top
                vertices[i + 1] = new Vector3(
                    hexSize * Mathf.Cos(angle),
                    0f,
                    hexSize * Mathf.Sin(angle)
                );
            }

            mesh.vertices = vertices;

            // Triangles (winding order: clockwise when viewed from above for correct normals)
            int[] triangles = new int[18];
            for (int i = 0; i < 6; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = (i + 1) % 6 + 1; // Swapped order
                triangles[i * 3 + 2] = i + 1;             // Swapped order
            }

            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }

        public void HighlightTile(HexCoord coord, Color color)
        {
            if (tileObjects.TryGetValue(coord, out GameObject tileObj))
            {
                MeshRenderer renderer = tileObj.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material.color = color;
                }
            }
        }

        public void ResetTileHighlight(HexCoord coord, Tile tile)
        {
            if (tileObjects.TryGetValue(coord, out GameObject tileObj))
            {
                MeshRenderer renderer = tileObj.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    switch (tile.type)
                    {
                        case TileType.SEA:
                            renderer.material = seaMaterial;
                            break;
                        case TileType.LAND:
                            renderer.material = landMaterial;
                            break;
                        case TileType.HARBOR:
                            renderer.material = harborMaterial;
                            break;
                    }
                }
            }
        }

        private void ClearTiles()
        {
            foreach (GameObject tileObj in tileObjects.Values)
            {
                Destroy(tileObj);
            }
            tileObjects.Clear();
        }

        private void OnDestroy()
        {
            ClearTiles();
        }
    }
}
