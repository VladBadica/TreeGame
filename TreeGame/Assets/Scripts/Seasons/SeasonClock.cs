using System;
using TreeGame.Core;
using UnityEngine;

namespace TreeGame.Seasons
{
    /// <summary>
    /// Keeps the year's time: the seconds of play since the tree was planted, which start at the beginning of spring.
    /// Everything that changes with the seasons asks it how strongly an effect applies right now (see
    /// <see cref="Multiplier"/>), so none of them needs to know about seasons beyond that. It announces when the next
    /// season is coming up and when it arrives, e.g. for the HUD. Time only passes while playing.
    /// </summary>
    public class SeasonClock : MonoBehaviour, IAdvancesWithTime
    {
        [SerializeField] private GameConfig config;

        private float totalSeconds;

        /// <summary>Raised with the season that just began.</summary>
        public event Action<Season> SeasonChanged;

        /// <summary>Raised with the next season once it is close (see the config's coming up time), once per season.</summary>
        public event Action<Season> NextSeasonComingUp;

        /// <summary>Seconds of play since the tree was planted, e.g. to save it.</summary>
        public float TotalSeconds => totalSeconds;

        public float SecondsPerSeason => config != null ? config.Seasons.SecondsPerSeason : 300f;

        public Season Season => SeasonRules.SeasonAt(totalSeconds, SecondsPerSeason);

        public Season NextSeason => SeasonRules.Next(Season);

        public SeasonPart Part => SeasonRules.PartAt(totalSeconds, SecondsPerSeason);

        public int Year => SeasonRules.YearAt(totalSeconds, SecondsPerSeason);

        /// <summary>How far through the year it is, 0 at the start of spring to just under 1 at the end of winter.</summary>
        public float YearShare => SeasonRules.YearShare(totalSeconds, SecondsPerSeason);

        public float SecondsLeftInSeason => SeasonRules.SecondsLeftInSeason(totalSeconds, SecondsPerSeason);

        /// <summary>True while the next season is close enough that the player has been told it's coming.</summary>
        public bool IsNextSeasonComingUp => config != null && SecondsLeftInSeason <= config.Seasons.ComingUpSeconds;

        /// <summary>How the tree looks now: its leaves and the snow.</summary>
        public SeasonLook Look => SeasonRules.LookAt(totalSeconds, SecondsPerSeason);

        private void OnEnable()
        {
            if (config == null)
            {
                Debug.LogWarning($"{nameof(SeasonClock)}: needs a {nameof(GameConfig)}.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            // Scaled time, so pausing via Time.timeScale also stops the year.
            Advance(Time.deltaTime);
        }

        /// <summary>How strongly a season effect applies now. 1 without a config, as without seasons.</summary>
        public float Multiplier(SeasonEffect effect)
        {
            return config != null ? config.Seasons.Multiplier(effect, totalSeconds) : 1f;
        }

        /// <summary>A season effect's multiplier from this clock, or 1 if there is none, e.g. in a scene without seasons.</summary>
        public static float MultiplierOf(SeasonClock clock, SeasonEffect effect)
        {
            return clock != null ? clock.Multiplier(effect) : 1f;
        }

        public void Advance(float seconds)
        {
            // Called directly after a minigame too, so it must hold up to what it is given.
            if (!(seconds > 0f) || !float.IsFinite(seconds))
            {
                return;
            }

            Season before = Season;
            bool wasComingUp = IsNextSeasonComingUp;
            totalSeconds += seconds;

            // Even a long catch-up announces only where it ends up, not every season in between.
            if (Season != before)
            {
                SeasonChanged?.Invoke(Season);
            }
            else if (!wasComingUp && IsNextSeasonComingUp)
            {
                NextSeasonComingUp?.Invoke(NextSeason);
            }
        }

        /// <summary>
        /// Moves the year to this play time without announcing anything, as when loading a save. A damaged or negative
        /// value starts the year over from the beginning of spring.
        /// </summary>
        public void SetTotalSeconds(float seconds)
        {
            totalSeconds = float.IsFinite(seconds) && seconds > 0f ? seconds : 0f;
        }

        /// <summary>Jumps to the start of the next season and announces it, e.g. from the debug panel.</summary>
        public void SkipToNextSeason()
        {
            Season before = Season;
            totalSeconds = SeasonRules.StartOfNextSeason(totalSeconds, SecondsPerSeason);

            // Rounding can leave the time a hair before the boundary.
            if (Season == before)
            {
                totalSeconds += 0.01f;
            }

            SeasonChanged?.Invoke(Season);
        }
    }
}
