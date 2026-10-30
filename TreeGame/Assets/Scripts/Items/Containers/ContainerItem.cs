using UnityEngine;

namespace TreeGame.Items
{
    /// <summary>
    /// Base for items that hold an amount of something, from empty up to their definition's capacity
    /// (e.g. liters of water, grams of compost). Shows how full it is as its display text. Starts empty.
    /// </summary>
    public abstract class ContainerItem : Item
    {
        private readonly ContainerDefinition definition;

        protected ContainerItem(ContainerDefinition definition) : base(definition)
        {
            this.definition = definition;
        }

        public float Capacity => definition.Capacity;

        /// <summary>How much the container holds, in its definition's unit.</summary>
        public float StoredAmount { get; private set; }

        /// <summary>How full the container is, 0..1.</summary>
        public float Fill => StoredAmount / Capacity;

        public bool IsEmpty => StoredAmount <= 0f;

        public bool IsFull => StoredAmount >= Capacity;

        public override string DisplayText => $"{Fill * 100f:0}%";

        /// <summary>Adds a non-negative amount, clamped to the capacity.</summary>
        public void AddAmount(float amount)
        {
            if (IsValidAmount(amount))
            {
                SetStoredAmount(StoredAmount + amount);
            }
        }

        /// <summary>Removes a non-negative amount, clamped to zero.</summary>
        public void RemoveAmount(float amount)
        {
            if (IsValidAmount(amount))
            {
                SetStoredAmount(StoredAmount - amount);
            }
        }

        /// <summary>Sets the amount directly (clamped). Intended for emptying, testing and loading saved state.</summary>
        public void SetStoredAmount(float amount)
        {
            if (!float.IsFinite(amount))
            {
                Debug.LogWarning($"{DisplayName}: ignored non-finite amount {amount}.");
                return;
            }

            float clamped = Mathf.Clamp(amount, 0f, Capacity);
            if (clamped == StoredAmount)
            {
                return;
            }

            StoredAmount = clamped;
            NotifyChanged();
        }

        private bool IsValidAmount(float amount)
        {
            if (!float.IsFinite(amount) || amount < 0f)
            {
                Debug.LogWarning($"{DisplayName}: ignored invalid amount {amount}; use a positive value.");
                return false;
            }

            return true;
        }
    }
}
