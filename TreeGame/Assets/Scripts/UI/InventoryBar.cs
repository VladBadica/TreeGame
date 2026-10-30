using TreeGame.Items;
using UnityEngine;

namespace TreeGame.UI
{
    /// <summary>
    /// Shows the <see cref="Inventory"/> as a row of slots, one per inventory slot, numbered from 1.
    /// The slots are copies of a template made once at startup, so the bar follows the configured slot count.
    /// </summary>
    public class InventoryBar : MonoBehaviour
    {
        [SerializeField] private Inventory inventory;
        [Tooltip("An inactive slot used as the template; copies are placed next to it.")]
        [SerializeField] private InventorySlotView slotTemplate;

        private InventorySlotView[] slotViews;

        private void Awake()
        {
            if (inventory == null || slotTemplate == null)
            {
                Debug.LogWarning($"{nameof(InventoryBar)}: needs an {nameof(Inventory)} and a slot template.", this);
                enabled = false;
                return;
            }

            slotTemplate.gameObject.SetActive(false);
            slotViews = new InventorySlotView[inventory.SlotCount];
            for (int i = 0; i < slotViews.Length; i++)
            {
                InventorySlotView view = Instantiate(slotTemplate, slotTemplate.transform.parent);
                view.name = $"Slot {i + 1}";
                view.SetKeyLabel((i + 1).ToString());
                view.gameObject.SetActive(true);
                slotViews[i] = view;
            }
        }

        private void OnEnable()
        {
            inventory.SlotChanged += RefreshSlot;
            inventory.SlotActivated += PlayActivated;

            for (int i = 0; i < slotViews.Length; i++)
            {
                RefreshSlot(i);
            }
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.SlotChanged -= RefreshSlot;
                inventory.SlotActivated -= PlayActivated;
            }
        }

        private void RefreshSlot(int index)
        {
            slotViews[index].ShowItem(inventory.GetItem(index));
        }

        private void PlayActivated(int index)
        {
            slotViews[index].PlayActivated();
        }
    }
}
