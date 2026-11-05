using TMPro;
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
        // Required by the music's license. Keep in step with THIRD_PARTY_NOTICES.md.
        private const string CreditsText =
            "Music: \"Path to Lake Land\"\n" +
            "Alexandr Zhelanov, https://soundcloud.com/alexandr-zhelanov\n" +
            "https://opengameart.org/content/path-to-lake-land\n" +
            "Licensed under CC BY 3.0\n" +
            "https://creativecommons.org/licenses/by/3.0/\n" +
            "\n" +
            "Music: \"Menu/Town Music\"\n" +
            "Gigawowski, https://opengameart.org/users/gigawowski\n" +
            "https://opengameart.org/content/menutown-music\n" +
            "Licensed under CC BY 4.0\n" +
            "https://creativecommons.org/licenses/by/4.0/\n" +
            "\n" +
            "Sound: \"Running water\"\n" +
            "Arthur, https://opengameart.org/content/running-water\n" +
            "Licensed under CC BY-SA 3.0\n" +
            "https://creativecommons.org/licenses/by-sa/3.0/\n" +
            "\n" +
            "Sound: \"Menu Selection Click\"\n" +
            "NenadSimic, https://opengameart.org/content/menu-selection-click\n" +
            "Licensed under CC BY 3.0\n" +
            "https://creativecommons.org/licenses/by/3.0/\n" +
            "\n" +
            "Sound: \"Grass Foot Step Sounds (Yo Frankie!)\"\n" +
            "Blender Foundation, https://opengameart.org/content/grass-foot-step-sounds-yo-frankie\n" +
            "Licensed under CC BY 3.0, converted from FLAC to WAV\n" +
            "https://creativecommons.org/licenses/by/3.0/\n" +
            "\n" +
            "Sounds: \"Punch, slap, n' kick\"\n" +
            "CGEffex, http://freesound.org/people/CGEffex/sounds/98341/\n" +
            "Split into separate sounds by congusbongus, https://opengameart.org/content/punch-slap-n-kick\n" +
            "Licensed under CC BY 3.0\n" +
            "https://creativecommons.org/licenses/by/3.0/";

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

            CreateUICamera(SceneLayout.SkyColor);
            MusicBuilder.CreateMenuMusic();
            CreateEventSystem();
            Transform canvas = CreateCanvas("Menu", 0);
            CreateScreens(canvas);

            SaveGeneratedScene(scene, scenePath);

            Debug.Log($"Main menu created at {scenePath}. Open it and press Play to start from the menu.");
        }

        private static void CreateScreens(Transform canvas)
        {
            DefaultControls.Resources resources = GetUIResources();

            GameObject mainPanel = CreateScreenPanel(canvas, "Main Panel", PlayerSettings.productName, height: 470f);
            // Fits its buttons, as how many are showing depends on whether there is a save.
            mainPanel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Button continueGame = CreateMenuButton(resources, mainPanel.transform, "Continue");
            Button newTree = CreateMenuButton(resources, mainPanel.transform, "New Tree");
            Button settings = CreateMenuButton(resources, mainPanel.transform, "Settings");
            Button credits = CreateMenuButton(resources, mainPanel.transform, "Credits");
            Button quit = CreateMenuButton(resources, mainPanel.transform, "Quit");

            GameObject settingsPanel = CreateSettingsPanel(canvas);
            GameObject creditsPanel = CreateTextPagePanel(canvas, "Credits Panel", "Credits", CreditsText);
            Button settingsBack = CreateMenuButton(resources, settingsPanel.transform, "Back");
            Button creditsBack = CreateMenuButton(resources, creditsPanel.transform, "Back");

            GameObject newTreePanel = CreateNewTreePanel(canvas, resources, out GameObject saveWarning,
                out Button cancelNewTree, out Button grow);

            var saveDependentButtons = canvas.gameObject.AddComponent<SaveDependentButtons>();
            SetReferenceArray(saveDependentButtons, "shownWithSave", continueGame.gameObject, saveWarning);

            var menu = canvas.gameObject.AddComponent<MenuPanels>();
            SetReferenceArray(menu, "panels", mainPanel, settingsPanel, creditsPanel, newTreePanel);
            SetReferences(menu, ("startPanel", mainPanel));
            SetReferences(canvas.gameObject.AddComponent<MenuBackInput>(),
                ("backAction", FindActionReference("UI", "Cancel")),
                ("panels", menu),
                ("homePanel", mainPanel));

            ConnectPanelButton(settings, menu, settingsPanel);
            ConnectPanelButton(credits, menu, creditsPanel);
            ConnectPanelButton(settingsBack, menu, mainPanel);
            ConnectPanelButton(creditsBack, menu, mainPanel);
            ConnectPanelButton(newTree, menu, newTreePanel);
            ConnectPanelButton(cancelNewTree, menu, mainPanel);
            ConnectLoadSceneButton(continueGame, BuildSceneList.TreeGamePath, startFromSave: true);
            ConnectLoadSceneButton(grow, BuildSceneList.TreeGamePath);
            quit.gameObject.AddComponent<QuitGameButton>();
            AddClickSound(canvas);

            // Saved hidden so only the main panel shows from the first frame.
            settingsPanel.SetActive(false);
            creditsPanel.SetActive(false);
            newTreePanel.SetActive(false);
        }

        /// <summary>
        /// The New Tree screen: an optional tree code (empty grows a random tree), whether it can grow, a warning shown
        /// only when there is a save it would replace, and Cancel and Grow.
        /// </summary>
        private static GameObject CreateNewTreePanel(Transform canvas, DefaultControls.Resources resources,
            out GameObject saveWarning, out Button cancel, out Button grow)
        {
            GameObject panel = CreateScreenPanel(canvas, "New Tree Panel", "New Tree", height: 520f);
            // Fits what shows, as the warning only shows with a save.
            panel.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text fieldLabel = CreateText("Tree Code Label", 22f, Color.white, TextAlignmentOptions.MidlineLeft);
            AddToLayout(fieldLabel.gameObject, panel.transform, 30f);
            fieldLabel.text = "Tree code (optional)";

            // First on the screen, so it is selected when the screen opens and a code can be typed or pasted at once.
            TMP_InputField codeField = CreateInputField("Tree Code Field", 26f, "Leave empty for a random tree");
            AddToLayout(codeField.gameObject, panel.transform, 56f);
            codeField.characterLimit = 20;

            TMP_Text status = CreateText("Tree Code Status", 22f, Color.white, TextAlignmentOptions.Center);
            AddToLayout(status.gameObject, panel.transform, 34f);

            TMP_Text warning = CreateText("Save Warning", 22f, new Color(1f, 0.85f, 0.6f), TextAlignmentOptions.Center);
            AddToLayout(warning.gameObject, panel.transform, 60f);
            warning.text = "Your current save will be replaced when this game saves.";
            saveWarning = warning.gameObject;

            cancel = CreateMenuButton(resources, panel.transform, "Cancel");
            grow = CreateMenuButton(resources, panel.transform, "Grow");

            SetReferences(panel.AddComponent<NewTreeScreen>(),
                ("codeField", codeField),
                ("statusLabel", status),
                ("growButton", grow));

            return panel;
        }
    }
}
