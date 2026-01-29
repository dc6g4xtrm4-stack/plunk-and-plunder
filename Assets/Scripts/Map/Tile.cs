using System;

namespace PlunkAndPlunder.Map
{
    [Serializable]
    public class Tile
    {
        public HexCoord coord;
        public TileType type;
        public int islandId; // -1 for sea, >= 0 for island membership

        public Tile(HexCoord coord, TileType type, int islandId = -1)
        {
            this.coord = coord;
            this.type = type;
            this.islandId = islandId;
        }

        public bool IsNavigable()
        {
            return type == TileType.SEA || type == TileType.HARBOR;
        }
    }
}
