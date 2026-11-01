using System;
using System.Collections.Generic;
using TreeGame.Minigames.WellSpin;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace TreeGame.DevTools
{
    /// <summary>
    /// Checks the rules of Well Spin without a scene or Play Mode. The rules have no Unity dependency, so this also
    /// compiles and runs outside Unity; only the menu item is Editor-specific.
    /// </summary>
    public static class WellSpinRulesCheck
    {
        private const float TimeStep = 0.002f;
        private const int SeedsToTry = 2000;

        /// <summary>Runs every check. Returns a description of each failure; empty means all passed.</summary>
        public static List<string> Run()
        {
            var failures = new List<string>();
            CheckStartsClockwiseAndWraps(failures);
            CheckHit(failures);
            CheckMiss(failures);
            CheckHitDetectionAgainstIndependentMath(failures);
            CheckTargetsAcrossTheTopOfTheRing(failures);
            CheckNewTargetsKeepTheirDistance(failures);
            CheckEndsAfterMaxTries(failures);
            CheckEndsAfterMaxMisses(failures);
            CheckEndsWhenTimeRunsOut(failures);
            CheckSpeedUps(failures);
            CheckBadTimeIsIgnored(failures);
            CheckSameSeedPlaysTheSame(failures);
            CheckBadTuningIsRejected(failures);
            return failures;
        }

#if UNITY_EDITOR
        [MenuItem("Tree Game/Run Well Spin Rules Check")]
        private static void RunFromMenu()
        {
            List<string> failures = Run();
            if (failures.Count == 0)
            {
                Debug.Log("Well Spin rules check PASSED.");
            }
            else
            {
                Debug.LogError("Well Spin rules check FAILED:\n- " + string.Join("\n- ", failures));
            }
        }
#endif

        private static WellSpinRules NewGame(int seed = 1, WellSpinTuning tuning = null)
        {
            return new WellSpinRules(tuning ?? new WellSpinTuning(), new System.Random(seed));
        }

        private static void Expect(List<string> failures, bool condition, string failure)
        {
            if (!condition)
            {
                failures.Add(failure);
            }
        }

        /// <summary>Moves the indicator until it is inside (or outside) the target.</summary>
        private static void TickUntil(WellSpinRules game, bool insideTarget)
        {
            const int safetyLimit = 100000;
            for (int i = 0; i < safetyLimit && game.IsIndicatorInTarget != insideTarget; i++)
            {
                game.Tick(TimeStep);
            }
        }

        private static void Hit(WellSpinRules game)
        {
            TickUntil(game, insideTarget: true);
            game.Press();
        }

        private static void Miss(WellSpinRules game)
        {
            TickUntil(game, insideTarget: false);
            game.Press();
        }

        private static void CheckStartsClockwiseAndWraps(List<string> failures)
        {
            WellSpinRules game = NewGame();
            Expect(failures, game.Direction == 1, "the indicator should start clockwise");
            Expect(failures, game.IndicatorAngle == 0f, "the indicator should start at the top");

            game.Tick(0.1f);
            Expect(failures, Near(game.IndicatorAngle, 18f), $"0.1s at 180 degrees/s should reach 18, got {game.IndicatorAngle}");

            game.Tick(2f);
            Expect(failures, Near(game.IndicatorAngle, 18f), $"a full turn should come back to 18, got {game.IndicatorAngle}");
            Expect(failures, game.IndicatorAngle >= 0f && game.IndicatorAngle < 360f, "the angle should stay within the ring");
        }

        private static void CheckHit(List<string> failures)
        {
            WellSpinRules game = NewGame();
            TickUntil(game, insideTarget: true);
            float oldTarget = game.TargetCenter;

            WellSpinPress press = game.Press();

            Expect(failures, press == WellSpinPress.Hit, $"pressing inside the target should hit, got {press}");
            Expect(failures, game.Hits == 1 && game.Misses == 0, "a hit should add one hit and no misses");
            Expect(failures, game.Direction == -1, "a hit should reverse the indicator");
            Expect(failures, game.TargetCenter != oldTarget, "a hit should move the target");

            // After reversing, the indicator really does run the other way, including across the top of the ring.
            float before = game.IndicatorAngle;
            game.Tick(0.01f);
            Expect(failures, Near(ShortestChange(before, game.IndicatorAngle), -1.8f), "after a hit the indicator should run counterclockwise");

            Hit(game);
            Expect(failures, game.Direction == 1, "a second hit should reverse the indicator back to clockwise");
        }

        private static void CheckMiss(List<string> failures)
        {
            WellSpinRules game = NewGame();
            TickUntil(game, insideTarget: false);
            float target = game.TargetCenter;

            WellSpinPress press = game.Press();

            Expect(failures, press == WellSpinPress.Miss, $"pressing outside the target should miss, got {press}");
            Expect(failures, game.Hits == 0 && game.Misses == 1, "a miss should add one miss and no hits");
            Expect(failures, game.Direction == 1, "a miss should not reverse the indicator");
            Expect(failures, game.TargetCenter == target, "a miss should not move the target");
        }

        /// <summary>Compares hit detection with a differently written calculation, at many positions and targets.</summary>
        private static void CheckHitDetectionAgainstIndependentMath(List<string> failures)
        {
            int disagreements = 0;
            for (int seed = 0; seed < SeedsToTry; seed++)
            {
                var random = new System.Random(seed);
                WellSpinRules game = NewGame(seed);
                Hit(game); // The first target can't touch the top of the ring; later ones can, so test those.
                game.Tick((float)random.NextDouble() * 2f);

                // Same angle on a circle: compare by cosine, which has no special case at the top of the ring.
                double radians = (game.IndicatorAngle - game.TargetCenter) * Math.PI / 180.0;
                double limit = game.TargetWidth / 2.0 * Math.PI / 180.0;
                double margin = Math.Abs(Math.Cos(radians) - Math.Cos(limit));
                bool expected = Math.Cos(radians) >= Math.Cos(limit);

                // Positions right on the target's edge are too close to call with floats either way.
                if (margin > 1e-4 && game.IsIndicatorInTarget != expected)
                {
                    disagreements++;
                }
            }

            Expect(failures, disagreements == 0, $"hit detection disagreed with the independent calculation {disagreements} times");
        }

        /// <summary>
        /// A target crossing 0/360 must count both sides of the top as inside, which naive subtraction gets wrong.
        /// Sweeps a full turn and measures how much of the ring counts as inside: it must be the target's whole width.
        /// </summary>
        private static void CheckTargetsAcrossTheTopOfTheRing(List<string> failures)
        {
            int found = 0;
            for (int seed = 0; seed < SeedsToTry && found < 50; seed++)
            {
                WellSpinRules game = NewGame(seed);
                Hit(game); // The first target can't touch the top, as the indicator starts there; later ones can.

                bool crossesTop = game.TargetCenter < game.TargetWidth / 2f || game.TargetCenter > 360f - game.TargetWidth / 2f;
                if (!crossesTop)
                {
                    continue;
                }

                found++;
                const int steps = 3600;
                float secondsPerStep = 360f / game.CurrentSpeed / steps;
                int stepsInside = 0;
                for (int i = 0; i < steps; i++)
                {
                    game.Tick(secondsPerStep);
                    if (game.IsIndicatorInTarget)
                    {
                        stepsInside++;
                    }
                }

                float degreesInside = stepsInside * 360f / steps;
                Expect(failures, Math.Abs(degreesInside - game.TargetWidth) < 1f,
                    $"seed {seed}: a target across the top should cover {game.TargetWidth} degrees, covered {degreesInside}");
            }

            Expect(failures, found > 0, "no target crossing the top of the ring came up, so this wasn't tested");
        }

        private static void CheckNewTargetsKeepTheirDistance(List<string> failures)
        {
            var tuning = new WellSpinTuning();
            float nearestAllowed = tuning.TargetWidth / 2f + tuning.MinTargetGap;

            for (int seed = 0; seed < SeedsToTry; seed++)
            {
                WellSpinRules game = NewGame(seed);
                for (int i = 0; i < 3; i++)
                {
                    Hit(game);
                    float distance = Math.Abs(ShortestChange(game.IndicatorAngle, game.TargetCenter));
                    if (distance < nearestAllowed - 0.01f)
                    {
                        failures.Add($"seed {seed}: new target only {distance} degrees from the indicator");
                        return;
                    }
                }
            }
        }

        private static void CheckEndsAfterMaxTries(List<string> failures)
        {
            WellSpinRules game = NewGame();
            for (int i = 0; i < 9; i++)
            {
                Hit(game);
            }

            Expect(failures, !game.IsFinished, "the game should still run after 9 tries");
            Hit(game);
            Expect(failures, game.IsFinished, "the game should end after 10 tries");
            Expect(failures, game.Hits == 10, $"10 hits in a row should score 10, got {game.Hits}");

            float angle = game.IndicatorAngle;
            game.Tick(0.5f);
            Expect(failures, game.IndicatorAngle == angle, "the indicator should stop when the game is over");
            Expect(failures, game.Press() == WellSpinPress.Ignored, "a press after the end should be ignored");
            Expect(failures, game.Tries == 10, "a press after the end should not count");

            // Two misses still allow the full 10 tries.
            game = NewGame();
            Miss(game);
            Miss(game);
            for (int i = 0; i < 8; i++)
            {
                Hit(game);
            }

            Expect(failures, game.IsFinished && game.Hits == 8 && game.Misses == 2, "8 hits and 2 misses should end the game after 10 tries");
        }

        private static void CheckEndsAfterMaxMisses(List<string> failures)
        {
            WellSpinRules game = NewGame();
            Miss(game);
            Hit(game);
            Miss(game);
            Expect(failures, !game.IsFinished, "the game should still run after 2 misses");
            Miss(game);
            Expect(failures, game.IsFinished, "the game should end on the 3rd miss, even with hits in between");
            Expect(failures, game.Tries == 4, $"this game should have ended after 4 tries, got {game.Tries}");
        }

        private static void CheckEndsWhenTimeRunsOut(List<string> failures)
        {
            WellSpinRules game = NewGame();
            Expect(failures, Near(game.TimeLeft, 30f), $"a game should start with 30 seconds, got {game.TimeLeft}");

            game.Tick(10f);
            Expect(failures, Near(game.TimeLeft, 20f), $"10 seconds of play should leave 20, got {game.TimeLeft}");

            game = NewGame();
            for (int i = 0; i < 299; i++)
            {
                game.Tick(0.1f);
            }

            Expect(failures, !game.IsFinished, "the game should still run just before the time limit");
            game.Tick(0.2f);
            Expect(failures, game.IsFinished && game.IsOutOfTime, "the game should end when the time limit is reached");
            Expect(failures, game.TimeLeft == 0f, $"the time left should stop at 0, got {game.TimeLeft}");
            Expect(failures, game.Tries == 0, "running out of time should not count as a try");

            float angle = game.IndicatorAngle;
            game.Tick(1f);
            Expect(failures, game.IndicatorAngle == angle, "the indicator should stop when time runs out");
            Expect(failures, game.Press() == WellSpinPress.Ignored, "a press after time runs out should be ignored");

            // Pressing doesn't use up time, and hits and misses don't refill it.
            game = NewGame();
            TickUntil(game, insideTarget: true);
            float timeLeft = game.TimeLeft;
            game.Press();
            Expect(failures, game.TimeLeft == timeLeft, "a press should not change the time left");

            // A custom limit is respected.
            game = NewGame(tuning: new WellSpinTuning { TimeLimitSeconds = 5f });
            game.Tick(5f);
            Expect(failures, game.IsOutOfTime, "a 5 second limit should end the game after 5 seconds");
        }

        private static void CheckSpeedUps(List<string> failures)
        {
            var tuning = new WellSpinTuning();
            float baseSpeed = tuning.DegreesPerSecond;
            float multiplier = tuning.SpeedUpMultiplier;
            WellSpinRules game = NewGame();

            int[] hitsToSpeedSteps = { 0, 0, 0, 1, 1, 1, 2, 2, 2 };
            for (int hits = 0; hits < hitsToSpeedSteps.Length; hits++)
            {
                float expected = baseSpeed * MathF.Pow(multiplier, hitsToSpeedSteps[hits]);
                Expect(failures, Near(game.CurrentSpeed, expected), $"after {hits} hits the speed should be {expected}, got {game.CurrentSpeed}");
                Hit(game);
            }

            // Misses must not speed the game up, and the faster speed must actually move the indicator faster.
            // 3 tries, but only 1 hit: below the first speed-up, which is counted in hits and not in tries.
            game = NewGame();
            Miss(game);
            Miss(game);
            Hit(game);
            Expect(failures, Near(game.CurrentSpeed, baseSpeed), "misses should not count toward speed-ups");

            game = NewGame();
            for (int i = 0; i < 3; i++)
            {
                Hit(game);
            }

            float before = game.IndicatorAngle;
            game.Tick(0.01f);
            float moved = Math.Abs(ShortestChange(before, game.IndicatorAngle));
            Expect(failures, Near(moved, baseSpeed * multiplier * 0.01f), $"after 3 hits the indicator should move faster, moved {moved}");
        }

        private static void CheckBadTimeIsIgnored(List<string> failures)
        {
            WellSpinRules game = NewGame();
            game.Tick(0.1f);
            float angle = game.IndicatorAngle;

            game.Tick(float.NaN);
            game.Tick(float.PositiveInfinity);
            game.Tick(-1f);
            game.Tick(0f);

            Expect(failures, game.IndicatorAngle == angle, "invalid time steps should not move the indicator");
        }

        private static void CheckSameSeedPlaysTheSame(List<string> failures)
        {
            WellSpinRules first = NewGame(seed: 42);
            WellSpinRules second = NewGame(seed: 42);
            for (int i = 0; i < 5; i++)
            {
                Hit(first);
                Hit(second);
                Expect(failures, first.TargetCenter == second.TargetCenter, "the same seed should give the same targets");
            }
        }

        private static void CheckBadTuningIsRejected(List<string> failures)
        {
            ExpectRejected(failures, new WellSpinTuning { MaxTries = 0 }, "zero tries");
            ExpectRejected(failures, new WellSpinTuning { MaxMisses = 0 }, "zero misses");
            ExpectRejected(failures, new WellSpinTuning { TimeLimitSeconds = 0f }, "no time");
            ExpectRejected(failures, new WellSpinTuning { DegreesPerSecond = 0f }, "zero speed");
            ExpectRejected(failures, new WellSpinTuning { DegreesPerSecond = float.NaN }, "NaN speed");
            ExpectRejected(failures, new WellSpinTuning { TargetWidth = 0f }, "zero target width");
            ExpectRejected(failures, new WellSpinTuning { TargetWidth = 300f, MinTargetGap = 45f }, "a target too large to place");
        }

        private static void ExpectRejected(List<string> failures, WellSpinTuning tuning, string description)
        {
            try
            {
                NewGame(tuning: tuning);
                failures.Add($"tuning with {description} should be rejected");
            }
            catch (ArgumentException)
            {
                // Expected.
            }
        }

        private static bool Near(float a, float b)
        {
            return Math.Abs(a - b) < 0.01f;
        }

        /// <summary>The signed shortest change from one angle to another, -180 to 180.</summary>
        private static float ShortestChange(float from, float to)
        {
            float change = (to - from) % 360f;
            if (change > 180f)
            {
                change -= 360f;
            }
            else if (change < -180f)
            {
                change += 360f;
            }

            return change;
        }
    }
}
