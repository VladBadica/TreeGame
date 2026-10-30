using System.Collections.Generic;
using System.Text;
using TreeGame.Care;
using TreeGame.Trees;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.DevTools
{
    /// <summary>
    /// Play Mode sanity check for the growth foundation: clamping, reset, determinism,
    /// slider wiring and that repeated growth changes never create extra objects.
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
            float originalGrowth = growth.Growth;
            LogType originalFilter = Debug.unityLogger.filterLogType;

            try
            {
                // Silences the warnings that the deliberate invalid inputs below are expected to produce.
                Debug.unityLogger.filterLogType = LogType.Error;

                SetAndSnap(growth, visuals,0f);
                string initialState = Snapshot(visuals.transform, out int objectCount);

                // Repeated changes must only update existing parts.
                var random = new System.Random(1);
                for (int i = 0; i < 500; i++)
                {
                    SetAndSnap(growth, visuals,(float)random.NextDouble());
                    growth.AddGrowth(0.05f);
                    visuals.SnapToTarget();
                }

                Snapshot(visuals.transform, out int countAfterChanges);
                Expect(failures, countAfterChanges == objectCount,
                    $"object count changed from {objectCount} to {countAfterChanges} after repeated growth changes");

                // Clamping and validation.
                growth.SetGrowth(5f);
                Expect(failures, growth.Growth == 1f, $"SetGrowth(5) gave {growth.Growth}, expected 1");
                growth.AddGrowth(1f);
                Expect(failures, growth.Growth == 1f, $"AddGrowth past the maximum gave {growth.Growth}, expected 1");
                growth.SetGrowth(-3f);
                Expect(failures, growth.Growth == 0f, $"SetGrowth(-3) gave {growth.Growth}, expected 0");
                growth.SetGrowth(0.5f);
                growth.AddGrowth(-0.2f);
                Expect(failures, growth.Growth == 0.5f, $"negative AddGrowth changed growth to {growth.Growth}");
                growth.AddGrowth(float.NaN);
                growth.SetGrowth(float.PositiveInfinity);
                Expect(failures, growth.Growth == 0.5f, $"non-finite input changed growth to {growth.Growth}");

                // Determinism: the same value gives the same tree regardless of history.
                SetAndSnap(growth, visuals,0.63f);
                string first = Snapshot(visuals.transform, out _);
                SetAndSnap(growth, visuals,0.1f);
                SetAndSnap(growth, visuals,0.9f);
                SetAndSnap(growth, visuals,0.63f);
                Expect(failures, first == Snapshot(visuals.transform, out _), "the same growth value produced a different tree");

                // Reset restores the initial appearance.
                SetAndSnap(growth, visuals,0f);
                Expect(failures, initialState == Snapshot(visuals.transform, out _), "reset did not restore the initial appearance");

                // The debug slider drives growth and the tree.
                var slider = Object.FindAnyObjectByType<Slider>();
                if (slider == null)
                {
                    failures.Add("no growth slider found");
                }
                else
                {
                    slider.value = 0.4f;
                    visuals.SnapToTarget();
                    Expect(failures, Mathf.Approximately(growth.Growth, 0.4f), $"slider set growth to {growth.Growth}, expected 0.4");
                    Expect(failures, initialState != Snapshot(visuals.transform, out _), "moving the slider did not change the tree");
                }

                CheckResources(failures);
            }
            finally
            {
                Debug.unityLogger.filterLogType = originalFilter;
                SetAndSnap(growth, visuals, originalGrowth);
            }

            if (failures.Count == 0)
            {
                Debug.Log("Growth self-check PASSED: clamping, validation, determinism, reset, slider, no duplicate parts and resources.");
            }
            else
            {
                Debug.LogError("Growth self-check FAILED:\n- " + string.Join("\n- ", failures));
            }
        }

        /// <summary>Visuals ease toward the target over time; snapping makes snapshots reflect the final pose.</summary>
        private static void SetAndSnap(TreeGrowth growth, TreeVisuals visuals, float value)
        {
            growth.SetGrowth(value);
            visuals.SnapToTarget();
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
        }

        private static void Expect(List<string> failures, bool condition, string failure)
        {
            if (!condition)
            {
                failures.Add(failure);
            }
        }

        /// <summary>Text fingerprint of every part's name, visibility, position and scale.</summary>
        private static string Snapshot(Transform root, out int objectCount)
        {
            var builder = new StringBuilder();
            Transform[] parts = root.GetComponentsInChildren<Transform>(true);
            objectCount = parts.Length;

            foreach (Transform part in parts)
            {
                builder.Append(part.name).Append(part.gameObject.activeSelf ? " on " : " off ")
                    .Append(part.localPosition.ToString("F4")).Append(part.localScale.ToString("F4")).Append('\n');
            }

            return builder.ToString();
        }
    }
}
