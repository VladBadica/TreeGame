using TMPro;
using TreeGame.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace TreeGame.EditorTools
{
    /// <summary>UI building blocks shared by every scene builder, so all screens are styled and scaled the same way.</summary>
    internal static class UIFactory
    {
        /// <summary>A camera for a scene that is all UI, so there is nothing to frame and it only needs a background color.</summary>
        public static void CreateUICamera(Color backgroundColor)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            go.AddComponent<AudioListener>();
        }

        public static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            // The project uses the Input System package only, so the legacy StandaloneInputModule would not work.
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        public static Transform CreateCanvas(string objectName, int sortingOrder)
        {
            var canvasGo = new GameObject(objectName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvasGo.transform;
        }

        /// <summary>A full-screen black fade above all other UI, starting clear.</summary>
        public static ScreenFade CreateScreenFade(Transform parent)
        {
            const int sortingOrder = 1000;

            Transform canvas = CreateCanvas("Screen Fade", sortingOrder);
            canvas.SetParent(parent, false);

            var cover = new GameObject("Cover", typeof(RectTransform), typeof(Image));
            StretchInParent(cover, canvas);
            var image = cover.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.enabled = false; // Off while clear, so it doesn't block clicks.

            var fade = canvas.gameObject.AddComponent<ScreenFade>();
            BuilderHelpers.SetReferences(fade, ("image", image));
            return fade;
        }

        /// <summary>
        /// A minigame's clock: a bar across the top of the screen that shrinks away from the left, with the time left
        /// just outside its left end, where it stays readable as the bar shrinks away from it.
        /// </summary>
        public static CountdownBar CreateCountdownBar(Transform canvas, Color labelColor)
        {
            const float width = 900f;
            const float height = 34f;

            var bar = new GameObject("Timer Bar", typeof(RectTransform), typeof(Image));
            var barRect = (RectTransform)bar.transform;
            barRect.SetParent(canvas, false);
            barRect.anchorMin = barRect.anchorMax = barRect.pivot = new Vector2(0.5f, 1f);
            barRect.anchoredPosition = new Vector2(0f, -28f);
            barRect.sizeDelta = new Vector2(width, height);
            StyleImage(bar.GetComponent<Image>(), new Color(0.2f, 0.25f, 0.3f, 0.6f));

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            RectTransform fillRect = StretchInParent(fill, barRect);
            StyleImage(fill.GetComponent<Image>(), new Color(0.96f, 0.72f, 0.26f));

            var fillBar = bar.AddComponent<FillBar>();
            BuilderHelpers.SetReferences(fillBar, ("fill", fillRect));
            var serializedBar = new SerializedObject(fillBar);
            serializedBar.FindProperty("anchoredRight").boolValue = true;
            serializedBar.ApplyModifiedPropertiesWithoutUndo();

            TMP_Text label = CreateText("Time Left", 30f, labelColor, TextAlignmentOptions.MidlineRight);
            var labelRect = (RectTransform)label.transform;
            labelRect.SetParent(barRect, false);
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 0.5f);
            labelRect.pivot = new Vector2(1f, 0.5f);
            labelRect.anchoredPosition = new Vector2(-14f, 0f);
            labelRect.sizeDelta = new Vector2(100f, height);
            label.fontStyle = FontStyles.Bold;
            label.text = "30s"; // Placeholder; the bar shows the real time left at runtime.

            var countdown = bar.AddComponent<CountdownBar>();
            BuilderHelpers.SetReferences(countdown, ("bar", fillBar), ("label", label));
            return countdown;
        }

        /// <summary>A line of instructions in its own dark panel at the bottom of the screen, so it reads clearly over anything.</summary>
        public static TMP_Text CreateHintBar(Transform canvas, float width)
        {
            GameObject panel = CreatePanel(canvas, "Hint Panel", new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(width, 72f));
            TMP_Text hint = CreateText("Hint", 30f, Color.white, TextAlignmentOptions.Center);
            AddToLayout(hint.gameObject, panel.transform, 44f);
            return hint;
        }

        /// <summary>Semi-transparent, vertically stacking panel anchored to a screen corner.</summary>
        public static GameObject CreatePanel(Transform canvas, string objectName, Vector2 corner, Vector2 offset, Vector2 size)
        {
            var panel = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            var rect = (RectTransform)panel.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = corner;
            rect.anchoredPosition = offset;
            rect.sizeDelta = size;

            StyleImage(panel.GetComponent<Image>(), new Color(0f, 0f, 0f, 0.45f));

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 16);
            layout.spacing = 10f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return panel;
        }

        public static RectTransform StretchInParent(GameObject go, Transform parent)
        {
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static Button CreateButton(DefaultControls.Resources resources, Transform parent, string objectName, string caption,
            float fontSize = 20f)
        {
            // TMP's variant of the standard button, so its caption is a TextMeshPro label.
            GameObject go = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources { standard = resources.standard });
            go.name = objectName;
            go.transform.SetParent(parent, false);

            TMP_Text text = go.GetComponentInChildren<TMP_Text>();
            StyleText(text, fontSize, new Color(0.2f, 0.2f, 0.2f), TextAlignmentOptions.Center);
            text.text = caption;

            return go.GetComponent<Button>();
        }

        /// <summary>
        /// A square button showing a white icon, tinted dark like button captions, in place of text. Gives the icon's
        /// image too, for anything that swaps the icon.
        /// </summary>
        public static Button CreateIconButton(DefaultControls.Resources resources, Transform parent, string objectName,
            Sprite icon, float size, out Image iconImage)
        {
            Button button = CreateButton(resources, parent, objectName, "");
            Object.DestroyImmediate(button.GetComponentInChildren<TMP_Text>().gameObject);
            ((RectTransform)button.transform).sizeDelta = new Vector2(size, size);

            var iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            RectTransform iconRect = StretchInParent(iconObject, button.transform);
            // A margin all round, so the icon sits inside the button's border.
            float margin = size * 0.18f;
            iconRect.offsetMin = new Vector2(margin, margin);
            iconRect.offsetMax = new Vector2(-margin, -margin);

            iconImage = iconObject.GetComponent<Image>();
            iconImage.sprite = icon;
            iconImage.color = new Color(0.2f, 0.2f, 0.2f);
            iconImage.preserveAspect = true;
            // Only the button takes clicks; the icon on it shouldn't get in the way.
            iconImage.raycastTarget = false;
            return button;
        }

        /// <summary>A TextMeshPro dropdown with larger text and rows than Unity's defaults. Options are set at runtime.</summary>
        public static TMP_Dropdown CreateDropdown(string objectName, float fontSize)
        {
            const float rowHeight = 40f;
            const float listHeight = 240f;

            GameObject go = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources
            {
                standard = GetUIResources().standard,
                background = GetUIResources().background,
                checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
                dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
                mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
            });
            go.name = objectName;

            var dropdown = go.GetComponent<TMP_Dropdown>();
            dropdown.captionText.fontSize = fontSize;
            dropdown.itemText.fontSize = fontSize;

            // The row is the item text's parent, inside a scrolling content area; each must fit the larger text.
            var row = (RectTransform)dropdown.itemText.transform.parent;
            row.sizeDelta = new Vector2(row.sizeDelta.x, rowHeight);
            var content = (RectTransform)row.parent;
            content.sizeDelta = new Vector2(content.sizeDelta.x, rowHeight);
            dropdown.template.sizeDelta = new Vector2(dropdown.template.sizeDelta.x, listHeight);

            return dropdown;
        }

        /// <summary>A TextMeshPro text field with larger text than Unity's default, and a grey hint while it is empty.</summary>
        public static TMP_InputField CreateInputField(string objectName, float fontSize, string placeholder)
        {
            GameObject go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources
            {
                inputField = GetUIResources().inputField,
            });
            go.name = objectName;

            var field = go.GetComponent<TMP_InputField>();
            field.pointSize = fontSize;
            field.textComponent.alignment = TextAlignmentOptions.Center;
            if (field.placeholder is TMP_Text hint)
            {
                hint.text = placeholder;
                hint.alignment = TextAlignmentOptions.Center;
            }

            return field;
        }

        /// <summary>A TextMeshPro label; TMP renders crisp text at any size and resolution.</summary>
        public static TMP_Text CreateText(string objectName, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            TMP_Text text = go.GetComponent<TMP_Text>();
            StyleText(text, fontSize, color, alignment);
            return text;
        }

        public static void AddToLayout(GameObject go, Transform parent, float preferredHeight)
        {
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
        }

        public static void StyleImage(Image image, Color color)
        {
            image.sprite = GetUIResources().standard;
            image.type = Image.Type.Sliced;
            image.color = color;
        }

        public static void StyleText(TMP_Text text, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            // The font comes from TMP's default (set up by Window > TextMeshPro > Import TMP Essential Resources).
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
        }

        public static DefaultControls.Resources GetUIResources()
        {
            // The same built-in sprites Unity's GameObject > UI menu uses.
            return new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            };
        }
    }
}
