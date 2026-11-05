using System;
using System.Globalization;

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
        /// <summary>
        /// The format this game writes. Raise it whenever the format changes, and add a step to
        /// <see cref="SaveUpgrades"/> that brings the previous version's saves up to it.
        /// Version 2 split the tree's one growth value into trunk, branches and foliage.
        /// Version 3 added the seed the tree grows from.
        /// Version 4 added when the game was saved.
        /// Version 5 added the tree's name.
        /// </summary>
        public const int CurrentVersion = 5;

        public int version;

        /// <summary>When the game was saved, in UTC, as an ISO 8601 round-trip text. Empty if unknown.</summary>
        public string savedAtUtc = "";

        public int treeSeed;

        /// <summary>The name the player gave the tree. Empty if it has none.</summary>
        public string treeName = "";

        public float trunkGrowth;
        public float branchGrowth;
        public float foliageGrowth;

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

        /// <summary>When the game was saved, in UTC. False if that isn't known, or the stored text is damaged.</summary>
        public bool TryGetSavedAt(out DateTime utc)
        {
            bool known = DateTime.TryParse(savedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out utc);
            utc = utc.ToUniversalTime();
            return known;
        }

        public void SetSavedAt(DateTime utc)
        {
            savedAtUtc = utc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
        }
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
