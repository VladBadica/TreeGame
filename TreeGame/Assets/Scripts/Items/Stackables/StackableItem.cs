using UnityEngine;

namespace TreeGame.Items
{
    /// <summary>A stack of identical items in one slot (at least 1, at most the definition's max). Shows its count.</summary>
    public class StackableItem : Item
    {
        private readonly StackableItemDefinition definition;

        public StackableItem(StackableItemDefinition definition) : base(definition)
        {
            this.definition = definition;
            Count = 1;
        }

        public int Count { get; private set; }

        public int MaxStack => definition.MaxStack;

        public int SpaceLeft => Mathf.Max(0, MaxStack - Count);

        public override string DisplayText => Count.ToString();

        public override void OnUse(GameObject user)
        {
            // Plain materials do nothing when used; other systems (e.g. composting) will consume them instead.
        }

        /// <summary>Adds as many as fit. Returns how many didn't fit.</summary>
        public int AddToStack(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int added = Mathf.Min(amount, SpaceLeft);
            SetCount(Count + added);
            return amount - added;
        }

        /// <summary>Sets the count directly, clamped to 1..max (an empty stack is removed from its slot instead).</summary>
        public void SetCount(int count)
        {
            int clamped = Mathf.Clamp(count, 1, MaxStack);
            if (clamped == Count)
            {
                return;
            }

            Count = clamped;
            NotifyChanged();
        }
    }
}
