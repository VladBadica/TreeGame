using UnityEngine;
using UnityEngine.InputSystem;

namespace TreeGame.Items
{
    /// <summary>
    /// Activates inventory slots from a hotbar action. The action's bindings map to slots in order:
    /// the first binding activates slot 1, the second slot 2, and so on, so keys can be rebound in the input asset.
    /// Event-driven: nothing runs until a key is pressed. Disable the component to block hotbar use.
    /// </summary>
    public class HotbarInput : MonoBehaviour
    {
        [Tooltip("A pass-through button action with one binding per slot, in slot order.")]
        [SerializeField] private InputActionReference hotbarAction;
        [SerializeField] private Inventory inventory;

        private void OnEnable()
        {
            if (hotbarAction == null || hotbarAction.action == null || inventory == null)
            {
                Debug.LogWarning($"{nameof(HotbarInput)}: needs a hotbar action and an {nameof(Inventory)}.", this);
                enabled = false;
                return;
            }

            hotbarAction.action.performed += OnHotbarPerformed;
            hotbarAction.action.Enable();
        }

        private void OnDisable()
        {
            if (hotbarAction != null && hotbarAction.action != null)
            {
                hotbarAction.action.performed -= OnHotbarPerformed;
                hotbarAction.action.Disable();
            }
        }

        private void OnHotbarPerformed(InputAction.CallbackContext context)
        {
            // A pass-through action reports every key change, so several keys work independently;
            // that includes releases, which are ignored here.
            if (!context.control.IsPressed())
            {
                return;
            }

            int bindingIndex = context.action.GetBindingIndexForControl(context.control);
            if (bindingIndex >= 0)
            {
                inventory.ActivateSlot(bindingIndex);
            }
        }
    }
}
