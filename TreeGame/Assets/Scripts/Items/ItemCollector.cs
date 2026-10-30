using System;
using TreeGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TreeGame.Items
{
    /// <summary>
    /// Picks up every <see cref="ItemPickup"/> in reach into the inventory when the collect key is pressed.
    /// Separate from the Interactor on purpose: pickups lying around must never take focus away from
    /// interactables such as the tree. Needs a collider and kinematic Rigidbody on this object, like the Interactor.
    /// It knows nothing about UI: prompts listen to <see cref="NearestChanged"/>.
    /// </summary>
    public class ItemCollector : MonoBehaviour
    {
        [SerializeField] private InputActionReference collectAction;
        [SerializeField] private Inventory inventory;

        private readonly ProximityTracker<ItemPickup> pickupsInRange = new ProximityTracker<ItemPickup>();

        /// <summary>Raised with the nearest pickup in reach whenever it changes, or null when none is in reach.</summary>
        public event Action<ItemPickup> NearestChanged;

        /// <summary>The closest pickup in reach, or null.</summary>
        public ItemPickup Nearest { get; private set; }

        public InputAction CollectAction => collectAction != null ? collectAction.action : null;

        private void OnEnable()
        {
            if (collectAction == null || collectAction.action == null || inventory == null)
            {
                Debug.LogWarning($"{nameof(ItemCollector)}: needs a collect action and an {nameof(Inventory)}.", this);
                enabled = false;
                return;
            }

            collectAction.action.performed += OnCollectPerformed;
            collectAction.action.Enable();
        }

        private void OnDisable()
        {
            if (collectAction != null && collectAction.action != null)
            {
                collectAction.action.performed -= OnCollectPerformed;
                collectAction.action.Disable();
            }
        }

        private void OnTriggerEnter(Collider other) => pickupsInRange.OnTriggerEnter(other);

        private void OnTriggerExit(Collider other) => pickupsInRange.OnTriggerExit(other);

        private void Update()
        {
            ItemPickup nearest = pickupsInRange.FindNearest(transform.position.x);
            if (nearest != Nearest)
            {
                Nearest = nearest;
                NearestChanged?.Invoke(Nearest);
            }
        }

        private void OnCollectPerformed(InputAction.CallbackContext context)
        {
            foreach (ItemPickup pickup in pickupsInRange.GetActive())
            {
                pickup.CollectInto(inventory);
            }
        }
    }
}
