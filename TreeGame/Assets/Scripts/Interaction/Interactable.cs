using System;
using UnityEngine;

namespace TreeGame.Interaction
{
    /// <summary>
    /// Something the player can interact with while standing in its trigger zone (this object's trigger collider).
    /// It only describes the interaction and announces it; what actually happens (e.g. starting a minigame)
    /// is up to whatever subscribes to <see cref="Interacted"/>.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Interactable : MonoBehaviour
    {
        [Tooltip("Completes the prompt \"Press [key] to ...\", e.g. \"get water\".")]
        [SerializeField] private string actionDescription = "interact";
        [Tooltip("Where the prompt appears. Empty = this object's position.")]
        [SerializeField] private Transform promptAnchor;

        /// <summary>Raised when the player interacts with this object.</summary>
        public event Action Interacted;

        public string ActionDescription => actionDescription;

        public Vector3 PromptPosition => promptAnchor != null ? promptAnchor.position : transform.position;

        public void Interact()
        {
            Interacted?.Invoke();
        }

        private void Reset()
        {
            // The zone only detects the player; it should never block movement.
            GetComponent<Collider>().isTrigger = true;
        }
    }
}
