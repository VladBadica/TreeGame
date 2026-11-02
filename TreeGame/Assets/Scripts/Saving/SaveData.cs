using System;

namespace TreeGame.Saving
{
    /// <summary>
    /// Everything a save holds, as plain data with no references to the game, so its shape is the file's format. Fields
    /// are public and simply named because JSON stores them by name: renaming one breaks existing saves unless
    /// <see cref="CurrentVersion"/> goes up and old files are upgraded. Values aren't trusted here; each system checks
    /// what it is given when it restores its state.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>The format this game writes. Raise it whenever the format changes.</summary>
        public const int CurrentVersion = 1;

        public int version;

        public float treeGrowth;

        // As shares of their maximum (0..1), so changing a maximum in the config keeps the same proportion.
        public float waterFill;
        public float nutrientsFill;
        public float healthFill;

        public float playerX;

        public SavedSlot[] inventorySlots = new SavedSlot[0];
        public SavedComposter composter = new SavedComposter();

        /// <summary>How long each insect on the tree has been there, in seconds, oldest first.</summary>
        public float[] insectAges = new float[0];

        public SavedLeaf[] fallenLeaves = new SavedLeaf[0];
    }

    /// <summary>One inventory slot. An empty item name means an empty slot.</summary>
    [Serializable]
    public class SavedSlot
    {
        /// <summary>The name of the item's definition asset.</summary>
        public string itemName = "";

        /// <summary>How many are in the stack, for stackable items.</summary>
        public int count;

        /// <summary>How much it holds, for containers such as buckets.</summary>
        public float storedAmount;
    }

    [Serializable]
    public class SavedComposter
    {
        /// <summary>The composter's state as a number, because it is the composter that knows what they mean.</summary>
        public int state;
        public float workSeconds;
        public float workDuration;
        public float compostGrams;
    }

    /// <summary>Where a fallen leaf lies. Only its place is saved: its tint and tilt are small random details.</summary>
    [Serializable]
    public class SavedLeaf
    {
        public float x;
        public float y;
        public float z;
    }
}
