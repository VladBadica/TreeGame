using System;
using System.Collections.Generic;
using TreeGame.Seasons;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace TreeGame.DevTools
{
    /// <summary>
    /// Checks what a moment of play time means for the seasons, without a scene or Play Mode. The rules have no Unity
    /// dependency, so this also compiles and runs outside Unity; only the menu item is Editor-specific.
    /// </summary>
    public static class SeasonRulesCheck
    {
        private const float Tolerance = 0.0001f;
        private const float SeasonLength = 300f;
        private const float Year = SeasonLength * 4f;

        /// <summary>Runs every check. Returns a description of each failure; empty means all passed.</summary>
        public static List<string> Run()
        {
            var failures = new List<string>();

            void Expect(bool condition, string failure)
            {
                if (!condition)
                {
                    failures.Add(failure);
                }
            }

            bool Near(float actual, float expected) => MathF.Abs(actual - expected) < Tolerance;

            // The year starts in spring and goes through the seasons in order, then starts over.
            Expect(SeasonRules.SeasonAt(0f, SeasonLength) == Season.Spring, "a new tree should start in spring");
            Expect(SeasonRules.SeasonAt(SeasonLength, SeasonLength) == Season.Summer, "summer should start after one season");
            Expect(SeasonRules.SeasonAt(SeasonLength * 2.5f, SeasonLength) == Season.Autumn, "halfway through the third season should be autumn");
            Expect(SeasonRules.SeasonAt(Year - 1f, SeasonLength) == Season.Winter, "the end of the year should be winter");
            Expect(SeasonRules.SeasonAt(Year, SeasonLength) == Season.Spring, "a new year should start in spring again");
            Expect(SeasonRules.YearAt(0f, SeasonLength) == 1 && SeasonRules.YearAt(Year - 1f, SeasonLength) == 1, "the first year should be year 1");
            Expect(SeasonRules.YearAt(Year, SeasonLength) == 2 && SeasonRules.YearAt(Year * 5f + 10f, SeasonLength) == 6, "years should count up");
            Expect(SeasonRules.Next(Season.Winter) == Season.Spring, "after winter comes spring");

            // Thirds of a season.
            Expect(SeasonRules.PartAt(10f, SeasonLength) == SeasonPart.Early, "the start of a season should be early");
            Expect(SeasonRules.PartAt(SeasonLength * 0.5f, SeasonLength) == SeasonPart.Mid, "the middle of a season should be mid");
            Expect(SeasonRules.PartAt(SeasonLength * 0.9f, SeasonLength) == SeasonPart.Late, "the end of a season should be late");
            Expect(SeasonRules.Describe(Year + SeasonLength * 2.9f, SeasonLength) == "Year 2 · Late Autumn",
                $"should describe as Year 2 · Late Autumn, not {SeasonRules.Describe(Year + SeasonLength * 2.9f, SeasonLength)}");

            // Time left and where in the year it is.
            Expect(Near(SeasonRules.SecondsLeftInSeason(SeasonLength + 100f, SeasonLength), 200f), "100s into summer should leave 200s");
            Expect(Near(SeasonRules.YearShare(SeasonLength * 2f, SeasonLength), 0.5f), "the start of autumn should be halfway through the year");
            Expect(Near(SeasonRules.StartOfNextSeason(SeasonLength + 100f, SeasonLength), SeasonLength * 2f), "the next season after summer should start at 600s");

            // Damaged times and lengths can't break anything.
            foreach (float bad in new[] { -50f, float.NaN, float.NegativeInfinity, float.PositiveInfinity })
            {
                Expect(SeasonRules.SeasonAt(bad, SeasonLength) == Season.Spring && SeasonRules.YearAt(bad, SeasonLength) == 1,
                    $"a time of {bad} should count as the very start");
            }

            foreach (float bad in new[] { 0f, -1f, float.NaN })
            {
                Expect(SeasonRules.SeasonAt(SeasonLength, bad) == Season.Summer, $"a season length of {bad} should fall back to 5 minutes");
            }

            // Multipliers hold for a season, ease into the next in its last moments, and arrive exactly as it starts.
            var values = new SeasonValues(spring: 1.5f, summer: 1f, autumn: 0.5f, winter: 0f);
            const float blend = 45f;
            Expect(Near(SeasonRules.Blend(values, 10f, SeasonLength, blend), 1.5f), "early spring should use spring's value");
            Expect(Near(SeasonRules.Blend(values, SeasonLength - blend - 1f, SeasonLength, blend), 1.5f), "spring's value should hold until the blend starts");
            float halfway = SeasonRules.Blend(values, SeasonLength - blend * 0.5f, SeasonLength, blend);
            Expect(Near(halfway, 1.25f), $"halfway through the blend should be between the seasons, not {halfway}");
            Expect(Near(SeasonRules.Blend(values, SeasonLength * 3f, SeasonLength, blend), 0f), "growth should be fully paused from the first moment of winter");
            Expect(Near(SeasonRules.Blend(values, SeasonLength * 3.5f, SeasonLength, blend), 0f), "growth should stay paused through winter");
            Expect(SeasonRules.Blend(values, Year - 1f, SeasonLength, blend) > 1f, "the very end of winter should be easing into spring");
            Expect(Near(SeasonRules.Blend(values, SeasonLength * 2f, SeasonLength, 0f), 0.5f), "with no blend, a season's value should apply at once");
            Expect(SeasonRules.Blend(new SeasonValues(float.NaN, -2f, 1f, 1f), 10f, SeasonLength, blend) == 1f, "a damaged value should count as 1");
            Expect(SeasonRules.Blend(new SeasonValues(1f, -2f, 1f, 1f), SeasonLength + 10f, SeasonLength, blend) == 0f, "a negative value should count as 0");

            // Continuous: no sudden jumps anywhere in the year.
            float previous = SeasonRules.Blend(values, 0f, SeasonLength, blend);
            for (float t = 1f; t <= Year * 2f; t += 1f)
            {
                float now = SeasonRules.Blend(values, t, SeasonLength, blend);
                if (MathF.Abs(now - previous) > 0.05f)
                {
                    failures.Add($"the value jumped from {previous} to {now} at {t}s");
                    break;
                }

                previous = now;
            }

            // The leaves: bare at the start of spring, full in summer, turning then falling in autumn, bare in winter.
            Expect(Near(SeasonRules.LookAt(0f, SeasonLength).LeafCover, 0f), "a tree should start spring bare");
            Expect(Near(SeasonRules.LookAt(SeasonLength * 0.6f, SeasonLength).LeafCover, 1f), "new leaves should be fully in by the middle of spring");
            Expect(SeasonRules.LookAt(10f, SeasonLength).Fresh > 0.9f, "early spring leaves should be fresh");
            SeasonLook summer = SeasonRules.LookAt(SeasonLength * 1.5f, SeasonLength);
            Expect(Near(summer.LeafCover, 1f) && summer.Autumn == 0f && summer.Fresh == 0f, "summer should be a full green crown");
            SeasonLook earlyAutumn = SeasonRules.LookAt(SeasonLength * 2.3f, SeasonLength);
            Expect(Near(earlyAutumn.LeafCover, 1f) && earlyAutumn.Autumn > 0.5f, "early autumn leaves should be turning but not yet falling");
            Expect(SeasonRules.LookAt(SeasonLength * 2.7f, SeasonLength).LeafCover < 0.7f, "late autumn should have lost many leaves");
            Expect(Near(SeasonRules.LookAt(SeasonLength * 2.97f, SeasonLength).LeafCover, 0f), "every leaf should be down just before winter");
            Expect(Near(SeasonRules.LookAt(SeasonLength * 3.5f, SeasonLength).LeafCover, 0f), "winter should be bare");

            // Gentle words, never a ticking countdown, and an exact time for the tooltip.
            Expect(SeasonRules.TimeLeftGently(Season.Autumn, 130f) == "Autumn in about 2 min",
                $"130s should read gently as about 2 min, not '{SeasonRules.TimeLeftGently(Season.Autumn, 130f)}'");
            Expect(SeasonRules.TimeLeftGently(Season.Autumn, 45f) == "Autumn in about 1 min", "45s should read as about 1 min");
            Expect(SeasonRules.TimeLeftGently(Season.Winter, 20f) == "Winter is almost here", "the last seconds should read as almost here");
            Expect(SeasonRules.TimeLeftExactly(Season.Autumn, 83.2f) == "1:24 until Autumn",
                $"83.2s should read 1:24, not '{SeasonRules.TimeLeftExactly(Season.Autumn, 83.2f)}'");

            // A tree's autumn colors come from its seed, the same every time, and every palette gets used.
            var used = new HashSet<int>();
            for (int seed = 0; seed < 200; seed++)
            {
                int palette = SeasonRules.AutumnPalette(seed, 3);
                Expect(palette >= 0 && palette < 3, $"seed {seed} picked palette {palette}, out of range");
                Expect(palette == SeasonRules.AutumnPalette(seed, 3), $"seed {seed} should always pick the same palette");
                used.Add(palette);
            }

            Expect(used.Count == 3, "every autumn palette should be picked by some seed");
            Expect(SeasonRules.AutumnPalette(int.MinValue, 3) >= 0 && SeasonRules.AutumnPalette(-7, 3) >= 0, "negative seeds should still pick a palette");

            return failures;
        }

#if UNITY_EDITOR
        [MenuItem("Tree Game/Run SeasonLength Rules Check")]
        private static void RunFromMenu()
        {
            List<string> failures = Run();
            if (failures.Count == 0)
            {
                Debug.Log("SeasonLength rules check PASSED.");
            }
            else
            {
                Debug.LogError("SeasonLength rules check FAILED:\n- " + string.Join("\n- ", failures));
            }
        }
#endif
    }
}
