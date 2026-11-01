using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TreeGame.Minigames.WellSpin
{
    /// <summary>
    /// Runs a game of Well Spin in the scene: feeds time and key presses into <see cref="WellSpinRules"/> and
    /// announces changes for the visuals. It holds no rules and draws nothing itself.
    /// </summary>
    public class WellSpinGame : MonoBehaviour
    {
        [SerializeField] private WellSpinSettings settings;
        [Tooltip("The key the player presses when the indicator is in the target.")]
        [SerializeField] private InputActionReference pressAction;

        /// <summary>The current game. Available from Start on.</summary>
        public WellSpinRules Rules { get; private set; }

        /// <summary>Raised when a new game starts or a press changes the score or target.</summary>
        public event Action Changed;

        private void OnEnable()
        {
            if (settings == null || pressAction == null || pressAction.action == null)
            {
                Debug.LogWarning($"{nameof(WellSpinGame)}: needs settings and a press action.", this);
                enabled = false;
                return;
            }

            // This action belongs to the minigame alone, so it is only on while the minigame is.
            pressAction.action.Enable();
        }

        private void OnDisable()
        {
            if (pressAction != null && pressAction.action != null)
            {
                pressAction.action.Disable();
            }
        }

        // In Start so listeners, which subscribe in OnEnable, hear about the first game.
        private void Start()
        {
            StartNewGame();
        }

        private void Update()
        {
            // The press is judged before moving the indicator, so it counts where the player saw it when pressing.
            if (pressAction.action.WasPressedThisFrame())
            {
                OnPressed();
            }

            Rules.Tick(Time.deltaTime);
        }

        private void OnPressed()
        {
            // Temporary until the results screen: a press after the end starts over, for quick tuning.
            if (Rules.IsFinished)
            {
                StartNewGame();
                return;
            }

            Rules.Press();
            Changed?.Invoke();
        }

        private void StartNewGame()
        {
            // Not seeded on purpose: each game should place its targets differently.
            Rules = new WellSpinRules(settings.ToTuning(), new System.Random());
            Changed?.Invoke();
        }
    }
}
