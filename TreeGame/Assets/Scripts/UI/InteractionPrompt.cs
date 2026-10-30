using TreeGame.Interaction;
using UnityEngine;

namespace TreeGame.UI
{
    /// <summary>
    /// Shows "Press [key] to [action]" above whatever the <see cref="Interactor"/> is focused on.
    /// The key comes from the interact action's current binding (so rebinding updates the prompt automatically),
    /// unless the target names its own key.
    /// </summary>
    public class InteractionPrompt : MonoBehaviour
    {
        [SerializeField] private Interactor interactor;
        [SerializeField] private WorldPopup popup;
        [Tooltip("Which control scheme's binding to show, e.g. \"Keyboard&Mouse\".")]
        [SerializeField] private string bindingGroup = "Keyboard&Mouse";

        private void OnEnable()
        {
            if (interactor == null || popup == null)
            {
                Debug.LogWarning($"{nameof(InteractionPrompt)}: needs an interactor and a popup.", this);
                enabled = false;
                return;
            }

            interactor.FocusChanged += Show;
            Show(interactor.Focused);
        }

        private void OnDisable()
        {
            if (interactor != null)
            {
                interactor.FocusChanged -= Show;
            }

            if (popup != null)
            {
                popup.Hide();
            }
        }

        private void Show(Interactable target)
        {
            if (target == null)
            {
                popup.Hide();
                return;
            }

            string key = string.IsNullOrEmpty(target.PromptKeyOverride)
                ? BindingText.KeyName(interactor.InteractAction, bindingGroup, fallback: "Interact")
                : target.PromptKeyOverride;
            popup.Show(target.PromptAnchor, BindingText.PressToPrompt(key, target.ActionDescription));
        }
    }
}
