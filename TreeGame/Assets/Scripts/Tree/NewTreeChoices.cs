namespace TreeGame.Trees
{
    /// <summary>
    /// What the player chose for a new tree on the main menu, carried across the scene change into the game: its seed,
    /// if they typed a tree code, and its name, if they gave one. Static, because nothing else survives a scene being
    /// replaced. Each choice is taken once, by whatever owns it in the game.
    /// </summary>
    public static class NewTreeChoices
    {
        public static readonly PendingValue<int> Seed = new PendingValue<int>();
        public static readonly PendingValue<string> Name = new PendingValue<string>();
    }

    /// <summary>One value waiting to be taken, once.</summary>
    public sealed class PendingValue<T>
    {
        private bool isRequested;
        private T requested;

        public void Request(T value)
        {
            requested = value;
            isRequested = true;
        }

        /// <summary>Gives the requested value, if any, and clears the request so it is only used once.</summary>
        public bool TryTake(out T value)
        {
            value = requested;
            bool wasRequested = isRequested;
            isRequested = false;
            requested = default;
            return wasRequested;
        }

        /// <summary>Forgets any request, e.g. when the player cancels.</summary>
        public void Clear()
        {
            isRequested = false;
            requested = default;
        }
    }
}
