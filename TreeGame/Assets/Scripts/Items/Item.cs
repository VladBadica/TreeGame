using System;
using UnityEngine;

namespace TreeGame.Items
{
    /// <summary>
    /// Base class for every item the player can hold. Holds what all items have in common: a name, an icon,
    /// optional short display text and what happens when used. Each item type derives from this and keeps its
    /// own state (e.g. how much water a bucket holds); shared, editable settings live in its <see cref="ItemDefinition"/>.
    /// </summary>
    public abstract class Item
    {
        protected Item(ItemDefinition definition)
        {
            Definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
        }

        /// <summary>Raised when anything shown about the item changes (e.g. its display text), so UI can refresh.</summary>
        public event Action Changed;

        public ItemDefinition Definition { get; }

        public string DisplayName => Definition.DisplayName;

        public virtual Sprite Icon => Definition.Icon;

        /// <summary>Short text shown on the item's slot, e.g. a bucket's "40%". Null = nothing shown.</summary>
        public virtual string DisplayText => null;

        /// <summary>Called when the player uses the item, e.g. by pressing its slot's number key.</summary>
        /// <param name="user">The object using the item (the player), so items can act relative to it.</param>
        public abstract void OnUse(GameObject user);

        /// <summary>Call after any change that affects how the item is shown.</summary>
        protected void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
