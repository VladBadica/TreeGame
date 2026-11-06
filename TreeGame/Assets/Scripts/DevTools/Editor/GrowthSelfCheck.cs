using System.Collections.Generic;
using System.Linq;
using System.Text;
using TreeGame.Care;
using TreeGame.Trees;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.DevTools
{
    /// <summary>
    /// Play Mode sanity check for the growth foundation: clamping, that the trunk, branches and foliage grow
    /// independently, reset, determinism, slider wiring and that repeated growth changes never create extra objects.
    /// </summary>
    public static class GrowthSelfCheck
    {
        private const string MenuPath = "Tree Game/Run Growth Self-Check (Play Mode)";

        [MenuItem(MenuPath, true)]
        private static bool CanRun() => Application.isPlaying;

        [MenuItem(MenuPath)]
        private static void Run()
        {
            var growth = Object.FindAnyObjectByType<TreeGrowth>();
            var visuals = Object.FindAnyObjectByType<TreeVisuals>();
            if (growth == null || visuals == null)
            {
                Debug.LogError("Growth self-check: no TreeGrowth/TreeVisuals found in the open scene.");
                return;
            }

            var failures = new List<string>();
            var originalGrowth = new float[TreeGrowth.Parts.Length];
            foreach (TreeGrowthPart part in TreeGrowth.Parts)
            {
                originalGrowth[(int)part] = growth.GetGrowth(part);
            }

            LogType originalFilter = Debug.unityLogger.filterLogType;

            try
            {
                // Silences the warnings that the deliberate invalid inputs below are expected to produce.
                Debug.unityLogger.filterLogType = LogType.Error;

                SetAllAndSnap(growth, visuals, 0f);
                string initialState = Snapshot(visuals.transform, out int objectCount);

                // Repeated changes, in any mix of the parts, must only update existing parts.
                var random = new System.Random(1);
                for (int i = 0; i < 500; i++)
                {
                    SetAndSnap(growth, visuals, (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
                    growth.AddGrowth(TreeGrowth.Parts[i % TreeGrowth.Parts.Length], 0.05f);
                    visuals.SnapToTarget();
                }

                Snapshot(visuals.transform, out int countAfterChanges);
                Expect(failures, countAfterChanges == objectCount,
                    $"object count changed from {objectCount} to {countAfterChanges} after repeated growth changes");

                foreach (TreeGrowthPart part in TreeGrowth.Parts)
                {
                    CheckClampingAndValidation(failures, growth, part);
                }

                CheckPartsAreIndependent(failures, growth, visuals);

                // Determinism: the same values give the same tree regardless of history.
                SetAndSnap(growth, visuals, 0.63f, 0.2f, 0.81f);
                string first = Snapshot(visuals.transform, out _);
                SetAndSnap(growth, visuals, 0.1f, 0.9f, 0.3f);
                SetAndSnap(growth, visuals, 0.9f, 0.1f, 0.5f);
                SetAndSnap(growth, visuals, 0.63f, 0.2f, 0.81f);
                Expect(failures, first == Snapshot(visuals.transform, out _), "the same growth values produced a different tree");

                // Reset restores the initial appearance.
                SetAllAndSnap(growth, visuals, 0f);
                Expect(failures, initialState == Snapshot(visuals.transform, out _), "reset did not restore the initial appearance");

                CheckDebugSliders(failures, growth, visuals, initialState);
                CheckAboveGround(failures, growth, visuals);
                CheckExtraGrowth(failures, growth);
                CheckExtraGrowthShows(failures, growth, visuals);
                // Light and shade and the proportions are aimed at the fully grown tree; the cost is of the biggest tree.
                WithAllExtraGrowth(growth, visuals, 0f, () => CheckCrownLightAndShade(failures, growth, visuals));
                WithAllExtraGrowth(growth, visuals, 0f, () => CheckProportions(failures, growth, visuals));
                WithAllExtraGrowth(growth, visuals, 1f, () => ReportCost(growth, visuals));
                CheckResources(failures);
            }
            finally
            {
                Debug.unityLogger.filterLogType = originalFilter;
                foreach (TreeGrowthPart part in TreeGrowth.Parts)
                {
                    growth.SetGrowth(part, originalGrowth[(int)part]);
                }

                visuals.SnapToTarget();
            }

            if (failures.Count == 0)
            {
                Debug.Log("Growth self-check PASSED: clamping, validation, independent parts, determinism, reset, sliders, no duplicate parts and resources.");
            }
            else
            {
                Debug.LogError("Growth self-check FAILED:\n- " + string.Join("\n- ", failures));
            }
        }

        /// <summary>Visuals ease toward the target over time; snapping makes snapshots reflect the final pose.</summary>
        private static void SetAndSnap(TreeGrowth growth, TreeVisuals visuals, float trunk, float branches, float foliage)
        {
            growth.SetGrowth(TreeGrowthPart.Trunk, trunk);
            growth.SetGrowth(TreeGrowthPart.Branches, branches);
            growth.SetGrowth(TreeGrowthPart.Foliage, foliage);
            visuals.SnapToTarget();
        }

        private static void SetAllAndSnap(TreeGrowth growth, TreeVisuals visuals, float value)
        {
            SetAndSnap(growth, visuals, value, value, value);
        }

        private static void CheckClampingAndValidation(List<string> failures, TreeGrowth growth, TreeGrowthPart part)
        {
            growth.SetGrowth(part, 5f);
            Expect(failures, growth.GetGrowth(part) == 1f, $"{part}: SetGrowth(5) gave {growth.GetGrowth(part)}, expected 1");
            growth.AddGrowth(part, 1f);
            Expect(failures, growth.GetGrowth(part) == 1f, $"{part}: AddGrowth past the maximum gave {growth.GetGrowth(part)}, expected 1");
            float minimum = growth.MinimumGrowth(part);
            growth.SetGrowth(part, -3f);
            Expect(failures, growth.GetGrowth(part) == minimum, $"{part}: SetGrowth(-3) gave {growth.GetGrowth(part)}, expected its minimum {minimum}");
            growth.SetGrowth(part, minimum * 0.5f);
            Expect(failures, growth.GetGrowth(part) == minimum, $"{part}: went below its minimum {minimum}, to {growth.GetGrowth(part)}");
            growth.SetGrowth(part, 0.5f);
            growth.AddGrowth(part, -0.2f);
            Expect(failures, growth.GetGrowth(part) == 0.5f, $"{part}: negative AddGrowth changed growth to {growth.GetGrowth(part)}");
            growth.AddGrowth(part, float.NaN);
            growth.SetGrowth(part, float.PositiveInfinity);
            Expect(failures, growth.GetGrowth(part) == 0.5f, $"{part}: non-finite input changed growth to {growth.GetGrowth(part)}");
        }

        /// <summary>
        /// Each part's value changes only on its own. In the tree, foliage leaves the trunk and branches alone, and branches
        /// leave the trunk alone. Trunk growth is allowed to move the branches, as they grow out of it.
        /// </summary>
        private static void CheckPartsAreIndependent(List<string> failures, TreeGrowth growth, TreeVisuals visuals)
        {
            foreach (TreeGrowthPart changed in TreeGrowth.Parts)
            {
                SetAllAndSnap(growth, visuals, 0.5f);
                growth.SetGrowth(changed, 0.9f);
                foreach (TreeGrowthPart other in TreeGrowth.Parts)
                {
                    if (other != changed)
                    {
                        Expect(failures, growth.GetGrowth(other) == 0.5f, $"setting {changed} growth changed {other} growth to {growth.GetGrowth(other)}");
                    }
                }
            }

            Transform trunk = visuals.transform.Find("Generated/Trunk");
            if (trunk == null)
            {
                failures.Add("no trunk found in the tree's generated parts");
                return;
            }

            SetAllAndSnap(growth, visuals, 0.5f);
            string trunkBefore = Snapshot(trunk, out _);
            string woodBefore = Snapshot(visuals.transform, out _, includeFoliage: false);

            growth.SetGrowth(TreeGrowthPart.Foliage, 0.9f);
            visuals.SnapToTarget();
            Expect(failures, Snapshot(visuals.transform, out _, includeFoliage: false) == woodBefore, "foliage growth changed the trunk or branches");

            growth.SetGrowth(TreeGrowthPart.Branches, 0.9f);
            visuals.SnapToTarget();
            Expect(failures, Snapshot(trunk, out _) == trunkBefore, "branch growth changed the trunk");
        }

        /// <summary>
        /// No wood or leaves reach below the ground, at any mix of growth, including a young trunk with long branches
        /// and full foliage. The trunk's base sits on the ground, so a hair below counts as on it.
        /// </summary>
        private static void CheckAboveGround(List<string> failures, TreeGrowth growth, TreeVisuals visuals)
        {
            const float onTheGround = 0.01f;
            float ground = visuals.transform.position.y;
            float[] amounts = { 0f, 0.3f, 0.6f, 1f };

            foreach (float trunk in amounts)
            {
                foreach (float branches in amounts)
                {
                    foreach (float foliage in amounts)
                    {
                        SetAndSnap(growth, visuals, trunk, branches, foliage);
                        foreach (Renderer part in visuals.GetComponentsInChildren<Renderer>())
                        {
                            if (part.bounds.min.y < ground - onTheGround)
                            {
                                failures.Add($"{part.name} reaches {ground - part.bounds.min.y:F2} below the ground at trunk {trunk}, branches {branches}, foliage {foliage}");
                                return;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Passive growth fills a part's extra growth only while its rate is above 7, faster the higher the rate, from
        /// the start, and keeps it once the rate drops; it stops at full. Leaves the tree's rates and extra growth as
        /// they were.
        /// </summary>
        private static void CheckExtraGrowth(List<string> failures, TreeGrowth growth)
        {
            var passive = growth.GetComponent<PassiveGrowth>();
            var rates = growth.GetComponent<TreeGrowthRates>();
            var extra = growth.GetComponent<TreeExtraGrowth>();
            if (passive == null || rates == null || extra == null)
            {
                failures.Add("the tree needs passive growth, growth rates and extra growth");
                return;
            }

            var originalRates = new int[TreeGrowth.Parts.Length];
            var originalExtra = new float[TreeGrowth.Parts.Length];
            var originalGrowth = new float[TreeGrowth.Parts.Length];
            for (int i = 0; i < TreeGrowth.Parts.Length; i++)
            {
                originalRates[i] = rates.GetRate(TreeGrowth.Parts[i]);
                originalExtra[i] = extra.GetExtraGrowth(TreeGrowth.Parts[i]);
                originalGrowth[i] = growth.GetGrowth(TreeGrowth.Parts[i]);
            }

            try
            {
                // From the start: a part at nothing yet still fills extra growth while its rate is high.
                foreach (TreeGrowthPart part in TreeGrowth.Parts)
                {
                    growth.SetGrowth(part, 0f);
                    extra.SetExtraGrowth(part, 0f);
                }

                rates.SetRate(TreeGrowthPart.Trunk, 10);
                rates.SetRate(TreeGrowthPart.Branches, 7);
                rates.SetRate(TreeGrowthPart.Foliage, 8);
                passive.Advance(30f);

                float trunk = extra.GetExtraGrowth(TreeGrowthPart.Trunk);
                float foliage = extra.GetExtraGrowth(TreeGrowthPart.Foliage);
                Expect(failures, trunk > 0f, "a part at rate 10 should fill extra growth, even before it is fully grown");
                Expect(failures, extra.GetExtraGrowth(TreeGrowthPart.Branches) == 0f, "a part at rate 7 shouldn't fill extra growth");
                Expect(failures, foliage > 0f && Mathf.Abs(trunk - 3f * foliage) < 0.0001f,
                    $"rate 10 should fill extra growth three times as fast as rate 8 ({trunk} against {foliage})");

                // Kept when the rate drops.
                rates.SetRate(TreeGrowthPart.Trunk, 4);
                passive.Advance(30f);
                Expect(failures, extra.GetExtraGrowth(TreeGrowthPart.Trunk) == trunk, "extra growth should be kept, unchanged, once the rate drops to 7 or below");

                // Stops at full, and bad time steps change nothing.
                rates.SetRate(TreeGrowthPart.Trunk, 10);
                passive.Advance(1000000f);
                Expect(failures, extra.GetExtraGrowth(TreeGrowthPart.Trunk) == 1f, "extra growth should stop at full (100% extra)");
                Expect(failures, growth.GetGrowth(TreeGrowthPart.Trunk) == 1f, "normal growth should still stop at 100%");

                float before = extra.GetExtraGrowth(TreeGrowthPart.Foliage);
                foreach (float bad in new[] { float.NaN, float.PositiveInfinity, -50f })
                {
                    passive.Advance(bad);
                }

                Expect(failures, extra.GetExtraGrowth(TreeGrowthPart.Foliage) == before, "a bad time step changed extra growth");

                // Extra growth can't be taken away by adding, and bad values are ignored.
                extra.AddExtraGrowth(TreeGrowthPart.Foliage, -0.5f);
                extra.SetExtraGrowth(TreeGrowthPart.Foliage, float.NaN);
                Expect(failures, extra.GetExtraGrowth(TreeGrowthPart.Foliage) == before, "a negative or NaN amount changed extra growth");
                extra.SetExtraGrowth(TreeGrowthPart.Foliage, 7f);
                Expect(failures, extra.GetExtraGrowth(TreeGrowthPart.Foliage) == 1f, "extra growth above full should be capped at 1");

                // Rates stay whole numbers from 1 to 10.
                rates.SetRate(TreeGrowthPart.Branches, 0);
                Expect(failures, rates.GetRate(TreeGrowthPart.Branches) == 1, "a rate of 0 should become 1");
                rates.SetRate(TreeGrowthPart.Branches, 99);
                Expect(failures, rates.GetRate(TreeGrowthPart.Branches) == 10, "a rate of 99 should become 10");
            }
            finally
            {
                for (int i = 0; i < TreeGrowth.Parts.Length; i++)
                {
                    rates.SetRate(TreeGrowth.Parts[i], originalRates[i]);
                    extra.SetExtraGrowth(TreeGrowth.Parts[i], originalExtra[i]);
                    growth.SetGrowth(TreeGrowth.Parts[i], originalGrowth[i]);
                }
            }
        }

        /// <summary>Runs something with every part's extra growth at this value, then puts it back as it was.</summary>
        private static void WithAllExtraGrowth(TreeGrowth growth, TreeVisuals visuals, float value, System.Action run)
        {
            var extra = growth.GetComponent<TreeExtraGrowth>();
            if (extra == null)
            {
                run();
                return;
            }

            float[] original = TreeGrowth.Parts.Select(extra.GetExtraGrowth).ToArray();
            try
            {
                foreach (TreeGrowthPart part in TreeGrowth.Parts)
                {
                    extra.SetExtraGrowth(part, value);
                }

                visuals.SnapToTarget();
                run();
            }
            finally
            {
                for (int i = 0; i < original.Length; i++)
                {
                    extra.SetExtraGrowth(TreeGrowth.Parts[i], original[i]);
                }

                visuals.SnapToTarget();
            }
        }

        /// <summary>
        /// Extra growth carries each part past its full size, only that part, and a tree without it looks exactly as it
        /// did before extra growth existed: taking it away again gives back the very same tree.
        /// </summary>
        private static void CheckExtraGrowthShows(List<string> failures, TreeGrowth growth, TreeVisuals visuals)
        {
            var extra = growth.GetComponent<TreeExtraGrowth>();
            Transform trunk = visuals.transform.Find("Generated/Trunk");
            if (extra == null || trunk == null)
            {
                failures.Add("the tree needs extra growth and a trunk to check how extra growth shows");
                return;
            }

            var original = new float[TreeGrowth.Parts.Length];
            for (int i = 0; i < original.Length; i++)
            {
                original[i] = extra.GetExtraGrowth(TreeGrowth.Parts[i]);
            }

            void SetExtra(TreeGrowthPart part, float value)
            {
                extra.SetExtraGrowth(part, value);
                visuals.SnapToTarget();
            }

            int ShownLeaves() => visuals.GetComponentsInChildren<MeshRenderer>().Count(renderer => renderer.name.StartsWith("Foliage"));

            try
            {
                foreach (TreeGrowthPart part in TreeGrowth.Parts)
                {
                    extra.SetExtraGrowth(part, 0f);
                }

                SetAllAndSnap(growth, visuals, 1f);
                string withoutExtra = Snapshot(visuals.transform, out _);
                float trunkHeight = trunk.localScale.y;
                string woodWithoutExtra = Snapshot(visuals.transform, out _, includeFoliage: false);
                int leaves = ShownLeaves();

                // The trunk: taller. The branches move up with it, but the leaves don't change.
                SetExtra(TreeGrowthPart.Trunk, 1f);
                Expect(failures, trunk.localScale.y > trunkHeight * 1.05f, "a trunk with all its extra growth should be taller than fully grown");
                Expect(failures, ShownLeaves() == leaves, "extra trunk growth shouldn't change how many leaves show");
                SetExtra(TreeGrowthPart.Trunk, 0f);

                // The branches: longer, the trunk the same.
                SetExtra(TreeGrowthPart.Branches, 1f);
                Expect(failures, trunk.localScale.y == trunkHeight, "extra branch growth shouldn't change the trunk");
                Expect(failures, Snapshot(visuals.transform, out _, includeFoliage: false) != woodWithoutExtra, "extra branch growth should change the branches");
                SetExtra(TreeGrowthPart.Branches, 0f);

                // The foliage: more leaves, the wood the same.
                SetExtra(TreeGrowthPart.Foliage, 1f);
                Expect(failures, ShownLeaves() > leaves, $"extra foliage growth should show more leaves ({ShownLeaves()} against {leaves})");
                Expect(failures, Snapshot(visuals.transform, out _, includeFoliage: false) == woodWithoutExtra, "extra foliage growth shouldn't change the trunk or branches");
                SetExtra(TreeGrowthPart.Foliage, 0f);

                Expect(failures, Snapshot(visuals.transform, out _) == withoutExtra, "taking extra growth away again didn't give back the same tree");
            }
            finally
            {
                for (int i = 0; i < original.Length; i++)
                {
                    extra.SetExtraGrowth(TreeGrowth.Parts[i], original[i]);
                }

                visuals.SnapToTarget();
            }
        }

        /// <summary>
        /// On the fully grown tree, the leaves in the top third of the crown are lighter on average than those in the
        /// bottom third, as the crown is lit from above. Checks the shading lands on the right leaves in the real tree.
        /// </summary>
        private static void CheckCrownLightAndShade(List<string> failures, TreeGrowth growth, TreeVisuals visuals)
        {
            SetAllAndSnap(growth, visuals, 1f);

            var leaves = new List<MeshRenderer>();
            foreach (MeshRenderer renderer in visuals.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.name.StartsWith("Foliage") && renderer.sharedMaterial != null)
                {
                    leaves.Add(renderer);
                }
            }

            if (leaves.Count < 10)
            {
                failures.Add($"only {leaves.Count} leaf clusters with a material on the grown tree, too few to check light and shade");
                return;
            }

            float lowest = float.MaxValue;
            float highest = float.MinValue;
            foreach (MeshRenderer leaf in leaves)
            {
                lowest = Mathf.Min(lowest, leaf.transform.position.y);
                highest = Mathf.Max(highest, leaf.transform.position.y);
            }

            float third = (highest - lowest) / 3f;
            float topLight = 0f, bottomLight = 0f;
            int topCount = 0, bottomCount = 0;
            foreach (MeshRenderer leaf in leaves)
            {
                float light = leaf.sharedMaterial.GetColor("_BaseColor").grayscale;
                if (leaf.transform.position.y > highest - third)
                {
                    topLight += light;
                    topCount++;
                }
                else if (leaf.transform.position.y < lowest + third)
                {
                    bottomLight += light;
                    bottomCount++;
                }
            }

            Expect(failures, topCount > 0 && bottomCount > 0 && topLight / topCount > bottomLight / bottomCount,
                "the leaves on top of the grown crown aren't lighter than those underneath");
        }

        /// <summary>
        /// Measures the fully grown tree as drawn and compares it with an open-grown broadleaf tree: a trunk clear of
        /// leaves for 40 to 50% of the height, and a crown about as wide as the tree is tall. Always logs the numbers, so
        /// tuning can be compared; fails only when well outside, as leaf cards make the edges a little uneven.
        /// </summary>
        private static void CheckProportions(List<string> failures, TreeGrowth growth, TreeVisuals visuals)
        {
            SetAllAndSnap(growth, visuals, 1f);
            float ground = visuals.transform.position.y;

            float top = ground;
            float crownBottom = float.MaxValue;
            float left = float.MaxValue;
            float right = float.MinValue;
            foreach (MeshRenderer renderer in visuals.GetComponentsInChildren<MeshRenderer>())
            {
                Bounds bounds = renderer.bounds;
                top = Mathf.Max(top, bounds.max.y);
                if (renderer.name.StartsWith("Foliage"))
                {
                    crownBottom = Mathf.Min(crownBottom, bounds.min.y);
                    left = Mathf.Min(left, bounds.min.x);
                    right = Mathf.Max(right, bounds.max.x);
                }
            }

            float height = top - ground;
            if (height <= 0f || crownBottom == float.MaxValue)
            {
                failures.Add("the grown tree has no height or no leaves to measure its proportions by");
                return;
            }

            float clearShare = (crownBottom - ground) / height;
            float widthShare = (right - left) / height;
            Debug.Log($"Grown tree proportions: {height:F2} tall, trunk clear of leaves for {clearShare:P0} of it " +
                      $"(aim 40-50%), crown {right - left:F2} wide = {widthShare:F2}x the height (aim 0.9-1.0).");

            Expect(failures, clearShare >= 0.35f && clearShare <= 0.55f,
                $"the trunk is clear of leaves for {clearShare:P0} of the tree's height, well outside the aimed 40-50%");
            Expect(failures, widthShare >= 0.8f && widthShare <= 1.25f,
                $"the crown is {widthShare:F2}x as wide as the tree is tall, well outside the aimed 0.9-1.0");
        }

        /// <summary>
        /// Logs what a fully grown tree costs: its objects, what is drawn, and how long redrawing it for a growth change
        /// takes. Not a pass or fail, as timings depend on the computer; it gives numbers to compare as the tree changes.
        /// </summary>
        private static void ReportCost(TreeGrowth growth, TreeVisuals visuals)
        {
            const int redraws = 200;
            SetAllAndSnap(growth, visuals, 1f);

            Transform[] objects = visuals.GetComponentsInChildren<Transform>(true);
            MeshFilter[] meshes = visuals.GetComponentsInChildren<MeshFilter>();
            int triangles = 0;
            foreach (MeshFilter mesh in meshes)
            {
                triangles += mesh.sharedMesh != null ? mesh.sharedMesh.triangles.Length / 3 : 0;
            }

            // Renderers sharing a material (and without per-renderer overrides) can be batched together.
            var materials = new HashSet<Material>();
            int overridden = 0;
            foreach (MeshRenderer renderer in visuals.GetComponentsInChildren<MeshRenderer>())
            {
                materials.Add(renderer.sharedMaterial);
                overridden += renderer.HasPropertyBlock() ? 1 : 0;
            }

            // Alternating, so every redraw really changes something.
            var watch = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < redraws; i++)
            {
                SetAllAndSnap(growth, visuals, i % 2 == 0 ? 0.99f : 1f);
            }

            watch.Stop();
            double millisecondsPerRedraw = watch.Elapsed.TotalMilliseconds / redraws;
            Debug.Log($"Tree cost when fully grown with all its extra growth: {objects.Length} objects, {meshes.Length} drawn, {triangles} triangles, " +
                      $"{materials.Count} materials, {overridden} with their own property overrides (which stop batching); " +
                      $"a redraw for a growth change takes {millisecondsPerRedraw:F3} ms (it happens each frame while the tree grows).");
        }

        /// <summary>Each debug slider drives its own part, and only that part.</summary>
        private static void CheckDebugSliders(List<string> failures, TreeGrowth growth, TreeVisuals visuals, string initialState)
        {
            var panel = Object.FindAnyObjectByType<GrowthDebugPanel>();
            if (panel == null)
            {
                failures.Add("no growth debug panel found");
                return;
            }

            // The growth sliders only: the panel also has a slider per part for extra growth.
            Slider[] sliders = panel.GetComponentsInChildren<Slider>().Where(slider => !slider.name.Contains("Extra")).ToArray();
            if (sliders.Length != TreeGrowth.Parts.Length)
            {
                failures.Add($"the debug panel has {sliders.Length} sliders, expected one per growth part");
                return;
            }

            for (int i = 0; i < sliders.Length; i++)
            {
                TreeGrowthPart part = TreeGrowth.Parts[i];
                SetAllAndSnap(growth, visuals, 0f);
                sliders[i].value = 0.4f;
                visuals.SnapToTarget();

                Expect(failures, Mathf.Approximately(growth.GetGrowth(part), 0.4f), $"the {part} slider set {part} growth to {growth.GetGrowth(part)}, expected 0.4");
                foreach (TreeGrowthPart other in TreeGrowth.Parts)
                {
                    if (other != part)
                    {
                        Expect(failures, growth.GetGrowth(other) == growth.MinimumGrowth(other), $"the {part} slider changed {other} growth");
                    }
                }

                Expect(failures, initialState != Snapshot(visuals.transform, out _), $"moving the {part} slider did not change the tree");
            }
        }

        private static void CheckResources(List<string> failures)
        {
            TreeResource[] resources = Object.FindObjectsByType<TreeResource>();
            foreach (TreeResourceType type in System.Enum.GetValues(typeof(TreeResourceType)))
            {
                Expect(failures, System.Array.Exists(resources, r => r.Type == type), $"no {type} resource in the scene");
            }

            foreach (TreeResource resource in resources)
            {
                float original = resource.Amount;

                resource.SetAmount(resource.MaxAmount * 10f);
                Expect(failures, resource.Amount == resource.MaxAmount, $"{resource.Type} exceeded its maximum");
                resource.Consume(resource.MaxAmount * 10f);
                Expect(failures, resource.Amount == 0f, $"{resource.Type} went below zero");
                resource.Add(-5f);
                resource.Consume(float.NaN);
                Expect(failures, resource.Amount == 0f, $"{resource.Type} accepted an invalid amount");
                resource.Add(resource.MaxAmount * 0.25f);
                Expect(failures, Mathf.Approximately(resource.Normalized, 0.25f), $"{resource.Type} normalized value is {resource.Normalized}, expected 0.25");

                resource.SetAmount(original);
            }

            CheckHealthRegeneration(failures, resources);
            CheckNeglectDamage(failures, resources);
        }

        private static void CheckNeglectDamage(List<string> failures, TreeResource[] resources)
        {
            NeglectDamage neglect = Object.FindAnyObjectByType<NeglectDamage>();
            Expect(failures, neglect != null, "no neglect damage in the scene");
            if (neglect == null)
            {
                return;
            }

            TreeResource health = System.Array.Find(resources, r => r.Type == TreeResourceType.Health);
            TreeResource water = System.Array.Find(resources, r => r.Type == TreeResourceType.Water);
            TreeResource nutrients = System.Array.Find(resources, r => r.Type == TreeResourceType.Nutrients);
            float originalHealth = health.Amount;
            float originalWater = water.Amount;
            float originalNutrients = nutrients.Amount;

            // Plays from half health, with water and nutrients as shares of their maximum.
            float HealthLost(float waterShare, float nutrientsShare)
            {
                water.SetAmount(water.MaxAmount * waterShare);
                nutrients.SetAmount(nutrients.MaxAmount * nutrientsShare);
                health.SetAmount(health.MaxAmount * 0.5f);
                neglect.Advance(10f);
                return health.MaxAmount * 0.5f - health.Amount;
            }

            float waterEmptyLoss = HealthLost(0f, 1f);
            float nutrientsEmptyLoss = HealthLost(1f, 0f);
            float bothEmptyLoss = HealthLost(0f, 0f);

            Expect(failures, waterEmptyLoss > 0f, "health should drop with water empty");
            Expect(failures, nutrientsEmptyLoss > 0f, "health should drop with nutrients empty");
            Expect(failures, Mathf.Approximately(waterEmptyLoss, nutrientsEmptyLoss), $"each empty resource should cost the same, got {waterEmptyLoss} and {nutrientsEmptyLoss}");
            Expect(failures, Mathf.Abs(bothEmptyLoss - waterEmptyLoss * 2f) < 0.001f, $"both empty should cost double, got {bothEmptyLoss} against {waterEmptyLoss}");
            Expect(failures, HealthLost(0.01f, 0.01f) == 0f, "health should not drop while water and nutrients aren't empty");

            water.SetAmount(0f);
            nutrients.SetAmount(0f);
            health.SetAmount(health.MaxAmount * 0.01f);
            neglect.Advance(100000f);
            Expect(failures, health.Amount == 0f, $"health should stop at 0, got {health.Amount}");

            health.SetAmount(originalHealth);
            water.SetAmount(originalWater);
            nutrients.SetAmount(originalNutrients);
        }

        private static void CheckHealthRegeneration(List<string> failures, TreeResource[] resources)
        {
            HealthRegeneration regeneration = Object.FindAnyObjectByType<HealthRegeneration>();
            Expect(failures, regeneration != null, "no health regeneration in the scene");
            if (regeneration == null)
            {
                return;
            }

            TreeResource health = System.Array.Find(resources, r => r.Type == TreeResourceType.Health);
            TreeResource water = System.Array.Find(resources, r => r.Type == TreeResourceType.Water);
            TreeResource nutrients = System.Array.Find(resources, r => r.Type == TreeResourceType.Nutrients);
            float originalHealth = health.Amount;
            float originalWater = water.Amount;
            float originalNutrients = nutrients.Amount;

            // Plays from half health, with water and nutrients as shares of their maximum.
            float HealthGained(float waterShare, float nutrientsShare)
            {
                water.SetAmount(water.MaxAmount * waterShare);
                nutrients.SetAmount(nutrients.MaxAmount * nutrientsShare);
                health.SetAmount(health.MaxAmount * 0.5f);
                regeneration.Advance(10f);
                return health.Amount - health.MaxAmount * 0.5f;
            }

            Expect(failures, HealthGained(0.6f, 0.6f) > 0f, "health should regenerate with water and nutrients above half");
            Expect(failures, HealthGained(0.4f, 0.6f) == 0f, "health should not regenerate with water below half");
            Expect(failures, HealthGained(0.6f, 0.4f) == 0f, "health should not regenerate with nutrients below half");
            Expect(failures, HealthGained(0.5f, 0.6f) == 0f, "health should not regenerate with water at exactly half");
            Expect(failures, HealthGained(0.6f, 0.5f) == 0f, "health should not regenerate with nutrients at exactly half");

            water.SetAmount(water.MaxAmount);
            nutrients.SetAmount(nutrients.MaxAmount);
            health.SetAmount(health.MaxAmount * 0.99f);
            regeneration.Advance(100000f);
            Expect(failures, health.Amount == health.MaxAmount, $"health should stop at its maximum, got {health.Amount}");

            health.SetAmount(originalHealth);
            water.SetAmount(originalWater);
            nutrients.SetAmount(originalNutrients);
        }

        private static void Expect(List<string> failures, bool condition, string failure)
        {
            if (!condition)
            {
                failures.Add(failure);
            }
        }

        /// <summary>Text fingerprint of every part's name, visibility, position and scale.</summary>
        /// <param name="includeFoliage">False leaves out the leaf clusters, to compare only the trunk and branches.</param>
        private static string Snapshot(Transform root, out int objectCount, bool includeFoliage = true)
        {
            var builder = new StringBuilder();
            Transform[] parts = root.GetComponentsInChildren<Transform>(true);
            objectCount = parts.Length;

            foreach (Transform part in parts)
            {
                if (!includeFoliage && part.name.StartsWith("Foliage"))
                {
                    continue;
                }

                // A hidden part keeps whatever it last showed, which depends on history, and can't be seen anyway.
                if (!part.gameObject.activeInHierarchy)
                {
                    builder.Append(part.name).Append(" hidden\n");
                    continue;
                }

                builder.Append(part.name).Append(part.gameObject.activeSelf ? " on " : " off ")
                    .Append(part.localPosition.ToString("F4")).Append(part.localScale.ToString("F4")).Append('\n');
            }

            return builder.ToString();
        }
    }
}
