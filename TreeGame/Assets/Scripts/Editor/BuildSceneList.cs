using System;
using System.Linq;
using UnityEditor;

namespace TreeGame.EditorTools
{
    /// <summary>
    /// Keeps the scenes in Build Profiles in the right order. The game starts with the first scene in the list,
    /// so the fixed scenes always stay on top no matter which one was generated last; any others follow them.
    /// </summary>
    internal static class BuildSceneList
    {
        public const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        public const string TreeGamePath = "Assets/Scenes/TreeGame.unity";

        private static readonly string[] FixedOrder = { MainMenuPath, TreeGamePath };

        /// <summary>Adds a scene to the build (or re-enables it) and restores the order.</summary>
        public static void Add(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.path != scenePath).ToList();
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));

            // OrderBy is stable, so scenes that aren't in the fixed order keep their relative order.
            EditorBuildSettings.scenes = scenes.OrderBy(scene => GetRank(scene.path)).ToArray();
        }

        private static int GetRank(string scenePath)
        {
            int index = Array.IndexOf(FixedOrder, scenePath);
            return index >= 0 ? index : FixedOrder.Length;
        }
    }
}
