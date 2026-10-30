using UnityEngine;

namespace TreeGame.Items
{
    /// <summary>
    /// The shared, editable data for one type of item (one asset per type), and the factory for its runtime
    /// <see cref="Item"/>. Each item type subclasses this with its own settings, e.g. a bucket's capacity,
    /// and adds a [CreateAssetMenu] so its assets can be created from Assets > Create > Tree Game > Items.
    /// </summary>
    public abstract class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Item";
        [SerializeField] private Sprite icon;

        public string DisplayName => displayName;

        public Sprite Icon => icon;

        /// <summary>Creates a new item of this type, e.g. to put into an inventory.</summary>
        public abstract Item CreateItem();
    }
}
