using System;
using System.Collections.Generic;
using TreeGame.Trees;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace TreeGame.DevTools
{
    /// <summary>
    /// Checks the meshes built for the tree's wood without a scene or Play Mode. The builder has no Unity dependency,
    /// so this also compiles and runs outside Unity; only the menu item is Editor-specific.
    /// </summary>
    public static class TubeMeshCheck
    {
        private const float Tolerance = 0.001f;

        /// <summary>A spread of shapes, from a plain straight tube to coarse, flared and strongly curved ones.</summary>
        private static readonly TubeShape[] Shapes =
        {
            new TubeShape(),
            new TubeShape { Sides = 3, Rings = 1, TipRadiusRatio = 1f },
            new TubeShape { Sides = 12, Rings = 8, TipRadiusRatio = 0.55f, Flare = 0.5f, TextureRepeatsAlong = 2.5f },
            new TubeShape { Sides = 5, Rings = 4, TipRadiusRatio = 0.2f, Bend = 15f },
            // As sharp as a tube this thick can bend without folding over: its arc is only a little wider than it is.
            new TubeShape { Sides = 7, Rings = 6, TipRadiusRatio = 0.35f, Bend = 90f },
            new TubeShape { Sides = 7, Rings = 6, TipRadiusRatio = 0.35f, Bend = 50f, Flare = 0.5f },
            new TubeShape { Sides = 6, Rings = 3, Bend = 0.00001f },

            // A branch and a twig at their real sizes, long and thin.
            new TubeShape { Sides = 7, Rings = 4, TipRadiusRatio = 0.35f, Bend = 15f, Length = 1.7f, Thickness = 0.12f, TextureRepeatsAlong = 4.5f },
            new TubeShape { Sides = 5, Rings = 4, TipRadiusRatio = 0.35f, Bend = 10f, Length = 0.3f, Thickness = 0.02f, TextureRepeatsAlong = 4.8f },
        };

        /// <summary>Runs every check. Returns a description of each failure; empty means all passed.</summary>
        public static List<string> Run()
        {
            var failures = new List<string>();
            foreach (TubeShape shape in Shapes)
            {
                string name = $"({shape.Sides} sides, {shape.Rings} rings, bend {shape.Bend}, flare {shape.Flare}, {shape.Length} by {shape.Thickness})";
                MeshData mesh = TubeMesh.Build(shape);
                CheckCounts(failures, mesh, shape, name);
                CheckValuesAreUsable(failures, mesh, name);
                CheckFacesPointOutward(failures, mesh, shape, name);
                CheckRadiusAndSpine(failures, mesh, shape, name);
                CheckTextureCoordinates(failures, mesh, shape, name);
            }

            CheckSpine(failures);
            CheckProfile(failures);
            CheckBadShapesAreRejected(failures);
            return failures;
        }

#if UNITY_EDITOR
        [MenuItem("Tree Game/Run Tube Mesh Check")]
        private static void RunFromMenu()
        {
            List<string> failures = Run();
            if (failures.Count == 0)
            {
                Debug.Log("Tube mesh check PASSED.");
            }
            else
            {
                Debug.LogError("Tube mesh check FAILED:\n- " + string.Join("\n- ", failures));
            }
        }
#endif

        private static void Expect(List<string> failures, bool condition, string failure)
        {
            if (!condition)
            {
                failures.Add(failure);
            }
        }

        private static void CheckCounts(List<string> failures, MeshData mesh, TubeShape shape, string name)
        {
            int expectedVertices = (shape.Rings + 1) * (shape.Sides + 1) + 1;
            int expectedIndices = shape.Rings * shape.Sides * 6 + shape.Sides * 3;

            Expect(failures, mesh.Vertices.Length == expectedVertices, $"{name}: {mesh.Vertices.Length} vertices, expected {expectedVertices}");
            Expect(failures, mesh.Normals.Length == mesh.Vertices.Length && mesh.Uvs.Length == mesh.Vertices.Length, $"{name}: normals or texture coordinates don't match the vertices");
            Expect(failures, mesh.Triangles.Length == expectedIndices, $"{name}: {mesh.Triangles.Length} triangle indices, expected {expectedIndices}");
        }

        private static void CheckValuesAreUsable(List<string> failures, MeshData mesh, string name)
        {
            foreach (int index in mesh.Triangles)
            {
                if (index < 0 || index >= mesh.Vertices.Length)
                {
                    failures.Add($"{name}: a triangle uses vertex {index}, which doesn't exist");
                    return;
                }
            }

            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                if (!IsFinite(mesh.Vertices[i]) || !IsFinite(mesh.Normals[i]) || !float.IsFinite(mesh.Uvs[i].X) || !float.IsFinite(mesh.Uvs[i].Y))
                {
                    failures.Add($"{name}: vertex {i} has a value that isn't a number");
                    return;
                }

                if (MathF.Abs(mesh.Normals[i].Length() - 1f) > Tolerance)
                {
                    failures.Add($"{name}: vertex {i}'s normal has length {mesh.Normals[i].Length()}, not 1");
                    return;
                }
            }

            for (int i = 0; i < mesh.Triangles.Length; i += 3)
            {
                if (Area(mesh, i) < 1e-7f)
                {
                    failures.Add($"{name}: triangle {i / 3} has no area");
                    return;
                }
            }
        }

        /// <summary>
        /// Every triangle must face the way its vertices' normals point: out of the tube along its sides, and along the
        /// tube at its tip. A triangle facing inward would be invisible from outside.
        /// </summary>
        private static void CheckFacesPointOutward(List<string> failures, MeshData mesh, TubeShape shape, string name)
        {
            int capIndex = mesh.Vertices.Length - 1;
            for (int i = 0; i < mesh.Triangles.Length; i += 3)
            {
                int a = mesh.Triangles[i];
                int b = mesh.Triangles[i + 1];
                int c = mesh.Triangles[i + 2];
                Vector3 facing = Vector3.Cross(mesh.Vertices[b] - mesh.Vertices[a], mesh.Vertices[c] - mesh.Vertices[a]);

                bool isCap = a == capIndex || b == capIndex || c == capIndex;
                Vector3 expected = isCap
                    ? TubeMesh.SpineDirection(1f, shape.Bend)
                    : mesh.Normals[a] + mesh.Normals[b] + mesh.Normals[c];

                if (Vector3.Dot(facing, expected) <= 0f)
                {
                    failures.Add($"{name}: {(isCap ? "a tip" : "a side")} triangle faces inward");
                    return;
                }
            }
        }

        private static void CheckRadiusAndSpine(List<string> failures, MeshData mesh, TubeShape shape, string name)
        {
            int ringSize = shape.Sides + 1;
            for (int ring = 0; ring <= shape.Rings; ring++)
            {
                float t = (float)ring / shape.Rings;
                Vector3 center = TubeMesh.SpinePoint(t, shape.Bend) * shape.Length;
                Vector3 direction = TubeMesh.SpineDirection(t, shape.Bend);
                float radius = TubeMesh.Radius(t, shape) * shape.Thickness;

                for (int side = 0; side < ringSize; side++)
                {
                    Vector3 offset = mesh.Vertices[ring * ringSize + side] - center;
                    if (MathF.Abs(offset.Length() - radius) > Tolerance)
                    {
                        failures.Add($"{name}: ring {ring} has a vertex {offset.Length()} from the spine, expected {radius}");
                        return;
                    }

                    // Each ring lies flat across the spine, so the tube keeps its thickness through curves.
                    if (MathF.Abs(Vector3.Dot(offset, direction)) > Tolerance)
                    {
                        failures.Add($"{name}: ring {ring} isn't square to the spine");
                        return;
                    }
                }
            }

            Expect(failures, Vector3.Distance(mesh.Vertices[mesh.Vertices.Length - 1], TubeMesh.SpinePoint(1f, shape.Bend) * shape.Length) < Tolerance,
                $"{name}: the tip doesn't close at the end of the spine");
        }

        private static void CheckTextureCoordinates(List<string> failures, MeshData mesh, TubeShape shape, string name)
        {
            int ringSize = shape.Sides + 1;
            for (int ring = 0; ring <= shape.Rings; ring++)
            {
                int first = ring * ringSize;
                int last = first + shape.Sides;

                // The first and last vertices of a ring are the same point, so the texture wraps round without a jump.
                if (Vector3.Distance(mesh.Vertices[first], mesh.Vertices[last]) > Tolerance || mesh.Uvs[first].X != 0f || MathF.Abs(mesh.Uvs[last].X - 1f) > Tolerance)
                {
                    failures.Add($"{name}: ring {ring} doesn't wrap the texture around from 0 to 1");
                    return;
                }

                float expectedV = (float)ring / shape.Rings * shape.TextureRepeatsAlong;
                if (MathF.Abs(mesh.Uvs[first].Y - expectedV) > Tolerance)
                {
                    failures.Add($"{name}: ring {ring} has texture coordinate {mesh.Uvs[first].Y} along, expected {expectedV}");
                    return;
                }
            }
        }

        private static void CheckSpine(List<string> failures)
        {
            // Straight: up the Y axis, exactly 1 long.
            Expect(failures, TubeMesh.SpinePoint(0f, 0f) == Vector3.Zero, "a straight spine doesn't start at the origin");
            Expect(failures, Vector3.Distance(TubeMesh.SpinePoint(1f, 0f), new Vector3(0f, 1f, 0f)) < Tolerance, "a straight spine doesn't end 1 up");
            Expect(failures, TubeMesh.SpineDirection(0.5f, 0f) == new Vector3(0f, 1f, 0f), "a straight spine doesn't point up");

            // The spine itself has no thickness, so it can curve as far as a half circle.
            foreach (float bend in new[] { 5f, 30f, 90f, 180f })
            {
                // Its length stays 1 however much it curves, measured by walking along it.
                float length = 0f;
                const int steps = 1000;
                Vector3 previous = TubeMesh.SpinePoint(0f, bend);
                bool turnsTowardMinusX = true;
                for (int i = 1; i <= steps; i++)
                {
                    Vector3 point = TubeMesh.SpinePoint((float)i / steps, bend);
                    length += Vector3.Distance(previous, point);
                    turnsTowardMinusX &= point.X <= previous.X + Tolerance;
                    previous = point;
                }

                Expect(failures, MathF.Abs(length - 1f) < Tolerance, $"a spine bent {bend} degrees is {length} long, expected 1");
                Expect(failures, turnsTowardMinusX, $"a spine bent {bend} degrees doesn't curve toward -X");
                Expect(failures, TubeMesh.SpinePoint(0f, bend) == Vector3.Zero, $"a spine bent {bend} degrees doesn't start at the origin");

                // It ends pointing the whole bend away from where it started.
                Vector3 end = TubeMesh.SpineDirection(1f, bend);
                float endAngle = MathF.Acos(Math.Clamp(end.Y, -1f, 1f)) * 180f / MathF.PI;
                Expect(failures, MathF.Abs(endAngle - bend) < 0.01f, $"a spine bent {bend} degrees ends turned {endAngle}");
                Expect(failures, MathF.Abs(end.Length() - 1f) < Tolerance, $"a spine bent {bend} degrees has a direction of length {end.Length()}");

                // Its direction really is the way it runs, compared with a small step along it.
                Vector3 step = TubeMesh.SpinePoint(0.501f, bend) - TubeMesh.SpinePoint(0.5f, bend);
                Expect(failures, Vector3.Dot(Vector3.Normalize(step), TubeMesh.SpineDirection(0.5f, bend)) > 0.9999f, $"a spine bent {bend} degrees runs a different way than its direction says");
            }
        }

        private static void CheckProfile(List<string> failures)
        {
            var plain = new TubeShape { TipRadiusRatio = 0.4f };
            Expect(failures, MathF.Abs(TubeMesh.Radius(0f, plain) - TubeMesh.BaseRadius) < Tolerance, "an unflared tube's base isn't the standard radius");
            Expect(failures, MathF.Abs(TubeMesh.Radius(1f, plain) - TubeMesh.BaseRadius * 0.4f) < Tolerance, "the tip isn't the tip share of the base");

            var flared = new TubeShape { TipRadiusRatio = 0.4f, Flare = 0.5f };
            Expect(failures, MathF.Abs(TubeMesh.Radius(0f, flared) - TubeMesh.BaseRadius * 1.5f) < Tolerance, "a flare of 0.5 should make the base half again as wide");
            Expect(failures, MathF.Abs(TubeMesh.Radius(0.5f, flared) - TubeMesh.Radius(0.5f, plain)) < 0.01f, "the flare should have faded by halfway up");

            // Never gets wider going up, so nothing bulges out of the wood above it.
            foreach (TubeShape shape in Shapes)
            {
                float last = float.MaxValue;
                for (int i = 0; i <= 100; i++)
                {
                    float radius = TubeMesh.Radius(i / 100f, shape);
                    if (radius > last + 1e-6f || radius <= 0f)
                    {
                        failures.Add($"a tube's radius grows or vanishes going up, at {i}%");
                        return;
                    }

                    last = radius;
                }
            }
        }

        private static void CheckBadShapesAreRejected(List<string> failures)
        {
            void ExpectRejected(string description, TubeShape shape)
            {
                try
                {
                    TubeMesh.Build(shape);
                    failures.Add($"a shape with {description} should be rejected");
                }
                catch (ArgumentException)
                {
                    // Expected. ArgumentNullException is a kind of this.
                }
            }

            ExpectRejected("no shape at all", null);
            ExpectRejected("2 sides", new TubeShape { Sides = 2 });
            ExpectRejected("no rings", new TubeShape { Rings = 0 });
            ExpectRejected("a tip of nothing", new TubeShape { TipRadiusRatio = 0f });
            ExpectRejected("a tip wider than the base", new TubeShape { TipRadiusRatio = 1.5f });
            ExpectRejected("a NaN tip", new TubeShape { TipRadiusRatio = float.NaN });
            ExpectRejected("a negative flare", new TubeShape { Flare = -1f });
            ExpectRejected("a backward bend", new TubeShape { Bend = -10f });
            ExpectRejected("a NaN bend", new TubeShape { Bend = float.NaN });
            ExpectRejected("no texture repeats", new TubeShape { TextureRepeatsAlong = 0f });
            ExpectRejected("a bend too sharp for its thickness", new TubeShape { Bend = 90f, Flare = 1f });
            ExpectRejected("a half-circle bend", new TubeShape { Bend = 180f });
            ExpectRejected("no length", new TubeShape { Length = 0f });
            ExpectRejected("no thickness", new TubeShape { Thickness = 0f });
            ExpectRejected("an infinite length", new TubeShape { Length = float.PositiveInfinity });
            ExpectRejected("a NaN thickness", new TubeShape { Thickness = float.NaN });

            // How sharp a bend is allowed depends on how thick the tube is for its length.
            ExpectRejected("a short, thick tube bent sharply", new TubeShape { Bend = 60f, Length = 0.5f, Thickness = 1f });
            try
            {
                // Thin enough that even this sharp a bend can't fold its inside over.
                TubeMesh.Build(new TubeShape { Bend = 120f, Length = 1f, Thickness = 0.1f });
            }
            catch (ArgumentException)
            {
                failures.Add("a thin tube bent 120 degrees should be allowed");
            }
        }

        private static bool IsFinite(Vector3 value)
        {
            return float.IsFinite(value.X) && float.IsFinite(value.Y) && float.IsFinite(value.Z);
        }

        private static float Area(MeshData mesh, int firstIndex)
        {
            Vector3 a = mesh.Vertices[mesh.Triangles[firstIndex]];
            Vector3 b = mesh.Vertices[mesh.Triangles[firstIndex + 1]];
            Vector3 c = mesh.Vertices[mesh.Triangles[firstIndex + 2]];
            return Vector3.Cross(b - a, c - a).Length() / 2f;
        }
    }
}
