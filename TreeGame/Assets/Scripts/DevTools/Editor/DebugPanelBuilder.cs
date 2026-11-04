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
            GameObject panel = CreatePanel(canvas, "Growth Debug Panel", new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(420f, 290f));

            // Mouse-only: otherwise a selected control would also react to the arrow keys used for walking.
            var noNavigation = new Navigation { mode = Navigation.Mode.None };

            int partCount = TreeGrowth.Parts.Length;
            var labels = new Object[partCount];
            var sliders = new Object[partCount];
            for (int i = 0; i < partCount; i++)
            {
                TreeGrowthPart part = TreeGrowth.Parts[i];

                TMP_Text label = CreateText($"{part} Label", 22f, Color.white, TextAlignmentOptions.MidlineLeft);
                AddToLayout(label.gameObject, panel.transform, 28f);
                label.text = $"{part}: 0%"; // Placeholder; the panel shows the real growth at runtime.
                labels[i] = label;

                Slider slider = DefaultControls.CreateSlider(resources).GetComponent<Slider>();
                slider.name = $"{part} Slider";
                AddToLayout(slider.gameObject, panel.transform, 20f);
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.value = treeGrowth.GetGrowth(part);
                slider.navigation = noNavigation;
                sliders[i] = slider;
            }

            var buttonRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            AddToLayout(buttonRow, panel.transform, 40f);
            var rowLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10f;
            rowLayout.childControlWidth = rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = true;

            // The panel sets the real caption from its growth step at runtime.
            Button addButton = CreateButton(resources, buttonRow.transform, "Add Growth Button", "Grow");
            Button resetButton = CreateButton(resources, buttonRow.transform, "Reset Button", "Reset");
            addButton.navigation = noNavigation;
            resetButton.navigation = noNavigation;

            var debugPanel = panel.AddComponent<GrowthDebugPanel>();
            SetReferences(debugPanel,
                ("treeGrowth", treeGrowth),
                ("addGrowthButton", addButton),
                ("resetButton", resetButton));
            SetReferenceArray(debugPanel, "sliders", sliders);
            SetReferenceArray(debugPanel, "labels", labels);
        }
    }
}
