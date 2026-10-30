using System;
using UnityEngine;

namespace TreeGame.Care
{
    /// <summary>
    /// A single stock of a care resource (e.g. water) between 0 and <see cref="MaxAmount"/>.
    /// It doesn't know who fills or drains it: future minigames call <see cref="Add"/>,
    /// future depletion/growth rules call <see cref="Consume"/>, and UI listens to <see cref="Changed"/>.
    /// </summary>
    public class TreeResource : MonoBehaviour
    {
        [SerializeField] private TreeResourceType type;
        [SerializeField, Min(0.01f)] private float maxAmount = 100f;
        [SerializeField] private float amount = 100f;

        /// <summary>Raised with this resource whenever its amount changes.</summary>
        public event Action<TreeResource> Changed;

        public TreeResourceType Type => type;

        public float Amount => amount;

        public float MaxAmount => maxAmount;

        public float Normalized => amount / maxAmount;

        /// <summary>Adds a non-negative amount, clamped to the maximum.</summary>
        public void Add(float value)
        {
            if (IsValidDelta(value))
            {
                SetAmount(amount + value);
            }
        }

        /// <summary>Removes a non-negative amount, clamped to zero.</summary>
        public void Consume(float value)
        {
            if (IsValidDelta(value))
            {
                SetAmount(amount - value);
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

            float clamped = Mathf.Clamp(value, 0f, maxAmount);
            if (clamped == amount)
            {
                return;
            }

            amount = clamped;
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

        private void OnValidate()
        {
            amount = Mathf.Clamp(amount, 0f, maxAmount);

            // Lets Inspector edits during Play Mode reach listeners like the HUD bars.
            if (Application.isPlaying)
            {
                Changed?.Invoke(this);
            }
        }
    }
}
