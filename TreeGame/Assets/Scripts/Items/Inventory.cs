using System;
using TreeGame.Core;
using UnityEngine;

namespace TreeGame.Items
{
    /// <summary>
    /// The player's item slots. It doesn't read input or draw anything: input calls <see cref="ActivateSlot"/>,
    /// which uses the slot's item (each item decides what that does), and the UI listens to the events.
    /// Starts with the config's starting items.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        private Item[] slots;
        // One handler per slot, kept so it can be unsubscribed when the slot's item is replaced.
        private Action[] itemChangedHandlers;

        /// <summary>
        /// Raised with the slot index when a slot's item is replaced or removed, or the item itself changes
        /// (e.g. a bucket being filled).
        /// </summary>
        public event Action<int> SlotChanged;

        /// <summary>
        /// Raised with the slot index when the player uses a slot (e.g. presses its number key), even if it's empty,
        /// so feedback can show the key worked.
        /// </summary>
        public event Action<int> SlotActivated;

        public int SlotCount => Slots.Length;

        // Created on first use, so other components can read the inventory regardless of initialization order.
        private Item[] Slots
        {
            get
            {
                if (slots == null)
                {
                    CreateSlots();
                }

                return slots;
            }
        }

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError($"{nameof(Inventory)}: no {nameof(GameConfig)} assigned; it cannot work without one.", this);
            }
        }

        /// <summary>The item in a slot, or null if the slot is empty or doesn't exist.</summary>
        public Item GetItem(int index)
        {
            return IsValidIndex(index) ? Slots[index] : null;
        }

        /// <summary>The first item of a given type (optionally also matching a condition), or null if there is none.</summary>
        public T FindFirst<T>(Func<T, bool> match = null) where T : Item
        {
            foreach (Item item in Slots)
            {
                if (item is T typed && (match == null || match(typed)))
                {
                    return typed;
                }
            }

            return null;
        }

        /// <summary>Puts a new item of this type into the first empty slot. Returns false if the inventory is full.</summary>
        public bool TryAdd(ItemDefinition definition)
        {
            return definition != null && TryAdd(definition.CreateItem());
        }

        /// <summary>Puts the item into the first empty slot. Returns false if the inventory is full.</summary>
        public bool TryAdd(Item item)
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
        public void SetItem(int index, Item item)
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

            if (Slots[index] != null)
            {
                Slots[index].Changed -= itemChangedHandlers[index];
            }

            Slots[index] = item;
            if (item != null)
            {
                item.Changed += itemChangedHandlers[index];
            }

            SlotChanged?.Invoke(index);
        }

        public void ClearSlot(int index)
        {
            SetItem(index, null);
        }

        /// <summary>
        /// Uses the item in a slot, with this inventory's owner as the user. Empty slots still raise
        /// <see cref="SlotActivated"/>; indexes beyond the slot count are ignored (e.g. pressing 9 with only 5 slots).
        /// </summary>
        public void ActivateSlot(int index)
        {
            if (!IsValidIndex(index))
            {
                return;
            }

            Slots[index]?.OnUse(gameObject);
            SlotActivated?.Invoke(index);
        }

        private void CreateSlots()
        {
            slots = new Item[config.InventorySlotCount];
            itemChangedHandlers = new Action[slots.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                int slotIndex = i; // Captured per slot; the loop variable itself would be shared.
                itemChangedHandlers[i] = () => SlotChanged?.Invoke(slotIndex);
            }

            foreach (ItemDefinition definition in config.StartingItems)
            {
                if (definition != null && !TryAdd(definition))
                {
                    Debug.LogWarning($"{nameof(Inventory)}: no room for starting item '{definition.DisplayName}'.", this);
                }
            }
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < Slots.Length;
        }
    }
}
