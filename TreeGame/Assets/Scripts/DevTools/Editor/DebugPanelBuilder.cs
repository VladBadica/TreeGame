using TMPro;
using TreeGame.EditorTools;
using TreeGame.Trees;
using UnityEngine;
using UnityEngine.UI;
using static TreeGame.EditorTools.BuilderHelpers;
using static TreeGame.EditorTools.UIFactory;

namespace TreeGame.DevTools
{
    /// <summary>Builds the development-only growth panel. Kept with the dev tools so they can be removed together.</summary>
    internal static class DebugPanelBuilder
    {
        public static void CreateDebugPanel(Transform canvas, TreeGrowth treeGrowth)
        {
            DefaultControls.Resources resources = GetUIResources();
            GameObject panel = CreatePanel(canvas, "Growth Debug Panel", new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(420f, 160f));

            TMP_Text label = CreateText("Growth Label", 24f, Color.white, TextAlignmentOptions.MidlineLeft);
            AddToLayout(label.gameObject, panel.transform, 32f);
            label.text = "Growth: 0%";

            Slider slider = DefaultControls.CreateSlider(resources).GetComponent<Slider>();
            slider.name = "Growth Slider";
            AddToLayout(slider.gameObject, panel.transform, 24f);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = treeGrowth.Growth;

            var buttonRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            AddToLayout(buttonRow, panel.transform, 40f);
            var rowLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10f;
            rowLayout.childControlWidth = rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = true;

            // The panel sets the real caption from its growth step at runtime.
            Button addButton = CreateButton(resources, buttonRow.transform, "Add Growth Button", "Grow");
            Button resetButton = CreateButton(resources, buttonRow.transform, "Reset Button", "Reset");

            // Mouse-only: otherwise a selected slider would also react to the arrow keys used for walking.
            var noNavigation = new Navigation { mode = Navigation.Mode.None };
            slider.navigation = noNavigation;
            addButton.navigation = noNavigation;
            resetButton.navigation = noNavigation;

            var debugPanel = panel.AddComponent<GrowthDebugPanel>();
            SetReferences(debugPanel,
                ("treeGrowth", treeGrowth),
                ("growthSlider", slider),
                ("addGrowthButton", addButton),
                ("resetButton", resetButton),
                ("growthLabel", label));
        }
    }
}
