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

        public HexGrid GenerateMap(int numSeaTiles = 500, int numIslands = 25, int minIslandSize = 4, int maxIslandSize = 8)
        {
            HexGrid grid = new HexGrid();

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

            // Generate islands
            List<HexCoord> seaList = new List<HexCoord>(seaCoords);
            HashSet<HexCoord> usedCoords = new HashSet<HexCoord>();

            for (int islandId = 0; islandId < numIslands; islandId++)
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

                // Convert tiles to LAND and add one HARBOR
                int harborIndex = random.Next(islandTiles.Count);
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
    }
}
