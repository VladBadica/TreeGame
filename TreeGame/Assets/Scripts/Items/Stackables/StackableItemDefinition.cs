using UnityEngine;

namespace TreeGame.Items
{
    /// <summary>
    /// An item that piles up in one slot, e.g. brown leaves. Plain materials need no code of their own:
    /// create an asset of this type. Subclass it only when a stackable item needs its own behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "StackableItem", menuName = "Tree Game/Items/Stackable Item")]
    public class StackableItemDefinition : ItemDefinition
    {
        [Tooltip("How many fit in one inventory slot.")]
        [SerializeField, Min(1)] private int maxStack = 9999;

        public int MaxStack => maxStack;

        public override Item CreateItem() => CreateStack();

        /// <summary>A new stack of one. Subclasses with their own stack type override this.</summary>
        public virtual StackableItem CreateStack() => new StackableItem(this);
    }
}
