using System;
using UnityEngine;

namespace PlunkAndPlunder.Map
{
    /// <summary>
    /// Axial coordinate system for hexagonal grids (q, r)
    /// </summary>
    [Serializable]
    public struct HexCoord : IEquatable<HexCoord>
    {
        public int q;
        public int r;

        public HexCoord(int q, int r)
        {
            this.q = q;
            this.r = r;
        }

        public int s => -q - r; // Cube coordinate s

        // Neighbor directions (pointy-top hexes)
        public static readonly HexCoord[] Directions = new HexCoord[]
        {
            new HexCoord(1, 0),   // E
            new HexCoord(1, -1),  // NE
            new HexCoord(0, -1),  // NW
            new HexCoord(-1, 0),  // W
            new HexCoord(-1, 1),  // SW
            new HexCoord(0, 1)    // SE
        };

        public HexCoord GetNeighbor(int direction)
        {
            HexCoord dir = Directions[direction];
            return new HexCoord(q + dir.q, r + dir.r);
        }

        public HexCoord[] GetNeighbors()
        {
            HexCoord[] neighbors = new HexCoord[6];
            for (int i = 0; i < 6; i++)
            {
                neighbors[i] = GetNeighbor(i);
            }
            return neighbors;
        }

        public int Distance(HexCoord other)
        {
            return (Mathf.Abs(q - other.q) + Mathf.Abs(r - other.r) + Mathf.Abs(s - other.s)) / 2;
        }

        public Vector3 ToWorldPosition(float hexSize = 1f)
        {
            float x = hexSize * (Mathf.Sqrt(3f) * q + Mathf.Sqrt(3f) / 2f * r);
            float z = hexSize * (3f / 2f * r);
            return new Vector3(x, 0, z);
        }

        public static HexCoord FromWorldPosition(Vector3 worldPos, float hexSize = 1f)
        {
            float q = (Mathf.Sqrt(3f) / 3f * worldPos.x - 1f / 3f * worldPos.z) / hexSize;
            float r = (2f / 3f * worldPos.z) / hexSize;
            return RoundToHex(q, r);
        }

        private static HexCoord RoundToHex(float q, float r)
        {
            float s = -q - r;
            int rq = Mathf.RoundToInt(q);
            int rr = Mathf.RoundToInt(r);
            int rs = Mathf.RoundToInt(s);

            float qDiff = Mathf.Abs(rq - q);
            float rDiff = Mathf.Abs(rr - r);
            float sDiff = Mathf.Abs(rs - s);

            if (qDiff > rDiff && qDiff > sDiff)
            {
                rq = -rr - rs;
            }
            else if (rDiff > sDiff)
            {
                rr = -rq - rs;
            }

            return new HexCoord(rq, rr);
        }

        public bool Equals(HexCoord other)
        {
            return q == other.q && r == other.r;
        }

        public override bool Equals(object obj)
        {
            return obj is HexCoord other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(q, r);
        }

        public static bool operator ==(HexCoord a, HexCoord b) => a.Equals(b);
        public static bool operator !=(HexCoord a, HexCoord b) => !a.Equals(b);

        public override string ToString() => $"({q}, {r})";
    }
}
