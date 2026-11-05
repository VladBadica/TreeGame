using System;
using System.Collections.Generic;
using TreeGame.Menus;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace TreeGame.DevTools
{
    /// <summary>
    /// Checks the words on the main menu's save slot cards, without a scene or Play Mode. The text has no Unity
    /// dependency, so this also compiles and runs outside Unity; only the menu item is Editor-specific.
    /// </summary>
    public static class SlotTextCheck
    {
        /// <summary>Runs every check. Returns a description of each failure; empty means all passed.</summary>
        public static List<string> Run()
        {
            var failures = new List<string>();

            void ExpectText(string actual, string expected, string what)
            {
                if (actual != expected)
                {
                    failures.Add($"{what} read '{actual}', expected '{expected}'");
                }
            }

            // Growth: the average of the parts, rounded, never outside 0 to 100.
            ExpectText(SlotText.Growth(0f, 0f, 0f), "0% grown", "nothing grown");
            ExpectText(SlotText.Growth(1f, 1f, 1f), "100% grown", "fully grown");
            ExpectText(SlotText.Growth(0.3f, 0.6f, 0.9f), "60% grown", "an even spread");
            ExpectText(SlotText.Growth(0.333f, 0.333f, 0.335f), "33% grown", "rounding down");
            ExpectText(SlotText.Growth(0.665f, 0.665f, 0.67f), "67% grown", "rounding up");
            ExpectText(SlotText.Growth(5f, -2f, 1f), "67% grown", "values outside 0 to 1, as the save isn't trusted");
            ExpectText(SlotText.Growth(float.NaN, float.PositiveInfinity, 1f), "33% grown", "values that aren't numbers");

            // Last played: counted in the player's days, not 24-hour spans.
            var now = new DateTime(2026, 11, 5, 9, 30, 0);
            ExpectText(SlotText.LastPlayed(now.AddHours(-2), now), "Played today", "two hours ago");
            ExpectText(SlotText.LastPlayed(new DateTime(2026, 11, 4, 23, 50, 0), now), "Played yesterday", "late last night");
            ExpectText(SlotText.LastPlayed(new DateTime(2026, 11, 4, 0, 5, 0), now), "Played yesterday", "early yesterday");
            ExpectText(SlotText.LastPlayed(new DateTime(2026, 11, 3, 23, 0, 0), now), "Played 2 days ago", "two days back");
            ExpectText(SlotText.LastPlayed(now.AddDays(-SlotText.DaysShownAsDays), now), $"Played {SlotText.DaysShownAsDays} days ago", "the last day shown in days");
            ExpectText(SlotText.LastPlayed(now.AddDays(-SlotText.DaysShownAsDays - 1), now), "Played 29 Oct 2026", "a week back, shown as the date");
            ExpectText(SlotText.LastPlayed(new DateTime(2025, 3, 14), now), "Played 14 Mar 2025", "long ago");
            ExpectText(SlotText.LastPlayed(now.AddDays(3), now), "Played today", "a save time after now, from a clock set back");

            return failures;
        }

#if UNITY_EDITOR
        [MenuItem("Tree Game/Run Slot Text Check")]
        private static void RunFromMenu()
        {
            List<string> failures = Run();
            if (failures.Count == 0)
            {
                Debug.Log("Slot text check PASSED.");
            }
            else
            {
                Debug.LogError("Slot text check FAILED:\n- " + string.Join("\n- ", failures));
            }
        }
#endif
    }
}
