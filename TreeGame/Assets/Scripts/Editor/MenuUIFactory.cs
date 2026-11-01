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

        /// <summary>A centered panel with a large title at the top; buttons and text stack below it.</summary>
        public static GameObject CreateScreenPanel(Transform canvas, string objectName, string title, float height)
        {
            var center = new Vector2(0.5f, 0.5f);
            GameObject panel = CreatePanel(canvas, objectName, center, Vector2.zero, new Vector2(PanelWidth, height));

            TMP_Text titleLabel = CreateText("Title", 52f, Color.white, TextAlignmentOptions.Center);
            AddToLayout(titleLabel.gameObject, panel.transform, TitleHeight);
            titleLabel.fontStyle = FontStyles.Bold;
            titleLabel.text = title;

            return panel;
        }

        /// <summary>A screen with a title and a block of text, ready for its buttons.</summary>
        public static GameObject CreateInfoPanel(Transform canvas, string objectName, string title, string body, float height = 320f,
            float bodyFontSize = 26f, float bodyHeight = 120f)
        {
            GameObject panel = CreateScreenPanel(canvas, objectName, title, height);

            TMP_Text bodyLabel = CreateText("Body", bodyFontSize, Color.white, TextAlignmentOptions.Center);
            AddToLayout(bodyLabel.gameObject, panel.transform, bodyHeight);
            bodyLabel.text = body;

            return panel;
        }

        /// <summary>The settings screen, the same in every menu. The caller adds its Back button.</summary>
        public static GameObject CreateSettingsPanel(Transform canvas)
        {
            GameObject panel = CreateScreenPanel(canvas, "Settings Panel", "Settings", height: 380f);

            TMP_Dropdown resolution = CreateLabeledDropdown(panel.transform, "Resolution");
            TMP_Dropdown displayMode = CreateLabeledDropdown(panel.transform, "Display Mode");
            SetReferences(panel.AddComponent<SettingsPanel>(),
                ("resolutionDropdown", resolution),
                ("displayModeDropdown", displayMode));

            return panel;
        }

        private static TMP_Dropdown CreateLabeledDropdown(Transform panel, string caption)
        {
            TMP_Text label = CreateText($"{caption} Label", 22f, Color.white, TextAlignmentOptions.MidlineLeft);
            AddToLayout(label.gameObject, panel, 32f);
            label.text = caption;

            TMP_Dropdown dropdown = CreateDropdown($"{caption} Dropdown", 22f);
            AddToLayout(dropdown.gameObject, panel, 44f);
            return dropdown;
        }

        public static Button CreateMenuButton(DefaultControls.Resources resources, Transform panel, string caption)
        {
            Button button = CreateButton(resources, panel, $"{caption} Button", caption, ButtonFontSize);
            AddToLayout(button.gameObject, panel, ButtonHeight);
            return button;
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
