using System;
using UnityEngine;

namespace TreeGame.Minigames.WellSpin
{
    /// <summary>
    /// Runs Well Spin in the scene: feeds time and key presses into <see cref="WellSpinRules"/>, announcing changes for
    /// the visuals. It holds no rules and draws nothing itself.
    /// </summary>
    public class WellSpinGame : PhasedMinigame
    {
        [SerializeField] private WellSpinSettings settings;

        /// <summary>The current game. Available from Start on.</summary>
        public WellSpinRules Rules { get; private set; }

        /// <summary>Raised with the outcome of each press during play, for feedback.</summary>
        public event Action<WellSpinPress> Pressed;

        protected override bool HasWhatItNeeds => settings != null;

        protected override bool IsGameOver => Rules.IsFinished;

        protected override float SecondsBeforeResultsCanClose => settings.SecondsBeforeResultsCanClose;

        protected override MinigameResult Result => new MinigameResult(Rules.IsFinished, (float)Rules.Hits / Rules.MaxTries);

        protected override void PrepareNewGame()
        {
            // Not seeded on purpose: each game should place its targets differently.
            Rules = new WellSpinRules(settings.ToTuning(), new System.Random());
        }

        protected override void PlayFrame(bool pressed)
        {
            // The press is judged before moving the indicator, so it counts where the player saw it.
            if (pressed)
            {
                Pressed?.Invoke(Rules.Press());
                if (!Rules.IsFinished)
                {
                    NotifyChanged();
                }
            }

            Rules.Tick(Time.deltaTime);
        }
    }
}
