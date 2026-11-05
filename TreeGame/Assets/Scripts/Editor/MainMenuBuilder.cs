using TMPro;
using TreeGame.Core;
using TreeGame.Menus;
using TreeGame.Saving;
using TreeGame.Trees;
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

            MenuTreePreview preview = CreateTreeScene();
            MusicBuilder.CreateMenuMusic();
            CreateEventSystem();
            Transform canvas = CreateCanvas("Menu", 0);
            CreateScreens(canvas, preview);

            SaveGeneratedScene(scene, scenePath);

            Debug.Log($"Main menu created at {scenePath}. Open it and press Play to start from the menu.");
        }

        /// <summary>
        /// The world behind the menu: the game's own stage and hills, with a live tree standing on the right, where the
        /// save slots' trees are shown. The same builders as the game's, so the menu looks like the place the game is set.
        /// </summary>
        private static MenuTreePreview CreateTreeScene()
        {
            GameConfig config = GameAssets.GetOrCreateConfig();
            AssetDatabase.SaveAssets();

            WorldBuilder.CreateStage();
            Transform camera = WorldBuilder.CreateViewCamera(SceneLayout.MenuCameraTarget, SceneLayout.MenuCameraOrthographicSize);
            WorldBuilder.CreateBackdrop(camera);

            GameObject tree = TreeBuilder.CreateTreeModel(config);
            var visuals = tree.GetComponent<TreeVisuals>();

            // Slower than in the game, so a tree visibly grows into place when its slot is shown.
            var serializedVisuals = new SerializedObject(visuals);
            serializedVisuals.FindProperty("growthSmoothTime").floatValue = 0.8f;
            serializedVisuals.ApplyModifiedPropertiesWithoutUndo();

            var preview = tree.AddComponent<MenuTreePreview>();
            SetReferences(preview,
                ("treeSeed", tree.GetComponent<TreeSeed>()),
                ("treeGrowth", tree.GetComponent<TreeGrowth>()),
                ("treeVisuals", visuals));
            return preview;
        }

        private static void CreateScreens(Transform canvas, MenuTreePreview preview)
        {
            DefaultControls.Resources resources = GetUIResources();

            // The whole screen, but see-through: the slots sit on the left, the chosen slot's actions under its tree.
            var mainPanel = new GameObject("Main Panel", typeof(RectTransform));
            StretchInParent(mainPanel, canvas);
            SaveSlotCard[] cards = CreateSlotColumn(mainPanel.transform, resources, out Button settings, out Button credits, out Button quit);
            CreateSlotActions(mainPanel.transform, resources, out Button continueGame, out Button deleteTree, out Button newTree);

            GameObject settingsPanel = CreateSettingsPanel(canvas);
            GameObject creditsPanel = CreateTextPagePanel(canvas, "Credits Panel", "Credits", CreditsText);
            Button settingsBack = CreateMenuButton(resources, settingsPanel.transform, "Back");
            Button creditsBack = CreateMenuButton(resources, creditsPanel.transform, "Back");

            GameObject newTreePanel = CreateNewTreePanel(canvas, resources, out Button cancelNewTree, out Button grow);

            GameObject deletePanel = CreateInfoPanel(canvas, "Delete Panel", "Delete this tree?", "", height: 390f);
            // Cancel comes first, so it is the button selected by default and a stray Enter can't delete a tree.
            Button cancelDelete = CreateMenuButton(resources, deletePanel.transform, "Cancel");
            Button confirmDelete = CreateMenuButton(resources, deletePanel.transform, "Delete");

            var menu = canvas.gameObject.AddComponent<MenuPanels>();
            SetReferenceArray(menu, "panels", mainPanel, settingsPanel, creditsPanel, newTreePanel, deletePanel);

            var slotMenu = canvas.gameObject.AddComponent<SaveSlotMenu>();
            SetReferenceArray(slotMenu, "cards", cards);
            SetReferences(slotMenu,
                ("preview", preview),
                ("continueButton", continueGame),
                ("deleteButton", deleteTree),
                ("newTreeButton", newTree),
                ("panels", menu),
                ("mainPanel", mainPanel),
                ("deletePanel", deletePanel),
                ("deleteQuestion", deletePanel.transform.Find("Body").GetComponent<TMP_Text>()),
                ("confirmDeleteButton", confirmDelete));
            ConnectPanelButton(cancelDelete, menu, mainPanel);
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
            deletePanel.SetActive(false);
        }

        /// <summary>
        /// The left of the main screen: the game's name, a card per save slot, and Settings, Credits and Quit along the
        /// bottom, small, so they don't compete with the trees.
        /// </summary>
        private static SaveSlotCard[] CreateSlotColumn(Transform mainPanel, DefaultControls.Resources resources,
            out Button settings, out Button credits, out Button quit)
        {
            const float cardHeight = 116f;

            GameObject column = CreatePanel(mainPanel, "Save Slots", new Vector2(0f, 0.5f), new Vector2(60f, 0f), new Vector2(560f, 820f));
            var layout = column.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 24, 28);
            layout.spacing = 18f;

            TMP_Text title = CreateText("Title", 56f, Color.white, TextAlignmentOptions.MidlineLeft);
            AddToLayout(title.gameObject, column.transform, 90f);
            title.fontStyle = FontStyles.Bold;
            title.text = PlayerSettings.productName;

            var cards = new SaveSlotCard[SaveSlots.Count];
            for (int i = 0; i < cards.Length; i++)
            {
                // First in the column, so the first card is what the keyboard or gamepad starts on.
                Button button = CreateButton(resources, column.transform, $"Tree {i + 1} Card", $"Tree {i + 1}", 32f);
                AddToLayout(button.gameObject, column.transform, cardHeight);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.margin = new Vector4(24f, 0f, 16f, 0f);

                cards[i] = button.gameObject.AddComponent<SaveSlotCard>();
                SetReferences(cards[i],
                    ("label", label),
                    ("background", button.GetComponent<Image>()));
            }

            // Takes the spare height, pushing the small buttons to the bottom.
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            AddToLayout(spacer, column.transform, 0f);
            spacer.GetComponent<LayoutElement>().flexibleHeight = 1f;

            // Wide, flat bars as wide as the cards, stacked close together: clearly secondary to the cards above them.
            const float barHeight = 44f;
            const float barSpacing = 8f;
            var group = new GameObject("Other Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup));
            AddToLayout(group, column.transform, 3f * barHeight + 2f * barSpacing);
            var groupLayout = group.GetComponent<VerticalLayoutGroup>();
            groupLayout.spacing = barSpacing;
            groupLayout.childControlWidth = groupLayout.childControlHeight = true;
            groupLayout.childForceExpandWidth = true;
            groupLayout.childForceExpandHeight = false;

            settings = CreateBarButton(resources, group.transform, "Settings", barHeight);
            credits = CreateBarButton(resources, group.transform, "Credits", barHeight);
            quit = CreateBarButton(resources, group.transform, "Quit", barHeight);
            return cards;
        }

        private static Button CreateBarButton(DefaultControls.Resources resources, Transform group, string caption, float height)
        {
            Button button = CreateButton(resources, group, $"{caption} Button", caption, 24f);
            AddToLayout(button.gameObject, group, height);
            return button;
        }

        /// <summary>
        /// What can be done with the chosen slot, centered under its tree: Continue and Delete for a tree, New Tree for
        /// an empty slot. Which show is decided at runtime by the slot menu.
        /// </summary>
        private static void CreateSlotActions(Transform mainPanel, DefaultControls.Resources resources,
            out Button continueGame, out Button deleteTree, out Button newTree)
        {
            const float buttonWidth = 240f;

            var row = new GameObject("Slot Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rect = (RectTransform)row.transform;
            rect.SetParent(mainPanel, false);
            // Under the tree, which the menu camera places about three quarters of the way across the screen.
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.73f, 0f);
            rect.anchoredPosition = new Vector2(0f, 48f);
            rect.sizeDelta = new Vector2(2f * buttonWidth + 20f, 64f);

            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = false;

            continueGame = CreateActionButton(resources, row.transform, "Continue", buttonWidth);
            newTree = CreateActionButton(resources, row.transform, "New Tree", buttonWidth);
            deleteTree = CreateActionButton(resources, row.transform, "Delete", buttonWidth);
        }

        private static Button CreateActionButton(DefaultControls.Resources resources, Transform row, string caption, float width)
        {
            Button button = CreateButton(resources, row, $"{caption} Button", caption, 28f);
            button.gameObject.AddComponent<LayoutElement>().preferredWidth = width;
            return button;
        }

        /// <summary>
        /// The New Tree screen, for an empty slot: an optional tree code (empty grows a random tree), whether it can
        /// grow, and Cancel and Grow.
        /// </summary>
        private static GameObject CreateNewTreePanel(Transform canvas, DefaultControls.Resources resources,
            out Button cancel, out Button grow)
        {
            GameObject panel = CreateScreenPanel(canvas, "New Tree Panel", "New Tree", height: 540f);

            // First on the screen, so it is selected when the screen opens and a name can be typed at once. Its hint,
            // the slot's own name, is set when the screen opens.
            TMP_InputField nameField = CreateLabeledField(panel.transform, "Tree Name", "Name (optional)", "Tree 1");
            nameField.characterLimit = TreeNames.MaxLength;

            TMP_InputField codeField = CreateLabeledField(panel.transform, "Tree Code", "Tree code (optional)", "Leave empty for a random tree");
            codeField.characterLimit = 20;

            TMP_Text status = CreateText("Tree Code Status", 22f, Color.white, TextAlignmentOptions.Center);
            AddToLayout(status.gameObject, panel.transform, 34f);

            cancel = CreateMenuButton(resources, panel.transform, "Cancel");
            grow = CreateMenuButton(resources, panel.transform, "Grow");

            SetReferences(panel.AddComponent<NewTreeScreen>(),
                ("nameField", nameField),
                ("codeField", codeField),
                ("statusLabel", status),
                ("growButton", grow));

            return panel;
        }

        /// <summary>A text field with its label above it, both added to a panel's layout.</summary>
        private static TMP_InputField CreateLabeledField(Transform panel, string objectName, string caption, string hint)
        {
            TMP_Text label = CreateText($"{objectName} Label", 22f, Color.white, TextAlignmentOptions.MidlineLeft);
            AddToLayout(label.gameObject, panel, 30f);
            label.text = caption;

            TMP_InputField field = CreateInputField($"{objectName} Field", 26f, hint);
            AddToLayout(field.gameObject, panel, 56f);
            return field;
        }
    }
}
