using System;
using System.Collections.Generic;
using TreeGame.Trees;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace TreeGame.DevTools
{
    /// <summary>
    /// Checks what growth rates mean for normal and extra growth, without a scene or Play Mode. The rules have no Unity
    /// dependency, so this also compiles and runs outside Unity; only the menu item is Editor-specific.
    /// </summary>
    public static class GrowthRatesCheck
    {
        private const float Tolerance = 0.00001f;

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

            // Rates are whole numbers from 1 to 10.
            Expect(GrowthRates.Clamp(0) == 1 && GrowthRates.Clamp(-5) == 1, "rates below 1 should become 1, as there is no 0");
            Expect(GrowthRates.Clamp(11) == 10 && GrowthRates.Clamp(int.MaxValue) == 10, "rates above 10 should become 10");
            Expect(GrowthRates.Clamp(7) == 7, "a rate in range should stay as it is");

            // Normal growth: at the middle rate a part takes the given time; twice the rate, half the time.
            const float middleSeconds = 300f;
            Expect(Near(GrowthRates.GrowthPerSecond(GrowthRates.Middle, middleSeconds) * middleSeconds, 1f), "at the middle rate a part should grow fully in the given time");
            Expect(Near(GrowthRates.GrowthPerSecond(10, middleSeconds), 2f * GrowthRates.GrowthPerSecond(5, middleSeconds)), "rate 10 should grow twice as fast as rate 5");
            Expect(Near(GrowthRates.GrowthPerSecond(1, middleSeconds) * 5f, GrowthRates.GrowthPerSecond(5, middleSeconds)), "rate 1 should grow five times slower than rate 5");
            Expect(GrowthRates.GrowthPerSecond(1, middleSeconds) > 0f, "every rate should still grow, as there is no 0");
            Expect(Near(GrowthRates.GrowthPerSecond(50, middleSeconds), GrowthRates.GrowthPerSecond(10, middleSeconds)), "a rate past 10 should grow as 10");

            // Extra growth: nothing at 7 or below, only 8, 9 and 10 fill it.
            const float maxSeconds = 600f;
            for (int rate = GrowthRates.Min; rate <= GrowthRates.ExtraGrowthAbove; rate++)
            {
                Expect(GrowthRates.ExtraGrowthPerSecond(rate, maxSeconds) == 0f, $"rate {rate} shouldn't fill extra growth");
            }

            Expect(GrowthRates.ExtraGrowthPerSecond(0, maxSeconds) == 0f && GrowthRates.ExtraGrowthPerSecond(-20, maxSeconds) == 0f,
                "a rate below 1 shouldn't fill extra growth");

            // At 10 it fills in the given time; 9 takes 1.5 times as long and 8 three times as long.
            float AtRate(int rate) => GrowthRates.ExtraGrowthPerSecond(rate, maxSeconds);
            Expect(Near(AtRate(10) * maxSeconds, 1f), "at rate 10 extra growth should fill in the given time");
            Expect(Near(1f / AtRate(9), 1.5f * maxSeconds), $"at rate 9 extra growth should take 1.5 times as long, took {1f / AtRate(9)}s");
            Expect(Near(1f / AtRate(8), 3f * maxSeconds), $"at rate 8 extra growth should take 3 times as long, took {1f / AtRate(8)}s");
            Expect(Near(AtRate(15), AtRate(10)), "a rate past 10 should fill extra growth as 10");

            // Bad times give no growth rather than infinite or negative growth.
            foreach (float bad in new[] { 0f, -10f, float.NaN, float.PositiveInfinity })
            {
                Expect(GrowthRates.GrowthPerSecond(10, bad) == 0f, $"a growth time of {bad} should give no growth");
                Expect(GrowthRates.ExtraGrowthPerSecond(10, bad) == 0f, $"an extra growth time of {bad} should give no extra growth");
            }

            return failures;
        }

#if UNITY_EDITOR
        [MenuItem("Tree Game/Run Growth Rates Check")]
        private static void RunFromMenu()
        {
            List<string> failures = Run();
            if (failures.Count == 0)
            {
                Debug.Log("Growth rates check PASSED.");
            }
            else
            {
                Debug.LogError("Growth rates check FAILED:\n- " + string.Join("\n- ", failures));
            }
        }
#endif
    }
}
