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

            if (data.version == 3)
            {
                // Version 4 only added when the game was saved, which an older save can't know; reading it fills that
                // in from the file's own time.
                data.version = 4;
            }

            if (data.version == 4)
            {
                // Version 5 only added the tree's name; trees from before have none.
                data.treeName = "";
                data.version = 5;
            }

            if (data.version == 5)
            {
                // Version 6 added growth rates and extra growth. Before, rates couldn't change while playing, so the
                // parts keep the rates a game starts at (a saved rate of 0), and no tree had grown past full size.
                data.trunkGrowthRate = data.branchGrowthRate = data.foliageGrowthRate = 0;
                data.trunkExtraGrowth = data.branchExtraGrowth = data.foliageExtraGrowth = 0f;
                data.version = 6;
            }

            if (data.version == 6)
            {
                // Version 7 added the countdown to the next insect, which older saves didn't keep: they get a fresh wait,
                // as they did when loaded before.
                data.secondsUntilNextInsect = -1f;
                data.version = 7;
            }

            if (data.version == 7)
            {
                // Version 8 added overwatering, which didn't exist before, so no older tree is overwatered.
                data.overwateredSecondsLeft = 0f;
                data.version = 8;
            }

            if (data.version == 8)
            {
                // Version 9 added overfeeding, which didn't exist before, so no older tree is overfed.
                data.overfedSecondsLeft = 0f;
                data.version = 9;
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
