using System;
using TreeGame.Core;
using TreeGame.Trees;
using UnityEngine;

namespace TreeGame.Care
{
    /// <summary>
    /// Slows the tree's growth for a while when too much of a resource is poured on at once: overwatering for water,
    /// overfeeding for compost. One per resource, each with its own settings in <see cref="GameConfig"/>. Overflow up to an
    /// allowed share of the resource's maximum is simply lost; past it, every part grows slower for a base time plus more
    /// for each percent past the allowed share. Pouring too much again while it lasts keeps whichever time is longer,
    /// rather than adding them up. Penalties from different resources add up. Connects pouring and growth rates at this
    /// level, so neither knows about the other.
    /// </summary>
    public class OverflowPenalty : MonoBehaviour, IAdvancesWithTime
    {
        [Tooltip("Where the resource is poured onto the tree. Its resource decides which settings apply.")]
        [SerializeField] private ResourceReceiver receiver;
        [SerializeField] private TreeGrowthRates growthRates;
        [SerializeField] private GameConfig config;

        private float secondsLeft;

        /// <summary>Raised when the penalty starts or ends, and when pouring too much again makes it last longer.</summary>
        public event Action Changed;

        /// <summary>Which resource poured in excess brings this penalty, e.g. water for overwatering.</summary>
        public TreeResourceType ResourceType => receiver.ResourceType;

        /// <summary>Seconds of play until the tree recovers; 0 when the penalty isn't active.</summary>
        public float SecondsLeft => secondsLeft;

        public bool IsActive => secondsLeft > 0f;

        private OverflowPenaltySettings Settings => config.GetOverflowPenaltySettings(ResourceType);

        private void OnEnable()
        {
            if (receiver == null || growthRates == null || config == null || receiver.Resource == null || Settings == null)
            {
                Debug.LogWarning($"{nameof(OverflowPenalty)}: needs a receiver for a resource with penalty settings, the growth rates and a {nameof(GameConfig)}.", this);
                enabled = false;
                return;
            }

            receiver.Overflowed += OnOverflowed;
            ApplyPenalty();
        }

        private void OnDisable()
        {
            if (receiver != null)
            {
                receiver.Overflowed -= OnOverflowed;
            }

            // Not lifted here: the runner switches everything off during a minigame, and the penalty must still count
            // when it catches growth up afterwards.
        }

        private void Update()
        {
            // Scaled time, so pausing via Time.timeScale also pauses the countdown.
            Advance(Time.deltaTime);
        }

        public void Advance(float seconds)
        {
            if (!IsActive || !(seconds > 0f) || !float.IsFinite(seconds))
            {
                return;
            }

            secondsLeft = Mathf.Max(0f, secondsLeft - seconds);
            if (!IsActive)
            {
                ApplyPenalty();
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// How long pouring this much too much would slow growth for, in seconds; 0 if it is within what's allowed.
        /// Public so the rule can be checked on its own.
        /// </summary>
        public float SecondsFor(float overflow)
        {
            float max = receiver.Resource.MaxAmount;
            OverflowPenaltySettings settings = Settings;
            if (settings == null || !(overflow > 0f) || !float.IsFinite(overflow) || !(max > 0f))
            {
                return 0f;
            }

            float overflowShare = overflow / max;
            if (overflowShare <= settings.AllowedOverflow)
            {
                return 0f;
            }

            float percentPastAllowed = (overflowShare - settings.AllowedOverflow) * 100f;
            return settings.BaseSeconds + settings.SecondsPerExtraPercent * percentPastAllowed;
        }

        /// <summary>Continues a saved penalty. Negative or damaged values count as no penalty.</summary>
        public void RestoreSecondsLeft(float seconds)
        {
            secondsLeft = seconds > 0f && float.IsFinite(seconds) ? seconds : 0f;
            ApplyPenalty();
            Changed?.Invoke();
        }

        private void OnOverflowed(float overflow)
        {
            float seconds = SecondsFor(overflow);
            if (seconds <= secondsLeft)
            {
                return;
            }

            secondsLeft = seconds;
            ApplyPenalty();
            Changed?.Invoke();
        }

        private void ApplyPenalty()
        {
            if (growthRates != null && config != null && receiver != null && receiver.Resource != null && Settings != null)
            {
                growthRates.SetTemporaryChange(this, IsActive ? -Settings.RatePenalty : 0);
            }
        }
    }
}
