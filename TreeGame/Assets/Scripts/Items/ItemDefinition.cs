using UnityEngine;

namespace TreeGame.Items
{
    /// <summary>
    /// What an item is (its name and look), shared by every copy of it. Each item type is one asset,
    /// created via Assets > Create > Tree Game > Item. What an item *does* is decided by whoever reacts
    /// to it being used, not by the definition.
    /// </summary>
    [CreateAssetMenu(fileName = "Item", menuName = "Tree Game/Item")]
    public class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Item";
        [SerializeField] private Sprite icon;

        public string DisplayName => displayName;

        public Sprite Icon => icon;
    }
}
