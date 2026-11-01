using UnityEngine;

namespace TreeGame.Minigames.WellSpin
{
    /// <summary>
    /// The tuning of Well Spin as an asset, so it can be adjusted in the Inspector. Defaults come from
    /// <see cref="WellSpinTuning"/>, so they live in one place. Changes apply from the next game, and like other
    /// assets, edits made during Play Mode are kept afterwards.
    /// </summary>
    [CreateAssetMenu(fileName = "WellSpinSettings", menuName = "Tree Game/Minigames/Well Spin Settings")]
    public class WellSpinSettings : ScriptableObject
    {
        private static readonly WellSpinTuning Defaults = new WellSpinTuning();

        [Header("Game Length")]
        [Tooltip("A game ends after this many presses, hit or miss.")]
        [SerializeField, Min(1)] private int maxTries = Defaults.MaxTries;
        [Tooltip("A game also ends early once this many presses have missed.")]
        [SerializeField, Min(1)] private int maxMisses = Defaults.MaxMisses;
        [Tooltip("A game also ends once this many seconds of play have passed.")]
        [SerializeField, Min(1f)] private float timeLimitSeconds = Defaults.TimeLimitSeconds;

        [Header("Speed")]
        [Tooltip("The indicator's starting speed, in degrees per second. 360 is one full turn per second.")]
        [SerializeField, Min(1f)] private float degreesPerSecond = Defaults.DegreesPerSecond;
        [Tooltip("The total hits at which the indicator speeds up.")]
        [SerializeField] private int[] speedUpAfterHits = Defaults.SpeedUpAfterHits;
        [Tooltip("Each speed-up multiplies the current speed by this.")]
        [SerializeField, Min(0.01f)] private float speedUpMultiplier = Defaults.SpeedUpMultiplier;

        [Header("Target Area")]
        [Tooltip("How much of the ring the target covers, in degrees. Smaller is harder.")]
        [SerializeField, Range(5f, 170f)] private float targetWidth = Defaults.TargetWidth;
        [Tooltip("The least distance between the indicator and the edge of a new target, in degrees.")]
        [SerializeField, Range(0f, 90f)] private float minTargetGap = Defaults.MinTargetGap;

        [Header("Flow")]
        [Tooltip("How long the results ignore the press key, so a press just after the game ends can't skip them.")]
        [SerializeField, Min(0f)] private float secondsBeforeResultsCanClose = 0.5f;

        public float SecondsBeforeResultsCanClose => secondsBeforeResultsCanClose;

        public WellSpinTuning ToTuning()
        {
            return new WellSpinTuning
            {
                MaxTries = maxTries,
                MaxMisses = maxMisses,
                TimeLimitSeconds = timeLimitSeconds,
                DegreesPerSecond = degreesPerSecond,
                SpeedUpAfterHits = speedUpAfterHits,
                SpeedUpMultiplier = speedUpMultiplier,
                TargetWidth = targetWidth,
                MinTargetGap = minTargetGap,
            };
        }
    }
}
