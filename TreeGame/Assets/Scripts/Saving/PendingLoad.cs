namespace TreeGame.Saving
{
    /// <summary>
    /// Remembers across a scene change that the game scene should start from the save. It has to be static, because
    /// nothing else survives a scene being replaced. It holds this one flag and nothing else.
    /// </summary>
    public static class PendingLoad
    {
        private static bool isRequested;

        /// <summary>Asks the next game scene to load the save as it starts.</summary>
        public static void Request()
        {
            isRequested = true;
        }

        /// <summary>Returns whether a load was requested, and clears the request so it only happens once.</summary>
        public static bool TakeRequest()
        {
            bool wasRequested = isRequested;
            isRequested = false;
            return wasRequested;
        }
    }
}
