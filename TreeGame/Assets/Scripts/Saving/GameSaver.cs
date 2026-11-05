using System;
using TreeGame.Menus;
using TreeGame.Minigames;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TreeGame.Saving
{
    /// <summary>
    /// Decides when the game saves: on the quick save key, when the window closes, and on quitting to the menu. The key
    /// is ignored while paused or in a minigame, so a save made on purpose is never of a half-finished moment; exits
    /// save regardless, or progress would be lost. Must sit on an object that stays on during minigames.
    /// </summary>
    public class GameSaver : MonoBehaviour
    {
        [SerializeField] private WorldState world;
        [Tooltip("A button action, e.g. Player/QuickSave.")]
        [SerializeField] private InputActionReference quickSaveAction;
        [SerializeField] private PauseMenu pauseMenu;
        [SerializeField] private MinigameRunner minigameRunner;
        [Tooltip("The button that leaves the game for the menu.")]
        [SerializeField] private Button quitToMenuButton;

        private SaveFile saveFile;

        /// <summary>Raised after each save, with whether it succeeded.</summary>
        public event Action<bool> Saved;

        private bool HasWhatItNeeds =>
            world != null && world.IsComplete && quickSaveAction != null && quickSaveAction.action != null &&
            pauseMenu != null && minigameRunner != null && quitToMenuButton != null;

        private void Awake()
        {
            saveFile = CurrentSaveSlot.File();
        }

        private void OnEnable()
        {
            if (!HasWhatItNeeds)
            {
                Debug.LogWarning($"{nameof(GameSaver)}: needs a complete world state, a quick save action, a pause menu, a minigame runner and a quit button.", this);
                enabled = false;
                return;
            }

            quickSaveAction.action.Enable();
            quitToMenuButton.onClick.AddListener(SaveNow);
        }

        private void OnDisable()
        {
            if (quickSaveAction != null && quickSaveAction.action != null)
            {
                quickSaveAction.action.Disable();
            }

            if (quitToMenuButton != null)
            {
                quitToMenuButton.onClick.RemoveListener(SaveNow);
            }
        }

        private void Update()
        {
            if (quickSaveAction.action.WasPressedThisFrame() && !pauseMenu.IsPaused && !minigameRunner.IsRunning)
            {
                SaveNow();
            }
        }

        private void OnApplicationQuit()
        {
            // Unity also calls this on a component that switched itself off for missing what it needs.
            if (HasWhatItNeeds)
            {
                SaveNow();
            }
        }

        private void SaveNow()
        {
            Saved?.Invoke(saveFile.TryWrite(world.Capture()));
        }
    }
}
