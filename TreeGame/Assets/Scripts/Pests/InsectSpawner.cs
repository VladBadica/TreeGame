using TreeGame.Core;
using TreeGame.Seasons;
using UnityEngine;

namespace TreeGame.Pests
{
    /// <summary>
    /// Brings insects to the tree at random intervals (settings in <see cref="GameConfig"/>), more or less often with the
    /// season, and none in winter.
    /// </summary>
    public class InsectSpawner : MonoBehaviour, IAdvancesWithTime
    {
        [SerializeField] private InsectInfestation infestation;
        [SerializeField] private GameConfig config;
        [Tooltip("Makes insects come more or less often with the season. Without one, they come the same all year.")]
        [SerializeField] private SeasonClock seasons;

        private float secondsUntilNextInsect;
        // The first wait is picked on first use rather than in Start, so a save restored before or after Start keeps
        // its countdown instead of having it replaced by a fresh one.
        private bool hasWait;

        /// <summary>How long until the next insect comes, in seconds of play, e.g. to save it.</summary>
        public float SecondsUntilNextInsect
        {
            get
            {
                PickFirstWaitIfNeeded();
                return secondsUntilNextInsect;
            }
        }

        private void OnEnable()
        {
            if (infestation == null || config == null)
            {
                Debug.LogWarning($"{nameof(InsectSpawner)}: needs an {nameof(InsectInfestation)} and a {nameof(GameConfig)}.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        /// <summary>
        /// Continues a saved countdown to the next insect. A negative value means none was saved, so a fresh wait is
        /// picked; a damaged one (NaN, infinite) is treated the same. A wait longer than the config's longest is cut to
        /// it, so a bad value can't keep insects away for good.
        /// </summary>
        public void RestoreSecondsUntilNextInsect(float seconds)
        {
            if (config == null)
            {
                return;
            }

            hasWait = true;
            secondsUntilNextInsect = seconds >= 0f && float.IsFinite(seconds)
                ? Mathf.Min(seconds, config.Insects.LongestSecondsBetweenInsects)
                : config.Insects.RandomSecondsBetweenInsects();
        }

        public void Advance(float seconds)
        {
            PickFirstWaitIfNeeded();

            // The season makes the wait count down faster or slower; in winter it stands still, so no insects come.
            secondsUntilNextInsect -= seconds * SeasonClock.MultiplierOf(seasons, SeasonEffect.Insects);

            // A loop, because catching up after a long time can mean several insects are due at once.
            while (secondsUntilNextInsect <= 0f)
            {
                infestation.TryAddInsect();
                secondsUntilNextInsect += config.Insects.RandomSecondsBetweenInsects();
            }
        }

        private void PickFirstWaitIfNeeded()
        {
            if (hasWait || config == null)
            {
                return;
            }

            hasWait = true;
            secondsUntilNextInsect = config.Insects.RandomSecondsBetweenInsects();
        }
    }
}
