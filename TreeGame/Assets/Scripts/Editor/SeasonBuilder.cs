using TMPro;
using TreeGame.Care;
using TreeGame.Core;
using TreeGame.Pests;
using TreeGame.Seasons;
using TreeGame.Trees;
using TreeGame.UI;
using TreeGame.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static TreeGame.EditorTools.BuilderHelpers;
using static TreeGame.EditorTools.UIFactory;

namespace TreeGame.EditorTools
{
    /// <summary>
    /// Builds the seasons: the clock that keeps the year, its connections to everything that changes with the season,
    /// the world's seasonal colors, and the season wheel and notices on the HUD.
    /// </summary>
    internal static class SeasonBuilder
    {
        private static readonly Color WoodColor = new Color(0.36f, 0.25f, 0.17f, 0.95f);
        private static readonly Color PaperColor = new Color(0.98f, 0.94f, 0.84f, 0.96f);
        private static readonly Color InkColor = new Color(0.3f, 0.21f, 0.14f);

        /// <summary>
        /// On a root object of its own in the world, so it stops with the rest of the world during a minigame and catches
        /// up with it afterwards.
        /// </summary>
        public static SeasonClock CreateClock(GameConfig config)
        {
            var clock = new GameObject("Seasons").AddComponent<SeasonClock>();
            SetReferences(clock, ("config", config));
            return clock;
        }

        /// <summary>Lets the season change the tree's growth and needs, the insects, the leaves and the composter, and shows it on the tree.</summary>
        public static void ConnectToSeasons(SeasonClock clock, GameObject tree, TreeResource water, TreeResource nutrients, Composter composter)
        {
            SetReferences(tree.GetComponent<PassiveGrowth>(), ("seasons", clock));
            SetReferences(tree.GetComponent<FallenLeafSpawner>(), ("seasons", clock));
            SetReferences(tree.GetComponent<InsectSpawner>(), ("seasons", clock));
            SetReferences(water.GetComponent<ResourceDepletion>(), ("seasons", clock));
            SetReferences(nutrients.GetComponent<ResourceDepletion>(), ("seasons", clock));
            SetReferences(composter, ("seasons", clock));

            SetReferences(tree.AddComponent<TreeSeasonLook>(),
                ("clock", clock),
                ("visuals", tree.GetComponent<TreeVisuals>()));
        }

        /// <summary>
        /// Colors the ground and each layer of hills for the season: fresh in spring, golden in autumn, snowy in winter.
        /// Each keeps its own color in summer, so the further hills stay paler.
        /// </summary>
        public static void ColorScenery(SeasonClock clock, Renderer ground, Transform backdrop)
        {
            AddScenery(clock, ground.gameObject, new[] { ground }, snowiness: 0.88f);
            foreach (Transform layer in backdrop)
            {
                AddScenery(clock, layer.gameObject, layer.GetComponentsInChildren<Renderer>(), snowiness: 0.75f);
            }
        }

        private static void AddScenery(SeasonClock clock, GameObject owner, Renderer[] renderers, float snowiness)
        {
            if (renderers.Length == 0 || renderers[0].sharedMaterial == null)
            {
                return;
            }

            Color summer = renderers[0].sharedMaterial.GetColor("_BaseColor");
            var scenery = owner.AddComponent<SeasonScenery>();
            SetReferences(scenery, ("clock", clock));
            SetReferenceArray(scenery, "renderers", renderers);

            var serialized = new SerializedObject(scenery);
            serialized.FindProperty("spring").colorValue = Color.Lerp(summer, new Color(0.68f, 0.88f, 0.52f), 0.3f);
            serialized.FindProperty("summer").colorValue = summer;
            serialized.FindProperty("autumn").colorValue = Color.Lerp(summer, new Color(0.86f, 0.62f, 0.34f), 0.45f);
            serialized.FindProperty("winter").colorValue = Color.Lerp(summer, new Color(0.93f, 0.95f, 0.98f), snowiness);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The season wheel in the top-left corner, with the season and time to the next one beside it and the exact time
        /// in a tooltip on hover; the "coming up" note that slides out under it; and the banner at the top when a season
        /// arrives.
        /// </summary>
        public static void CreateSeasonHud(Transform canvas, SeasonClock clock)
        {
            const float dialSize = 120f;

            var root = new GameObject("Season Wheel", typeof(RectTransform), typeof(Image));
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(canvas, false);
            rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(24f, -24f);
            rootRect.sizeDelta = new Vector2(390f, dialSize + 16f);
            // A soft warm backing, so the words read over the sky and hills.
            StyleImage(root.GetComponent<Image>(), new Color(0.2f, 0.14f, 0.1f, 0.5f));
            root.GetComponent<Image>().raycastTarget = false;

            // The dial, centered in a square on the left.
            var dial = new GameObject("Dial", typeof(RectTransform));
            var dialRect = (RectTransform)dial.transform;
            dialRect.SetParent(rootRect, false);
            dialRect.anchorMin = dialRect.anchorMax = new Vector2(0f, 0.5f);
            dialRect.pivot = new Vector2(0.5f, 0.5f);
            dialRect.anchoredPosition = new Vector2(8f + dialSize * 0.5f, 0f);
            dialRect.sizeDelta = new Vector2(dialSize, dialSize);

            Sprite disc = PlaceholderArt.GetOrCreateNotificationBubbleSprite();
            Image glow = CreateDisc(dialRect, "Glow", disc, dialSize + 22f, Color.clear);
            glow.enabled = false;
            // The wooden face takes raycasts, so hovering over the dial shows the tooltip.
            Image face = CreateDisc(dialRect, "Face", disc, dialSize, WoodColor);
            face.raycastTarget = true;

            var quarters = new Object[SeasonRules.SeasonsPerYear];
            var icons = new Object[SeasonRules.SeasonsPerYear];
            for (int i = 0; i < SeasonRules.SeasonsPerYear; i++)
            {
                var season = (Season)i;

                // A quarter of a disc, from the top clockwise, turned to its place: spring top right, then clockwise.
                Image quarter = CreateDisc(dialRect, $"{season} Quarter", disc, dialSize - 12f, Color.white);
                quarter.type = Image.Type.Filled;
                quarter.fillMethod = Image.FillMethod.Radial360;
                quarter.fillOrigin = (int)Image.Origin360.Top;
                quarter.fillClockwise = true;
                quarter.fillAmount = 0.25f;
                quarter.transform.localRotation = Quaternion.Euler(0f, 0f, -90f * i);
                quarters[i] = quarter;

                // In the middle of its quarter, and upright.
                float angle = (45f + 90f * i) * Mathf.Deg2Rad;
                Image icon = CreateDisc(dialRect, $"{season} Icon", PlaceholderArt.GetOrCreateSeasonIcon(season), 30f, InkColor);
                icon.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * (dialSize * 0.25f);
                icon.preserveAspect = true;
                icons[i] = icon;
            }

            CreateDisc(dialRect, "Middle", disc, 16f, WoodColor);

            // The marker sits at the top of a pivot in the middle, which turns with the year.
            var markerPivot = new GameObject("Marker Pivot", typeof(RectTransform));
            var markerPivotRect = (RectTransform)markerPivot.transform;
            markerPivotRect.SetParent(dialRect, false);
            markerPivotRect.sizeDelta = Vector2.zero;
            Image markerRim = CreateDisc(markerPivotRect, "Marker", disc, 22f, InkColor);
            markerRim.rectTransform.anchoredPosition = new Vector2(0f, dialSize * 0.5f - 8f);
            CreateDisc(markerRim.rectTransform, "Marker Fill", disc, 14f, PaperColor);

            // The words to the right of the dial.
            TMP_Text seasonLabel = CreateLabel(rootRect, "Season", 30f, Color.white, FontStyles.Bold,
                new Vector2(dialSize + 24f, -20f), "Early Spring");
            TMP_Text timeLeftLabel = CreateLabel(rootRect, "Time Left", 22f, new Color(1f, 1f, 1f, 0.85f), FontStyles.Italic,
                new Vector2(dialSize + 24f, -64f), "Summer in about 5 min");

            TMP_Text tooltipLabel = CreateTooltip(dialRect, out GameObject tooltip);
            SetReferences(face.gameObject.AddComponent<HoverTooltip>(), ("tooltip", tooltip));

            var wheel = root.AddComponent<SeasonWheel>();
            SetReferences(wheel,
                ("clock", clock),
                ("markerPivot", markerPivotRect),
                ("glow", glow),
                ("seasonLabel", seasonLabel),
                ("timeLeftLabel", timeLeftLabel),
                ("tooltipLabel", tooltipLabel));
            SetReferenceArray(wheel, "quarters", quarters);
            SetReferenceArray(wheel, "icons", icons);

            CreateNotices(canvas, clock, rootRect);
        }

        /// <summary>The "coming up" note under the wheel and the banner at the top, both saved hidden.</summary>
        private static void CreateNotices(Transform canvas, SeasonClock clock, RectTransform wheel)
        {
            // The component lives on an always-active object; it shows and hides the notices.
            var root = new GameObject("Season Notices", typeof(RectTransform));
            StretchInParent(root, canvas);

            // A paper note just under the wheel, as wide as it.
            GameObject note = CreatePanel(root.transform, "Coming Up Note", new Vector2(0f, 1f),
                wheel.anchoredPosition + new Vector2(0f, -wheel.sizeDelta.y - 10f), new Vector2(wheel.sizeDelta.x, 116f));
            note.GetComponent<Image>().color = PaperColor;
            note.AddComponent<CanvasGroup>().blocksRaycasts = false;
            TMP_Text noteTitle = CreateText("Title", 24f, InkColor, TextAlignmentOptions.MidlineLeft);
            AddToLayout(noteTitle.gameObject, note.transform, 30f);
            noteTitle.fontStyle = FontStyles.Bold;
            noteTitle.text = "Autumn is on its way"; // Placeholder; the component sets the real words at runtime.
            TMP_Text noteBody = CreateText("Body", 20f, InkColor, TextAlignmentOptions.TopLeft);
            AddToLayout(noteBody.gameObject, note.transform, 52f);
            noteBody.text = "Leaves will start falling a lot. Time to make compost.";
            note.SetActive(false);

            // A banner at the top middle, under where the save message shows.
            GameObject banner = CreatePanel(root.transform, "Arrival Banner", new Vector2(0.5f, 1f), new Vector2(0f, -100f),
                new Vector2(680f, 84f));
            banner.GetComponent<Image>().color = new Color(0.2f, 0.14f, 0.1f, 0.6f);
            banner.AddComponent<CanvasGroup>().blocksRaycasts = false;
            TMP_Text bannerLabel = CreateText("Label", 40f, Color.white, TextAlignmentOptions.Center);
            AddToLayout(bannerLabel.gameObject, banner.transform, 56f);
            bannerLabel.fontStyle = FontStyles.Italic;
            bannerLabel.text = "Spring has arrived · Year 2"; // Placeholder; the component sets the real words at runtime.
            banner.SetActive(false);

            SetReferences(root.AddComponent<SeasonNotice>(),
                ("clock", clock),
                ("note", note.GetComponent<CanvasGroup>()),
                ("noteTitle", noteTitle),
                ("noteBody", noteBody),
                ("banner", banner.GetComponent<CanvasGroup>()),
                ("bannerLabel", bannerLabel));
        }

        /// <summary>A round image centered in its parent, which takes no raycasts.</summary>
        private static Image CreateDisc(RectTransform parent, string objectName, Sprite sprite, float size, Color color)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>One line of words, its top left at this point in the wheel's top-left corner.</summary>
        private static TMP_Text CreateLabel(RectTransform parent, string objectName, float fontSize, Color color,
            FontStyles style, Vector2 topLeft, string placeholder)
        {
            TMP_Text label = CreateText(objectName, fontSize, color, TextAlignmentOptions.TopLeft);
            var rect = (RectTransform)label.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = new Vector2(parent.sizeDelta.x - topLeft.x - 12f, fontSize * 1.4f);
            label.fontStyle = style;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.text = placeholder; // Placeholder; the wheel sets the real words at runtime.
            return label;
        }

        /// <summary>A dark text box under the dial, sized to its words, hidden until the pointer is over the dial.</summary>
        private static TMP_Text CreateTooltip(RectTransform dial, out GameObject tooltip)
        {
            tooltip = new GameObject("Tooltip", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            var rect = (RectTransform)tooltip.transform;
            rect.SetParent(dial, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(-8f, -12f);
            var background = tooltip.GetComponent<Image>();
            StyleImage(background, new Color(0f, 0f, 0f, 0.8f));
            background.raycastTarget = false;

            var layout = tooltip.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 8, 8);
            layout.childControlWidth = layout.childControlHeight = true;
            var fitter = tooltip.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text label = CreateText("Label", 22f, Color.white, TextAlignmentOptions.Left);
            label.transform.SetParent(rect, false);
            label.raycastTarget = false;
            label.text = "1:24 until Autumn · Year 1"; // Placeholder; the wheel sets the real words while it shows.

            tooltip.SetActive(false);
            return label;
        }
    }
}
