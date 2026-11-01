using System;

namespace TreeGame.Minigames.WellSpin
{
    /// <summary>What a press of the action key did.</summary>
    public enum WellSpinPress
    {
        /// <summary>The indicator was inside the target.</summary>
        Hit,

        /// <summary>The indicator was outside the target.</summary>
        Miss,

        /// <summary>The game was already over, so the press counted for nothing.</summary>
        Ignored,
    }

    /// <summary>
    /// The rules of Well Spin: an indicator circles a ring and the player presses a key while it is inside a target
    /// area. A hit scores a point, moves the target and reverses the indicator; a miss changes nothing but counts
    /// against the player. Plain C# with no Unity dependency, so it can be tested on its own and any visuals can
    /// sit on top. Angles are in degrees, 0 at the top of the ring and increasing clockwise.
    /// </summary>
    public sealed class WellSpinRules
    {
        private const float FullCircle = 360f;
        private const int Clockwise = 1;

        private readonly WellSpinTuning tuning;
        private readonly Random random;

        /// <param name="tuning">The numbers to play by.</param>
        /// <param name="random">Where new targets come from; pass a fixed seed for a repeatable game.</param>
        public WellSpinRules(WellSpinTuning tuning, Random random)
        {
            this.tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            tuning.Validate();

            Direction = Clockwise;
            MoveTarget();
        }

        /// <summary>Where the indicator is on the ring, 0 up to (not including) 360.</summary>
        public float IndicatorAngle { get; private set; }

        /// <summary>1 when the indicator moves clockwise, -1 when it moves counterclockwise.</summary>
        public int Direction { get; private set; }

        /// <summary>The middle of the target area.</summary>
        public float TargetCenter { get; private set; }

        public float TargetWidth => tuning.TargetWidth;

        public int Hits { get; private set; }

        public int Misses { get; private set; }

        public int Tries => Hits + Misses;

        public int MaxTries => tuning.MaxTries;

        public int MaxMisses => tuning.MaxMisses;

        public bool IsFinished => Tries >= tuning.MaxTries || Misses >= tuning.MaxMisses;

        public bool IsIndicatorInTarget => DistanceBetween(IndicatorAngle, TargetCenter) <= tuning.TargetWidth / 2f;

        /// <summary>The indicator's current speed in degrees per second, including any speed-ups earned so far.</summary>
        public float CurrentSpeed
        {
            get
            {
                float speed = tuning.DegreesPerSecond;
                foreach (int hitsNeeded in tuning.SpeedUpAfterHits)
                {
                    if (Hits >= hitsNeeded)
                    {
                        speed *= tuning.SpeedUpMultiplier;
                    }
                }

                return speed;
            }
        }

        /// <summary>Moves the indicator on by this many seconds. Does nothing once the game is over.</summary>
        public void Tick(float deltaSeconds)
        {
            if (IsFinished || !float.IsFinite(deltaSeconds) || deltaSeconds <= 0f)
            {
                return;
            }

            IndicatorAngle = WrapAngle(IndicatorAngle + Direction * CurrentSpeed * deltaSeconds);
        }

        /// <summary>Judges a press of the action key at the indicator's current position.</summary>
        public WellSpinPress Press()
        {
            if (IsFinished)
            {
                return WellSpinPress.Ignored;
            }

            if (!IsIndicatorInTarget)
            {
                Misses++;
                return WellSpinPress.Miss;
            }

            Hits++;
            Direction = -Direction;
            MoveTarget();
            return WellSpinPress.Hit;
        }

        /// <summary>Puts the target somewhere random, keeping its nearest edge at least the minimum gap from the indicator.</summary>
        private void MoveTarget()
        {
            float nearest = tuning.TargetWidth / 2f + tuning.MinTargetGap;
            float offset = nearest + (float)random.NextDouble() * (FullCircle - 2f * nearest);
            TargetCenter = WrapAngle(IndicatorAngle + offset);
        }

        private static float WrapAngle(float angle)
        {
            float wrapped = angle - MathF.Floor(angle / FullCircle) * FullCircle;

            // Rounding can land exactly on 360 for tiny negative angles.
            return wrapped >= FullCircle ? 0f : wrapped;
        }

        /// <summary>The shortest way around the ring between two angles, 0 to 180. Handles crossing the top of the ring.</summary>
        private static float DistanceBetween(float a, float b)
        {
            float difference = MathF.Abs(a - b);
            return difference > FullCircle / 2f ? FullCircle - difference : difference;
        }
    }
}
