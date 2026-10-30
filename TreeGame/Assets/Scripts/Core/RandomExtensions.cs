namespace TreeGame.Core
{
    /// <summary>
    /// Helpers for <see cref="System.Random"/>, used for seeded, deterministic placement
    /// (unlike UnityEngine.Random, which is global and shared with everything else).
    /// </summary>
    public static class RandomExtensions
    {
        public static float NextFloat(this System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
