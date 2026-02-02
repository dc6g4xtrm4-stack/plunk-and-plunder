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

        // Min-heap priority queue for A* (O(log n) operations)
        private class PriorityQueue<T>
        {
            private List<(T item, int priority)> heap = new List<(T, int)>();
            private HashSet<T> itemSet = new HashSet<T>(); // O(1) Contains check

            public int Count => heap.Count;

            public void Enqueue(T item, int priority)
            {
                heap.Add((item, priority));
                itemSet.Add(item);
                BubbleUp(heap.Count - 1);
            }

            public T Dequeue()
            {
                T result = heap[0].item;
                itemSet.Remove(result);

                int lastIndex = heap.Count - 1;
                heap[0] = heap[lastIndex];
                heap.RemoveAt(lastIndex);

                if (heap.Count > 0)
                    BubbleDown(0);

                return result;
            }

            public bool Contains(T item)
            {
                return itemSet.Contains(item);
            }

            private void BubbleUp(int index)
            {
                while (index > 0)
                {
                    int parentIndex = (index - 1) / 2;
                    if (heap[index].priority >= heap[parentIndex].priority)
                        break;

                    Swap(index, parentIndex);
                    index = parentIndex;
                }
            }

            private void BubbleDown(int index)
            {
                while (true)
                {
                    int leftChild = 2 * index + 1;
                    int rightChild = 2 * index + 2;
                    int smallest = index;

                    if (leftChild < heap.Count && heap[leftChild].priority < heap[smallest].priority)
                        smallest = leftChild;

                    if (rightChild < heap.Count && heap[rightChild].priority < heap[smallest].priority)
                        smallest = rightChild;

                    if (smallest == index)
                        break;

                    Swap(index, smallest);
                    index = smallest;
                }
            }

            private void Swap(int i, int j)
            {
                var temp = heap[i];
                heap[i] = heap[j];
                heap[j] = temp;
            }
        }
    }
}
