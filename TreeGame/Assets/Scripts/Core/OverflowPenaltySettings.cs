using System;
using UnityEngine;

namespace TreeGame.Core
{
    /// <summary>
    /// Tuning for pouring too much of a resource onto the tree at once (e.g. overwatering), edited as part of
    /// <see cref="GameConfig"/>. Overflow up to an allowed share of the resource's maximum is simply lost; past it, the
    /// tree grows slower for a while.
    /// </summary>
    [Serializable]
    public class OverflowPenaltySettings
    {
        [Tooltip("Pouring this share (0..1) of the tree's maximum more than it has room for brings the penalty. Overflow " +
                 "up to this share is simply lost.")]
        [SerializeField, Range(0f, 1f)] private float allowedOverflow = 0.1f;
        [Tooltip("Seconds of play the penalty lasts when the overflow is just over the allowed share.")]
        [SerializeField, Min(0f)] private float baseSeconds = 10f;
        [Tooltip("Extra seconds of penalty for each percent of the tree's maximum poured past the allowed share.")]
        [SerializeField, Min(0f)] private float secondsPerExtraPercent = 2f;
        [Tooltip("How much every part's growth rate drops while the penalty lasts. Rates never go below 1.")]
        [SerializeField, Min(0)] private int ratePenalty = 1;

        public float AllowedOverflow => allowedOverflow;

        public float BaseSeconds => baseSeconds;

        public float SecondsPerExtraPercent => secondsPerExtraPercent;

        public int RatePenalty => ratePenalty;
    }
}
