using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TreeGame.Minigames.WellSpin
{
    /// <summary>The stages of one sitting of Well Spin.</summary>
    public enum WellSpinPhase
    {
        /// <summary>Waiting for the player to start; the indicator stands still.</summary>
        Intro,

        /// <summary>The indicator spins and presses count.</summary>
        Playing,

        /// <summary>The game is over and its score is showing.</summary>
        Results,
    }

    /// <summary>
    /// Runs Well Spin in the scene: moves through the phases and feeds time and key presses into
    /// <see cref="WellSpinRules"/>, announcing changes for the visuals. It holds no rules and draws nothing itself.
    /// </summary>
    public class WellSpinGame : Minigame
    {
        [SerializeField] private WellSpinSettings settings;
        [Tooltip("The key the player presses when the indicator is in the target.")]
        [SerializeField] private InputActionReference pressAction;
        [Tooltip("The key that leaves the minigame.")]
        [SerializeField] private InputActionReference quitAction;

        private float resultsCanCloseTime;

        /// <summary>The current game. Available from Start on.</summary>
        public WellSpinRules Rules { get; private set; }

        public WellSpinPhase Phase { get; private set; }

        /// <summary>The currently bound press key, for prompts.</summary>
        public string PressKeyName => pressAction.action.GetBindingDisplayString();

        /// <summary>The currently bound quit key, for prompts.</summary>
        public string QuitKeyName => quitAction.action.GetBindingDisplayString();

        /// <summary>Raised when the phase changes or a press changes the score or target.</summary>
        public event Action Changed;

        /// <summary>Raised with the outcome of each press during play, for feedback.</summary>
        public event Action<WellSpinPress> Pressed;

        private void OnEnable()
        {
            if (settings == null || pressAction == null || pressAction.action == null ||
                quitAction == null || quitAction.action == null)
            {
                Debug.LogWarning($"{nameof(WellSpinGame)}: needs settings, a press action and a quit action.", this);
                enabled = false;
                return;
            }

            // These actions belong to the minigame alone, so they are only on while the minigame is.
            pressAction.action.Enable();
            quitAction.action.Enable();
        }

        private void OnDisable()
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
                case WellSpinPhase.Intro:
                    if (pressed)
                    {
                        SetPhase(WellSpinPhase.Playing);
                    }

                    break;

                case WellSpinPhase.Playing:
                    // The press is judged before moving the indicator, so it counts where the player saw it.
                    if (pressed)
                    {
                        JudgePress();
                    }

                    Rules.Tick(Time.deltaTime);
                    break;

                case WellSpinPhase.Results:
                    if (pressed && Time.time >= resultsCanCloseTime)
                    {
                        Leave();
                    }

                    break;
            }
        }

        private void JudgePress()
        {
            WellSpinPress result = Rules.Press();
            Pressed?.Invoke(result);

            if (Rules.IsFinished)
            {
                // Players are often still pressing when the game ends; don't let that skip past their score.
                resultsCanCloseTime = Time.time + settings.SecondsBeforeResultsCanClose;
                SetPhase(WellSpinPhase.Results);
            }
            else
            {
                Changed?.Invoke();
            }
        }

        private void ShowIntro()
        {
            // Not seeded on purpose: each game should place its targets differently.
            Rules = new WellSpinRules(settings.ToTuning(), new System.Random());
            SetPhase(WellSpinPhase.Intro);
        }

        /// <summary>Ends this sitting. Leaving before the game is over counts as abandoning it.</summary>
        private void Leave()
        {
            var result = new MinigameResult(Rules.IsFinished, (float)Rules.Hits / Rules.MaxTries);

            // Played directly from its own scene there is nothing to return to, so start over.
            if (!ReportFinished(result))
            {
                ShowIntro();
            }
        }

        private void SetPhase(WellSpinPhase phase)
        {
            Phase = phase;
            Changed?.Invoke();
        }
    }
}
