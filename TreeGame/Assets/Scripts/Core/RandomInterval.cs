using UnityEngine;

namespace TreeGame.Core
{
    /// <summary>Picks random waits between recurring events. Not seeded on purpose: they should differ each game.</summary>
    public static class RandomInterval
    {
        // Above zero, so a loop waiting for the next event always moves forward.
        private const float ShortestAllowed = 0.1f;

        /// <summary>A random wait within a range (shortest, longest), tolerating a range entered the wrong way round.</summary>
        public static float Pick(Vector2 secondsRange)
        {
            float shortest = Mathf.Max(ShortestAllowed, Mathf.Min(secondsRange.x, secondsRange.y));
            float longest = Mathf.Max(shortest, Mathf.Max(secondsRange.x, secondsRange.y));
            return Random.Range(shortest, longest);
        }
    }
}
