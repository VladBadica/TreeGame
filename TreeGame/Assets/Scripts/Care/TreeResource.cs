using System;
using TreeGame.Core;
using UnityEngine;

namespace TreeGame.Care
{
    /// <summary>
    /// A single stock of a care resource (e.g. water) between 0 and <see cref="MaxAmount"/>, starting full.
    /// It doesn't know who fills or drains it: future minigames call <see cref="Add"/>,
    /// depletion and future growth rules call <see cref="Consume"/>, and UI listens to <see cref="Changed"/>.
    /// </summary>
    public class TreeResource : MonoBehaviour
    {
        [SerializeField] private TreeResourceType type;
        [SerializeField] private GameConfig config;

        // Stored as a 0..1 fill level rather than an amount: valid before any initialization order,
        // and changing the maximum in the config keeps the same proportion.
        private float fill = 1f;

        /// <summary>Raised with this resource whenever its amount changes.</summary>
        public event Action<TreeResource> Changed;

        public TreeResourceType Type => type;

        public float MaxAmount => config.GetResourceSettings(type).MaxAmount;

        public float Amount => fill * MaxAmount;

        public float Normalized => fill;

        /// <summary>Adds a non-negative amount, clamped to the maximum.</summary>
        public void Add(float value)
        {
            if (IsValidDelta(value))
            {
                SetAmount(Amount + value);
            }
        }

        /// <summary>Removes a non-negative amount, clamped to zero.</summary>
        public void Consume(float value)
        {
            if (IsValidDelta(value))
            {
                SetAmount(Amount - value);
            }
        }

        /// <summary>Sets the amount directly (clamped). Intended for testing, resets and loading saved state.</summary>
        public void SetAmount(float value)
        {
            if (!float.IsFinite(value))
            {
                Debug.LogWarning($"{type}: ignored non-finite amount {value}.", this);
                return;
            }

            float newFill = Mathf.Clamp01(value / MaxAmount);
            if (newFill == fill)
            {
                return;
            }

            fill = newFill;
            Changed?.Invoke(this);
        }

        private bool IsValidDelta(float value)
        {
            if (!float.IsFinite(value) || value < 0f)
            {
                Debug.LogWarning($"{type}: ignored invalid amount {value}; use Add or Consume with a positive value.", this);
                return false;
            }

            return true;
        }

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError($"{type}: no {nameof(GameConfig)} assigned; the resource cannot work without one.", this);
            }
        }
    }
}
