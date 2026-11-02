using TMPro;
using TreeGame.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TreeGame.Menus
{
    /// <summary>Shows the key currently bound to an action, so the list of keybinds can't drift from the real bindings.</summary>
    public class KeybindLabel : MonoBehaviour
    {
        [SerializeField] private InputActionReference action;
        [SerializeField] private TMP_Text label;
        [Tooltip("Which control scheme's key to show, as named in the input actions.")]
        [SerializeField] private string bindingGroup = "Keyboard&Mouse";

        private void OnEnable()
        {
            if (action == null || action.action == null || label == null)
            {
                Debug.LogWarning($"{nameof(KeybindLabel)}: needs an action and a label.", this);
                enabled = false;
                return;
            }

            label.text = BindingText.KeyName(action.action, bindingGroup, fallback: "Not bound");
        }
    }
}
