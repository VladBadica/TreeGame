using System;
using UnityEngine;

namespace TreeGame.Core
{
    /// <summary>Tuning values for one care resource, edited as part of <see cref="GameConfig"/>.</summary>
    [Serializable]
    public class ResourceSettings
    {
        [SerializeField, Min(0.01f)] private float maxAmount;
        [Tooltip("Seconds of play time for the resource to drain from full to empty.")]
        [SerializeField, Min(1f)] private float secondsToEmpty;

        public ResourceSettings(float maxAmount, float secondsToEmpty)
        {
            this.maxAmount = maxAmount;
            this.secondsToEmpty = secondsToEmpty;
        }

        public float MaxAmount => maxAmount;

        public float SecondsToEmpty => secondsToEmpty;
    }
}
