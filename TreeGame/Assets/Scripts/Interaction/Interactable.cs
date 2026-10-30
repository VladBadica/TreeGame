using System;
using System.Collections.Generic;
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
        /// <summary>One more "Press [key] to [action]" line shown with the main prompt, e.g. "Press 2 to feed".</summary>
        [Serializable]
        public struct ExtraPrompt
        {
            [Tooltip("Key shown in the prompt, e.g. \"2\". Empty = the interact key.")]
            public string key;
            [Tooltip("Completes the prompt \"Press [key] to ...\".")]
            public string actionDescription;
        }

        [Tooltip("Completes the prompt \"Press [key] to ...\", e.g. \"get water\".")]
        [SerializeField] private string actionDescription = "interact";
        [Tooltip("Where the prompt appears. Empty = this object's position.")]
        [SerializeField] private Transform promptAnchor;
        [Tooltip("Key shown in the prompt. Empty = the interact key. Set it when the action happens through another key, " +
                 "e.g. \"1\" for using the item in hotbar slot 1.")]
        [SerializeField] private string promptKeyOverride;
        [Tooltip("More prompts shown at the same time, each in its own popup above the main one.")]
        [SerializeField] private ExtraPrompt[] extraPrompts = new ExtraPrompt[0];

        // Set at runtime by whatever drives this object, e.g. "Composting..." while something is busy.
        private string statusText;

        /// <summary>Raised with the interacting object (e.g. the player) when someone interacts with this object.</summary>
        public event Action<GameObject> Interacted;

        /// <summary>Raised when the prompt's wording changes, so a visible prompt can refresh.</summary>
        public event Action PromptChanged;

        public string ActionDescription => actionDescription;

        /// <summary>When set, the prompt shows only this text (no "Press [key] to ..."). Null = the normal prompt.</summary>
        public string StatusText => statusText;

        /// <summary>The key to show instead of the interact key, or null/empty to show the interact key.</summary>
        public string PromptKeyOverride => promptKeyOverride;

        public IReadOnlyList<ExtraPrompt> ExtraPrompts => extraPrompts;

        /// <summary>Where the prompt appears: the prompt anchor, or this object if none is set.</summary>
        public Transform PromptAnchor => promptAnchor != null ? promptAnchor : transform;

        /// <summary>Changes what the prompt offers, e.g. from "drop leaves" to "collect" as an object's state changes.</summary>
        public void SetActionDescription(string description)
        {
            if (description == actionDescription)
            {
                return;
            }

            actionDescription = description;
            PromptChanged?.Invoke();
        }

        /// <summary>Shows a status line instead of the normal prompt (e.g. "Composting..."); null restores the prompt.</summary>
        public void SetStatusText(string text)
        {
            if (text == statusText)
            {
                return;
            }

            statusText = text;
            PromptChanged?.Invoke();
        }

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
