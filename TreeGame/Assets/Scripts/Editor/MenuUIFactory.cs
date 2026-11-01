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

        /// <summary>The settings page, the same in every menu. The caller adds its Back button, which ends up at the bottom.</summary>
        public static GameObject CreateSettingsPanel(Transform canvas)
        {
            GameObject panel = CreatePagePanel(canvas, "Settings Panel", "Settings");

            TMP_Dropdown resolution = CreateLabeledDropdown(panel.transform, "Resolution");
            TMP_Dropdown displayMode = CreateLabeledDropdown(panel.transform, "Display Mode");
            SetReferences(panel.AddComponent<SettingsPanel>(),
                ("resolutionDropdown", resolution),
                ("displayModeDropdown", displayMode));

            var spacer = new GameObject("Spacer", typeof(RectTransform));
            AddToLayout(spacer, panel.transform, 0f);
            spacer.GetComponent<LayoutElement>().flexibleHeight = 1f;

            return panel;
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

        private static TMP_Dropdown CreateLabeledDropdown(Transform panel, string caption)
        {
            TMP_Text label = CreateText($"{caption} Label", 22f, Color.white, TextAlignmentOptions.MidlineLeft);
            AddControlToLayout(label.gameObject, panel, 32f);
            label.text = caption;

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

        public static void ConnectLoadSceneButton(Button button, string scenePath)
        {
            var loadScene = button.gameObject.AddComponent<LoadSceneButton>();
            var serializedLoadScene = new SerializedObject(loadScene);
            serializedLoadScene.FindProperty("sceneName").stringValue = Path.GetFileNameWithoutExtension(scenePath);
            serializedLoadScene.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
