using TMPro;
using TreeGame.Care;
using TreeGame.Interaction;
using TreeGame.Items;
using TreeGame.Pests;
using TreeGame.Saving;
using TreeGame.Trees;
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
        public static void CreateResourceHud(Transform canvas, TreeResource health, TreeResource water, TreeResource nutrients)
        {
            GameObject panel = CreatePanel(canvas, "Tree Needs", new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(360f, 168f));
            CreateResourceBar(panel.transform, health, new Color(0.85f, 0.36f, 0.38f));
            CreateResourceBar(panel.transform, water, new Color(0.36f, 0.64f, 0.93f));
            CreateResourceBar(panel.transform, nutrients, new Color(0.9f, 0.68f, 0.28f));
        }

        /// <summary>
        /// A row of badges just under the tree's needs, at its right edge, one per penalty slowing the tree's growth: a
        /// down arrow over the seconds left, so two penalties at once show two arrows. Hidden badges take no room, so
        /// whichever are showing sit at the right edge. Hovering over a badge says what it means.
        /// </summary>
        public static void CreatePenaltyBadges(Transform canvas, OverflowPenalty overwatering, OverflowPenalty overfeeding)
        {
            // Under the Tree Needs panel (24 from the top, 168 tall), with a small gap.
            var row = new GameObject("Growth Penalties", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            var rowRect = (RectTransform)row.transform;
            rowRect.SetParent(canvas, false);
            rowRect.anchorMin = rowRect.anchorMax = rowRect.pivot = new Vector2(1f, 1f);
            rowRect.anchoredPosition = new Vector2(-24f, -204f);
            rowRect.sizeDelta = new Vector2(360f, 84f);
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperRight;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;

            // Overwatering last, so on its own it sits at the right edge, where it always has.
            CreatePenaltyBadge(rowRect, overfeeding, "Overfed, slower growth", new Color(0.55f, 0.38f, 0.16f, 0.85f));
            CreatePenaltyBadge(rowRect, overwatering, "Overwatered, slower growth", new Color(0.16f, 0.32f, 0.55f, 0.85f));
        }

        /// <summary>One penalty's badge in the row, hidden until the penalty is active. Its component lives on the always-active row.</summary>
        private static void CreatePenaltyBadge(RectTransform row, OverflowPenalty penalty, string tooltipText, Color color)
        {
            var badge = new GameObject($"{penalty.ResourceType} Penalty Badge", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            var badgeRect = (RectTransform)badge.transform;
            badgeRect.SetParent(row, false);
            var size = badge.GetComponent<LayoutElement>();
            size.preferredWidth = 76f;
            size.preferredHeight = 84f;
            // Takes raycasts, so the pointer hovering over it can be noticed.
            StyleImage(badge.GetComponent<Image>(), color);

            var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            var arrowRect = (RectTransform)arrow.transform;
            arrowRect.SetParent(badgeRect, false);
            arrowRect.anchorMin = arrowRect.anchorMax = arrowRect.pivot = new Vector2(0.5f, 1f);
            arrowRect.anchoredPosition = new Vector2(0f, -8f);
            arrowRect.sizeDelta = new Vector2(44f, 44f);
            var arrowImage = arrow.GetComponent<Image>();
            arrowImage.sprite = PlaceholderArt.GetOrCreateDownArrowIcon();
            arrowImage.preserveAspect = true;
            arrowImage.raycastTarget = false;

            TMP_Text label = CreateText("Time Left", 22f, Color.white, TextAlignmentOptions.Bottom);
            RectTransform labelRect = StretchInParent(label.gameObject, badgeRect);
            labelRect.offsetMin = new Vector2(0f, 6f);
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;
            label.text = "10s"; // Placeholder; the component sets the real time left at runtime.

            GameObject tooltip = CreateTooltip(badgeRect, tooltipText);
            SetReferences(badge.AddComponent<HoverTooltip>(), ("tooltip", tooltip));
            badge.SetActive(false); // Saved hidden; shown only while the penalty is active.

            SetReferences(row.gameObject.AddComponent<OverflowPenaltyStatus>(),
                ("penalty", penalty),
                ("badge", badge),
                ("label", label));
        }

        /// <summary>
        /// A dark text box to the left of a HUD element, hidden until something shows it. It takes no raycasts, so it never
        /// steals the hover from the element it describes.
        /// </summary>
        private static GameObject CreateTooltip(RectTransform owner, string text)
        {
            var tooltip = new GameObject("Tooltip", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            var tooltipRect = (RectTransform)tooltip.transform;
            tooltipRect.SetParent(owner, false);
            tooltipRect.anchorMin = tooltipRect.anchorMax = new Vector2(0f, 0.5f);
            tooltipRect.pivot = new Vector2(1f, 0.5f);
            tooltipRect.anchoredPosition = new Vector2(-10f, 0f);
            var background = tooltip.GetComponent<Image>();
            StyleImage(background, new Color(0f, 0f, 0f, 0.75f));
            background.raycastTarget = false;

            // Sized to its text, growing to the left from the owner.
            var layout = tooltip.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 8, 8);
            layout.childControlWidth = layout.childControlHeight = true;
            var fitter = tooltip.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text label = CreateText("Label", 22f, Color.white, TextAlignmentOptions.Center);
            label.transform.SetParent(tooltipRect, false);
            label.raycastTarget = false;
            label.text = text;

            tooltip.SetActive(false);
            return tooltip;
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

        /// <summary>A short message at the top of the screen saying the game saved, between the panels in the corners.</summary>
        public static void CreateSaveMessage(Transform canvas, GameSaver saver)
        {
            // The component lives on an always-active object; it shows and hides the panel child.
            var root = new GameObject("Save Message", typeof(RectTransform));
            StretchInParent(root, canvas);

            GameObject panel = CreatePanel(root.transform, "Panel", new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(340f, 64f));
            TMP_Text label = CreateText("Label", 26f, Color.white, TextAlignmentOptions.Center);
            AddToLayout(label.gameObject, panel.transform, 36f);
            label.text = "Game saved"; // Placeholder; the component sets the real text at runtime.
            panel.SetActive(false); // Saved hidden; shown only after a save.

            SetReferences(root.AddComponent<SaveMessage>(),
                ("saver", saver),
                ("panel", panel),
                ("label", label));
        }

        /// <summary>The bubble at the screen's edge that tells the player about insects on a tree that is out of view.</summary>
        public static void CreateOffScreenInsectBubble(Transform canvas, InsectInfestation infestation, TreeVisuals tree,
            Camera viewCamera, Sprite insectSprite, Sprite bubbleSprite)
        {
            const float bubbleSize = 96f;
            const float outlineWidth = 4f;

            // The component lives on an always-active object; it shows and hides the bubble child.
            var root = new GameObject("Off-Screen Insect Bubble", typeof(RectTransform));
            StretchInParent(root, canvas);

            var bubble = new GameObject("Bubble", typeof(RectTransform), typeof(Image));
            var bubbleRect = (RectTransform)bubble.transform;
            bubbleRect.SetParent(root.transform, false);
            bubbleRect.sizeDelta = Vector2.one * bubbleSize;
            StyleBubbleImage(bubble.GetComponent<Image>(), bubbleSprite, new Color(0.16f, 0.2f, 0.25f));

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            RectTransform fillRect = StretchInParent(fill, bubbleRect);
            fillRect.offsetMin = Vector2.one * outlineWidth;
            fillRect.offsetMax = Vector2.one * -outlineWidth;
            StyleBubbleImage(fill.GetComponent<Image>(), bubbleSprite, Color.white);

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            RectTransform iconRect = StretchInParent(iconObject, bubbleRect);
            // Extra room at the bottom for the count.
            iconRect.offsetMin = new Vector2(18f, 24f);
            iconRect.offsetMax = new Vector2(-18f, -12f);
            var icon = iconObject.GetComponent<Image>();
            StyleBubbleImage(icon, insectSprite, Color.white);
            icon.preserveAspect = true;

            TMP_Text count = CreateText("Count", 20f, new Color(0.16f, 0.2f, 0.25f), TextAlignmentOptions.Bottom);
            RectTransform countRect = StretchInParent(count.gameObject, bubbleRect);
            countRect.offsetMin = new Vector2(0f, 6f);
            count.fontStyle = FontStyles.Bold;
            count.raycastTarget = false;
            count.text = "x2"; // Placeholder; the component sets the real count at runtime.

            bubble.SetActive(false); // Saved hidden; shown only while the tree is out of view.

            SetReferences(root.AddComponent<OffScreenInsectBubble>(),
                ("infestation", infestation),
                ("tree", tree),
                ("viewCamera", viewCamera),
                ("bubble", bubbleRect),
                ("countLabel", count));
        }

        /// <summary>Takes no clicks, so clicks on the world still work through the bubble.</summary>
        private static void StyleBubbleImage(Image image, Sprite sprite, Color color)
        {
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
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
