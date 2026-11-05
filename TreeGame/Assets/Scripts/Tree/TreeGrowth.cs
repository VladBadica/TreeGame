using System;
using UnityEngine;

namespace TreeGame.Trees
{
    /// <summary>
    /// Owns how grown each part of the tree is (see <see cref="TreeGrowthPart"/>), as normalized values: 0 = not at all,
    /// 1 = fully grown. The parts are independent: changing one never changes another, so anything can grow one part
    /// more than the others. It knows nothing about visuals or where growth comes from; other systems (visuals, UI,
    /// passive growth, saving) talk to it through its public API and event.
    /// </summary>
    public class TreeGrowth : MonoBehaviour
    {
        /// <summary>Every part, in order, for code that handles them all the same way.</summary>
        public static readonly TreeGrowthPart[] Parts = { TreeGrowthPart.Trunk, TreeGrowthPart.Branches, TreeGrowthPart.Foliage };

        // Serialized directly so the values are valid before any Awake/OnEnable ordering issues,
        // and so they can be inspected and tweaked at runtime.
        [SerializeField, Range(0f, 1f)] private float trunk;
        [SerializeField, Range(0f, 1f)] private float branches;
        [SerializeField, Range(0f, 1f)] private float foliage;

        [Tooltip("The least branch growth a tree ever has, even a brand new one. Leaves only grow on branches, so this " +
                 "gives a new tree a little branch to carry its first leaves.")]
        [SerializeField, Range(0f, 1f)] private float minimumBranches = 0.05f;

        /// <summary>Raised with the part and its new growth whenever a part's growth changes.</summary>
        public event Action<TreeGrowthPart, float> GrowthChanged;

        /// <summary>The least growth a part can have; it never goes below this, and starts there.</summary>
        public float MinimumGrowth(TreeGrowthPart part)
        {
            return part == TreeGrowthPart.Branches ? Mathf.Clamp01(minimumBranches) : 0f;
        }

        public float GetGrowth(TreeGrowthPart part)
        {
            float stored = part switch
            {
                TreeGrowthPart.Trunk => trunk,
                TreeGrowthPart.Branches => branches,
                TreeGrowthPart.Foliage => foliage,
                _ => throw new ArgumentOutOfRangeException(nameof(part), part, null),
            };

            // Applied on reading too, so a stored value below the minimum (an older scene, an Inspector edit) never
            // shows, whatever order components start in.
            return Mathf.Max(stored, MinimumGrowth(part));
        }

        /// <summary>Adds a non-negative amount of growth to one part; the result is clamped to 1.</summary>
        public void AddGrowth(TreeGrowthPart part, float amount)
        {
            if (!float.IsFinite(amount))
            {
                Debug.LogWarning($"{nameof(TreeGrowth)}: ignored non-finite {part} growth amount {amount}.", this);
                return;
            }

            if (amount < 0f)
            {
                // Shrinking is not a gameplay concept yet; use SetGrowth if a decrease is really intended.
                Debug.LogWarning($"{nameof(TreeGrowth)}: ignored negative {part} growth amount {amount}.", this);
                return;
            }

            SetGrowth(part, GetGrowth(part) + amount);
        }

        /// <summary>
        /// Sets one part's growth directly (clamped between its <see cref="MinimumGrowth"/> and 1). Intended for testing,
        /// resets and loading saved state.
        /// </summary>
        public void SetGrowth(TreeGrowthPart part, float value)
        {
            if (!float.IsFinite(value))
            {
                Debug.LogWarning($"{nameof(TreeGrowth)}: ignored non-finite {part} growth value {value}.", this);
                return;
            }

            float clamped = Mathf.Clamp(value, MinimumGrowth(part), 1f);
            if (clamped == GetGrowth(part))
            {
                return;
            }

            Store(part, clamped);
            GrowthChanged?.Invoke(part, clamped);
        }

        private void Store(TreeGrowthPart part, float value)
        {
            switch (part)
            {
                case TreeGrowthPart.Trunk:
                    trunk = value;
                    break;
                case TreeGrowthPart.Branches:
                    branches = value;
                    break;
                case TreeGrowthPart.Foliage:
                    foliage = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(part), part, null);
            }
        }

        private void OnValidate()
        {
            // Lets Inspector edits during Play Mode propagate to listeners like the visuals.
            if (Application.isPlaying)
            {
                foreach (TreeGrowthPart part in Parts)
                {
                    GrowthChanged?.Invoke(part, GetGrowth(part));
                }
            }
        }
    }
}
