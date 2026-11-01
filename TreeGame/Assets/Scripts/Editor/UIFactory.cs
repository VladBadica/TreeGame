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
