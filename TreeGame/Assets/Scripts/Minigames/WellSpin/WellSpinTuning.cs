using System;

namespace TreeGame.Minigames.WellSpin
{
    /// <summary>The numbers that define how a game of Well Spin plays. Angles are in degrees.</summary>
    public sealed class WellSpinTuning
    {
        /// <summary>A game ends after this many presses, hit or miss.</summary>
        public int MaxTries { get; set; } = 10;

        /// <summary>A game also ends early once this many presses have missed.</summary>
        public int MaxMisses { get; set; } = 3;

        /// <summary>A game also ends once this many seconds of play have passed.</summary>
        public float TimeLimitSeconds { get; set; } = 30f;

        /// <summary>The indicator's starting speed, in degrees per second.</summary>
        public float DegreesPerSecond { get; set; } = 180f;

        /// <summary>The total hits at which the indicator speeds up, e.g. after 3 hits and after 6 hits.</summary>
        public int[] SpeedUpAfterHits { get; set; } = { 3, 6 };

        /// <summary>Each speed-up multiplies the current speed by this.</summary>
        public float SpeedUpMultiplier { get; set; } = 1.3f;

        /// <summary>How much of the ring the target area covers.</summary>
        public float TargetWidth { get; set; } = 40f;

        /// <summary>The least distance between the indicator and the edge of a new target, so it's never a free hit.</summary>
        public float MinTargetGap { get; set; } = 45f;

        /// <summary>Rejects numbers no game could be played with, so mistakes show up at once rather than as odd behavior.</summary>
        public void Validate()
        {
            if (MaxTries < 1 || MaxMisses < 1)
            {
                throw new ArgumentException("Tries and misses must be at least 1.");
            }

            if (!(TimeLimitSeconds > 0f))
            {
                throw new ArgumentException("The time limit must be positive.");
            }

            if (!(DegreesPerSecond > 0f) || !(SpeedUpMultiplier > 0f))
            {
                throw new ArgumentException("Speed and speed-up multiplier must be positive.");
            }

            if (!(TargetWidth > 0f) || !(MinTargetGap >= 0f))
            {
                throw new ArgumentException("Target width must be positive and the gap can't be negative.");
            }

            // A target and its gap on both sides must fit on the ring, or there is nowhere to put a new target.
            if (TargetWidth + 2f * MinTargetGap >= 360f)
            {
                throw new ArgumentException("Target width plus the gap on both sides must be less than the full ring.");
            }

            if (SpeedUpAfterHits == null)
            {
                throw new ArgumentException("Speed-up hits can't be null; use an empty list for no speed-ups.");
            }
        }
    }
}
