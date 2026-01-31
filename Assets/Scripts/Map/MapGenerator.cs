using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlunkAndPlunder.Map
{
    public class MapGenerator
    {
        private System.Random random;
        private int seed;

        public MapGenerator(int seed = 0)
        {
            this.seed = seed == 0 ? UnityEngine.Random.Range(0, int.MaxValue) : seed;
            this.random = new System.Random(this.seed);
        }

        public HexGrid GenerateMap(int numSeaTiles = 500, int numIslands = 25, int minIslandSize = 4, int maxIslandSize = 8, int numPlayers = 4)
        {
            HexGrid grid = new HexGrid();

            Debug.Log($"[MapGenerator] Generating map for {numPlayers} players with seed {seed}");

            // Generate sea tiles in a rough circular pattern
            int radius = Mathf.CeilToInt(Mathf.Sqrt(numSeaTiles / Mathf.PI));
            HashSet<HexCoord> seaCoords = new HashSet<HexCoord>();

            // Start from center and expand
            Queue<HexCoord> frontier = new Queue<HexCoord>();
            HexCoord center = new HexCoord(0, 0);
            frontier.Enqueue(center);
            seaCoords.Add(center);

            while (seaCoords.Count < numSeaTiles && frontier.Count > 0)
            {
                HexCoord current = frontier.Dequeue();

                foreach (HexCoord neighbor in current.GetNeighbors())
                {
                    if (!seaCoords.Contains(neighbor) && neighbor.Distance(center) <= radius)
                    {
                        seaCoords.Add(neighbor);
                        frontier.Enqueue(neighbor);

                        if (seaCoords.Count >= numSeaTiles)
                            break;
                    }
                }
            }

            // Create sea tiles
            foreach (HexCoord coord in seaCoords)
            {
                grid.AddTile(new Tile(coord, TileType.SEA));
            }

            // Track used coordinates to avoid overlaps
            HashSet<HexCoord> usedCoords = new HashSet<HexCoord>();

            // CRITICAL: Generate player start islands FIRST with proper spacing
            List<PlayerStartIsland> playerStartIslands = GeneratePlayerStartIslands(numPlayers, seaCoords, usedCoords, radius);

            // Add player start islands to the grid
            int playerIslandId = 0;
            foreach (PlayerStartIsland startIsland in playerStartIslands)
            {
                foreach (HexCoord coord in startIsland.landTiles)
                {
                    grid.tiles[coord] = new Tile(coord, TileType.LAND, playerIslandId);
                    usedCoords.Add(coord);
                }

                foreach (HexCoord coord in startIsland.harbourTiles)
                {
                    grid.tiles[coord] = new Tile(coord, TileType.HARBOR, playerIslandId);
                    usedCoords.Add(coord);
                }

                Debug.Log($"[MapGenerator] Player {playerIslandId} start island created: {startIsland.landTiles.Count} land + {startIsland.harbourTiles.Count} harbours at center {startIsland.center}");
                playerIslandId++;
            }

            // Store player start islands in a way GameManager can access them
            grid.playerStartIslands = playerStartIslands;

            // Generate additional random islands (not player starts)
            List<HexCoord> seaList = new List<HexCoord>(seaCoords);

            for (int islandId = playerIslandId; islandId < playerIslandId + numIslands; islandId++)
            {
                // Pick random sea tile as island center
                HexCoord islandCenter;
                int attempts = 0;
                do
                {
                    islandCenter = seaList[random.Next(seaList.Count)];
                    attempts++;
                } while (usedCoords.Contains(islandCenter) && attempts < 100);

                if (attempts >= 100) continue; // Skip if can't find spot

                // Generate island
                int islandSize = random.Next(minIslandSize, maxIslandSize + 1);
                List<HexCoord> islandTiles = GenerateIsland(islandCenter, islandSize, seaCoords, usedCoords);

                // Find tiles adjacent to sea (valid harbor locations)
                List<int> validHarborIndices = new List<int>();
                for (int i = 0; i < islandTiles.Count; i++)
                {
                    HexCoord coord = islandTiles[i];
                    // Check if any neighbor is a sea tile
                    foreach (HexCoord neighbor in coord.GetNeighbors())
                    {
                        if (seaCoords.Contains(neighbor) && !usedCoords.Contains(neighbor))
                        {
                            validHarborIndices.Add(i);
                            break; // This tile is valid, no need to check other neighbors
                        }
                    }
                }

                // Pick harbor from valid locations (or fallback to any tile if none found)
                int harborIndex;
                if (validHarborIndices.Count > 0)
                {
                    harborIndex = validHarborIndices[random.Next(validHarborIndices.Count)];
                }
                else
                {
                    // Fallback: pick the tile closest to the edge (should rarely happen)
                    Debug.LogWarning($"[MapGenerator] Island {islandId} has no sea-adjacent tiles, using fallback");
                    harborIndex = random.Next(islandTiles.Count);
                }

                // Convert tiles to LAND and add one HARBOR
                for (int i = 0; i < islandTiles.Count; i++)
                {
                    HexCoord coord = islandTiles[i];
                    TileType type = (i == harborIndex) ? TileType.HARBOR : TileType.LAND;
                    grid.tiles[coord] = new Tile(coord, type, islandId);
                    usedCoords.Add(coord);
                }
            }

            Debug.Log($"Map generated with seed {seed}: {seaCoords.Count} sea tiles, {numIslands} islands");
            return grid;
        }

        private List<HexCoord> GenerateIsland(HexCoord center, int size, HashSet<HexCoord> validCoords, HashSet<HexCoord> usedCoords)
        {
            List<HexCoord> island = new List<HexCoord>();
            HashSet<HexCoord> islandSet = new HashSet<HexCoord>();
            Queue<HexCoord> frontier = new Queue<HexCoord>();

            if (!usedCoords.Contains(center) && validCoords.Contains(center))
            {
                island.Add(center);
                islandSet.Add(center);
                frontier.Enqueue(center);
            }

            while (island.Count < size && frontier.Count > 0)
            {
                HexCoord current = frontier.Dequeue();
                List<HexCoord> neighbors = new List<HexCoord>(current.GetNeighbors());

                // Shuffle neighbors for variety
                for (int i = 0; i < neighbors.Count; i++)
                {
                    int j = random.Next(i, neighbors.Count);
                    HexCoord temp = neighbors[i];
                    neighbors[i] = neighbors[j];
                    neighbors[j] = temp;
                }

                foreach (HexCoord neighbor in neighbors)
                {
                    if (!islandSet.Contains(neighbor) && !usedCoords.Contains(neighbor) && validCoords.Contains(neighbor))
                    {
                        island.Add(neighbor);
                        islandSet.Add(neighbor);
                        frontier.Enqueue(neighbor);

                        if (island.Count >= size)
                            break;
                    }
                }
            }

            return island;
        }

        /// <summary>
        /// Generate player start islands with proper spacing
        /// Each island has 15-20 land tiles and 3 connected harbour tiles along an edge
        /// </summary>
        private List<PlayerStartIsland> GeneratePlayerStartIslands(int numPlayers, HashSet<HexCoord> seaCoords, HashSet<HexCoord> usedCoords, int mapRadius)
        {
            List<PlayerStartIsland> startIslands = new List<PlayerStartIsland>();

            // Calculate positions for player islands spaced evenly around the map
            // Place them at ~60-70% of the map radius for good spacing
            float placementRadius = mapRadius * 0.65f;
            float angleStep = 360f / numPlayers;

            Debug.Log($"[MapGenerator] Generating {numPlayers} player start islands at radius {placementRadius}");

            for (int i = 0; i < numPlayers; i++)
            {
                float angle = angleStep * i;
                float angleRad = angle * Mathf.Deg2Rad;

                // Calculate island center position
                // Use axial coordinates conversion from cartesian
                float x = placementRadius * Mathf.Cos(angleRad);
                float z = placementRadius * Mathf.Sin(angleRad);

                // Convert to axial hex coordinates (flat-top hexagons)
                int q = Mathf.RoundToInt((Mathf.Sqrt(3f) / 3f * x - 1f / 3f * z));
                int r = Mathf.RoundToInt((2f / 3f * z));

                HexCoord islandCenter = new HexCoord(q, r);

                // Ensure the center is a sea tile
                if (!seaCoords.Contains(islandCenter))
                {
                    // Find nearest sea tile
                    HexCoord nearestSea = FindNearestSeaTile(islandCenter, seaCoords);
                    if (nearestSea != null)
                    {
                        islandCenter = nearestSea;
                    }
                }

                Debug.Log($"[MapGenerator] Player {i} island center: {islandCenter} (angle: {angle}°)");

                // Generate the island
                PlayerStartIsland island = GeneratePlayerStartIsland(islandCenter, seaCoords, usedCoords, i);
                if (island != null)
                {
                    startIslands.Add(island);
                }
                else
                {
                    Debug.LogError($"[MapGenerator] Failed to generate start island for player {i}");
                }
            }

            return startIslands;
        }

        /// <summary>
        /// Generate a single player start island
        /// </summary>
        private PlayerStartIsland GeneratePlayerStartIsland(HexCoord center, HashSet<HexCoord> seaCoords, HashSet<HexCoord> usedCoords, int playerId)
        {
            PlayerStartIsland island = new PlayerStartIsland();
            island.center = center;
            island.playerId = playerId;
            island.landTiles = new List<HexCoord>();
            island.harbourTiles = new List<HexCoord>();
            island.shipyardPosition = default(HexCoord);
            island.shipSpawnPositions = new List<HexCoord>();

            // Generate 15-20 land tiles using flood fill
            int targetSize = random.Next(15, 21); // 15-20 tiles
            HashSet<HexCoord> islandSet = new HashSet<HexCoord>();
            Queue<HexCoord> frontier = new Queue<HexCoord>();

            if (seaCoords.Contains(center) && !usedCoords.Contains(center))
            {
                island.landTiles.Add(center);
                islandSet.Add(center);
                frontier.Enqueue(center);
            }
            else
            {
                Debug.LogError($"[MapGenerator] Invalid center for player {playerId} island: {center}");
                return null;
            }

            // Grow the island
            while (island.landTiles.Count < targetSize && frontier.Count > 0)
            {
                HexCoord current = frontier.Dequeue();
                List<HexCoord> neighbors = new List<HexCoord>(current.GetNeighbors());

                // Shuffle for variety
                for (int i = 0; i < neighbors.Count; i++)
                {
                    int j = random.Next(i, neighbors.Count);
                    HexCoord temp = neighbors[i];
                    neighbors[i] = neighbors[j];
                    neighbors[j] = temp;
                }

                foreach (HexCoord neighbor in neighbors)
                {
                    if (!islandSet.Contains(neighbor) && !usedCoords.Contains(neighbor) && seaCoords.Contains(neighbor))
                    {
                        island.landTiles.Add(neighbor);
                        islandSet.Add(neighbor);
                        frontier.Enqueue(neighbor);

                        if (island.landTiles.Count >= targetSize)
                            break;
                    }
                }
            }

            Debug.Log($"[MapGenerator] Player {playerId} island has {island.landTiles.Count} land tiles");

            // Find an edge of the island to place 3 connected harbours
            // Look for a sequence of 3 adjacent tiles that are all on the edge (have sea neighbors)
            List<HexCoord> edgeTiles = new List<HexCoord>();
            foreach (HexCoord tile in island.landTiles)
            {
                // Check if this tile is on the edge (has at least one sea neighbor)
                bool hasSeaNeighbor = false;
                foreach (HexCoord neighbor in tile.GetNeighbors())
                {
                    if (seaCoords.Contains(neighbor) && !islandSet.Contains(neighbor))
                    {
                        hasSeaNeighbor = true;
                        break;
                    }
                }
                if (hasSeaNeighbor)
                {
                    edgeTiles.Add(tile);
                }
            }

            // Find 3 connected edge tiles for harbours
            List<HexCoord> harbourPositions = FindConnectedEdgeTiles(edgeTiles, 3);
            if (harbourPositions != null && harbourPositions.Count == 3)
            {
                island.harbourTiles = harbourPositions;
                Debug.Log($"[MapGenerator] Player {playerId} has {island.harbourTiles.Count} harbour tiles: {string.Join(", ", island.harbourTiles)}");

                // Remove harbour tiles from land tiles
                foreach (HexCoord harbour in island.harbourTiles)
                {
                    island.landTiles.Remove(harbour);
                }

                // Pick one harbour for the shipyard (the middle one)
                island.shipyardPosition = island.harbourTiles[1];

                // Find ship spawn positions (sea tiles adjacent to harbours)
                HashSet<HexCoord> spawnSet = new HashSet<HexCoord>();
                foreach (HexCoord harbour in island.harbourTiles)
                {
                    foreach (HexCoord neighbor in harbour.GetNeighbors())
                    {
                        if (seaCoords.Contains(neighbor) && !islandSet.Contains(neighbor) && !usedCoords.Contains(neighbor))
                        {
                            spawnSet.Add(neighbor);
                            if (spawnSet.Count >= 3) break;
                        }
                    }
                    if (spawnSet.Count >= 3) break;
                }

                island.shipSpawnPositions = new List<HexCoord>(spawnSet);

                // Ensure we have at least 3 spawn positions
                if (island.shipSpawnPositions.Count < 3)
                {
                    Debug.LogWarning($"[MapGenerator] Player {playerId} only has {island.shipSpawnPositions.Count} spawn positions (need 3)");
                    // Try to find more spawn positions around the harbours
                    foreach (HexCoord harbour in island.harbourTiles)
                    {
                        foreach (HexCoord neighbor in harbour.GetNeighbors())
                        {
                            if (seaCoords.Contains(neighbor) && !island.shipSpawnPositions.Contains(neighbor))
                            {
                                island.shipSpawnPositions.Add(neighbor);
                                if (island.shipSpawnPositions.Count >= 3) break;
                            }
                        }
                        if (island.shipSpawnPositions.Count >= 3) break;
                    }
                }

                Debug.Log($"[MapGenerator] Player {playerId} has {island.shipSpawnPositions.Count} ship spawn positions");
            }
            else
            {
                Debug.LogError($"[MapGenerator] Could not find 3 connected harbour tiles for player {playerId}");
                return null;
            }

            return island;
        }

        /// <summary>
        /// Find a sequence of N connected tiles from a list
        /// </summary>
        private List<HexCoord> FindConnectedEdgeTiles(List<HexCoord> edgeTiles, int count)
        {
            if (edgeTiles.Count < count) return null;

            // Try each edge tile as a starting point
            foreach (HexCoord start in edgeTiles)
            {
                List<HexCoord> connected = new List<HexCoord> { start };
                HashSet<HexCoord> visited = new HashSet<HexCoord> { start };

                // Try to build a chain of connected tiles
                Queue<HexCoord> frontier = new Queue<HexCoord>();
                frontier.Enqueue(start);

                while (connected.Count < count && frontier.Count > 0)
                {
                    HexCoord current = frontier.Dequeue();

                    foreach (HexCoord neighbor in current.GetNeighbors())
                    {
                        if (edgeTiles.Contains(neighbor) && !visited.Contains(neighbor))
                        {
                            connected.Add(neighbor);
                            visited.Add(neighbor);
                            frontier.Enqueue(neighbor);

                            if (connected.Count >= count)
                                return connected;
                        }
                    }
                }
            }

            // Fallback: just take the first N tiles
            return edgeTiles.GetRange(0, Mathf.Min(count, edgeTiles.Count));
        }

        /// <summary>
        /// Find the nearest sea tile to a given position
        /// </summary>
        private HexCoord FindNearestSeaTile(HexCoord position, HashSet<HexCoord> seaCoords)
        {
            HexCoord nearest = default(HexCoord);
            int minDistance = int.MaxValue;

            foreach (HexCoord sea in seaCoords)
            {
                int dist = position.Distance(sea);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = sea;
                }
            }

            return nearest;
        }
    }

    /// <summary>
    /// Data structure for player starting islands
    /// </summary>
    [Serializable]
    public class PlayerStartIsland
    {
        public int playerId;
        public HexCoord center;
        public List<HexCoord> landTiles;
        public List<HexCoord> harbourTiles; // 3 connected harbours
        public HexCoord shipyardPosition; // One of the harbour tiles
        public List<HexCoord> shipSpawnPositions; // Sea tiles adjacent to harbours for ship spawning
    }
}
