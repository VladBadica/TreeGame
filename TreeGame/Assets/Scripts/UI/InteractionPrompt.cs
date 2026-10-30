using TMPro;
using TreeGame.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TreeGame.UI
{
    /// <summary>
    /// Shows "Press [key] to [action]" above whatever the <see cref="Interactor"/> is focused on.
    /// The key comes from the current input binding, so rebinding the action updates the prompt automatically.
    /// Lives on a screen-space canvas and follows its target on screen.
    /// </summary>
    public class InteractionPrompt : MonoBehaviour
    {
        [SerializeField] private Interactor interactor;
        [SerializeField] private Camera viewCamera;
        [Tooltip("The popup that is shown and hidden; its pivot is placed at the target's prompt position.")]
        [SerializeField] private RectTransform popup;
        [SerializeField] private TMP_Text label;
        [Tooltip("Which control scheme's binding to show, e.g. \"Keyboard&Mouse\".")]
        [SerializeField] private string bindingGroup = "Keyboard&Mouse";

        private Interactable target;

        private void OnEnable()
        {
            if (interactor == null || viewCamera == null || popup == null || label == null)
            {
                Debug.LogWarning($"{nameof(InteractionPrompt)}: needs an interactor, camera, popup and label.", this);
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
                popup.gameObject.SetActive(false);
            }
        }

        // Only does work while a prompt is visible.
        private void LateUpdate()
        {
            if (target != null)
            {
                FollowTarget();
            }
        }

        private void Show(Interactable interactable)
        {
            target = interactable;
            popup.gameObject.SetActive(target != null);
            if (target == null)
            {
                return;
            }

            label.text = $"Press <b>{GetKeyName()}</b> to {target.ActionDescription}";
            FollowTarget();
        }

        private void FollowTarget()
        {
            // On a screen-space overlay canvas, positions are in screen pixels.
            popup.position = viewCamera.WorldToScreenPoint(target.PromptPosition);
        }

        private string GetKeyName()
        {
            InputAction action = interactor.InteractAction;
            string key = action != null ? action.GetBindingDisplayString(InputBinding.MaskByGroup(bindingGroup)) : "";
            return string.IsNullOrEmpty(key) ? "Interact" : key;
        }
    }
}
