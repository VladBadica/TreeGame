using System;

namespace TreeGame.Seasons
{
    /// <summary>
    /// What a moment of play time means for the seasons: which season and year it is, how long until the next season,
    /// how strongly each season's effects apply and how the tree looks. Time is the play time since the tree was planted,
    /// which starts at the beginning of spring. Plain C# with no Unity dependency, so it can be checked on its own.
    /// </summary>
    public static class SeasonRules
    {
        public const int SeasonsPerYear = 4;

        /// <summary>
        /// Leaves turn their autumn colors over this first share of autumn; fully turned after that.
        /// </summary>
        private const float AutumnColorShare = 0.5f;

        /// <summary>Leaves start falling this far into autumn, and are all gone by <see cref="LeavesGoneShare"/>.</summary>
        private const float LeavesStartFallingShare = 0.35f;
        private const float LeavesGoneShare = 0.95f;

        /// <summary>New leaves grow in over this first share of spring.</summary>
        private const float LeafOutShare = 0.5f;

        /// <summary>The time left that still counts as "almost here", in seconds; shorter waits stop counting minutes.</summary>
        public const float AlmostHereSeconds = 30f;

        /// <summary>Which season it is.</summary>
        public static Season SeasonAt(float totalSeconds, float secondsPerSeason)
        {
            return (Season)(SeasonIndex(totalSeconds, secondsPerSeason) % SeasonsPerYear);
        }

        /// <summary>Which year it is, counting from 1 for the year the tree was planted.</summary>
        public static int YearAt(float totalSeconds, float secondsPerSeason)
        {
            return (int)(SeasonIndex(totalSeconds, secondsPerSeason) / SeasonsPerYear) + 1;
        }

        /// <summary>How far through its season it is, from 0 at its start to just under 1 at its end.</summary>
        public static float SeasonShare(float totalSeconds, float secondsPerSeason)
        {
            float seconds = ValidSeconds(totalSeconds);
            float length = ValidLength(secondsPerSeason);
            float share = (float)((double)seconds % length / length);
            return Math.Clamp(share, 0f, 1f);
        }

        /// <summary>How far through its year it is, from 0 at the start of spring to just under 1 at the end of winter.</summary>
        public static float YearShare(float totalSeconds, float secondsPerSeason)
        {
            float seasonInYear = (int)SeasonAt(totalSeconds, secondsPerSeason);
            return (seasonInYear + SeasonShare(totalSeconds, secondsPerSeason)) / SeasonsPerYear;
        }

        /// <summary>Early, mid or late in its season: each a third of it.</summary>
        public static SeasonPart PartAt(float totalSeconds, float secondsPerSeason)
        {
            float share = SeasonShare(totalSeconds, secondsPerSeason);
            return share < 1f / 3f ? SeasonPart.Early : share < 2f / 3f ? SeasonPart.Mid : SeasonPart.Late;
        }

        /// <summary>Seconds of play until the next season starts. Always more than 0.</summary>
        public static float SecondsLeftInSeason(float totalSeconds, float secondsPerSeason)
        {
            float length = ValidLength(secondsPerSeason);
            float left = length * (1f - SeasonShare(totalSeconds, secondsPerSeason));
            return Math.Max(left, 0.001f);
        }

        /// <summary>The play time at which the next season starts, e.g. to skip to it.</summary>
        public static float StartOfNextSeason(float totalSeconds, float secondsPerSeason)
        {
            return (float)((SeasonIndex(totalSeconds, secondsPerSeason) + 1) * (double)ValidLength(secondsPerSeason));
        }

        public static Season Next(Season season)
        {
            return (Season)(((int)season + 1) % SeasonsPerYear);
        }

        /// <summary>
        /// Which seasons' values apply now: <paramref name="from"/> and <paramref name="to"/>, mixed by
        /// <paramref name="toShare"/> (0 = only from, 1 = only to). Each season's values hold for all of it, except its last
        /// <paramref name="blendSeconds"/>, over which they ease into the next season's, arriving exactly as it starts. So
        /// the change comes gently, and a season's values hold from its first moment (winter growth is fully paused).
        /// </summary>
        public static void Transition(float totalSeconds, float secondsPerSeason, float blendSeconds,
            out Season from, out Season to, out float toShare)
        {
            from = SeasonAt(totalSeconds, secondsPerSeason);
            to = Next(from);

            float length = ValidLength(secondsPerSeason);
            float blend = float.IsFinite(blendSeconds) ? Math.Clamp(blendSeconds, 0f, length) : 0f;
            float left = length * (1f - SeasonShare(totalSeconds, secondsPerSeason));
            toShare = blend > 0f && left < blend ? SmoothStep(1f - left / blend) : 0f;
        }

        /// <summary>The value that applies now, eased from one season's into the next (see <see cref="Transition"/>).</summary>
        public static float Blend(SeasonValues values, float totalSeconds, float secondsPerSeason, float blendSeconds)
        {
            if (values == null)
            {
                return 1f;
            }

            Transition(totalSeconds, secondsPerSeason, blendSeconds, out Season from, out Season to, out float toShare);
            float value = Lerp(values.For(from), values.For(to), toShare);

            // A damaged value in the config shouldn't stop time or run it backwards.
            return float.IsFinite(value) ? Math.Max(0f, value) : 1f;
        }

        /// <summary>
        /// How the tree looks now. Spring: new leaves grow in over its first half, light and fresh at first. Summer: a
        /// full green crown. Autumn: leaves turn over its first half, and fall from about a third of the way in until
        /// they're all gone just before winter. Winter: bare branches.
        /// </summary>
        public static SeasonLook LookAt(float totalSeconds, float secondsPerSeason)
        {
            float share = SeasonShare(totalSeconds, secondsPerSeason);
            switch (SeasonAt(totalSeconds, secondsPerSeason))
            {
                case Season.Spring:
                    return new SeasonLook(
                        leafCover: SmoothStep(share / LeafOutShare),
                        autumn: 0f,
                        fresh: 1f - SmoothStep(share));
                case Season.Summer:
                    return SeasonLook.FullLeaves;
                case Season.Autumn:
                    return new SeasonLook(
                        leafCover: 1f - SmoothStep((share - LeavesStartFallingShare) / (LeavesGoneShare - LeavesStartFallingShare)),
                        autumn: SmoothStep(share / AutumnColorShare),
                        fresh: 0f);
                default:
                    return new SeasonLook(leafCover: 0f, autumn: 1f, fresh: 0f);
            }
        }

        /// <summary>"Late Summer": the part of the season and the season.</summary>
        public static string Name(float totalSeconds, float secondsPerSeason)
        {
            return $"{PartAt(totalSeconds, secondsPerSeason)} {SeasonAt(totalSeconds, secondsPerSeason)}";
        }

        /// <summary>"Year 3 · Late Autumn", e.g. for a save slot.</summary>
        public static string Describe(float totalSeconds, float secondsPerSeason)
        {
            return $"Year {YearAt(totalSeconds, secondsPerSeason)} · {Name(totalSeconds, secondsPerSeason)}";
        }

        /// <summary>
        /// The time until the next season said gently, without a ticking countdown: "Autumn in about 2 min", then
        /// "Autumn is almost here" for the last <see cref="AlmostHereSeconds"/>.
        /// </summary>
        public static string TimeLeftGently(Season next, float secondsLeft)
        {
            if (!(secondsLeft > AlmostHereSeconds))
            {
                return $"{next} is almost here";
            }

            int minutes = Math.Max(1, (int)MathF.Round(secondsLeft / 60f));
            return $"{next} in about {minutes} min";
        }

        /// <summary>The exact time until the next season, as minutes and seconds: "1:24 until Autumn".</summary>
        public static string TimeLeftExactly(Season next, float secondsLeft)
        {
            // Rounded up, so it never says 0:00 while some time is still left.
            int seconds = float.IsFinite(secondsLeft) ? Math.Max(0, (int)MathF.Ceiling(secondsLeft)) : 0;
            return $"{seconds / 60}:{seconds % 60:00} until {next}";
        }

        /// <summary>Which of <paramref name="count"/> autumn palettes a tree grown from this seed turns, the same every time.</summary>
        public static int AutumnPalette(int seed, int count)
        {
            if (count <= 1)
            {
                return 0;
            }

            // Mixed, so neighboring seeds don't simply take turns.
            uint mixed = unchecked((uint)seed * 2654435761u);
            return (int)((mixed >> 16) % (uint)count);
        }

        /// <summary>How many whole seasons have passed since the tree was planted.</summary>
        private static long SeasonIndex(float totalSeconds, float secondsPerSeason)
        {
            return (long)Math.Floor(ValidSeconds(totalSeconds) / (double)ValidLength(secondsPerSeason));
        }

        /// <summary>Play time can't be negative or damaged; such a value counts as the very start.</summary>
        private static float ValidSeconds(float seconds)
        {
            return float.IsFinite(seconds) && seconds > 0f ? seconds : 0f;
        }

        /// <summary>A season must last some time; a damaged length falls back to the default 5 minutes.</summary>
        private static float ValidLength(float secondsPerSeason)
        {
            return float.IsFinite(secondsPerSeason) && secondsPerSeason >= 1f ? secondsPerSeason : 300f;
        }

        private static float SmoothStep(float x)
        {
            float t = Math.Clamp(float.IsFinite(x) ? x : 0f, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }
    }
}
