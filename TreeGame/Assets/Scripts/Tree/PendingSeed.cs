namespace TreeGame.Trees
{
    /// <summary>
    /// Remembers across a scene change the seed a new tree should grow from, e.g. one typed in as a code on the main
    /// menu. It has to be static, because nothing else survives a scene being replaced. It holds this one seed and
    /// nothing else.
    /// </summary>
    public static class PendingSeed
    {
        private static bool isRequested;
        private static int requestedSeed;

        /// <summary>Asks the next game's tree to grow from this seed.</summary>
        public static void Request(int seed)
        {
            requestedSeed = seed;
            isRequested = true;
        }

        /// <summary>Gives the requested seed, if any, and clears the request so it is only used once.</summary>
        public static bool TryTake(out int seed)
        {
            seed = requestedSeed;
            bool wasRequested = isRequested;
            isRequested = false;
            return wasRequested;
        }
    }
}
