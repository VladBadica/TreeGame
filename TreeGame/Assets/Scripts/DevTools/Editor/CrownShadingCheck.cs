using System;
using System.Collections.Generic;
using TreeGame.Trees;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif
using Vector3 = System.Numerics.Vector3;

namespace TreeGame.DevTools
{
    /// <summary>
    /// Checks how leaf clusters are shaded by where they sit in the crown, without a scene or Play Mode. The shading has
    /// no Unity dependency, so this also compiles and runs outside Unity; only the menu item is Editor-specific.
    /// </summary>
    public static class CrownShadingCheck
    {
        private const float Tolerance = 0.0001f;

        private static readonly Vector3 CrownMin = new Vector3(-3f, 1f, -1f);
        private static readonly Vector3 CrownMax = new Vector3(3f, 5f, 1f);
        private static readonly Vector3 Middle = new Vector3(0f, 3f, 0f);

        /// <summary>Runs every check. Returns a description of each failure; empty means all passed.</summary>
        public static List<string> Run()
        {
            var failures = new List<string>();

            void Expect(bool condition, string failure)
            {
                if (!condition)
                {
                    failures.Add(failure);
                }
            }

            float Placed(Vector3 position) => CrownShading.Light(position, CrownMin, CrownMax, 0.5f, 1f);

            // Lit from above: higher is lighter, top lightest and bottom darkest.
            Expect(Placed(new Vector3(0f, 5f, 0f)) > Placed(Middle), "the top of the crown isn't lighter than its middle");
            Expect(Placed(Middle) > Placed(new Vector3(0f, 1f, 0f)), "the middle of the crown isn't lighter than its bottom");

            // Lit from outside: the edge is lighter than the inside, on either side and in depth.
            Expect(Placed(new Vector3(3f, 3f, 0f)) > Placed(Middle), "the right edge isn't lighter than the inside");
            Expect(Placed(new Vector3(-3f, 3f, 0f)) > Placed(Middle), "the left edge isn't lighter than the inside");
            Expect(Placed(new Vector3(0f, 3f, -1f)) > Placed(Middle), "the front edge isn't lighter than the inside");
            Expect(MathF.Abs(Placed(new Vector3(2f, 3f, 0f)) - Placed(new Vector3(-2f, 3f, 0f))) < Tolerance, "the two sides are shaded differently");

            // Height counts for more than how far out, as light comes from above.
            Expect(Placed(new Vector3(0f, 5f, 0f)) > Placed(new Vector3(3f, 1f, 0f)),
                "the top middle isn't lighter than the bottom edge, though light comes from above");

            // The extremes: the bottom middle is darkest, the top edge lightest.
            Expect(MathF.Abs(Placed(new Vector3(0f, 1f, 0f))) < Tolerance, "the bottom middle of the crown should be the darkest");
            Expect(MathF.Abs(Placed(new Vector3(3f, 5f, 0f)) - 1f) < Tolerance, "the top edge of the crown should be the lightest");

            // The random share mixes in each cluster's own variety: none of where it sits at 0, all of it at 1.
            Expect(MathF.Abs(CrownShading.Light(Middle, CrownMin, CrownMax, 0.9f, 0f) - 0.9f) < Tolerance, "with no share for where it sits, the light should be its own random value");
            float half = CrownShading.Light(Middle, CrownMin, CrownMax, 1f, 0.5f);
            Expect(half > Placed(Middle) && half < 1f, "half the share for where it sits should land between its place and its random value");

            // Always 0 to 1, even for clusters outside the crown or for bad input.
            var odd = new[]
            {
                CrownShading.Light(new Vector3(50f, 50f, 50f), CrownMin, CrownMax, 0.5f, 1f),
                CrownShading.Light(new Vector3(-50f, -50f, -50f), CrownMin, CrownMax, 0.5f, 1f),
                CrownShading.Light(Middle, CrownMin, CrownMax, 7f, -3f),
                CrownShading.Light(new Vector3(float.NaN, 0f, 0f), CrownMin, CrownMax, 0.5f, 1f),
                CrownShading.Light(Middle, CrownMin, CrownMax, float.NaN, float.PositiveInfinity),
                CrownShading.Light(Middle, Middle, Middle, 0.5f, 1f),
            };
            foreach (float light in odd)
            {
                Expect(light >= 0f && light <= 1f && float.IsFinite(light), $"a light of {light} is outside 0 to 1");
            }

            return failures;
        }

#if UNITY_EDITOR
        [MenuItem("Tree Game/Run Crown Shading Check")]
        private static void RunFromMenu()
        {
            List<string> failures = Run();
            if (failures.Count == 0)
            {
                Debug.Log("Crown shading check PASSED.");
            }
            else
            {
                Debug.LogError("Crown shading check FAILED:\n- " + string.Join("\n- ", failures));
            }
        }
#endif
    }
}
