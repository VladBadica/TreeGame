using System;
using TreeGame.Core;
using UnityEngine;

namespace TreeGame.Items
{
    /// <summary>
    /// The player's item slots: pure state. It doesn't read input or draw anything; input calls
    /// <see cref="ActivateSlot"/>, the UI listens to the events, and future item effects react to activation.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        private ItemDefinition[] slots;

        /// <summary>Raised with the slot index when an item is put into or taken out of a slot.</summary>
        public event Action<int> SlotChanged;

        /// <summary>
        /// Raised with the slot index when the player uses a slot (e.g. presses its number key), even if it's empty,
        /// so feedback can show the key worked. Listeners that act on items should check <see cref="GetItem"/>.
        /// </summary>
        public event Action<int> SlotActivated;

        public int SlotCount => Slots.Length;

        // Created on first use, so other components can read the inventory regardless of initialization order.
        private ItemDefinition[] Slots => slots ??= new ItemDefinition[config.InventorySlotCount];

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError($"{nameof(Inventory)}: no {nameof(GameConfig)} assigned; it cannot work without one.", this);
            }
        }

        /// <summary>The item in a slot, or null if the slot is empty or doesn't exist.</summary>
        public ItemDefinition GetItem(int index)
        {
            return IsValidIndex(index) ? Slots[index] : null;
        }

        /// <summary>Puts the item into the first empty slot. Returns false if the inventory is full.</summary>
        public bool TryAdd(ItemDefinition item)
        {
            if (item == null)
            {
                return false;
            }

            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] == null)
                {
                    SetItem(i, item);
                    return true;
                }
            }

            return false;
        }

        /// <summary>Puts an item into a slot (or empties it with null), replacing what was there.</summary>
        public void SetItem(int index, ItemDefinition item)
        {
            if (!IsValidIndex(index))
            {
                Debug.LogWarning($"{nameof(Inventory)}: slot {index} doesn't exist (slots: {SlotCount}).", this);
                return;
            }

            if (Slots[index] == item)
            {
                return;
            }

            Slots[index] = item;
            SlotChanged?.Invoke(index);
        }

        public void ClearSlot(int index)
        {
            SetItem(index, null);
        }

        /// <summary>Uses a slot. Indexes beyond the slot count are ignored, e.g. pressing 9 with only 5 slots.</summary>
        public void ActivateSlot(int index)
        {
            if (IsValidIndex(index))
            {
                SlotActivated?.Invoke(index);
            }
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < Slots.Length;
        }
    }
}
