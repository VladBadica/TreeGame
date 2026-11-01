using TMPro;
using TreeGame.Care;
using TreeGame.Interaction;
using TreeGame.Items;
using TreeGame.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static TreeGame.EditorTools.BuilderHelpers;
using static TreeGame.EditorTools.UIFactory;

namespace TreeGame.EditorTools
{
    /// <summary>Builds the game scene's HUD: resource bars, prompts and the inventory bar.</summary>
    internal static class HudBuilder
    {
        public static void CreateResourceHud(Transform canvas, TreeResource water, TreeResource nutrients)
        {
            GameObject panel = CreatePanel(canvas, "Tree Needs", new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(360f, 120f));
            CreateResourceBar(panel.transform, water, new Color(0.36f, 0.64f, 0.93f));
            CreateResourceBar(panel.transform, nutrients, new Color(0.9f, 0.68f, 0.28f));
        }

        private static void CreateResourceBar(Transform parent, TreeResource resource, Color fillColor)
        {
            var bar = new GameObject($"{resource.Type} Bar", typeof(RectTransform), typeof(Image));
            AddToLayout(bar, parent, 38f);
            StyleImage(bar.GetComponent<Image>(), new Color(0f, 0f, 0f, 0.35f));

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            RectTransform fillRect = StretchInParent(fill, bar.transform);
            StyleImage(fill.GetComponent<Image>(), fillColor);

            TMP_Text label = CreateText("Label", 20f, Color.white, TextAlignmentOptions.Center);
            StretchInParent(label.gameObject, bar.transform);
            label.fontStyle = FontStyles.Bold;
            label.text = resource.Type.ToString(); // Placeholder; the bar formats the value at runtime.

            var fillBar = bar.AddComponent<FillBar>();
            SetReferences(fillBar, ("fill", fillRect));
            SetReferences(bar.AddComponent<ResourceBar>(),
                ("resource", resource),
                ("bar", fillBar),
                ("label", label));
        }

        /// <summary>The prompt above whatever the player can interact with (F, or a named key such as 1).</summary>
        public static void CreateInteractionPrompt(Transform canvas, Interactor interactor, Camera viewCamera)
        {
            // One popup per line: the main prompt plus one extra (e.g. the tree's "Press 2 to feed").
            const int lineCount = 2;
            var popups = new WorldPopup[lineCount];
            for (int i = 0; i < lineCount; i++)
            {
                popups[i] = CreateWorldPopup(canvas, i == 0 ? "Interaction Prompt" : $"Interaction Prompt {i + 1}", viewCamera);
            }

            var prompt = popups[0].gameObject.AddComponent<InteractionPrompt>();
            SetReferences(prompt, ("interactor", interactor));
            SetReferenceArray(prompt, "popups", popups);
        }

        /// <summary>The prompt above the nearest item the player can pick up with the collect key (E).</summary>
        public static void CreateGatherPrompt(Transform canvas, ItemCollector collector, Camera viewCamera)
        {
            WorldPopup popup = CreateWorldPopup(canvas, "Gather Prompt", viewCamera);
            SetReferences(popup.gameObject.AddComponent<GatherPrompt>(),
                ("collector", collector),
                ("popup", popup));
        }

        /// <summary>A small rounded text popup that follows a point in the world; prompts decide its text.</summary>
        private static WorldPopup CreateWorldPopup(Transform canvas, string objectName, Camera viewCamera)
        {
            // The components live on an always-active object; they show and hide the panel child.
            var root = new GameObject(objectName, typeof(RectTransform));
            StretchInParent(root, canvas);

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(root.transform, false);
            // Anchored at one point and pivoted at its bottom center, so it sits just above the target position.
            panelRect.anchorMin = panelRect.anchorMax = Vector2.zero;
            panelRect.pivot = new Vector2(0.5f, 0f);
            StyleImage(panel.GetComponent<Image>(), new Color(0f, 0f, 0f, 0.6f));

            var layout = panel.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 8, 8);
            layout.childControlWidth = layout.childControlHeight = true;
            var fitter = panel.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text label = CreateText("Label", 24f, Color.white, TextAlignmentOptions.Center);
            label.transform.SetParent(panelRect, false);
            label.text = "Press F to interact"; // Placeholder; prompts build the real text from the binding.
            panel.SetActive(false); // Saved hidden; prompts show it when there is something to say.

            var popup = root.AddComponent<WorldPopup>();
            SetReferences(popup,
                ("viewCamera", viewCamera),
                ("panel", panelRect),
                ("label", label));
            return popup;
        }

        /// <summary>
        /// A row of square slots centered at the bottom of the screen. Only one inactive template slot is built here;
        /// the bar copies it once per inventory slot at startup.
        /// </summary>
        public static void CreateInventoryBar(Transform canvas, Inventory inventory)
        {
            const float slotSize = 72f;

            var bar = new GameObject("Inventory Bar", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            var barRect = (RectTransform)bar.transform;
            barRect.SetParent(canvas, false);
            barRect.anchorMin = barRect.anchorMax = barRect.pivot = new Vector2(0.5f, 0f);
            barRect.anchoredPosition = new Vector2(0f, 20f);

            var layout = bar.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            // Grows with the number of slots and stays centered thanks to the bottom-center pivot.
            var fitter = bar.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var slot = new GameObject("Slot Template", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            slot.transform.SetParent(barRect, false);
            var slotLayout = slot.GetComponent<LayoutElement>();
            slotLayout.preferredWidth = slotLayout.preferredHeight = slotSize;
            var background = slot.GetComponent<Image>();
            StyleImage(background, new Color(0f, 0f, 0f, 0.45f));

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            RectTransform iconRect = StretchInParent(iconObject, slot.transform);
            // Extra room at the bottom for the fill label, and at the top for the key number.
            iconRect.offsetMin = new Vector2(12f, 16f);
            iconRect.offsetMax = new Vector2(-12f, -8f);
            var icon = iconObject.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.enabled = false; // Empty until an item is placed in the slot.

            TMP_Text keyLabel = CreateText("Key", 18f, new Color(1f, 1f, 1f, 0.8f), TextAlignmentOptions.TopLeft);
            RectTransform keyRect = StretchInParent(keyLabel.gameObject, slot.transform);
            keyRect.offsetMin = new Vector2(7f, 0f);
            keyRect.offsetMax = new Vector2(0f, -4f);
            keyLabel.text = "1"; // Placeholder; each copy gets its own number at runtime.

            // Inside the square along its bottom edge, so it reads as part of the slot.
            TMP_Text displayTextLabel = CreateText("Display Text", 16f, Color.white, TextAlignmentOptions.Bottom);
            RectTransform displayTextRect = StretchInParent(displayTextLabel.gameObject, slot.transform);
            displayTextRect.offsetMin = new Vector2(0f, 3f);
            displayTextLabel.fontStyle = FontStyles.Bold;
            displayTextLabel.text = "";
            displayTextLabel.enabled = false; // Shown only for items that have display text.

            var slotView = slot.AddComponent<InventorySlotView>();
            SetReferences(slotView,
                ("backgroundFlash", slot.AddComponent<ImageFlash>()),
                ("icon", icon),
                ("keyLabel", keyLabel),
                ("displayTextLabel", displayTextLabel));
            slot.SetActive(false);

            SetReferences(bar.AddComponent<InventoryBar>(),
                ("inventory", inventory),
                ("slotTemplate", slotView));
        }
    }
}
