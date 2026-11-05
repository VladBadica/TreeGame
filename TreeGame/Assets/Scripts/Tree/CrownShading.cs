using System;
using System.Numerics;

namespace TreeGame.Trees
{
    /// <summary>
    /// How light a leaf cluster looks from where it sits in the crown. A real crown is lit from above and from outside:
    /// leaves on top and around the edge are bright, leaves inside and underneath are shaded by the rest. Plain C# with
    /// no Unity dependency, so it can be checked on its own.
    /// </summary>
    public static class CrownShading
    {
        /// <summary>
        /// How much height counts toward the light, against how far out the cluster is. Light comes from above, so
        /// height counts for more.
        /// </summary>
        public const float HeightWeight = 0.6f;

        private const float Tiny = 0.0001f;

        /// <summary>
        /// How light a cluster is, from 0 (darkest) to 1 (lightest). The crown is the box from crownMin to crownMax
        /// around all the clusters. randomValue (0 to 1) gives each cluster some variety of its own, and positionShare
        /// (0 to 1) is how much where it sits counts against that.
        /// </summary>
        public static float Light(Vector3 position, Vector3 crownMin, Vector3 crownMax, float randomValue, float positionShare)
        {
            if (!IsFinite(position) || !IsFinite(crownMin) || !IsFinite(crownMax))
            {
                return 0.5f;
            }

            float random = float.IsFinite(randomValue) ? Math.Clamp(randomValue, 0f, 1f) : 0.5f;
            float share = float.IsFinite(positionShare) ? Math.Clamp(positionShare, 0f, 1f) : 0f;

            float height = ShareBetween(position.Y, crownMin.Y, crownMax.Y);

            // How far from the crown's middle toward its edge, across and in depth, as an ellipse.
            Vector3 middle = (crownMin + crownMax) * 0.5f;
            float across = OffsetFromMiddle(position.X, middle.X, (crownMax.X - crownMin.X) * 0.5f);
            float depth = OffsetFromMiddle(position.Z, middle.Z, (crownMax.Z - crownMin.Z) * 0.5f);
            float outward = Math.Clamp(MathF.Sqrt(across * across + depth * depth), 0f, 1f);

            float placed = HeightWeight * height + (1f - HeightWeight) * outward;
            return random + (placed - random) * share;
        }

        private static float ShareBetween(float value, float min, float max)
        {
            return max - min > Tiny ? Math.Clamp((value - min) / (max - min), 0f, 1f) : 0.5f;
        }

        private static float OffsetFromMiddle(float value, float middle, float halfSize)
        {
            return halfSize > Tiny ? (value - middle) / halfSize : 0f;
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
        }
    }
}
