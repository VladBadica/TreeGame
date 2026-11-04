using System;
using System.Numerics;

namespace TreeGame.Trees
{
    /// <summary>The shape of one piece of wood: how it tapers, flares and curves, and how finely it is built.</summary>
    public sealed class TubeShape
    {
        /// <summary>Corners around the tube. More is rounder but costs more.</summary>
        public int Sides { get; set; } = 8;

        /// <summary>Sections along the tube. More makes a smoother curve.</summary>
        public int Rings { get; set; } = 5;

        /// <summary>The tip's radius as a share of the base's.</summary>
        public float TipRadiusRatio { get; set; } = 0.35f;

        /// <summary>How much wider the very base is, as a share of the radius, spreading out like roots. 0 = no flare.</summary>
        public float Flare { get; set; }

        /// <summary>Degrees the tube curves over its length, toward its own -X side.</summary>
        public float Bend { get; set; }

        /// <summary>How many times the texture repeats along the tube, so bark isn't stretched on long pieces.</summary>
        public float TextureRepeatsAlong { get; set; } = 1f;

        /// <summary>The length of the tube along its spine. The curve is built at this length, so it keeps its shape.</summary>
        public float Length { get; set; } = 1f;

        /// <summary>The tube's thickness (its diameter) at the base, before any flare.</summary>
        public float Thickness { get; set; } = 1f;

        /// <summary>Rejects shapes no tube could be built with, so mistakes show up at once rather than as odd meshes.</summary>
        public void Validate()
        {
            if (Sides < 3 || Rings < 1)
            {
                throw new ArgumentException("A tube needs at least 3 sides and 1 ring.");
            }

            if (!(TipRadiusRatio > 0f && TipRadiusRatio <= 1f))
            {
                throw new ArgumentException("The tip must be thinner than or as thick as the base, and not nothing.");
            }

            if (!(Flare >= 0f && Flare <= 5f) || !(Bend >= 0f && Bend <= 180f) || !(TextureRepeatsAlong > 0f && float.IsFinite(TextureRepeatsAlong)))
            {
                throw new ArgumentException("Flare, bend and texture repeats must be sensible, non-negative numbers.");
            }

            if (!(Length > 0f && float.IsFinite(Length)) || !(Thickness > 0f && float.IsFinite(Thickness)))
            {
                throw new ArgumentException("A tube needs a positive length and thickness.");
            }

            // Bent tighter than it is thick, the inside of the curve would fold through itself.
            float widestRadius = TubeMesh.BaseRadius * (1f + Flare) * Thickness;
            if (Bend * MathF.PI / 180f * widestRadius >= Length)
            {
                throw new ArgumentException("The tube is bent too sharply for its thickness; the inside of the curve would fold over.");
            }
        }
    }

    /// <summary>
    /// Builds a tapering, optionally curved tube with a closed tip, for trunks and branches, at the shape's length and
    /// thickness. Its spine starts at the origin and runs up +Y, curving toward -X. A straight tube built at length and
    /// thickness 1 can be scaled to any size; a curved one is best built at its full size and scaled down while it
    /// grows, as scaling its thickness alone would also flatten its curve. Plain C# with no Unity dependency, so it can
    /// be checked on its own.
    /// </summary>
    public static class TubeMesh
    {
        /// <summary>The radius at the base before any flare, so a tube scaled by its thickness has that thickness.</summary>
        public const float BaseRadius = 0.5f;

        // How quickly the flare fades going up: high, so only the very base spreads, like the start of roots.
        private const float FlareSharpness = 8f;
        private const float StraightBelow = 0.0001f;

        /// <summary>A point on the spine of a tube of length 1, a share t (0 to 1) of the way along it.</summary>
        public static Vector3 SpinePoint(float t, float bend)
        {
            float bendRadians = bend * MathF.PI / 180f;
            if (bendRadians < StraightBelow)
            {
                return new Vector3(0f, t, 0f);
            }

            // An arc of length 1 that turns through the whole bend: its radius makes the arc exactly that long.
            float arcRadius = 1f / bendRadians;
            float angle = bendRadians * t;
            return new Vector3(-arcRadius * (1f - MathF.Cos(angle)), arcRadius * MathF.Sin(angle), 0f);
        }

        /// <summary>The way the spine runs, as a length of 1, a share t of the way along it.</summary>
        public static Vector3 SpineDirection(float t, float bend)
        {
            float angle = bend * MathF.PI / 180f * t;
            return new Vector3(-MathF.Sin(angle), MathF.Cos(angle), 0f);
        }

        /// <summary>The radius a share t of the way along a tube of thickness 1 with this shape's taper and flare.</summary>
        public static float Radius(float t, TubeShape shape)
        {
            float taper = 1f + (shape.TipRadiusRatio - 1f) * t;
            float flare = 1f + shape.Flare * MathF.Pow(1f - t, FlareSharpness);
            return BaseRadius * taper * flare;
        }

        public static MeshData Build(TubeShape shape)
        {
            if (shape == null)
            {
                throw new ArgumentNullException(nameof(shape));
            }

            shape.Validate();

            // Each ring repeats its first vertex at the end, so the texture can wrap all the way round without a jump.
            int ringSize = shape.Sides + 1;
            int ringCount = shape.Rings + 1;
            int capIndex = ringCount * ringSize;

            var vertices = new Vector3[capIndex + 1];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];

            for (int ring = 0; ring < ringCount; ring++)
            {
                float t = (float)ring / shape.Rings;
                Vector3 center = SpinePoint(t, shape.Bend) * shape.Length;
                Vector3 direction = SpineDirection(t, shape.Bend);

                // Across the spine within its curve, and straight out of it; together they span each ring.
                var across = new Vector3(direction.Y, -direction.X, 0f);
                var outOf = new Vector3(0f, 0f, 1f);
                float radius = Radius(t, shape) * shape.Thickness;

                for (int side = 0; side < ringSize; side++)
                {
                    float angle = 2f * MathF.PI * side / shape.Sides;
                    Vector3 outward = across * MathF.Cos(angle) + outOf * MathF.Sin(angle);

                    int index = ring * ringSize + side;
                    vertices[index] = center + outward * radius;
                    normals[index] = outward;
                    uvs[index] = new Vector2((float)side / shape.Sides, t * shape.TextureRepeatsAlong);
                }
            }

            vertices[capIndex] = SpinePoint(1f, shape.Bend) * shape.Length;
            normals[capIndex] = SpineDirection(1f, shape.Bend);
            uvs[capIndex] = new Vector2(0.5f, shape.TextureRepeatsAlong);

            var triangles = new int[shape.Rings * shape.Sides * 6 + shape.Sides * 3];
            int next = 0;
            for (int ring = 0; ring < shape.Rings; ring++)
            {
                for (int side = 0; side < shape.Sides; side++)
                {
                    int a = ring * ringSize + side;
                    int b = a + 1;
                    int c = a + ringSize;
                    int d = c + 1;

                    // Both triangles of the quad between this ring and the next, wound to face outward.
                    triangles[next++] = a;
                    triangles[next++] = c;
                    triangles[next++] = b;
                    triangles[next++] = b;
                    triangles[next++] = c;
                    triangles[next++] = d;
                }
            }

            // The tip closes in a cone of triangles from the last ring to the spine's end, facing along the tube.
            int lastRing = shape.Rings * ringSize;
            for (int side = 0; side < shape.Sides; side++)
            {
                triangles[next++] = lastRing + side;
                triangles[next++] = capIndex;
                triangles[next++] = lastRing + side + 1;
            }

            return new MeshData(vertices, normals, uvs, triangles);
        }
    }
}
