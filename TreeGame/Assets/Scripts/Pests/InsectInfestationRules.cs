using System;
using System.Collections.Generic;

namespace TreeGame.Pests
{
    /// <summary>
    /// The insects on the tree and what they do over time: each one sits harmlessly at first, then does its damage at
    /// a steady rate, then flies off. It works on spans of time rather than single frames, so one long catch-up after
    /// a minigame gives the same result as many short frames. Has no Unity dependency, so it can be checked on its own.
    /// </summary>
    public sealed class InsectInfestationRules
    {
        private readonly float graceSeconds;
        private readonly float damageSeconds;
        private readonly float damagePerInsect;
        private readonly int maxInsects;
        private readonly List<float> insectAges = new List<float>();

        public InsectInfestationRules(float graceSeconds, float damageSeconds, float damagePerInsect, int maxInsects)
        {
            RequireFiniteAtLeast(graceSeconds, 0f, nameof(graceSeconds));
            RequireFiniteAtLeast(damagePerInsect, 0f, nameof(damagePerInsect));
            if (!float.IsFinite(damageSeconds) || damageSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(damageSeconds), damageSeconds, "Must be finite and above zero.");
            }

            if (maxInsects < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxInsects), maxInsects, "Can't be negative.");
            }

            this.graceSeconds = graceSeconds;
            this.damageSeconds = damageSeconds;
            this.damagePerInsect = damagePerInsect;
            this.maxInsects = maxInsects;
        }

        public int Count => insectAges.Count;

        private float SecondsOnTree => graceSeconds + damageSeconds;

        /// <summary>Puts a new insect on the tree. Returns false if the tree already has as many as it can.</summary>
        public bool TryAddInsect()
        {
            if (insectAges.Count >= maxInsects)
            {
                return false;
            }

            insectAges.Add(0f);
            return true;
        }

        /// <summary>Moves time forward, removing insects whose stay has ended. Returns the damage done in that time.</summary>
        public float Advance(float seconds)
        {
            if (!float.IsFinite(seconds) || seconds <= 0f)
            {
                return 0f;
            }

            float damage = 0f;

            // Backwards, so removing an insect doesn't skip the next one.
            for (int i = insectAges.Count - 1; i >= 0; i--)
            {
                float oldAge = insectAges[i];
                float newAge = oldAge + seconds;
                damage += DamageBetweenAges(oldAge, newAge);

                if (newAge >= SecondsOnTree)
                {
                    insectAges.RemoveAt(i);
                }
                else
                {
                    insectAges[i] = newAge;
                }
            }

            return damage;
        }

        /// <summary>The damage one insect does as it ages: only the part of that time spent in its damage period counts.</summary>
        private float DamageBetweenAges(float fromAge, float toAge)
        {
            float start = Math.Max(fromAge, graceSeconds);
            float end = Math.Min(toAge, SecondsOnTree);
            return end > start ? (end - start) / damageSeconds * damagePerInsect : 0f;
        }

        private static void RequireFiniteAtLeast(float value, float minimum, string name)
        {
            if (!float.IsFinite(value) || value < minimum)
            {
                throw new ArgumentOutOfRangeException(name, value, $"Must be finite and at least {minimum}.");
            }
        }
    }
}
