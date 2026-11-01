using UnityEditor;
using UnityEngine;

namespace TreeGame.EditorTools
{
    /// <summary>Generates every scene in one go. New scenes get added here as well as having their own builder.</summary>
    public static class AllScenesBuilder
    {
        // Far enough apart for Unity to draw a separator between this item and the single-scene ones.
        private const int MenuPriority = 0;
        internal const int SingleSceneMenuPriority = 20;

        [MenuItem("Tree Game/Create All Scenes", false, MenuPriority)]
        private static void CreateAllScenes()
        {
            if (!BuilderHelpers.ConfirmGenerating("Create All Scenes",
                    BuildSceneList.MainMenuPath, BuildSceneList.TreeGamePath, WellSpinSceneBuilder.ScenePath))
            {
                return;
            }

            WellSpinSceneBuilder.Build();
            PrototypeSceneBuilder.Build();

            // Last, so it is the scene left open: pressing Play then starts the game from the beginning.
            MainMenuBuilder.Build();

            Debug.Log("All scenes created. The main menu is open; press Play to start from the beginning.");
        }
    }
}
