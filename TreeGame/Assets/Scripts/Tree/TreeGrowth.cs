using System;
using UnityEngine;

namespace TreeGame.Trees
{
    /// <summary>
    /// Owns the tree's growth progress as a normalized value (0 = sprout, 1 = fully grown).
    /// It knows nothing about visuals or where growth comes from; other systems
    /// (visuals, UI, future resources/minigames/saving) talk to it through its public API and event.
    /// </summary>
    public class TreeGrowth : MonoBehaviour
    {
        // Serialized directly so the value is valid before any Awake/OnEnable ordering issues,
        // and so it can be inspected and tweaked at runtime.
        [SerializeField, Range(0f, 1f)] private float growth;

        /// <summary>Raised with the new normalized growth value whenever it changes.</summary>
        public event Action<float> GrowthChanged;

        public float Growth => growth;

        /// <summary>Adds a non-negative amount of growth; the result is clamped to 1.</summary>
        public void AddGrowth(float amount)
        {
            if (!float.IsFinite(amount))
            {
                Debug.LogWarning($"{nameof(TreeGrowth)}: ignored non-finite growth amount {amount}.", this);
                return;
            }

            if (amount < 0f)
            {
                // Shrinking is not a gameplay concept yet; use SetGrowth if a decrease is really intended.
                Debug.LogWarning($"{nameof(TreeGrowth)}: ignored negative growth amount {amount}.", this);
                return;
            }

            SetGrowth(growth + amount);
        }

        /// <summary>Sets growth directly (clamped to 0..1). Intended for testing, resets and loading saved state.</summary>
        public void SetGrowth(float value)
        {
            if (!float.IsFinite(value))
            {
                Debug.LogWarning($"{nameof(TreeGrowth)}: ignored non-finite growth value {value}.", this);
                return;
            }

            float clamped = Mathf.Clamp01(value);
            if (clamped == growth)
            {
                return;
            }

            growth = clamped;
            GrowthChanged?.Invoke(growth);
        }

        private void OnValidate()
        {
            // Lets Inspector edits during Play Mode propagate to listeners like the visuals.
            if (Application.isPlaying)
            {
                GrowthChanged?.Invoke(growth);
            }
        }
    }
}
