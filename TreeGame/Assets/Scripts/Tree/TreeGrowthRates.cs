using System;
using System.Collections.Generic;
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
        // One entry per source (e.g. overwatering, overfeeding), so several can apply at once and each can end on its own.
        private readonly Dictionary<UnityEngine.Object, int> temporaryChanges = new Dictionary<UnityEngine.Object, int>();
        private int temporaryChange;

        /// <summary>Raised with the part and its new rate whenever a part's rate changes.</summary>
        public event Action<TreeGrowthPart, int> RateChanged;

        /// <summary>Raised when the temporary change to every part's rate changes, e.g. a penalty starting or ending.</summary>
        public event Action TemporaryChangeChanged;

        /// <summary>
        /// Added to every part's rate while something temporary affects growth, e.g. -1 while overwatered and -2 while
        /// also overfed: the sum of every source's change. Kept apart from the rates themselves, so they come back exactly
        /// as they were once it ends, and purchases made meanwhile count in full. Not saved here: each source restores its own.
        /// </summary>
        public int TemporaryChange => temporaryChange;

        /// <summary>A part's own rate, without any temporary change: what is bought, saved and shown as the part's rate.</summary>
        public int GetRate(TreeGrowthPart part)
        {
            StartIfNeeded();
            return rates[IndexOf(part)];
        }

        /// <summary>The rate a part actually grows at right now: its own rate plus any temporary change, kept within 1 to 10.</summary>
        public int GetEffectiveRate(TreeGrowthPart part)
        {
            return GrowthRates.Clamp(GetRate(part) + temporaryChange);
        }

        /// <summary>Sets one source's change to every part's rate, replacing its earlier one; 0 removes it.</summary>
        public void SetTemporaryChange(UnityEngine.Object source, int change)
        {
            if (source == null)
            {
                return;
            }

            if (change == 0)
            {
                temporaryChanges.Remove(source);
            }
            else
            {
                temporaryChanges[source] = change;
            }

            int total = 0;
            foreach (int sourceChange in temporaryChanges.Values)
            {
                total += sourceChange;
            }

            if (total == temporaryChange)
            {
                return;
            }

            temporaryChange = total;
            TemporaryChangeChanged?.Invoke();
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
