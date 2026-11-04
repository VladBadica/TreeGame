using System;
using System.Collections.Generic;
using System.Linq;
using TreeGame.Trees;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif
using Vector3 = System.Numerics.Vector3;

namespace TreeGame.DevTools
{
    /// <summary>
    /// Checks the mesh the tree's leaf clusters are made of, without a scene or Play Mode. The builder has no Unity
    /// dependency, so this also compiles and runs outside Unity; only the menu item is Editor-specific.
    /// </summary>
    public static class LeafCardMeshCheck
    {
        private const float Tolerance = 0.001f;

        /// <summary>Runs every check. Returns a description of each failure; empty means all passed.</summary>
        public static List<string> Run()
        {
            var failures = new List<string>();
            foreach (int cards in new[] { 1, 2, 3, 6 })
            {
                for (int seed = 0; seed < 50; seed++)
                {
                    CheckCluster(failures, LeafCardMesh.Build(cards, seed), cards, seed);
                    if (failures.Count > 0)
                    {
                        return failures;
                    }
                }
            }

            CheckSameSeedSameCluster(failures);
            CheckBadInputIsRejected(failures);
            return failures;
        }

#if UNITY_EDITOR
        [MenuItem("Tree Game/Run Leaf Card Mesh Check")]
        private static void RunFromMenu()
        {
            List<string> failures = Run();
            if (failures.Count == 0)
            {
                Debug.Log("Leaf card mesh check PASSED.");
            }
            else
            {
                Debug.LogError("Leaf card mesh check FAILED:\n- " + string.Join("\n- ", failures));
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

        private static void CheckCluster(List<string> failures, MeshData mesh, int cards, int seed)
        {
            string name = $"{cards} cards, seed {seed}";
            Expect(failures, mesh.Vertices.Length == cards * 4 && mesh.Normals.Length == cards * 4 && mesh.Uvs.Length == cards * 4,
                $"{name}: {mesh.Vertices.Length} vertices, expected 4 per card");
            Expect(failures, mesh.Triangles.Length == cards * 6, $"{name}: {mesh.Triangles.Length} triangle indices, expected 6 per card");
            if (failures.Count > 0)
            {
                return;
            }

            float lowestFacing = -MathF.Cos(LeafCardMesh.MaxTurnFromCamera * MathF.PI / 180f);
            for (int card = 0; card < cards; card++)
            {
                int first = card * 4;
                Vector3 normal = mesh.Normals[first];

                // A card is a flat square, 1 across, the whole texture spread over it the right way up.
                Vector3 across = mesh.Vertices[first + 1] - mesh.Vertices[first];
                Vector3 up = mesh.Vertices[first + 2] - mesh.Vertices[first];
                Expect(failures, MathF.Abs(across.Length() - 1f) < Tolerance && MathF.Abs(up.Length() - 1f) < Tolerance && MathF.Abs(Vector3.Dot(across, up)) < Tolerance,
                    $"{name}: card {card} isn't a square 1 across");
                Expect(failures, Vector3.Distance(mesh.Vertices[first + 3], mesh.Vertices[first + 1] + up) < Tolerance, $"{name}: card {card} isn't flat");
                Expect(failures, mesh.Uvs[first] == new System.Numerics.Vector2(0f, 0f) && mesh.Uvs[first + 3] == new System.Numerics.Vector2(1f, 1f),
                    $"{name}: card {card} doesn't show the whole texture");

                // Its normal is square to it, and it faces the camera enough to be seen side-on.
                Expect(failures, MathF.Abs(normal.Length() - 1f) < Tolerance && MathF.Abs(Vector3.Dot(normal, across)) < Tolerance && MathF.Abs(Vector3.Dot(normal, up)) < Tolerance,
                    $"{name}: card {card}'s normal isn't square to it");
                Expect(failures, normal.Z <= lowestFacing + Tolerance, $"{name}: card {card} turns too far from the camera, facing {normal}");

                // Its stem, at the middle of its bottom edge, starts set back into the wood from where the cluster grows,
                // so it overlaps the branch, and the card points up from it.
                Vector3 stem = (mesh.Vertices[first] + mesh.Vertices[first + 1]) / 2f;
                Vector3 setBack = -Vector3.Normalize(up) * LeafCardMesh.StemInset;
                Expect(failures, Vector3.Distance(stem, setBack) <= LeafCardMesh.MaxStemOffset + Tolerance,
                    $"{name}: card {card}'s stem starts {Vector3.Distance(stem, setBack)} from just behind where the cluster grows");
                Expect(failures, Vector3.Dot(stem, Vector3.Normalize(up)) < 0f, $"{name}: card {card}'s stem starts beyond where the cluster grows, not in the wood");
                float fromUp = MathF.Acos(Math.Clamp(Vector3.Normalize(up).Y, -1f, 1f)) * 180f / MathF.PI;
                Expect(failures, up.Y > 0f && fromUp <= LeafCardMesh.MaxFan + 25f + 1f, $"{name}: card {card} points {fromUp} degrees from up, away from its stem");

                // Both triangles face the way its normal points.
                for (int i = card * 6; i < card * 6 + 6; i += 3)
                {
                    Vector3 a = mesh.Vertices[mesh.Triangles[i]], b = mesh.Vertices[mesh.Triangles[i + 1]], c = mesh.Vertices[mesh.Triangles[i + 2]];
                    Expect(failures, Vector3.Dot(Vector3.Cross(b - a, c - a), normal) > 0f, $"{name}: a triangle of card {card} faces away from its normal");
                    Expect(failures, mesh.Triangles.Skip(i).Take(3).All(index => index >= first && index < first + 4), $"{name}: a triangle of card {card} uses another card's corners");
                }

                if (failures.Count > 0)
                {
                    return;
                }
            }

            // Within its stated reach of where it grows, so a cluster's size is what it is scaled to.
            float farthest = mesh.Vertices.Max(v => v.Length());
            Expect(failures, farthest < LeafCardMesh.MaxReach, $"{name}: a card reaches {farthest} from where the cluster grows, past its stated reach");

            // The cluster as a whole grows upward from its stems: no card reaches much below where they meet.
            float lowest = mesh.Vertices.Min(v => v.Y);
            Expect(failures, lowest > -0.5f, $"{name}: a card reaches {-lowest} below where the cluster grows");
        }

        private static void CheckSameSeedSameCluster(List<string> failures)
        {
            MeshData first = LeafCardMesh.Build(3, 7);
            MeshData second = LeafCardMesh.Build(3, 7);
            MeshData other = LeafCardMesh.Build(3, 8);
            Expect(failures, first.Vertices.SequenceEqual(second.Vertices), "the same seed gave a different cluster");
            Expect(failures, !first.Vertices.SequenceEqual(other.Vertices), "different seeds gave the same cluster");

            // The cards in a cluster are turned differently from each other.
            MeshData cluster = LeafCardMesh.Build(3, 1);
            Expect(failures, cluster.Normals[0] != cluster.Normals[4] && cluster.Normals[4] != cluster.Normals[8], "cards in a cluster all face the same way");
        }

        private static void CheckBadInputIsRejected(List<string> failures)
        {
            foreach (int bad in new[] { 0, -1 })
            {
                try
                {
                    LeafCardMesh.Build(bad, 1);
                    failures.Add($"a cluster of {bad} cards should be rejected");
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Expected.
                }
            }
        }
    }
}
