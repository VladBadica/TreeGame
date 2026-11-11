using System;
using TreeGame.Seasons;
using UnityEngine;

namespace TreeGame.Core
{
    /// <summary>
    /// Tuning for the seasons, edited as part of <see cref="GameConfig"/>: how long they last and how much each one
    /// speeds up or slows down the tree's growth and needs. Every value is a multiplier, so 1 is the same as without
    /// seasons, 2 twice as fast and 0 stopped. Seasons only scale time; growth rates themselves stay 1 to 10.
    /// </summary>
    [Serializable]
    public class SeasonSettings
    {
        [Tooltip("Seconds of play each season lasts. A year is four of them.")]
        [SerializeField, Min(10f)] private float secondsPerSeason = 300f;
        [Tooltip("Over the last this many seconds of a season, its effects ease into the next season's, so changes come " +
                 "gently. The new season's effects are fully there from its first moment.")]
        [SerializeField, Min(0f)] private float blendSeconds = 45f;
        [Tooltip("Seconds before a season ends that the player is told the next one is coming, to get ready for it.")]
        [SerializeField, Min(0f)] private float comingUpSeconds = 60f;

        [Tooltip("How fast the tree grows, normal and extra growth alike. Winter's 0 pauses growth while the tree rests.")]
        [SerializeField] private SeasonValues growth = new SeasonValues(1.5f, 1f, 0.5f, 0f);
        [Tooltip("How fast water drains: less with spring rain, more in summer heat, little while the tree rests.")]
        [SerializeField] private SeasonValues waterDrain = new SeasonValues(0.8f, 1.5f, 1f, 0.2f);
        [Tooltip("How fast nutrients drain: most in spring, when new leaves grow, almost none in winter.")]
        [SerializeField] private SeasonValues nutrientDrain = new SeasonValues(1.3f, 1f, 0.6f, 0.1f);
        [Tooltip("How often insects come: most in summer, none in winter.")]
        [SerializeField] private SeasonValues insects = new SeasonValues(0.7f, 1.3f, 0.5f, 0f);
        [Tooltip("How often leaves fall: few in spring, lots in autumn, none from a bare winter tree.")]
        [SerializeField] private SeasonValues leafFall = new SeasonValues(0.3f, 0.6f, 2f, 0f);
        [Tooltip("How fast the composter works: faster when warm, slower in the cold.")]
        [SerializeField] private SeasonValues composting = new SeasonValues(1f, 1.3f, 1f, 0.5f);

        public float SecondsPerSeason => Mathf.Max(10f, secondsPerSeason);

        public float BlendSeconds => Mathf.Max(0f, blendSeconds);

        public float ComingUpSeconds => Mathf.Max(0f, comingUpSeconds);

        /// <summary>How strongly an effect applies at this play time, eased between seasons.</summary>
        public float Multiplier(SeasonEffect effect, float totalSeconds)
        {
            return SeasonRules.Blend(ValuesFor(effect), totalSeconds, SecondsPerSeason, BlendSeconds);
        }

        private SeasonValues ValuesFor(SeasonEffect effect)
        {
            return effect switch
            {
                SeasonEffect.Growth => growth,
                SeasonEffect.WaterDrain => waterDrain,
                SeasonEffect.NutrientDrain => nutrientDrain,
                SeasonEffect.Insects => insects,
                SeasonEffect.LeafFall => leafFall,
                SeasonEffect.Composting => composting,
                _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, "Add values for this effect to SeasonSettings."),
            };
        }
    }
}
