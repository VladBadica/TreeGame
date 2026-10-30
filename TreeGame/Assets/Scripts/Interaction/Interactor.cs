using System;
using TreeGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TreeGame.Interaction
{
    /// <summary>
    /// Lets its owner (the player) interact with the nearest <see cref="Interactable"/> whose trigger zone it is in.
    /// Detection uses trigger events, so this object needs a collider and a kinematic Rigidbody.
    /// It knows nothing about UI: prompts listen to <see cref="FocusChanged"/>.
    /// Disable the component to block interaction (e.g. while a minigame is open).
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        [SerializeField] private InputActionReference interactAction;

        private readonly ProximityTracker<Interactable> interactablesInRange = new ProximityTracker<Interactable>();

        /// <summary>Raised with the newly focused interactable, or null when none is in range.</summary>
        public event Action<Interactable> FocusChanged;

        /// <summary>The interactable that would be used if the player pressed interact now, or null.</summary>
        public Interactable Focused { get; private set; }

        public InputAction InteractAction => interactAction != null ? interactAction.action : null;

        private void OnEnable()
        {
            if (InteractAction == null)
            {
                Debug.LogWarning($"{nameof(Interactor)}: no interact action assigned.", this);
                enabled = false;
                return;
            }

            InteractAction.Enable();
        }

        private void OnDisable()
        {
            InteractAction?.Disable();
            SetFocus(null);
        }

        private void OnTriggerEnter(Collider other) => interactablesInRange.OnTriggerEnter(other);

        private void OnTriggerExit(Collider other) => interactablesInRange.OnTriggerExit(other);

        private void Update()
        {
            SetFocus(interactablesInRange.FindNearest(transform.position.x));

            if (Focused != null && InteractAction.WasPressedThisFrame())
            {
                Focused.Interact(gameObject);
            }
        }

        /// <summary>
        /// Interacts with a specific interactable (e.g. one the player clicked), but only if it is in range,
        /// the same rule as pressing the interact key. Returns whether it interacted.
        /// </summary>
        public bool TryInteract(Interactable target)
        {
            if (!enabled || !interactablesInRange.IsInRange(target))
            {
                return false;
            }

            target.Interact(gameObject);
            return true;
        }

        private void SetFocus(Interactable interactable)
        {
            if (interactable == Focused)
            {
                return;
            }

            Focused = interactable;
            FocusChanged?.Invoke(Focused);
        }
    }
}
