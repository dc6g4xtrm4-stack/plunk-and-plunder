using System.Collections.Generic;
using UnityEngine;

namespace PlunkAndPlunder.Map
{
    public class Pathfinding
    {
        private HexGrid grid;

        public Pathfinding(HexGrid grid)
        {
            this.grid = grid;
        }

        public List<HexCoord> FindPath(HexCoord start, HexCoord goal, int maxDistance = 100)
        {
            if (!grid.IsNavigable(start) || !grid.IsNavigable(goal))
                return null;

            Dictionary<HexCoord, HexCoord> cameFrom = new Dictionary<HexCoord, HexCoord>();
            Dictionary<HexCoord, int> gScore = new Dictionary<HexCoord, int>();
            Dictionary<HexCoord, int> fScore = new Dictionary<HexCoord, int>();

            PriorityQueue<HexCoord> openSet = new PriorityQueue<HexCoord>();
            HashSet<HexCoord> closedSet = new HashSet<HexCoord>();

            gScore[start] = 0;
            fScore[start] = start.Distance(goal);
            openSet.Enqueue(start, fScore[start]);

            while (openSet.Count > 0)
            {
                HexCoord current = openSet.Dequeue();

                if (current.Equals(goal))
                {
                    return ReconstructPath(cameFrom, current);
                }

                closedSet.Add(current);

                foreach (HexCoord neighbor in grid.GetNavigableNeighbors(current))
                {
                    if (closedSet.Contains(neighbor))
                        continue;

                    int tentativeGScore = gScore[current] + 1;

                    if (tentativeGScore > maxDistance)
                        continue;

                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + neighbor.Distance(goal);

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Enqueue(neighbor, fScore[neighbor]);
                        }
                    }
                }
            }

            return null; // No path found
        }

        private List<HexCoord> ReconstructPath(Dictionary<HexCoord, HexCoord> cameFrom, HexCoord current)
        {
            List<HexCoord> path = new List<HexCoord> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            return path;
        }

        // Simple priority queue for A*
        private class PriorityQueue<T>
        {
            private List<(T item, int priority)> elements = new List<(T, int)>();

            public int Count => elements.Count;

            public void Enqueue(T item, int priority)
            {
                elements.Add((item, priority));
            }

            public T Dequeue()
            {
                int bestIndex = 0;
                for (int i = 1; i < elements.Count; i++)
                {
                    if (elements[i].priority < elements[bestIndex].priority)
                    {
                        bestIndex = i;
                    }
                }

                T bestItem = elements[bestIndex].item;
                elements.RemoveAt(bestIndex);
                return bestItem;
            }

            public bool Contains(T item)
            {
                foreach (var element in elements)
                {
                    if (EqualityComparer<T>.Default.Equals(element.item, item))
                        return true;
                }
                return false;
            }
        }
    }
}
