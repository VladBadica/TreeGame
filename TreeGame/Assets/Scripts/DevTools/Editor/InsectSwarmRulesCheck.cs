using System;
using System.Collections.Generic;
using System.Linq;
using TreeGame.Minigames.InsectSwarm;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif
using Vector2 = System.Numerics.Vector2;

namespace TreeGame.DevTools
{
    /// <summary>
    /// Checks the rules of Insect Swarm without a scene or Play Mode. The rules have no Unity dependency, so this also
    /// compiles and runs outside Unity; only the menu item is Editor-specific.
    /// </summary>
    public static class InsectSwarmRulesCheck
    {
        private const float FrameSeconds = 1f / 60f;
        private const int SeedsToTry = 200;

        // A simple layout that's easy to reason about: one crack straight left of the patch, 7 units from its edge,
        // so at 2 units per second an insect gets through 3.5 seconds after it appears.
        private static readonly Vector2 OnlyCrack = new Vector2(0f, 4.5f);
        private const float SecondsToGetThrough = 3.5f;

        /// <summary>Runs every check. Returns a description of each failure; empty means all passed.</summary>
        public static List<string> Run()
        {
            var failures = new List<string>();
            CheckStart(failures);
            CheckArrivalsAreEvenlySpaced(failures);
            CheckBiggerSwarmsArriveFaster(failures);
            CheckCrawling(failures);
            CheckGettingThrough(failures);
            CheckClickStopsAnInsect(failures);
            CheckClickReach(failures);
            CheckClickIgnoresInsectsThatCantBeClicked(failures);
            CheckClickStopsOnlyTheNearest(failures);
            CheckEndsWhenAllAreDealtWith(failures);
            CheckEndsWhenTimeRunsOut(failures);
            CheckLongStepMatchesShortFrames(failures);
            CheckDefaultCracks(failures);
            CheckBadTimeIsIgnored(failures);
            CheckBadClickIsAMiss(failures);
            CheckSameSeedPlaysTheSame(failures);
            CheckBadSetupIsRejected(failures);
            return failures;
        }

#if UNITY_EDITOR
        [MenuItem("Tree Game/Run Insect Swarm Rules Check")]
        private static void RunFromMenu()
        {
            List<string> failures = Run();
            if (failures.Count == 0)
            {
                Debug.Log("Insect Swarm rules check PASSED.");
            }
            else
            {
                Debug.LogError("Insect Swarm rules check FAILED:\n- " + string.Join("\n- ", failures));
            }
        }
#endif

        private static InsectSwarmTuning SimpleTuning()
        {
            return new InsectSwarmTuning { CrawlSpeed = 2f, ArrivalSeconds = 20f, Cracks = new[] { OnlyCrack } };
        }

        private static InsectSwarmRules NewGame(int swarmSize = 1, InsectSwarmTuning tuning = null, int seed = 1)
        {
            return new InsectSwarmRules(tuning ?? SimpleTuning(), swarmSize, new System.Random(seed));
        }

        private static void Expect(List<string> failures, bool condition, string failure)
        {
            if (!condition)
            {
                failures.Add(failure);
            }
        }

        private static bool Near(float a, float b)
        {
            return Math.Abs(a - b) < 0.01f;
        }

        private static bool Near(Vector2 a, Vector2 b)
        {
            return Vector2.Distance(a, b) < 0.01f;
        }

        /// <summary>Plays a span of time in frames.</summary>
        private static void PlayFrames(InsectSwarmRules game, float seconds)
        {
            int frames = (int)Math.Round(seconds / FrameSeconds);
            for (int i = 0; i < frames; i++)
            {
                game.Tick(FrameSeconds);
            }
        }

        private static int CountInState(InsectSwarmRules game, SwarmInsectState state)
        {
            return game.Insects.Count(insect => insect.State == state);
        }

        private static void CheckStart(List<string> failures)
        {
            InsectSwarmRules game = NewGame(swarmSize: 4);
            Expect(failures, game.SwarmSize == 4 && game.Insects.Count == 4, "a swarm of 4 should have 4 insects");
            Expect(failures, CountInState(game, SwarmInsectState.Waiting) == 4, "every insect should start out waiting");
            Expect(failures, game.Insects.All(insect => insect.Position == OnlyCrack), "every insect should start at a crack");
            Expect(failures, game.Stopped == 0 && game.GotThrough == 0 && game.Score == 0f, "a new game should have no score");
            Expect(failures, Near(game.TimeLeft, 30f) && !game.IsFinished, "a new game should have its full time and be running");
        }

        private static void CheckArrivalsAreEvenlySpaced(List<string> failures)
        {
            // Too slow to get through during this check. 5 insects over 20 seconds arrive at 0, 5, 10, 15 and 20.
            InsectSwarmTuning tuning = SimpleTuning();
            tuning.CrawlSpeed = 0.01f;
            InsectSwarmRules game = NewGame(swarmSize: 5, tuning);

            game.Tick(FrameSeconds);
            Expect(failures, CountInState(game, SwarmInsectState.Crawling) == 1, "the first insect should appear at once");

            float secondsPlayed = 0f;
            void PlayUntil(float seconds)
            {
                PlayFrames(game, seconds - secondsPlayed);
                secondsPlayed = seconds;
            }

            for (int arrived = 1; arrived < 5; arrived++)
            {
                PlayUntil(arrived * 5f - 0.1f);
                Expect(failures, CountInState(game, SwarmInsectState.Crawling) == arrived, $"only {arrived} should have appeared just before {arrived * 5} seconds");
                PlayUntil(arrived * 5f + 0.1f);
                Expect(failures, CountInState(game, SwarmInsectState.Crawling) == arrived + 1, $"{arrived + 1} should have appeared just after {arrived * 5} seconds");
            }

            // A single insect has no others to be spaced from, and must not divide by zero getting there.
            InsectSwarmRules single = NewGame(swarmSize: 1);
            single.Tick(FrameSeconds);
            Expect(failures, CountInState(single, SwarmInsectState.Crawling) == 1, "a lone insect should appear at once");
        }

        private static void CheckBiggerSwarmsArriveFaster(List<string> failures)
        {
            // All of a bigger swarm still arrives within the same time.
            InsectSwarmTuning tuning = SimpleTuning();
            tuning.CrawlSpeed = 0.01f;
            foreach (int size in new[] { 2, 6, 16 })
            {
                InsectSwarmRules game = NewGame(size, tuning);
                PlayFrames(game, 19.5f);
                Expect(failures, CountInState(game, SwarmInsectState.Waiting) == 1, $"the last of {size} insects should still be waiting just before 20 seconds");
                PlayFrames(game, 1f);
                Expect(failures, CountInState(game, SwarmInsectState.Waiting) == 0, $"all {size} insects should have appeared by 20 seconds");
            }
        }

        private static void CheckCrawling(List<string> failures)
        {
            InsectSwarmRules game = NewGame();
            SwarmInsect insect = game.Insects[0];
            Expect(failures, Near(insect.Heading, new Vector2(1f, 0f)), $"an insect left of the patch should head right, got {insect.Heading}");

            PlayFrames(game, 1f);
            Expect(failures, Near(insect.Position, new Vector2(2f, 4.5f)), $"after 1 second at 2 units/s it should be 2 along, got {insect.Position}");

            // From a corner, so both axes count, it must still move at the crawl speed and head for the patch.
            var tuning = new InsectSwarmTuning { CrawlSpeed = 2f, Cracks = new[] { new Vector2(2f, 0.5f) } };
            game = NewGame(tuning: tuning);
            insect = game.Insects[0];
            PlayFrames(game, 0.5f);
            Expect(failures, Near(Vector2.Distance(insect.Position, new Vector2(2f, 0.5f)), 1f), "a diagonal crawl should cover the same distance");
            Expect(failures, Near(insect.Position, new Vector2(2f, 0.5f) + Vector2.Normalize(new Vector2(6f, 4f))), $"a diagonal crawl should head for the patch, got {insect.Position}");
        }

        private static void CheckGettingThrough(List<string> failures)
        {
            InsectSwarmRules game = NewGame();
            PlayFrames(game, SecondsToGetThrough - 0.1f);
            Expect(failures, game.GotThrough == 0 && game.Insects[0].State == SwarmInsectState.Crawling, "an insect got through too early");

            PlayFrames(game, 0.2f);
            Expect(failures, game.GotThrough == 1 && game.Insects[0].State == SwarmInsectState.GotThrough, "an insect should get through at the patch's edge");
            Expect(failures, Near(game.Insects[0].Position, new Vector2(7f, 4.5f)), $"it should stop at the patch's edge, got {game.Insects[0].Position}");
            Expect(failures, game.Score == 0f, "an insect getting through should not score");
        }

        private static void CheckClickStopsAnInsect(List<string> failures)
        {
            InsectSwarmRules game = NewGame(swarmSize: 2);
            PlayFrames(game, 1f);
            SwarmInsect insect = game.Insects[0];
            Vector2 stoppedAt = insect.Position;

            Expect(failures, game.Click(insect.Position) == SwarmClick.Stopped, "clicking right on an insect should stop it");
            Expect(failures, insect.State == SwarmInsectState.Stopped && game.Stopped == 1, "a stopped insect should be counted");
            Expect(failures, Near(game.Score, 0.5f), $"stopping 1 of 2 should score 0.5, got {game.Score}");

            PlayFrames(game, 1f);
            Expect(failures, insect.Position == stoppedAt, "a stopped insect should not keep crawling");

            PlayFrames(game, SecondsToGetThrough);
            Expect(failures, game.GotThrough == 0, "a stopped insect should not get through");

            Expect(failures, game.Click(stoppedAt) == SwarmClick.Miss, "clicking where a stopped insect is should miss");
            Expect(failures, game.Stopped == 1, "a stopped insect should only count once");
        }

        private static void CheckClickReach(List<string> failures)
        {
            float reach = new InsectSwarmTuning().ClickRadius;

            InsectSwarmRules game = NewGame();
            PlayFrames(game, 1f);
            Vector2 position = game.Insects[0].Position;
            Expect(failures, game.Click(position + new Vector2(0f, reach + 0.01f)) == SwarmClick.Miss, "a click just out of reach should miss");
            Expect(failures, game.Click(position + new Vector2(reach + 0.01f, 0f)) == SwarmClick.Miss, "a click just out of reach sideways should miss");
            Expect(failures, game.Stopped == 0, "a miss should not stop anything");
            Expect(failures, !game.IsFinished, "a miss should not end the game");
            Expect(failures, game.Click(position + new Vector2(0f, reach - 0.01f)) == SwarmClick.Stopped, "a click just in reach should stop it");
        }

        private static void CheckClickIgnoresInsectsThatCantBeClicked(List<string> failures)
        {
            // The second insect is still in its crack.
            InsectSwarmRules game = NewGame(swarmSize: 2);
            game.Tick(FrameSeconds);
            Expect(failures, game.Click(OnlyCrack + new Vector2(-0.5f, 0f)) == SwarmClick.Stopped, "this click should reach the crawling insect");
            Expect(failures, game.Click(OnlyCrack) == SwarmClick.Miss, "an insect still in its crack can't be clicked");
            Expect(failures, game.Stopped == 1, "an insect still in its crack should not count as stopped");

            // The first has got through, the second is still waiting for its turn.
            game = NewGame(swarmSize: 2);
            PlayFrames(game, SecondsToGetThrough + 0.1f);
            Expect(failures, game.Click(new Vector2(7f, 4.5f)) == SwarmClick.Miss, "an insect that got through can't be clicked");
        }

        private static void CheckClickStopsOnlyTheNearest(List<string> failures)
        {
            // Two insects crawling close together, the first ahead of the second.
            InsectSwarmTuning tuning = SimpleTuning();
            tuning.ArrivalSeconds = 0.2f;
            InsectSwarmRules game = NewGame(swarmSize: 2, tuning);
            PlayFrames(game, 1f);
            SwarmInsect ahead = game.Insects[0];
            SwarmInsect behind = game.Insects[1];
            Expect(failures, Vector2.Distance(ahead.Position, behind.Position) < 0.6f, "this check needs the insects within reach of one click");

            Expect(failures, game.Click(behind.Position) == SwarmClick.Stopped, "clicking the rear insect should stop one");
            Expect(failures, behind.State == SwarmInsectState.Stopped, "it should stop the insect nearest the click, not the first in the list");
            Expect(failures, ahead.State == SwarmInsectState.Crawling, "one click should stop only one insect");

            // The same again with the front one nearest, so going through the list in either order can't pass by luck.
            game = NewGame(swarmSize: 2, tuning);
            PlayFrames(game, 1f);
            Expect(failures, game.Click(game.Insects[0].Position) == SwarmClick.Stopped, "clicking the front insect should stop one");
            Expect(failures, game.Insects[0].State == SwarmInsectState.Stopped && game.Insects[1].State == SwarmInsectState.Crawling, "it should stop the front insect only");
        }

        private static void CheckEndsWhenAllAreDealtWith(List<string> failures)
        {
            // All stopped.
            InsectSwarmRules game = NewGame();
            game.Tick(FrameSeconds);
            game.Click(game.Insects[0].Position);
            Expect(failures, game.IsFinished && !game.IsOutOfTime, "stopping the whole swarm should end the game");
            Expect(failures, game.Score == 1f, $"stopping the whole swarm should score 1, got {game.Score}");

            float timeLeft = game.TimeLeft;
            game.Tick(1f);
            Expect(failures, game.TimeLeft == timeLeft, "the clock should stop when the game is over");
            Expect(failures, game.Click(game.Insects[0].Position) == SwarmClick.Ignored, "a click after the end should be ignored");

            // All through.
            game = NewGame();
            PlayFrames(game, SecondsToGetThrough + 0.1f);
            Expect(failures, game.IsFinished && game.Score == 0f, "the whole swarm getting through should end the game with no score");

            // One of each, with the game running until the last is dealt with.
            game = NewGame(swarmSize: 2, new InsectSwarmTuning { CrawlSpeed = 2f, ArrivalSeconds = 1f, Cracks = new[] { OnlyCrack } });
            PlayFrames(game, 0.5f);
            game.Click(game.Insects[0].Position);
            Expect(failures, !game.IsFinished, "the game should keep running while an insect is still to come");
            PlayFrames(game, 1f + SecondsToGetThrough);
            Expect(failures, game.IsFinished && game.Stopped == 1 && game.GotThrough == 1, "one stopped and one through should end the game");
            Expect(failures, Near(game.Score, 0.5f), $"one stopped and one through should score 0.5, got {game.Score}");
        }

        private static void CheckEndsWhenTimeRunsOut(List<string> failures)
        {
            // Too slow to reach the patch, so only the clock can end this game.
            InsectSwarmTuning tuning = SimpleTuning();
            tuning.CrawlSpeed = 0.01f;
            InsectSwarmRules game = NewGame(swarmSize: 2, tuning);

            PlayFrames(game, 10f);
            Expect(failures, Near(game.TimeLeft, 20f), $"10 seconds of play should leave 20, got {game.TimeLeft}");

            PlayFrames(game, 19.9f);
            Expect(failures, !game.IsFinished, "the game should still run just before the time limit");
            game.Click(game.Insects[0].Position);

            PlayFrames(game, 0.2f);
            Expect(failures, game.IsFinished && game.IsOutOfTime, "the game should end at the time limit");
            Expect(failures, game.TimeLeft == 0f, $"the time left should stop at 0, got {game.TimeLeft}");
            Expect(failures, Near(game.Score, 0.5f), $"an insect still crawling at the end should not count as stopped, score {game.Score}");
            Expect(failures, game.GotThrough == 0, "an insect still crawling at the end should not count as through");

            Vector2 position = game.Insects[1].Position;
            game.Tick(1f);
            Expect(failures, game.Insects[1].Position == position, "insects should stop when time runs out");
            Expect(failures, game.Click(position) == SwarmClick.Ignored, "a click after time runs out should be ignored");

            // A step past the limit must not use up more time than there was: this insect would need 3.5 seconds of
            // crawling, but only has 1 before the limit, so it mustn't get through.
            game = NewGame(tuning: new InsectSwarmTuning { CrawlSpeed = 2f, TimeLimitSeconds = 1f, Cracks = new[] { OnlyCrack } });
            game.Tick(0.5f);
            game.Tick(100f);
            Expect(failures, game.GotThrough == 0, "a step past the time limit let an insect through on time that wasn't left");
            Expect(failures, game.TimeLeft == 0f, $"a step past the limit should leave 0, got {game.TimeLeft}");
            Expect(failures, Near(game.Insects[0].Position, OnlyCrack + new Vector2(2f, 0f)), $"it should have crawled for the second there was, got {game.Insects[0].Position}");
        }

        private static void CheckLongStepMatchesShortFrames(List<string> failures)
        {
            // Unlike the frames of a slow computer, this also crosses an arrival partway through a step.
            InsectSwarmRules frames = NewGame(swarmSize: 3);
            InsectSwarmRules longStep = NewGame(swarmSize: 3);
            const float seconds = 12f;

            PlayFrames(frames, seconds);
            longStep.Tick(seconds);

            for (int i = 0; i < 3; i++)
            {
                Expect(failures, frames.Insects[i].State == longStep.Insects[i].State, $"insect {i} ended up {longStep.Insects[i].State} after one long step, but {frames.Insects[i].State} after short frames");
                Expect(failures, Near(frames.Insects[i].Position, longStep.Insects[i].Position), $"insect {i} ended up in a different place after one long step");
            }
        }

        private static void CheckDefaultCracks(List<string> failures)
        {
            var tuning = new InsectSwarmTuning();
            var cracksUsed = new HashSet<Vector2>();
            for (int seed = 0; seed < SeedsToTry; seed++)
            {
                InsectSwarmRules game = NewGame(swarmSize: 6, tuning, seed);
                foreach (SwarmInsect insect in game.Insects)
                {
                    cracksUsed.Add(insect.Position);
                    Expect(failures, Array.IndexOf(tuning.Cracks, insect.Position) >= 0, $"an insect started away from every crack, at {insect.Position}");
                }
            }

            Expect(failures, cracksUsed.Count == tuning.Cracks.Length, $"only {cracksUsed.Count} of {tuning.Cracks.Length} cracks were ever used");

            // The swarm is made to be dealt with inside the time limit, even when its slowest insect starts last.
            float longestCrawl = tuning.Cracks.Max(crack => (Vector2.Distance(crack, tuning.PatchCenter) - tuning.PatchRadius) / tuning.CrawlSpeed);
            Expect(failures, tuning.ArrivalSeconds + longestCrawl < tuning.TimeLimitSeconds, "by default the last insect wouldn't reach the patch before time runs out");
        }

        private static void CheckBadTimeIsIgnored(List<string> failures)
        {
            InsectSwarmRules game = NewGame();
            PlayFrames(game, 1f);
            Vector2 position = game.Insects[0].Position;
            float timeLeft = game.TimeLeft;

            foreach (float badTime in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f, 0f })
            {
                game.Tick(badTime);
            }

            Expect(failures, game.Insects[0].Position == position, "invalid time steps should not move an insect");
            Expect(failures, game.TimeLeft == timeLeft, "invalid time steps should not change the time left");
        }

        private static void CheckBadClickIsAMiss(List<string> failures)
        {
            InsectSwarmRules game = NewGame();
            PlayFrames(game, 1f);
            Expect(failures, game.Click(new Vector2(float.NaN, float.NaN)) == SwarmClick.Miss, "a NaN click should miss");
            Expect(failures, game.Click(new Vector2(float.PositiveInfinity, 0f)) == SwarmClick.Miss, "an infinite click should miss");
            Expect(failures, game.Stopped == 0, "a bad click should not stop anything");
        }

        private static void CheckSameSeedPlaysTheSame(List<string> failures)
        {
            var tuning = new InsectSwarmTuning();
            InsectSwarmRules first = NewGame(swarmSize: 16, tuning, seed: 42);
            InsectSwarmRules second = NewGame(swarmSize: 16, tuning, seed: 42);
            InsectSwarmRules other = NewGame(swarmSize: 16, tuning, seed: 43);

            Expect(failures, first.Insects.Select(i => i.Position).SequenceEqual(second.Insects.Select(i => i.Position)), "the same seed should use the same cracks");
            Expect(failures, !first.Insects.Select(i => i.Position).SequenceEqual(other.Insects.Select(i => i.Position)), "a different seed should use different cracks");
        }

        private static void CheckBadSetupIsRejected(List<string> failures)
        {
            void ExpectRejected(string description, Action create)
            {
                try
                {
                    create();
                    failures.Add($"{description} should be rejected");
                }
                catch (ArgumentException)
                {
                    // Expected. ArgumentNullException and ArgumentOutOfRangeException are kinds of this.
                }
            }

            void ExpectTuningRejected(string description, InsectSwarmTuning tuning)
            {
                ExpectRejected($"tuning with {description}", () => NewGame(tuning: tuning));
            }

            ExpectRejected("a swarm of 0", () => NewGame(swarmSize: 0));
            ExpectRejected("no tuning", () => new InsectSwarmRules(null, 1, new System.Random(1)));
            ExpectRejected("no random source", () => new InsectSwarmRules(new InsectSwarmTuning(), 1, null));

            ExpectTuningRejected("no field width", new InsectSwarmTuning { FieldWidth = 0f });
            ExpectTuningRejected("no field height", new InsectSwarmTuning { FieldHeight = 0f });
            ExpectTuningRejected("no time", new InsectSwarmTuning { TimeLimitSeconds = 0f });
            ExpectTuningRejected("NaN time", new InsectSwarmTuning { TimeLimitSeconds = float.NaN });
            ExpectTuningRejected("infinite time", new InsectSwarmTuning { TimeLimitSeconds = float.PositiveInfinity });
            ExpectTuningRejected("negative arrival time", new InsectSwarmTuning { ArrivalSeconds = -1f });
            ExpectTuningRejected("NaN arrival time", new InsectSwarmTuning { ArrivalSeconds = float.NaN });
            ExpectTuningRejected("infinite arrival time", new InsectSwarmTuning { ArrivalSeconds = float.PositiveInfinity });
            ExpectTuningRejected("zero speed", new InsectSwarmTuning { CrawlSpeed = 0f });
            ExpectTuningRejected("zero patch radius", new InsectSwarmTuning { PatchRadius = 0f });
            ExpectTuningRejected("zero click radius", new InsectSwarmTuning { ClickRadius = 0f });
            ExpectTuningRejected("a patch off the field", new InsectSwarmTuning { PatchCenter = new Vector2(100f, 4.5f) });
            ExpectTuningRejected("no cracks", new InsectSwarmTuning { Cracks = new Vector2[0] });
            ExpectTuningRejected("null cracks", new InsectSwarmTuning { Cracks = null });
            ExpectTuningRejected("a crack off the field", new InsectSwarmTuning { Cracks = new[] { new Vector2(-1f, 4.5f) } });
            ExpectTuningRejected("a crack inside the patch", new InsectSwarmTuning { Cracks = new[] { new Vector2(8f, 4.5f) } });
        }
    }
}
