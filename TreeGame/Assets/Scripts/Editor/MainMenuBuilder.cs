using TreeGame.Menus;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TreeGame.EditorTools.BuilderHelpers;
using static TreeGame.EditorTools.MenuUIFactory;
using static TreeGame.EditorTools.UIFactory;

namespace TreeGame.EditorTools
{
    /// <summary>Generates the main menu scene: one canvas with a panel per screen (main, settings, credits).</summary>
    public static class MainMenuBuilder
    {
        [MenuItem("Tree Game/Create Main Menu Scene", false, AllScenesBuilder.SingleSceneMenuPriority)]
        private static void CreateMainMenuScene()
        {
            if (ConfirmGenerating("Create Main Menu Scene", BuildSceneList.MainMenuPath))
            {
                Build();
            }
        }

        /// <summary>Generates the scene without asking; callers confirm first.</summary>
        internal static void Build()
        {
            const string scenePath = BuildSceneList.MainMenuPath;
            Scene scene = StartNewScene();

            CreateCamera();
            CreateEventSystem();
            Transform canvas = CreateCanvas("Menu", 0);
            CreateScreens(canvas);

            SaveGeneratedScene(scene, scenePath);

            Debug.Log($"Main menu created at {scenePath}. Open it and press Play to start from the menu.");
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SceneLayout.SkyColor;
            go.AddComponent<AudioListener>();
        }

        private static void CreateScreens(Transform canvas)
        {
            DefaultControls.Resources resources = GetUIResources();

            GameObject mainPanel = CreateScreenPanel(canvas, "Main Panel", PlayerSettings.productName, height: 400f);
            Button play = CreateMenuButton(resources, mainPanel.transform, "Play");
            Button settings = CreateMenuButton(resources, mainPanel.transform, "Settings");
            Button credits = CreateMenuButton(resources, mainPanel.transform, "Credits");
            Button quit = CreateMenuButton(resources, mainPanel.transform, "Quit");

            GameObject settingsPanel = CreateSettingsPanel(canvas);
            // Placeholder text until there are credits to show.
            GameObject creditsPanel = CreateInfoPanel(canvas, "Credits Panel", "Credits", "Coming soon.");
            Button settingsBack = CreateMenuButton(resources, settingsPanel.transform, "Back");
            Button creditsBack = CreateMenuButton(resources, creditsPanel.transform, "Back");

            var menu = canvas.gameObject.AddComponent<MenuPanels>();
            SetReferenceArray(menu, "panels", mainPanel, settingsPanel, creditsPanel);
            SetReferences(menu, ("startPanel", mainPanel));

            ConnectPanelButton(settings, menu, settingsPanel);
            ConnectPanelButton(credits, menu, creditsPanel);
            ConnectPanelButton(settingsBack, menu, mainPanel);
            ConnectPanelButton(creditsBack, menu, mainPanel);
            ConnectLoadSceneButton(play, BuildSceneList.TreeGamePath);
            quit.gameObject.AddComponent<QuitGameButton>();

            // Saved hidden so only the main panel shows from the first frame.
            settingsPanel.SetActive(false);
            creditsPanel.SetActive(false);
        }
    }
}
