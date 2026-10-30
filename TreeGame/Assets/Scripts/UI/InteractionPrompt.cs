using System.Collections.Generic;
using TreeGame.Interaction;
using UnityEngine;

namespace TreeGame.UI
{
    /// <summary>
    /// Shows "Press [key] to [action]" above whatever the <see cref="Interactor"/> is focused on, or the target's
    /// status text when it has one (e.g. "Composting..."), plus any extra prompts it lists (e.g. "Press 2 to feed"),
    /// each in its own popup stacked above the first. Refreshes when the target's wording changes.
    /// Keys come from the interact action's current binding (so rebinding updates the prompt automatically),
    /// unless the target names its own key.
    /// </summary>
    public class InteractionPrompt : MonoBehaviour
    {
        [SerializeField] private Interactor interactor;
        [Tooltip("One popup per line, bottom first. Lines beyond the number of popups are not shown.")]
        [SerializeField] private WorldPopup[] popups = new WorldPopup[0];
        [Tooltip("Vertical distance between stacked popups, in canvas units.")]
        [SerializeField, Min(0f)] private float stackSpacing = 52f;
        [Tooltip("Which control scheme's binding to show, e.g. \"Keyboard&Mouse\".")]
        [SerializeField] private string bindingGroup = "Keyboard&Mouse";

        private readonly List<string> lines = new List<string>();
        private Interactable target;

        private void OnEnable()
        {
            if (interactor == null || popups.Length == 0)
            {
                Debug.LogWarning($"{nameof(InteractionPrompt)}: needs an interactor and at least one popup.", this);
                enabled = false;
                return;
            }

            interactor.FocusChanged += SetTarget;
            SetTarget(interactor.Focused);
        }

        private void OnDisable()
        {
            if (interactor != null)
            {
                interactor.FocusChanged -= SetTarget;
            }

            SetTarget(null);
        }

        private void SetTarget(Interactable newTarget)
        {
            if (target != null)
            {
                target.PromptChanged -= Refresh;
            }

            target = newTarget;
            if (target != null)
            {
                target.PromptChanged += Refresh;
            }

            Refresh();
        }

        private void Refresh()
        {
            lines.Clear();
            if (target != null)
            {
                CollectLines(target);
            }

            for (int i = 0; i < popups.Length; i++)
            {
                if (popups[i] == null)
                {
                    continue;
                }

                if (i < lines.Count)
                {
                    popups[i].Show(target.PromptAnchor, lines[i], canvasOffset: Vector2.up * (i * stackSpacing));
                }
                else
                {
                    popups[i].Hide();
                }
            }
        }

        private void CollectLines(Interactable interactable)
        {
            lines.Add(string.IsNullOrEmpty(interactable.StatusText)
                ? PressTo(interactable.PromptKeyOverride, interactable.ActionDescription)
                : interactable.StatusText);

            foreach (Interactable.ExtraPrompt extra in interactable.ExtraPrompts)
            {
                lines.Add(PressTo(extra.key, extra.actionDescription));
            }
        }

        private string PressTo(string keyOverride, string actionDescription)
        {
            string key = string.IsNullOrEmpty(keyOverride)
                ? BindingText.KeyName(interactor.InteractAction, bindingGroup, fallback: "Interact")
                : keyOverride;
            return BindingText.PressToPrompt(key, actionDescription);
        }
    }
}
