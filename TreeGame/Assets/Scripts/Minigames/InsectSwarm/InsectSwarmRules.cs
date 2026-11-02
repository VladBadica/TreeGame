using System;
using System.Collections.Generic;
using System.Numerics;

namespace TreeGame.Minigames.InsectSwarm
{
    /// <summary>Where one insect is in its short life on the field.</summary>
    public enum SwarmInsectState
    {
        /// <summary>Still inside its crack; it can't be seen or clicked yet.</summary>
        Waiting,

        /// <summary>Crawling toward the patch; it can be clicked.</summary>
        Crawling,

        /// <summary>The player stopped it.</summary>
        Stopped,

        /// <summary>It reached the patch.</summary>
        GotThrough,
    }

    /// <summary>What a click did.</summary>
    public enum SwarmClick
    {
        /// <summary>A crawling insect was close enough and was stopped.</summary>
        Stopped,

        /// <summary>No crawling insect was close enough. This costs nothing but the moment it took.</summary>
        Miss,

        /// <summary>The game was already over, so the click counted for nothing.</summary>
        Ignored,
    }

    /// <summary>One insect in a game. Only the rules change it.</summary>
    public sealed class SwarmInsect
    {
        internal SwarmInsect(Vector2 crack, Vector2 heading, float arrivalTime)
        {
            Position = crack;
            Heading = heading;
            ArrivalTime = arrivalTime;
        }

        public Vector2 Position { get; internal set; }

        /// <summary>The direction it crawls in, as a length of 1.</summary>
        public Vector2 Heading { get; }

        public SwarmInsectState State { get; internal set; }

        internal float ArrivalTime { get; }
    }

    /// <summary>
    /// The rules of Insect Swarm: insects come out of cracks in the bark one after another and crawl straight toward a
    /// healthy patch, and the player clicks them before they get there. A game ends when every insect has been dealt
    /// with or time runs out. Plain C# with no Unity dependency, so it can be tested on its own and any visuals can sit
    /// on top. It knows nothing about why the insects are there or what the score is worth.
    /// </summary>
    public sealed class InsectSwarmRules
    {
        private readonly InsectSwarmTuning tuning;
        private readonly SwarmInsect[] insects;
        private float secondsPlayed;

        /// <param name="tuning">The numbers to play by.</param>
        /// <param name="swarmSize">How many insects come in this game, at least 1.</param>
        /// <param name="random">Which cracks insects come from; pass a fixed seed for a repeatable game.</param>
        public InsectSwarmRules(InsectSwarmTuning tuning, int swarmSize, Random random)
        {
            this.tuning = tuning ?? throw new ArgumentNullException(nameof(tuning));
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            tuning.Validate();
            if (swarmSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(swarmSize), swarmSize, "A swarm needs at least one insect.");
            }

            TimeLeft = tuning.TimeLimitSeconds;

            insects = new SwarmInsect[swarmSize];
            float secondsBetweenArrivals = swarmSize > 1 ? tuning.ArrivalSeconds / (swarmSize - 1) : 0f;
            for (int i = 0; i < swarmSize; i++)
            {
                Vector2 crack = tuning.Cracks[random.Next(tuning.Cracks.Length)];
                Vector2 heading = Vector2.Normalize(tuning.PatchCenter - crack);
                insects[i] = new SwarmInsect(crack, heading, i * secondsBetweenArrivals);
            }
        }

        /// <summary>Every insect of this game, including those that haven't appeared yet or are already dealt with.</summary>
        public IReadOnlyList<SwarmInsect> Insects => insects;

        public int SwarmSize => insects.Length;

        public float FieldWidth => tuning.FieldWidth;

        public float FieldHeight => tuning.FieldHeight;

        public Vector2 PatchCenter => tuning.PatchCenter;

        public float PatchRadius => tuning.PatchRadius;

        public IReadOnlyList<Vector2> Cracks => tuning.Cracks;

        public int Stopped { get; private set; }

        public int GotThrough { get; private set; }

        /// <summary>Seconds of play remaining, down to 0.</summary>
        public float TimeLeft { get; private set; }

        public float TimeLimit => tuning.TimeLimitSeconds;

        public bool IsOutOfTime => TimeLeft <= 0f;

        public bool IsFinished => Stopped + GotThrough == insects.Length || IsOutOfTime;

        /// <summary>The share of the swarm that was stopped, from 0 to 1. Insects still around at the end don't count.</summary>
        public float Score => (float)Stopped / insects.Length;

        /// <summary>Moves the clock and the insects on by this many seconds. Does nothing once the game is over.</summary>
        public void Tick(float deltaSeconds)
        {
            if (IsFinished || !float.IsFinite(deltaSeconds) || deltaSeconds <= 0f)
            {
                return;
            }

            // Never past the time limit, so insects can't get through on time that wasn't left.
            float seconds = MathF.Min(deltaSeconds, TimeLeft);
            TimeLeft -= seconds;
            secondsPlayed += seconds;

            foreach (SwarmInsect insect in insects)
            {
                if (insect.State == SwarmInsectState.Crawling)
                {
                    Crawl(insect, seconds);
                }
                else if (insect.State == SwarmInsectState.Waiting && secondsPlayed >= insect.ArrivalTime)
                {
                    // Only the time since it arrived, so a long step moves it as far as many short ones would.
                    insect.State = SwarmInsectState.Crawling;
                    Crawl(insect, secondsPlayed - insect.ArrivalTime);
                }
            }
        }

        /// <summary>Judges a click at a position on the field. If insects overlap, it stops only the nearest one.</summary>
        public SwarmClick Click(Vector2 position)
        {
            if (IsFinished)
            {
                return SwarmClick.Ignored;
            }

            SwarmInsect nearest = null;
            float nearestDistance = tuning.ClickRadius;
            foreach (SwarmInsect insect in insects)
            {
                if (insect.State != SwarmInsectState.Crawling)
                {
                    continue;
                }

                float distance = Vector2.Distance(position, insect.Position);
                if (distance <= nearestDistance)
                {
                    nearest = insect;
                    nearestDistance = distance;
                }
            }

            if (nearest == null)
            {
                return SwarmClick.Miss;
            }

            nearest.State = SwarmInsectState.Stopped;
            Stopped++;
            return SwarmClick.Stopped;
        }

        private void Crawl(SwarmInsect insect, float seconds)
        {
            float distanceLeft = Vector2.Distance(insect.Position, tuning.PatchCenter) - tuning.PatchRadius;
            float distance = tuning.CrawlSpeed * seconds;

            if (distance >= distanceLeft)
            {
                insect.Position = tuning.PatchCenter - insect.Heading * tuning.PatchRadius;
                insect.State = SwarmInsectState.GotThrough;
                GotThrough++;
            }
            else
            {
                insect.Position += insect.Heading * distance;
            }
        }
    }
}
