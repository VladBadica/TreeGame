using System;
using UnityEngine;

namespace TreeGame.Trees
{
    /// <summary>
    /// Owns each part's extra growth: growth past its full size, 0 = none, 1 = all there can be. It is separate from
    /// <see cref="TreeGrowth"/>, which stays 0 to 1, so a part can be fully grown and then some. Extra growth only ever
    /// goes up while playing: it is earned (see <see cref="GrowthRates"/>) and kept. It knows nothing about what earns it
    /// or how it looks.
    /// </summary>
    public class TreeExtraGrowth : MonoBehaviour
    {
        private readonly float[] extraGrowth = new float[TreeGrowth.Parts.Length];

        /// <summary>Raised with the part and its new extra growth whenever a part's extra growth changes.</summary>
        public event Action<TreeGrowthPart, float> ExtraGrowthChanged;

        public float GetExtraGrowth(TreeGrowthPart part)
        {
            return extraGrowth[IndexOf(part)];
        }

        /// <summary>Adds a non-negative amount of extra growth to one part; the result is capped at 1.</summary>
        public void AddExtraGrowth(TreeGrowthPart part, float amount)
        {
            // Nothing to add is the usual case (a rate at 7 or below), so it is quietly ignored, unlike bad numbers.
            if (amount == 0f)
            {
                return;
            }

            if (!float.IsFinite(amount) || amount < 0f)
            {
                Debug.LogWarning($"{nameof(TreeExtraGrowth)}: ignored {part} extra growth amount {amount}; it can only go up.", this);
                return;
            }

            SetExtraGrowth(part, GetExtraGrowth(part) + amount);
        }

        /// <summary>Sets one part's extra growth directly (clamped to 0..1). Intended for testing, resets and loading saved state.</summary>
        public void SetExtraGrowth(TreeGrowthPart part, float value)
        {
            if (!float.IsFinite(value))
            {
                Debug.LogWarning($"{nameof(TreeExtraGrowth)}: ignored non-finite {part} extra growth {value}.", this);
                return;
            }

            float clamped = Mathf.Clamp01(value);
            int index = IndexOf(part);
            if (extraGrowth[index] == clamped)
            {
                return;
            }

            extraGrowth[index] = clamped;
            ExtraGrowthChanged?.Invoke(part, clamped);
        }

        private static int IndexOf(TreeGrowthPart part)
        {
            int index = Array.IndexOf(TreeGrowth.Parts, part);
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(part), part, null);
            }

            return index;
        }
    }
}
