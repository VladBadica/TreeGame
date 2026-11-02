using System.IO;
using TMPro;
using TreeGame.Menus;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static TreeGame.EditorTools.BuilderHelpers;
using static TreeGame.EditorTools.UIFactory;

namespace TreeGame.EditorTools
{
    /// <summary>Building blocks shared by the main menu and the pause menu, so both look and work the same.</summary>
    internal static class MenuUIFactory
    {
        private const float TitleHeight = 90f;
        private const float ButtonHeight = 60f;
        private const float ButtonFontSize = 28f;
        private const float PanelWidth = 480f;
        private const int PagePadding = 40;
        private const float KeybindsWidth = 640f;
        private const float KeybindRowHeight = 36f;
        private const float KeybindFontSize = 24f;

        /// <summary>A centered panel with a large title at the top; buttons and text stack below it.</summary>
        public static GameObject CreateScreenPanel(Transform canvas, string objectName, string title, float height)
        {
            var center = new Vector2(0.5f, 0.5f);
            GameObject panel = CreatePanel(canvas, objectName, center, Vector2.zero, new Vector2(PanelWidth, height));
            CreateTitle(panel.transform, title);
            return panel;
        }

        /// <summary>A screen with a title and a block of text, ready for its buttons.</summary>
        public static GameObject CreateInfoPanel(Transform canvas, string objectName, string title, string body, float height = 320f)
        {
            GameObject panel = CreateScreenPanel(canvas, objectName, title, height);

            TMP_Text bodyLabel = CreateText("Body", 26f, Color.white, TextAlignmentOptions.Center);
            AddToLayout(bodyLabel.gameObject, panel.transform, 120f);
            bodyLabel.text = body;

            return panel;
        }

        /// <summary>
        /// A full-screen page with a block of text under its title. The text takes up the spare height, which pushes
        /// a button added by the caller to the bottom.
        /// </summary>
        public static GameObject CreateTextPagePanel(Transform canvas, string objectName, string title, string body)
        {
            GameObject panel = CreatePagePanel(canvas, objectName, title);

            TMP_Text bodyLabel = CreateText("Body", 26f, Color.white, TextAlignmentOptions.Top);
            AddToLayout(bodyLabel.gameObject, panel.transform, 0f);
            var layoutElement = bodyLabel.GetComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1f;
            layoutElement.flexibleHeight = 1f;
            bodyLabel.text = body;

            return panel;
        }

        /// <summary>
        /// The settings page, the same in every menu, with a tab for general settings and one listing the keybinds. The
        /// caller adds its Back button, which ends up at the bottom.
        /// </summary>
        public static GameObject CreateSettingsPanel(Transform canvas)
        {
            GameObject panel = CreatePagePanel(canvas, "Settings Panel", "Settings");

            var tabRow = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            AddControlToLayout(tabRow, panel.transform, 52f);
            var tabLayout = tabRow.GetComponent<HorizontalLayoutGroup>();
            tabLayout.spacing = 10f;
            tabLayout.childControlWidth = tabLayout.childControlHeight = true;
            tabLayout.childForceExpandWidth = tabLayout.childForceExpandHeight = true;
            KeepToPreferredSize(tabRow);
            DefaultControls.Resources resources = GetUIResources();
            Button generalTab = CreateButton(resources, tabRow.transform, "General Tab", "General", 26f);
            Button keybindsTab = CreateButton(resources, tabRow.transform, "Keybinds Tab", "Keybinds", 26f);

            GameObject general = CreateGeneralPage(panel);
            GameObject keybinds = CreateKeybindsPage(panel.transform);
            keybinds.SetActive(false); // Saved hidden; the first tab shows when the screen opens.

            var tabs = panel.AddComponent<MenuTabs>();
            SetReferenceArray(tabs, "tabs", generalTab, keybindsTab);
            SetReferenceArray(tabs, "pages", general, keybinds);

            return panel;
        }

        private static GameObject CreateGeneralPage(GameObject panel)
        {
            GameObject page = CreateTabPage(panel.transform, "General Page", PanelWidth);

            TMP_Dropdown resolution = CreateLabeledDropdown(page.transform, "Resolution");
            TMP_Dropdown displayMode = CreateLabeledDropdown(page.transform, "Display Mode");
            Slider volume = CreateLabeledSlider(page.transform, "Volume", out TMP_Text volumeLabel);

            // On the screen rather than the page, so settings are read when the screen opens, whichever tab is showing.
            SetReferences(panel.AddComponent<SettingsPanel>(),
                ("resolutionDropdown", resolution),
                ("displayModeDropdown", displayMode),
                ("volumeSlider", volume),
                ("volumeLabel", volumeLabel));

            return page;
        }

        /// <summary>
        /// A plain list of what each key does. Actions with one key read it from their binding. The rest are written out,
        /// because their bindings don't say it well: Move also has the up and down keys the game doesn't use, the
        /// hotbar is nine separate keys, and clicking and Esc are bound by more general rules.
        /// </summary>
        private static GameObject CreateKeybindsPage(Transform panel)
        {
            GameObject page = CreateTabPage(panel, "Keybinds Page", KeybindsWidth);

            CreateKeybindHeading(page.transform, "In the game");
            CreateKeybindRow(page.transform, "Walk", "A / D or Left / Right");
            CreateKeybindRow(page.transform, "Interact", FindActionReference("Player", "Interact"));
            CreateKeybindRow(page.transform, "Gather leaves", FindActionReference("Player", "Collect"));
            CreateKeybindRow(page.transform, "Use an item", "1 to 9");
            CreateKeybindRow(page.transform, "Click things in the world", "Left click");
            CreateKeybindRow(page.transform, "Pause / go back", "Esc");
            CreateKeybindRow(page.transform, "Quick save", FindActionReference("Player", "QuickSave"));
            CreateKeybindRow(page.transform, "Quick load", FindActionReference("Player", "QuickLoad"));

            CreateKeybindHeading(page.transform, "In minigames");
            CreateKeybindRow(page.transform, "Well: spin the wheel", FindActionReference("WellSpin", "Press", WellSpinSceneBuilder.InputActionsPath));
            CreateKeybindRow(page.transform, "Insects: shoo them away", "Left click");
            CreateKeybindRow(page.transform, "Leave the minigame", FindActionReference("WellSpin", "Quit", WellSpinSceneBuilder.InputActionsPath));

            return page;
        }

        private static void CreateKeybindHeading(Transform page, string heading)
        {
            TMP_Text label = CreateText($"{heading} Heading", 26f, Color.white, TextAlignmentOptions.BottomLeft);
            AddToLayout(label.gameObject, page, KeybindRowHeight + 8f);
            label.fontStyle = FontStyles.Bold;
            label.text = heading;
        }

        private static void CreateKeybindRow(Transform page, string description, string keys)
        {
            CreateKeybindRowLabels(page, description).text = keys;
        }

        private static void CreateKeybindRow(Transform page, string description, Object action)
        {
            TMP_Text key = CreateKeybindRowLabels(page, description);
            key.text = description; // Placeholder; the label shows the bound key at runtime.
            SetReferences(key.gameObject.AddComponent<KeybindLabel>(),
                ("action", action),
                ("label", key));
        }

        /// <summary>A row with the action on the left and an empty label for its key on the right, which it returns.</summary>
        private static TMP_Text CreateKeybindRowLabels(Transform page, string description)
        {
            var row = new GameObject($"{description} Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            AddToLayout(row, page, KeybindRowHeight);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = true;
            row.GetComponent<LayoutElement>().flexibleHeight = 0f;

            TMP_Text action = CreateText("Action", KeybindFontSize, Color.white, TextAlignmentOptions.MidlineLeft);
            action.transform.SetParent(row.transform, false);
            action.text = description;

            TMP_Text key = CreateText("Key", KeybindFontSize, Color.white, TextAlignmentOptions.MidlineRight);
            key.transform.SetParent(row.transform, false);
            key.fontStyle = FontStyles.Bold;
            return key;
        }

        /// <summary>
        /// Stops a row that stretches its children from growing itself. Stretching children makes its layout group report
        /// that it can grow, so without this the row takes a share of any spare room on the screen.
        /// </summary>
        private static void KeepToPreferredSize(GameObject row)
        {
            var layoutElement = row.GetComponent<LayoutElement>();
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
        }

        /// <summary>Takes up the screen's spare height, which pushes a button added by the caller to the bottom.</summary>
        private static GameObject CreateTabPage(Transform panel, string objectName, float width)
        {
            var page = new GameObject(objectName, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            page.transform.SetParent(panel, false);

            var layout = page.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var layoutElement = page.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.flexibleHeight = 1f;
            return page;
        }

        /// <summary>
        /// A panel filling the whole screen, with its title across the top. Controls keep their menu width and stay
        /// centered, because they would be hard to read and use stretched across a wide screen.
        /// </summary>
        private static GameObject CreatePagePanel(Transform canvas, string objectName, string title)
        {
            GameObject panel = CreatePanel(canvas, objectName, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            StretchInParent(panel, canvas);

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(PagePadding, PagePadding, PagePadding, PagePadding);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childForceExpandWidth = false;

            CreateTitle(panel.transform, title).GetComponent<LayoutElement>().flexibleWidth = 1f;
            return panel;
        }

        private static TMP_Text CreateTitle(Transform panel, string title)
        {
            TMP_Text titleLabel = CreateText("Title", 52f, Color.white, TextAlignmentOptions.Center);
            AddToLayout(titleLabel.gameObject, panel, TitleHeight);
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.text = title;
            return titleLabel;
        }

        private static TMP_Text CreateControlLabel(Transform panel, string caption)
        {
            TMP_Text label = CreateText($"{caption} Label", 22f, Color.white, TextAlignmentOptions.MidlineLeft);
            AddControlToLayout(label.gameObject, panel, 32f);
            label.text = caption;
            return label;
        }

        private static Slider CreateLabeledSlider(Transform panel, string caption, out TMP_Text label)
        {
            label = CreateControlLabel(panel, caption);

            Slider slider = DefaultControls.CreateSlider(GetUIResources()).GetComponent<Slider>();
            slider.name = $"{caption} Slider";
            AddControlToLayout(slider.gameObject, panel, 30f);
            return slider;
        }

        private static TMP_Dropdown CreateLabeledDropdown(Transform panel, string caption)
        {
            CreateControlLabel(panel, caption);

            TMP_Dropdown dropdown = CreateDropdown($"{caption} Dropdown", 22f);
            AddControlToLayout(dropdown.gameObject, panel, 44f);
            return dropdown;
        }

        public static Button CreateMenuButton(DefaultControls.Resources resources, Transform panel, string caption)
        {
            Button button = CreateButton(resources, panel, $"{caption} Button", caption, ButtonFontSize);
            AddControlToLayout(button.gameObject, panel, ButtonHeight);
            return button;
        }

        /// <summary>The width only applies on full-screen pages; smaller panels stretch their controls to fit.</summary>
        private static void AddControlToLayout(GameObject control, Transform panel, float height)
        {
            AddToLayout(control, panel, height);
            control.GetComponent<LayoutElement>().preferredWidth = PanelWidth;
        }

        public static void ConnectPanelButton(Button button, MenuPanels menu, GameObject panel)
        {
            SetReferences(button.gameObject.AddComponent<OpenPanelButton>(),
                ("menu", menu),
                ("panel", panel));
        }

        public static void ConnectLoadSceneButton(Button button, string scenePath, bool startFromSave = false)
        {
            var loadScene = button.gameObject.AddComponent<LoadSceneButton>();
            var serializedLoadScene = new SerializedObject(loadScene);
            serializedLoadScene.FindProperty("sceneName").stringValue = Path.GetFileNameWithoutExtension(scenePath);
            serializedLoadScene.FindProperty("startFromSave").boolValue = startFromSave;
            serializedLoadScene.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
