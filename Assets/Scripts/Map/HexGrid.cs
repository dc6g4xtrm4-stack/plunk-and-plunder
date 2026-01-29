using System;
using System.Collections.Generic;

namespace PlunkAndPlunder.Map
{
    [Serializable]
    public class HexGrid
    {
        public Dictionary<HexCoord, Tile> tiles = new Dictionary<HexCoord, Tile>();
        public float hexSize = 1f;

        public void AddTile(Tile tile)
        {
            tiles[tile.coord] = tile;
        }

        public Tile GetTile(HexCoord coord)
        {
            tiles.TryGetValue(coord, out Tile tile);
            return tile;
        }

        public bool HasTile(HexCoord coord)
        {
            return tiles.ContainsKey(coord);
        }

        public bool IsNavigable(HexCoord coord)
        {
            Tile tile = GetTile(coord);
            return tile != null && tile.IsNavigable();
        }

        public List<HexCoord> GetNavigableNeighbors(HexCoord coord)
        {
            List<HexCoord> neighbors = new List<HexCoord>();
            foreach (HexCoord neighbor in coord.GetNeighbors())
            {
                if (IsNavigable(neighbor))
                {
                    neighbors.Add(neighbor);
                }
            }
            return neighbors;
        }

        public List<Tile> GetAllTiles()
        {
            return new List<Tile>(tiles.Values);
        }

        public List<Tile> GetTilesOfType(TileType type)
        {
            List<Tile> result = new List<Tile>();
            foreach (Tile tile in tiles.Values)
            {
                if (tile.type == type)
                {
                    result.Add(tile);
                }
            }
            return result;
        }
    }
}
