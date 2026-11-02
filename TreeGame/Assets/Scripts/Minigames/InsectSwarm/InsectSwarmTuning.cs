using System;
using System.Numerics;

namespace TreeGame.Minigames.InsectSwarm
{
    /// <summary>
    /// The numbers that define how a game of Insect Swarm plays. Positions and distances are in field units: the field
    /// runs from 0,0 at its bottom left to <see cref="FieldWidth"/>, <see cref="FieldHeight"/> at its top right.
    /// </summary>
    public sealed class InsectSwarmTuning
    {
        public float FieldWidth { get; set; } = 16f;

        public float FieldHeight { get; set; } = 9f;

        /// <summary>A game also ends once this many seconds of play have passed.</summary>
        public float TimeLimitSeconds { get; set; } = 30f;

        /// <summary>
        /// The first insect appears at once and the last after this many seconds, with the others evenly spaced between.
        /// A bigger swarm therefore arrives faster rather than taking longer.
        /// </summary>
        public float ArrivalSeconds { get; set; } = 20f;

        /// <summary>How fast insects crawl, in field units per second.</summary>
        public float CrawlSpeed { get; set; } = 1.8f;

        /// <summary>The middle of the healthy patch of bark that insects crawl toward.</summary>
        public Vector2 PatchCenter { get; set; } = new Vector2(8f, 4.5f);

        /// <summary>An insect has got through once it comes this close to the patch's center.</summary>
        public float PatchRadius { get; set; } = 1f;

        /// <summary>A click stops an insect this close to it. Generous, because they are moving and this should feel cozy.</summary>
        public float ClickRadius { get; set; } = 0.6f;

        /// <summary>The cracks in the bark that insects crawl out of.</summary>
        public Vector2[] Cracks { get; set; } =
        {
            new Vector2(1.5f, 1.5f),
            new Vector2(1f, 4.5f),
            new Vector2(1.5f, 7.5f),
            new Vector2(3.5f, 7.8f),
            new Vector2(12.5f, 7.8f),
            new Vector2(14.5f, 7.5f),
            new Vector2(15f, 4.5f),
            new Vector2(14.5f, 1.5f),
            new Vector2(12.5f, 1.2f),
            new Vector2(3.5f, 1.2f),
        };

        /// <summary>Rejects numbers no game could be played with, so mistakes show up at once rather than as odd behavior.</summary>
        public void Validate()
        {
            if (!IsPositive(FieldWidth) || !IsPositive(FieldHeight))
            {
                throw new ArgumentException("The field's size must be positive.");
            }

            if (!IsPositive(TimeLimitSeconds))
            {
                throw new ArgumentException("The time limit must be positive.");
            }

            if (!(ArrivalSeconds >= 0f) || !float.IsFinite(ArrivalSeconds))
            {
                throw new ArgumentException("The arrival time can't be negative.");
            }

            if (!IsPositive(CrawlSpeed) || !IsPositive(PatchRadius) || !IsPositive(ClickRadius))
            {
                throw new ArgumentException("The crawl speed, patch radius and click radius must be positive.");
            }

            if (!IsInField(PatchCenter))
            {
                throw new ArgumentException("The patch must be on the field.");
            }

            if (Cracks == null || Cracks.Length == 0)
            {
                throw new ArgumentException("Insects need at least one crack to come out of.");
            }

            foreach (Vector2 crack in Cracks)
            {
                // A crack inside the patch would make every insect from it get through at once.
                if (!IsInField(crack) || !(Vector2.Distance(crack, PatchCenter) > PatchRadius))
                {
                    throw new ArgumentException($"The crack at {crack} must be on the field and outside the patch.");
                }
            }
        }

        private bool IsInField(Vector2 point)
        {
            return point.X >= 0f && point.X <= FieldWidth && point.Y >= 0f && point.Y <= FieldHeight;
        }

        private static bool IsPositive(float value)
        {
            return value > 0f && float.IsFinite(value);
        }
    }
}
