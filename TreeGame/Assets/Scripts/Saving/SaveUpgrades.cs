using System;
using UnityEngine;

namespace TreeGame.Saving
{
    /// <summary>
    /// Brings a save written by an earlier version of the game up to the current format, one version at a time, so old
    /// saves keep loading. Each step reads what the old format had from the save's original text, because fields that
    /// were renamed or removed no longer exist in <see cref="SaveData"/>.
    /// </summary>
    internal static class SaveUpgrades
    {
        /// <summary>Upgrades a save read from this text in place. A save already in the current format is left as it is.</summary>
        public static void Upgrade(SaveData data, string json)
        {
            if (data.version == 1)
            {
                FromVersion1(data, json);
            }

            if (data.version == 2)
            {
                FromVersion2(data);
            }
        }

        /// <summary>
        /// Before version 3 every tree grew from this one seed, so it is the one an older save's tree grew from. Written
        /// out rather than taken from the tree, as it must stay this even if the tree's default seed changes.
        /// </summary>
        private const int SeedBeforeVersion3 = 12345;

        /// <summary>Version 3 saves the tree's seed. Older trees all grew from the same one, so they keep looking the same.</summary>
        private static void FromVersion2(SaveData data)
        {
            data.treeSeed = SeedBeforeVersion3;
            data.version = 3;
        }

        /// <summary>The fields of version 1 that later versions don't have.</summary>
        [Serializable]
        private class Version1Fields
        {
            public float treeGrowth;
        }

        /// <summary>
        /// Version 1 had one growth value for the whole tree; version 2 grows the trunk, branches and foliage
        /// separately. Each starts from the old value, so a loaded tree looks as grown as it did.
        /// </summary>
        private static void FromVersion1(SaveData data, string json)
        {
            float growth = JsonUtility.FromJson<Version1Fields>(json).treeGrowth;
            data.trunkGrowth = growth;
            data.branchGrowth = growth;
            data.foliageGrowth = growth;
            data.version = 2;
        }
    }
}
