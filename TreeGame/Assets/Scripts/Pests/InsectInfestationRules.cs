using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>How long each insect has been on the tree, in seconds, oldest first.</summary>
        public IReadOnlyList<float> InsectAges => insectAges;

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

        /// <summary>
        /// Replaces the insects with ones of these ages, as when loading. Ages that couldn't happen (invalid, or past the
        /// end of a stay) are dropped, and the oldest are kept if there are more than the tree can hold.
        /// </summary>
        public void RestoreInsects(IEnumerable<float> ages)
        {
            insectAges.Clear();
            if (ages == null)
            {
                return;
            }

            insectAges.AddRange(ages
                .Where(age => float.IsFinite(age) && age >= 0f && age < SecondsOnTree)
                .OrderByDescending(age => age)
                .Take(maxInsects));
        }

        /// <summary>
        /// Removes a share of the insects (0 to 1), rounded up so any share above 0 removes at least one. The oldest go
        /// first: they are the ones doing damage. An invalid share removes nothing.
        /// </summary>
        public void RemoveShare(float share)
        {
            if (!float.IsFinite(share) || share <= 0f)
            {
                return;
            }

            int count = (int)Math.Ceiling(Math.Min(share, 1f) * insectAges.Count);
            insectAges.RemoveRange(0, count);
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
