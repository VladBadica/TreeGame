using System;

namespace TreeGame.Trees
{
    /// <summary>
    /// What a part's growth rate, a whole number from 1 (slow) to 10 (fast), means for how fast it grows. Above
    /// <see cref="ExtraGrowthAbove"/>, a part also fills its extra growth: growth past its full size, which it keeps even
    /// once its rate drops again. Plain C# with no Unity dependency, so it can be checked on its own.
    /// </summary>
    public static class GrowthRates
    {
        /// <summary>The slowest rate. There is no 0: every part of the tree always keeps growing.</summary>
        public const int Min = 1;
        public const int Max = 10;

        /// <summary>The rate a part grows at unless something changes it.</summary>
        public const int Middle = 5;

        /// <summary>A part fills its extra growth only at a rate above this, and faster the further above it is.</summary>
        public const int ExtraGrowthAbove = 7;

        /// <summary>Any rate brought into 1 to 10.</summary>
        public static int Clamp(int rate)
        {
            return Math.Clamp(rate, Min, Max);
        }

        /// <summary>
        /// The share of a part's growth (0 to 1) it gains per second of play at this rate, if a part takes
        /// <paramref name="secondsAtMiddleRate"/> to grow fully at the middle rate. Twice the rate is twice as fast.
        /// </summary>
        public static float GrowthPerSecond(int rate, float secondsAtMiddleRate)
        {
            if (!IsPositive(secondsAtMiddleRate))
            {
                return 0f;
            }

            return Clamp(rate) / (float)Middle / secondsAtMiddleRate;
        }

        /// <summary>
        /// The share of a part's extra growth (0 to 1) it gains per second of play at this rate, if extra growth takes
        /// <paramref name="secondsAtMaxRate"/> to fill at the top rate. Nothing at or below
        /// <see cref="ExtraGrowthAbove"/>; above it, in step with how far above: at 10 three times as fast as at 8.
        /// </summary>
        public static float ExtraGrowthPerSecond(int rate, float secondsAtMaxRate)
        {
            int above = Clamp(rate) - ExtraGrowthAbove;
            if (above <= 0 || !IsPositive(secondsAtMaxRate))
            {
                return 0f;
            }

            return above / (float)(Max - ExtraGrowthAbove) / secondsAtMaxRate;
        }

        private static bool IsPositive(float value)
        {
            return value > 0f && float.IsFinite(value);
        }
    }
}
