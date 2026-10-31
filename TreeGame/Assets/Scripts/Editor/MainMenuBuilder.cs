using System.IO;
using TMPro;
using TreeGame.Menus;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TreeGame.EditorTools.BuilderHelpers;
using static TreeGame.EditorTools.UIFactory;

namespace TreeGame.EditorTools
{
    /// <summary>Generates the main menu scene: one canvas with a panel per screen (main, settings, credits).</summary>
    public static class MainMenuBuilder
    {
        private const float TitleHeight = 90f;
        private const float ButtonHeight = 60f;
        private const float ButtonFontSize = 28f;
        private const float PanelWidth = 480f;

        [MenuItem("Tree Game/Create Main Menu Scene")]
        private static void CreateMainMenuScene()
        {
            const string scenePath = BuildSceneList.MainMenuPath;

            if (!TryStartNewScene(scenePath, "Create Main Menu Scene", out Scene scene))
            {
                return;
            }

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

            // Placeholder text until the settings panel exists and there are credits to show.
            GameObject settingsPanel = CreateInfoPanel(canvas, "Settings Panel", "Settings", "Coming soon.");
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

            var loadGame = play.gameObject.AddComponent<LoadSceneButton>();
            var serializedLoadGame = new SerializedObject(loadGame);
            serializedLoadGame.FindProperty("sceneName").stringValue = Path.GetFileNameWithoutExtension(BuildSceneList.TreeGamePath);
            serializedLoadGame.ApplyModifiedPropertiesWithoutUndo();

            quit.gameObject.AddComponent<QuitGameButton>();

            // Saved hidden so only the main panel shows from the first frame.
            settingsPanel.SetActive(false);
            creditsPanel.SetActive(false);
        }

        /// <summary>A centered panel with a large title at the top; buttons and text stack below it.</summary>
        private static GameObject CreateScreenPanel(Transform canvas, string objectName, string title, float height)
        {
            var center = new Vector2(0.5f, 0.5f);
            GameObject panel = CreatePanel(canvas, objectName, center, Vector2.zero, new Vector2(PanelWidth, height));

            TMP_Text titleLabel = CreateText("Title", 52f, Color.white, TextAlignmentOptions.Center);
            AddToLayout(titleLabel.gameObject, panel.transform, TitleHeight);
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.text = title;

            return panel;
        }

        /// <summary>A screen with a title and a block of text, ready for its Back button.</summary>
        private static GameObject CreateInfoPanel(Transform canvas, string objectName, string title, string body)
        {
            GameObject panel = CreateScreenPanel(canvas, objectName, title, height: 320f);

            TMP_Text bodyLabel = CreateText("Body", 26f, Color.white, TextAlignmentOptions.Center);
            AddToLayout(bodyLabel.gameObject, panel.transform, 120f);
            bodyLabel.text = body;

            return panel;
        }

        private static Button CreateMenuButton(DefaultControls.Resources resources, Transform panel, string caption)
        {
            Button button = CreateButton(resources, panel, $"{caption} Button", caption, ButtonFontSize);
            AddToLayout(button.gameObject, panel, ButtonHeight);
            return button;
        }

        private static void ConnectPanelButton(Button button, MenuPanels menu, GameObject panel)
        {
            SetReferences(button.gameObject.AddComponent<OpenPanelButton>(),
                ("menu", menu),
                ("panel", panel));
        }
    }
}
