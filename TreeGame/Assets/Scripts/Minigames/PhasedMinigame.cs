using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TreeGame.Minigames
{
    /// <summary>The stages of one sitting of a minigame.</summary>
    public enum MinigamePhase
    {
        /// <summary>Waiting for the player to start; nothing moves yet.</summary>
        Intro,

        /// <summary>The game is running and presses count.</summary>
        Playing,

        /// <summary>The game is over and its score is showing.</summary>
        Results,
    }

    /// <summary>
    /// A minigame that goes from an intro through play to its results, with one key or button to move on and another
    /// to leave at any time. Each minigame only says what a new game is and what a frame of play does.
    /// </summary>
    public abstract class PhasedMinigame : Minigame
    {
        [Tooltip("The key or button that starts the game, plays it and closes the results.")]
        [SerializeField] private InputActionReference pressAction;
        [Tooltip("The key that leaves the minigame.")]
        [SerializeField] private InputActionReference quitAction;

        private float resultsCanCloseTime;

        public MinigamePhase Phase { get; private set; }

        /// <summary>The currently bound press key, for prompts.</summary>
        public string PressKeyName => pressAction.action.GetBindingDisplayString();

        /// <summary>The currently bound quit key, for prompts.</summary>
        public string QuitKeyName => quitAction.action.GetBindingDisplayString();

        /// <summary>Raised when the phase changes, and whenever a minigame says something else worth showing changed.</summary>
        public event Action Changed;

        protected abstract bool IsGameOver { get; }

        /// <summary>How long the results ignore the press key, so a press just after the game ends can't skip them.</summary>
        protected abstract float SecondsBeforeResultsCanClose { get; }

        /// <summary>How the current game stands, for reporting when the player leaves.</summary>
        protected abstract MinigameResult Result { get; }

        /// <summary>False if something the minigame can't run without is missing. Checked before anything else runs.</summary>
        protected abstract bool HasWhatItNeeds { get; }

        /// <summary>Sets up a new game, ready to be played.</summary>
        protected abstract void PrepareNewGame();

        /// <summary>Runs one frame of play. <paramref name="pressed"/> says if the press key went down this frame.</summary>
        protected abstract void PlayFrame(bool pressed);

        protected virtual void OnEnable()
        {
            if (!HasWhatItNeeds || pressAction == null || pressAction.action == null ||
                quitAction == null || quitAction.action == null)
            {
                Debug.LogWarning($"{GetType().Name}: is missing its settings, a press action or a quit action.", this);
                enabled = false;
                return;
            }

            // These actions belong to the minigame alone, so they are only on while the minigame is.
            pressAction.action.Enable();
            quitAction.action.Enable();
        }

        protected virtual void OnDisable()
        {
            if (pressAction != null && pressAction.action != null)
            {
                pressAction.action.Disable();
            }

            if (quitAction != null && quitAction.action != null)
            {
                quitAction.action.Disable();
            }
        }

        // In Start so listeners, which subscribe in OnEnable, hear about the first phase.
        private void Start()
        {
            ShowIntro();
        }

        private void Update()
        {
            if (quitAction.action.WasPressedThisFrame())
            {
                Leave();
                return;
            }

            bool pressed = pressAction.action.WasPressedThisFrame();
            switch (Phase)
            {
                case MinigamePhase.Intro:
                    if (pressed)
                    {
                        SetPhase(MinigamePhase.Playing);
                    }

                    break;

                case MinigamePhase.Playing:
                    PlayFrame(pressed);

                    if (IsGameOver)
                    {
                        // Players are often still pressing when the game ends; don't let that skip past their score.
                        resultsCanCloseTime = Time.time + SecondsBeforeResultsCanClose;
                        SetPhase(MinigamePhase.Results);
                    }

                    break;

                case MinigamePhase.Results:
                    if (pressed && Time.time >= resultsCanCloseTime)
                    {
                        Leave();
                    }

                    break;
            }
        }

        protected void NotifyChanged()
        {
            Changed?.Invoke();
        }

        private void ShowIntro()
        {
            PrepareNewGame();
            SetPhase(MinigamePhase.Intro);
        }

        /// <summary>Ends this sitting. Leaving before the game is over counts as abandoning it.</summary>
        private void Leave()
        {
            // Played directly from its own scene there is nothing to return to, so start over.
            if (!ReportFinished(Result))
            {
                ShowIntro();
            }
        }

        private void SetPhase(MinigamePhase phase)
        {
            Phase = phase;
            Changed?.Invoke();
        }
    }
}
