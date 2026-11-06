using System;
using TreeGame.Core;
using UnityEngine;

namespace TreeGame.Trees
{
    /// <summary>
    /// Owns how fast each part of the tree grows right now: a whole number from 1 (slow) to 10 (fast) per part (see
    /// <see cref="GrowthRates"/>). A new game starts each part at its rate in <see cref="GameConfig"/>; anything can change
    /// a rate while playing, and the save keeps it. It knows nothing about what changes the rates or what they drive.
    /// </summary>
    public class TreeGrowthRates : MonoBehaviour
    {
        [Tooltip("Where each part's starting rate comes from. Without it, every part starts at the middle rate.")]
        [SerializeField] private GameConfig config;

        private readonly int[] rates = new int[TreeGrowth.Parts.Length];
        private bool started;

        /// <summary>Raised with the part and its new rate whenever a part's rate changes.</summary>
        public event Action<TreeGrowthPart, int> RateChanged;

        public int GetRate(TreeGrowthPart part)
        {
            StartIfNeeded();
            return rates[IndexOf(part)];
        }

        /// <summary>Sets a part's rate, brought into 1 to 10.</summary>
        public void SetRate(TreeGrowthPart part, int rate)
        {
            StartIfNeeded();
            int clamped = GrowthRates.Clamp(rate);
            int index = IndexOf(part);
            if (rates[index] == clamped)
            {
                return;
            }

            rates[index] = clamped;
            RateChanged?.Invoke(part, clamped);
        }

        /// <summary>On first use, so the rates are ready whichever component starts first.</summary>
        private void StartIfNeeded()
        {
            if (started)
            {
                return;
            }

            started = true;
            foreach (TreeGrowthPart part in TreeGrowth.Parts)
            {
                rates[IndexOf(part)] = config != null ? config.StartingGrowthRate(part) : GrowthRates.Middle;
            }
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
