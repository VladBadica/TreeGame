using System;
using System.Collections.Generic;
using TreeGame.Pests;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace TreeGame.DevTools
{
    /// <summary>
    /// Checks the timing and damage of insects on the tree without a scene or Play Mode. The rules have no Unity
    /// dependency, so this also compiles and runs outside Unity; only the menu item is Editor-specific.
    /// </summary>
    public static class InsectInfestationRulesCheck
    {
        private const float Grace = 20f;
        private const float DamagePeriod = 40f;
        private const float DamagePerInsect = 5f;
        private const int MaxInsects = 3;
        private const float FrameSeconds = 1f / 60f;
        // Adding up thousands of frames drifts a little in floats, so damage is only compared this closely.
        private const float DamageTolerance = 0.01f;

        /// <summary>Runs every check. Returns a description of each failure; empty means all passed.</summary>
        public static List<string> Run()
        {
            var failures = new List<string>();
            CheckNoDamageDuringGrace(failures);
            CheckDamageBuildsSteadily(failures);
            CheckTotalDamageAndLeaving(failures);
            CheckLongCatchUpMatchesShortFrames(failures);
            CheckCatchUpAcrossPartOfTheDamagePeriod(failures);
            CheckInsectsAreTimedSeparately(failures);
            CheckCountLimit(failures);
            CheckRemovingAShare(failures);
            CheckRemovingTakesTheOldestFirst(failures);
            CheckBadTimeIsIgnored(failures);
            CheckBadTuningIsRejected(failures);
            return failures;
        }

#if UNITY_EDITOR
        [MenuItem("Tree Game/Run Insect Rules Check")]
        private static void RunFromMenu()
        {
            List<string> failures = Run();
            if (failures.Count == 0)
            {
                Debug.Log("Insect rules check PASSED.");
            }
            else
            {
                Debug.LogError("Insect rules check FAILED:\n- " + string.Join("\n- ", failures));
            }
        }
#endif

        private static InsectInfestationRules NewRules()
        {
            return new InsectInfestationRules(Grace, DamagePeriod, DamagePerInsect, MaxInsects);
        }

        private static InsectInfestationRules NewRulesWithInsect()
        {
            InsectInfestationRules rules = NewRules();
            rules.TryAddInsect();
            return rules;
        }

        /// <summary>Plays a span of time in frames, returning the total damage done.</summary>
        private static float PlayFrames(InsectInfestationRules rules, float seconds)
        {
            float damage = 0f;
            int frames = (int)Math.Round(seconds / FrameSeconds);
            for (int i = 0; i < frames; i++)
            {
                damage += rules.Advance(FrameSeconds);
            }

            return damage;
        }

        private static void Expect(List<string> failures, bool condition, string failure)
        {
            if (!condition)
            {
                failures.Add(failure);
            }
        }

        private static void ExpectDamage(List<string> failures, float actual, float expected, string situation)
        {
            Expect(failures, Math.Abs(actual - expected) <= DamageTolerance, $"{situation}: damage was {actual}, expected {expected}");
        }

        private static void CheckNoDamageDuringGrace(List<string> failures)
        {
            InsectInfestationRules rules = NewRulesWithInsect();
            Expect(failures, PlayFrames(rules, Grace - 0.1f) == 0f, "an insect did damage during its grace period");
            Expect(failures, rules.Count == 1, "an insect left during its grace period");
        }

        private static void CheckDamageBuildsSteadily(List<string> failures)
        {
            InsectInfestationRules rules = NewRulesWithInsect();
            PlayFrames(rules, Grace);
            ExpectDamage(failures, PlayFrames(rules, DamagePeriod / 4f), DamagePerInsect / 4f, "first quarter of the damage period");
            ExpectDamage(failures, PlayFrames(rules, DamagePeriod / 2f), DamagePerInsect / 2f, "next half of the damage period");
        }

        private static void CheckTotalDamageAndLeaving(List<string> failures)
        {
            InsectInfestationRules rules = NewRulesWithInsect();
            float damage = PlayFrames(rules, Grace + DamagePeriod - 0.1f);
            Expect(failures, rules.Count == 1, "an insect left before its damage period ended");

            damage += PlayFrames(rules, 0.2f);
            Expect(failures, rules.Count == 0, "an insect stayed after its damage period ended");
            ExpectDamage(failures, damage, DamagePerInsect, "a whole stay");

            Expect(failures, PlayFrames(rules, 10f) == 0f, "damage continued after the insect left");
        }

        private static void CheckLongCatchUpMatchesShortFrames(List<string> failures)
        {
            // Far longer than a stay, as after a long minigame: all of its damage, and none extra for the spare time.
            InsectInfestationRules rules = NewRulesWithInsect();
            ExpectDamage(failures, rules.Advance((Grace + DamagePeriod) * 3f), DamagePerInsect, "one very long catch-up");
            Expect(failures, rules.Count == 0, "an insect stayed after a catch-up longer than its stay");
        }

        private static void CheckCatchUpAcrossPartOfTheDamagePeriod(List<string> failures)
        {
            // From 10s to 40s: only the 20s after the grace period ended count, which is half the damage period.
            InsectInfestationRules rules = NewRulesWithInsect();
            rules.Advance(10f);
            ExpectDamage(failures, rules.Advance(30f), DamagePerInsect / 2f, "a catch-up across the end of the grace period");
            Expect(failures, rules.Count == 1, "an insect left partway through its damage period");

            // From 40s to 100s: only the 20s before its stay ended count.
            ExpectDamage(failures, rules.Advance(60f), DamagePerInsect / 2f, "a catch-up across the end of the stay");
            Expect(failures, rules.Count == 0, "an insect stayed after a catch-up across the end of its stay");
        }

        private static void CheckInsectsAreTimedSeparately(List<string> failures)
        {
            InsectInfestationRules rules = NewRulesWithInsect();
            rules.Advance(Grace);
            rules.TryAddInsect();

            // The first is halfway through its damage period; the second is still in its grace period.
            ExpectDamage(failures, rules.Advance(Grace), DamagePerInsect / 2f, "one insect past its grace period and one not");

            // Now both are doing damage, the older from 40s to 60s and the newer from 20s to 40s.
            ExpectDamage(failures, rules.Advance(Grace), DamagePerInsect, "two insects at once");
            Expect(failures, rules.Count == 1, "the older insect should have left and the newer one stayed");
        }

        private static void CheckCountLimit(List<string> failures)
        {
            InsectInfestationRules rules = NewRules();
            for (int i = 0; i < MaxInsects; i++)
            {
                Expect(failures, rules.TryAddInsect(), $"insect {i + 1} of {MaxInsects} was refused");
            }

            Expect(failures, !rules.TryAddInsect(), "an insect was added beyond the limit");
            Expect(failures, rules.Count == MaxInsects, $"count is {rules.Count} after hitting the limit of {MaxInsects}");

            rules.Advance(Grace + DamagePeriod);
            Expect(failures, rules.TryAddInsect(), "no insect could be added after the others left");
        }

        private static InsectInfestationRules NewRulesWithInsects(int count)
        {
            var rules = new InsectInfestationRules(Grace, DamagePeriod, DamagePerInsect, count);
            for (int i = 0; i < count; i++)
            {
                rules.TryAddInsect();
            }

            return rules;
        }

        private static void CheckRemovingAShare(List<string> failures)
        {
            // Half, rounded up: an odd number leaves the smaller part, and a lone insect always goes.
            foreach ((int before, int after) in new[] { (5, 2), (4, 2), (3, 1), (2, 1), (1, 0), (0, 0) })
            {
                InsectInfestationRules half = NewRulesWithInsects(before);
                half.RemoveShare(0.5f);
                Expect(failures, half.Count == after, $"removing half of {before} left {half.Count}, expected {after}");
            }

            InsectInfestationRules rules = NewRulesWithInsects(5);
            rules.RemoveShare(1f);
            Expect(failures, rules.Count == 0, $"removing all of 5 left {rules.Count}");

            rules = NewRulesWithInsects(5);
            rules.RemoveShare(3f);
            Expect(failures, rules.Count == 0, $"removing more than all of 5 left {rules.Count}");

            // Any share at all removes at least one, while nothing or nonsense removes none.
            rules = NewRulesWithInsects(5);
            rules.RemoveShare(0.01f);
            Expect(failures, rules.Count == 4, $"removing a tiny share of 5 left {rules.Count}, expected 4");

            foreach (float badShare in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                rules = NewRulesWithInsects(5);
                rules.RemoveShare(badShare);
                Expect(failures, rules.Count == 5, $"removing a share of {badShare} left {rules.Count}, expected 5");
            }
        }

        private static void CheckRemovingTakesTheOldestFirst(List<string> failures)
        {
            // The older insect is doing damage and the newer one is still in its grace period.
            InsectInfestationRules rules = NewRulesWithInsect();
            rules.Advance(Grace + 1f);
            rules.TryAddInsect();

            rules.RemoveShare(0.5f);
            Expect(failures, rules.Count == 1, $"removing half of 2 left {rules.Count}");
            Expect(failures, rules.Advance(1f) == 0f, "the insect doing damage should have gone first, but damage continued");

            // What's left still runs its full time.
            ExpectDamage(failures, rules.Advance(Grace - 1f + DamagePeriod), DamagePerInsect, "the rest of the remaining insect's stay");
        }

        private static void CheckBadTimeIsIgnored(List<string> failures)
        {
            InsectInfestationRules rules = NewRulesWithInsect();
            rules.Advance(Grace + 1f);

            foreach (float badTime in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -5f, 0f })
            {
                Expect(failures, rules.Advance(badTime) == 0f, $"advancing by {badTime} did damage");
            }

            Expect(failures, rules.Count == 1, "bad time removed an insect");
            ExpectDamage(failures, rules.Advance(1f), DamagePerInsect / DamagePeriod, "the first good step after bad time");
        }

        private static void CheckBadTuningIsRejected(List<string> failures)
        {
            void ExpectRejected(string situation, Action create)
            {
                try
                {
                    create();
                    failures.Add($"{situation} was accepted");
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Expected.
                }
            }

            ExpectRejected("a negative grace period", () => new InsectInfestationRules(-1f, DamagePeriod, DamagePerInsect, MaxInsects));
            ExpectRejected("a NaN grace period", () => new InsectInfestationRules(float.NaN, DamagePeriod, DamagePerInsect, MaxInsects));
            ExpectRejected("a zero damage period", () => new InsectInfestationRules(Grace, 0f, DamagePerInsect, MaxInsects));
            ExpectRejected("an infinite damage period", () => new InsectInfestationRules(Grace, float.PositiveInfinity, DamagePerInsect, MaxInsects));
            ExpectRejected("negative damage", () => new InsectInfestationRules(Grace, DamagePeriod, -1f, MaxInsects));
            ExpectRejected("a negative limit", () => new InsectInfestationRules(Grace, DamagePeriod, DamagePerInsect, -1));
        }
    }
}
