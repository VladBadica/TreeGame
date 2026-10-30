using System;
using System.Collections.Generic;
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

        private readonly List<Interactable> interactablesInRange = new List<Interactable>();

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

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Interactable interactable) && !interactablesInRange.Contains(interactable))
            {
                interactablesInRange.Add(interactable);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out Interactable interactable))
            {
                interactablesInRange.Remove(interactable);
            }
        }

        private void Update()
        {
            SetFocus(FindNearest());

            if (Focused != null && InteractAction.WasPressedThisFrame())
            {
                Focused.Interact();
            }
        }

        private Interactable FindNearest()
        {
            Interactable nearest = null;
            float nearestDistance = float.MaxValue;

            // Backwards so destroyed entries can be dropped while iterating.
            for (int i = interactablesInRange.Count - 1; i >= 0; i--)
            {
                Interactable candidate = interactablesInRange[i];
                if (candidate == null)
                {
                    interactablesInRange.RemoveAt(i);
                    continue;
                }

                if (!candidate.isActiveAndEnabled)
                {
                    continue;
                }

                // Side-on game: only horizontal distance matters.
                float distance = Mathf.Abs(candidate.transform.position.x - transform.position.x);
                if (distance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            return nearest;
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
