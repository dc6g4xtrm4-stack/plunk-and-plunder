using System;

namespace PlunkAndPlunder.Resolution
{
    /// <summary>
    /// Deterministic random number generator for reproducible game resolution
    /// </summary>
    public class DeterministicRandom
    {
        private System.Random random;
        private int seed;

        public DeterministicRandom(int seed)
        {
            this.seed = seed;
            this.random = new System.Random(seed);
        }

        public int Next(int minValue, int maxValue)
        {
            return random.Next(minValue, maxValue);
        }

        public int Next(int maxValue)
        {
            return random.Next(maxValue);
        }

        public void Reset()
        {
            random = new System.Random(seed);
        }
    }
}
