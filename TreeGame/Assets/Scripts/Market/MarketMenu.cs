using System.Collections;
using System.Collections.Generic;
using TMPro;
using TreeGame.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TreeGame.Market
{
    /// <summary>
    /// The overlay listing what the market sells. Opens when the player uses the stall, and closes on the close button,
    /// the cancel key (Esc) or the interact key again. The world keeps running, but the listed gameplay components
    /// (walking, interacting, the pause menu) are switched off while it shows, so keys only reach the menu.
    /// </summary>
    public class MarketMenu : MonoBehaviour
    {
        [SerializeField] private MarketStall stall;
        [Tooltip("The stall's interactable; using it opens the menu.")]
        [SerializeField] private Interactable interactable;
        [Tooltip("Everything the menu shows, including the background that blocks clicks on the game.")]
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text coinLabel;
        [SerializeField] private Button closeButton;
        [Tooltip("A button action, e.g. UI/Cancel.")]
        [SerializeField] private InputActionReference cancelAction;
        [Tooltip("The interact action, so the key that opened the menu also closes it.")]
        [SerializeField] private InputActionReference interactAction;
        [Tooltip("Components that take player input, switched off while the menu shows.")]
        [SerializeField] private Behaviour[] gameplayInput = new Behaviour[0];

        private readonly List<Behaviour> switchedOff = new List<Behaviour>();
        private int openedOnFrame = -1;

        public bool IsOpen => root != null && root.activeSelf;

        private void OnEnable()
        {
            if (stall == null || interactable == null || root == null || coinLabel == null || closeButton == null ||
                cancelAction == null || cancelAction.action == null || interactAction == null || interactAction.action == null)
            {
                Debug.LogWarning($"{nameof(MarketMenu)}: needs a stall, its interactable, a root, a coin label, a close button and the cancel and interact actions.", this);
                enabled = false;
                return;
            }

            // Not disabled in OnDisable: the UI input module and the pause menu share this action.
            cancelAction.action.Enable();
            interactable.Interacted += Open;
            closeButton.onClick.AddListener(Close);
            stall.Changed += RefreshCoins;
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.Interacted -= Open;
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            if (stall != null)
            {
                stall.Changed -= RefreshCoins;
            }

            // E.g. a minigame switching this object off: there is no next frame to wait for, so give control back now.
            if (IsOpen)
            {
                root.SetActive(false);
            }

            StopAllCoroutines();
            RestoreInput();
        }

        private void Update()
        {
            // The key press that opened the menu must not close it again in the same frame.
            if (!IsOpen || Time.frameCount == openedOnFrame)
            {
                return;
            }

            if (cancelAction.action.WasPressedThisFrame() || interactAction.action.WasPressedThisFrame())
            {
                Close();
            }
        }

        private void Open(GameObject user)
        {
            if (IsOpen)
            {
                return;
            }

            openedOnFrame = Time.frameCount;

            // Only what is on now, so anything already switched off for another reason stays off afterwards.
            foreach (Behaviour component in gameplayInput)
            {
                if (component != null && component.enabled)
                {
                    component.enabled = false;
                    switchedOff.Add(component);
                }
            }

            // Switching the interactor off disables the interact action, which the menu still needs for closing.
            interactAction.action.Enable();

            root.SetActive(true);
            RefreshCoins();

            // Nothing selected, so the submit key can't buy something by accident.
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            root.SetActive(false);
            StartCoroutine(RestoreInputNextFrame());
        }

        /// <summary>
        /// Waits a frame, so the key that closed the menu can't also reach what is switched back on (e.g. Esc opening the
        /// pause menu, or the interact key opening this menu again).
        /// </summary>
        private IEnumerator RestoreInputNextFrame()
        {
            yield return null;
            RestoreInput();
        }

        private void RestoreInput()
        {
            foreach (Behaviour component in switchedOff)
            {
                if (component != null)
                {
                    component.enabled = true;
                }
            }

            switchedOff.Clear();
        }

        private void RefreshCoins()
        {
            int coins = stall.Coins;
            coinLabel.text = coins == 1 ? "You have 1 coin" : $"You have {coins} coins";
        }
    }
}
