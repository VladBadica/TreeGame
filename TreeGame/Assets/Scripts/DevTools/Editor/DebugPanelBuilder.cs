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
        public static void CreateDebugPanel(Transform canvas, GameObject tree)
        {
            var treeGrowth = tree.GetComponent<TreeGrowth>();
            DefaultControls.Resources resources = GetUIResources();
            GameObject panel = CreatePanel(canvas, "Growth Debug Panel", new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(460f, 560f));

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

            CreateRateControls(panel.transform, resources, noNavigation, tree);
        }

        /// <summary>
        /// A row per part with its rate between − and + buttons and its extra growth, a slider under it to set that extra
        /// growth, then a button to clear extra growth.
        /// </summary>
        private static void CreateRateControls(Transform panel, DefaultControls.Resources resources, Navigation noNavigation, GameObject tree)
        {
            int partCount = TreeGrowth.Parts.Length;
            var labels = new Object[partCount];
            var slowerButtons = new Object[partCount];
            var fasterButtons = new Object[partCount];
            var extraSliders = new Object[partCount];
            for (int i = 0; i < partCount; i++)
            {
                TreeGrowthPart part = TreeGrowth.Parts[i];
                var row = new GameObject($"{part} Rate", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                AddToLayout(row, panel, 32f);
                var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 8f;
                rowLayout.childControlWidth = rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = true;

                Button slower = CreateRateButton(resources, row.transform, $"{part} Slower", "-", noNavigation);

                TMP_Text label = CreateText($"{part} Rate Label", 20f, Color.white, TextAlignmentOptions.Center);
                label.transform.SetParent(row.transform, false);
                label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                label.text = $"{part} rate 5 · extra 0%"; // Placeholder; the panel shows the real values at runtime.

                Button faster = CreateRateButton(resources, row.transform, $"{part} Faster", "+", noNavigation);

                // Named "Extra", so it's never mistaken for one of the growth sliders.
                Slider extraSlider = DefaultControls.CreateSlider(resources).GetComponent<Slider>();
                extraSlider.name = $"{part} Extra Slider";
                AddToLayout(extraSlider.gameObject, panel, 18f);
                extraSlider.navigation = noNavigation;

                labels[i] = label;
                slowerButtons[i] = slower;
                fasterButtons[i] = faster;
                extraSliders[i] = extraSlider;
            }

            Button clearExtra = CreateButton(resources, panel, "Clear Extra Button", "Clear extra growth");
            AddToLayout(clearExtra.gameObject, panel, 34f);
            clearExtra.navigation = noNavigation;

            var ratePanel = panel.gameObject.AddComponent<GrowthRateDebugPanel>();
            SetReferences(ratePanel,
                ("growthRates", tree.GetComponent<TreeGrowthRates>()),
                ("extraGrowth", tree.GetComponent<TreeExtraGrowth>()),
                ("clearExtraButton", clearExtra));
            SetReferenceArray(ratePanel, "labels", labels);
            SetReferenceArray(ratePanel, "slowerButtons", slowerButtons);
            SetReferenceArray(ratePanel, "fasterButtons", fasterButtons);
            SetReferenceArray(ratePanel, "extraSliders", extraSliders);
        }

        private static Button CreateRateButton(DefaultControls.Resources resources, Transform row, string objectName, string caption, Navigation noNavigation)
        {
            Button button = CreateButton(resources, row, objectName, caption, 22f);
            button.gameObject.AddComponent<LayoutElement>().preferredWidth = 40f;
            button.navigation = noNavigation;
            return button;
        }
    }
}
