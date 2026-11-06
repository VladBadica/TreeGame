using TreeGame.Saving;
using TreeGame.Trees;
using UnityEngine;

namespace TreeGame.Menus
{
    /// <summary>
    /// Shows a save slot's tree on the main menu, live: grown from the slot's seed to the slot's growth. A newly shown
    /// tree starts small and grows into place, so changing slots feels like looking at a different tree rather than a
    /// picture swapping. An empty slot shows a young sapling, where a new tree would be planted.
    /// It only sets the tree's seed and growth; the tree draws itself as it does in the game.
    /// </summary>
    public class MenuTreePreview : MonoBehaviour
    {
        [SerializeField] private TreeSeed treeSeed;
        [SerializeField] private TreeGrowth treeGrowth;
        [SerializeField] private TreeExtraGrowth extraGrowth;
        [SerializeField] private TreeVisuals treeVisuals;

        [Header("Empty slot")]
        [Tooltip("How grown the sapling is that stands in for a tree not yet planted.")]
        [SerializeField, Range(0f, 1f)] private float saplingTrunk = 0.05f;
        [SerializeField, Range(0f, 1f)] private float saplingBranches = 0.15f;
        [SerializeField, Range(0f, 1f)] private float saplingFoliage = 0.3f;

        private bool HasWhatItNeeds => treeSeed != null && treeGrowth != null && extraGrowth != null && treeVisuals != null;

        private void OnEnable()
        {
            if (!HasWhatItNeeds)
            {
                Debug.LogWarning($"{nameof(MenuTreePreview)}: needs the tree's seed, growth and visuals.", this);
                enabled = false;
            }
        }

        /// <summary>Shows a slot's tree, or the sapling if the slot is empty or its save can't be read.</summary>
        public void ShowSlot(int slot)
        {
            if (!HasWhatItNeeds)
            {
                return;
            }

            if (SaveSlots.ForThisGame().Slot(slot).TryRead(out SaveData data))
            {
                Show(data.treeSeed,
                    new[] { data.trunkGrowth, data.branchGrowth, data.foliageGrowth },
                    new[] { data.trunkExtraGrowth, data.branchExtraGrowth, data.foliageExtraGrowth });
            }
            else
            {
                // Keeps whatever seed it has: any young tree will do for one not yet planted.
                Show(treeSeed.Seed, new[] { saplingTrunk, saplingBranches, saplingFoliage }, new float[TreeGrowth.Parts.Length]);
            }
        }

        /// <summary>Shows a tree grown this much, with this much extra growth, both given in the order of TreeGrowth.Parts.</summary>
        private void Show(int seed, float[] growth, float[] extra)
        {
            // A different tree starts from nothing, so it grows in instead of morphing out of the last one.
            if (seed != treeSeed.Seed)
            {
                foreach (TreeGrowthPart part in TreeGrowth.Parts)
                {
                    treeGrowth.SetGrowth(part, 0f);
                    extraGrowth.SetExtraGrowth(part, 0f);
                }

                treeVisuals.SnapToTarget();
                treeSeed.SetSeed(seed);
            }

            // Eased toward by the visuals, so the tree grows into place.
            for (int i = 0; i < TreeGrowth.Parts.Length; i++)
            {
                treeGrowth.SetGrowth(TreeGrowth.Parts[i], growth[i]);
                extraGrowth.SetExtraGrowth(TreeGrowth.Parts[i], extra[i]);
            }
        }
    }
}
