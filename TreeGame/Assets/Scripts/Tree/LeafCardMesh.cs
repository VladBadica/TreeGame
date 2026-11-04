using System;
using System.Numerics;

namespace TreeGame.Trees
{
    /// <summary>
    /// Builds a cluster of leaf cards: flat squares, 1 across, each showing a sprig of leaves whose stem is at the
    /// middle of the card's bottom edge. Every card's stem starts just behind the origin, where the cluster grows from its
    /// branch, so it begins inside the wood, and the cards fan out upward (+Y) from there, like sprigs off a twig. They mostly face the camera (along -Z), as
    /// the game is seen side-on and a card seen edge-on would vanish. Drawn from both sides, so which way a card faces
    /// only matters for lighting. Plain C# with no Unity dependency, so it can be checked on its own.
    /// </summary>
    public static class LeafCardMesh
    {
        /// <summary>The most a card turns away from facing the camera, sideways or up and down.</summary>
        public const float MaxTurnFromCamera = 50f;

        /// <summary>The most a card fans away from pointing straight up from its stem.</summary>
        public const float MaxFan = 55f;

        /// <summary>No part of a cluster is farther than this from where it grows, so a cluster scaled to a size fits within that size.</summary>
        public const float MaxReach = 1.25f;

        /// <summary>
        /// How far back from the origin, along the card, its stem starts, as a share of a card. The origin is where the
        /// cluster meets its branch, so the stem starts inside the wood and always looks attached.
        /// </summary>
        public const float StemInset = 0.08f;

        /// <summary>How far a card's stem may be nudged from where it should start, so the stems don't all meet at one point.</summary>
        public const float MaxStemOffset = 0.02f;

        private const float DegreesToRadians = MathF.PI / 180f;

        public static MeshData Build(int cardCount, int seed)
        {
            if (cardCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(cardCount), cardCount, "A cluster needs at least one card.");
            }

            var random = new Random(seed);
            var vertices = new Vector3[cardCount * 4];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[cardCount * 6];

            for (int card = 0; card < cardCount; card++)
            {
                // Fanned evenly from one side to the other, each nudged a little, and each turned a little differently
                // from facing the camera.
                float share = cardCount > 1 ? (float)card / (cardCount - 1) : 0.5f;
                float fan = Lerp(-0.8f, 0.8f, share) * MaxFan + Range(random, -0.2f, 0.2f) * MaxFan;
                float yaw = Range(random, -0.7f, 0.7f) * MaxTurnFromCamera;
                float pitch = Range(random, -25f, 25f);
                Quaternion turn = Quaternion.CreateFromYawPitchRoll(yaw * DegreesToRadians, pitch * DegreesToRadians, fan * DegreesToRadians);
                var nudge = new Vector3(Range(random, -MaxStemOffset, MaxStemOffset), Range(random, -MaxStemOffset, MaxStemOffset), 0f) * 0.7f;

                int first = card * 4;
                Vector3 normal = Vector3.Transform(new Vector3(0f, 0f, -1f), turn);
                for (int corner = 0; corner < 4; corner++)
                {
                    float u = corner % 2;
                    float v = corner / 2;
                    // Measured from the middle of the bottom edge, where the sprig's stem is, set back into the wood.
                    vertices[first + corner] = nudge + Vector3.Transform(new Vector3(u - 0.5f, v - StemInset, 0f), turn);
                    normals[first + corner] = normal;
                    uvs[first + corner] = new Vector2(u, v);
                }

                // Corners run bottom left, bottom right, top left, top right; these two triangles face along the normal.
                int next = card * 6;
                triangles[next] = first;
                triangles[next + 1] = first + 2;
                triangles[next + 2] = first + 1;
                triangles[next + 3] = first + 2;
                triangles[next + 4] = first + 3;
                triangles[next + 5] = first + 1;
            }

            return new MeshData(vertices, normals, uvs, triangles);
        }

        private static float Range(Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        private static float Lerp(float from, float to, float t)
        {
            return from + (to - from) * t;
        }
    }
}
