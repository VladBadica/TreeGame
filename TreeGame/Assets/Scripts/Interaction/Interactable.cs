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
        [Tooltip("Key shown in the prompt. Empty = the interact key. Set it when the action happens through another key, " +
                 "e.g. \"1\" for using the item in hotbar slot 1.")]
        [SerializeField] private string promptKeyOverride;

        /// <summary>Raised with the interacting object (e.g. the player) when someone interacts with this object.</summary>
        public event Action<GameObject> Interacted;

        public string ActionDescription => actionDescription;

        /// <summary>The key to show instead of the interact key, or null/empty to show the interact key.</summary>
        public string PromptKeyOverride => promptKeyOverride;

        public Vector3 PromptPosition => promptAnchor != null ? promptAnchor.position : transform.position;

        /// <param name="user">Who is interacting, so responses can act on them (e.g. fill their bucket).</param>
        public void Interact(GameObject user)
        {
            Interacted?.Invoke(user);
        }

        private void Reset()
        {
            // The zone only detects the player; it should never block movement.
            GetComponent<Collider>().isTrigger = true;
        }
    }
}
