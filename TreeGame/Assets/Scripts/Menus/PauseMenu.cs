using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TreeGame.Menus
{
    /// <summary>
    /// Pauses the game on the cancel key (Esc) and shows the pause menu. Time.timeScale freezes the world, since
    /// everything in it runs on scaled time, but doesn't stop input, so the listed gameplay components are also
    /// switched off while paused. In a sub-screen, the cancel key goes back one screen before resuming.
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [Tooltip("A button action, e.g. UI/Cancel.")]
        [SerializeField] private InputActionReference pauseAction;
        [SerializeField] private MenuPanels panels;
        [SerializeField] private GameObject mainPanel;
        [Tooltip("Dims the game and blocks clicks on anything behind the menu; shown only while paused.")]
        [SerializeField] private GameObject background;
        [SerializeField] private Button resumeButton;
        [Tooltip("Components that take player input, switched off while paused.")]
        [SerializeField] private Behaviour[] gameplayInput = new Behaviour[0];

        private readonly List<Behaviour> switchedOff = new List<Behaviour>();
        private bool isPaused;

        private void OnEnable()
        {
            if (pauseAction == null || pauseAction.action == null || panels == null || mainPanel == null ||
                background == null || resumeButton == null)
            {
                Debug.LogWarning($"{nameof(PauseMenu)}: needs a pause action, panels, a main panel, a background and a resume button.", this);
                enabled = false;
                return;
            }

            // Not disabled in OnDisable: the UI input module shares this action.
            pauseAction.action.Enable();
            resumeButton.onClick.AddListener(Resume);
        }

        private void OnDisable()
        {
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(Resume);
            }
        }

        private void OnDestroy()
        {
            // Leaving the scene while paused (Quit to Menu) must not leave the next scene frozen.
            if (isPaused)
            {
                Time.timeScale = 1f;
            }
        }

        // Update still runs at a time scale of 0, so the menu keeps responding while paused.
        private void Update()
        {
            if (!pauseAction.action.WasPressedThisFrame())
            {
                return;
            }

            if (!isPaused)
            {
                Pause();
            }
            else if (panels.CurrentPanel != mainPanel)
            {
                panels.Show(mainPanel);
            }
            else
            {
                Resume();
            }
        }

        private void Pause()
        {
            isPaused = true;
            Time.timeScale = 0f;

            // Only what is on now, so anything already switched off for another reason stays off afterwards.
            foreach (Behaviour component in gameplayInput)
            {
                if (component != null && component.enabled)
                {
                    component.enabled = false;
                    switchedOff.Add(component);
                }
            }

            background.SetActive(true);
            panels.Show(mainPanel);
        }

        private void Resume()
        {
            isPaused = false;
            Time.timeScale = 1f;

            foreach (Behaviour component in switchedOff)
            {
                component.enabled = true;
            }

            switchedOff.Clear();

            panels.Show(null);
            background.SetActive(false);
        }
    }
}
