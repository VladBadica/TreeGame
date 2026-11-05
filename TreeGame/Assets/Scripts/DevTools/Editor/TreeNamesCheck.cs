using System.Collections.Generic;
using TreeGame.Trees;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace TreeGame.DevTools
{
    /// <summary>
    /// Checks the rules for the names players give their trees, without a scene or Play Mode. The rules have no Unity
    /// dependency, so this also compiles and runs outside Unity; only the menu item is Editor-specific.
    /// </summary>
    public static class TreeNamesCheck
    {
        /// <summary>Runs every check. Returns a description of each failure; empty means all passed.</summary>
        public static List<string> Run()
        {
            var failures = new List<string>();

            void ExpectClean(string typed, string expected, string what)
            {
                string cleaned = TreeNames.Clean(typed);
                if (cleaned != expected)
                {
                    failures.Add($"{what}: '{typed ?? "null"}' became '{cleaned}', expected '{expected}'");
                }
            }

            ExpectClean("Old Oak", "Old Oak", "an ordinary name");
            ExpectClean("Åsa's Ünique 木", "Åsa's Ünique 木", "letters from any language");
            ExpectClean(null, "", "no name");
            ExpectClean("", "", "an empty name");
            ExpectClean("   \t ", "", "only spaces");
            ExpectClean("  Old   Oak  ", "Old Oak", "spaces at the ends and in a run");
            ExpectClean("Old\tOak\nTree", "Old Oak Tree", "tabs and line breaks");
            ExpectClean("<b>Bold</b> Oak", "bBold/b Oak", "rich text tags, which would format the card");
            ExpectClean("Oak\u0007\u0000", "Oak", "invisible control characters");

            string longName = new string('a', TreeNames.MaxLength + 10);
            ExpectClean(longName, new string('a', TreeNames.MaxLength), "a name too long for the card");

            // Cut to length, with no space left hanging at the end.
            string cutAtSpace = new string('a', TreeNames.MaxLength - 1) + " b";
            ExpectClean(cutAtSpace, new string('a', TreeNames.MaxLength - 1), "a cut that would leave a space at the end");

            // Spaces squeezed before cutting, so they don't use up the length.
            ExpectClean("a" + new string(' ', 40) + "b", "a b", "a long run of spaces");

            // Shown as its name, or the slot's own name without one.
            if (TreeNames.Display("  Old Oak ", "Tree 2") != "Old Oak")
            {
                failures.Add("a named tree should show its cleaned name");
            }

            foreach (string unnamed in new[] { null, "", "  ", "<>" })
            {
                if (TreeNames.Display(unnamed, "Tree 2") != "Tree 2")
                {
                    failures.Add($"a tree named '{unnamed ?? "null"}' should show as its slot's own name");
                }
            }

            return failures;
        }

#if UNITY_EDITOR
        [MenuItem("Tree Game/Run Tree Names Check")]
        private static void RunFromMenu()
        {
            List<string> failures = Run();
            if (failures.Count == 0)
            {
                Debug.Log("Tree names check PASSED.");
            }
            else
            {
                Debug.LogError("Tree names check FAILED:\n- " + string.Join("\n- ", failures));
            }
        }
#endif
    }
}
