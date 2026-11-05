namespace TreeGame.Saving
{
    /// <summary>
    /// The save slot the game being played belongs to, chosen on the main menu: saving and loading both use it. It has
    /// to be static, because nothing else survives the menu being replaced by the game. It holds this one number and
    /// nothing else. The first slot until one is chosen, e.g. when the game scene is played straight from the Editor.
    /// </summary>
    public static class CurrentSaveSlot
    {
        public static int Index { get; private set; }

        /// <summary>Plays in this slot from now on. A slot that doesn't exist is ignored.</summary>
        public static void Choose(int index)
        {
            if (index >= 0 && index < SaveSlots.Count)
            {
                Index = index;
            }
        }

        /// <summary>The save of the slot being played.</summary>
        public static SaveFile File()
        {
            return SaveSlots.ForThisGame().Slot(Index);
        }
    }
}
