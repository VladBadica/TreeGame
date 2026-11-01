using TMPro;
using TreeGame.Minigames.WellSpin;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TreeGame.EditorTools.BuilderHelpers;
using static TreeGame.EditorTools.UIFactory;

namespace TreeGame.EditorTools
{
    /// <summary>Generates the Well Spin minigame scene: the ring, its indicator and target, and the game running them.</summary>
    public static class WellSpinSceneBuilder
    {
        private const string MinigamesFolder = "Assets/Minigames";
        private const string Folder = MinigamesFolder + "/WellSpin";
        private const string ScenePath = Folder + "/WellSpin.unity";
        private const string SettingsPath = Folder + "/WellSpinSettings.asset";
        private const string InputActionsPath = Folder + "/WellSpin.inputactions";

        // The ring's size on the canvas. The band takes the outer fifth, which the indicator is sized to fit.
        private const float RingDiameter = 620f;
        private const float RingInnerRadiusRatio = 0.8f;
        private const float IndicatorWidth = 12f;
        private const float IndicatorOverhang = 12f;

        private static readonly Color TrackColor = new Color(0.2f, 0.25f, 0.3f, 0.85f);
        private static readonly Color TargetColor = new Color(0.45f, 0.82f, 0.36f);
        private static readonly Color IndicatorColor = Color.white;
        private static readonly Color TextColor = new Color(0.16f, 0.2f, 0.25f);

        [MenuItem("Tree Game/Create Well Spin Scene")]
        private static void CreateWellSpinScene()
        {
            if (!TryStartNewScene(ScenePath, "Create Well Spin Scene", out Scene scene))
            {
                return;
            }

            EnsureFolder(MinigamesFolder);
            EnsureFolder(Folder);
            WellSpinSettings settings = GetOrCreateSettings();
            Sprite ring = PlaceholderArt.GetOrCreateRingSprite(RingInnerRadiusRatio);
            AssetDatabase.SaveAssets();

            CreateCamera();

            var gameObject = new GameObject("Well Spin Game");
            var game = gameObject.AddComponent<WellSpinGame>();
            SetReferences(game,
                ("settings", settings),
                ("pressAction", FindActionReference("WellSpin", "Press", InputActionsPath)));

            CreateRingUI(CreateCanvas("Ring UI", 0), game, ring);

            SaveGeneratedScene(scene, ScenePath);

            Debug.Log($"Well Spin scene created at {ScenePath}. Press Play in it to test the minigame on its own.");
        }

        /// <summary>Reused if it exists, so tuning done in the Inspector survives regenerating the scene.</summary>
        private static WellSpinSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<WellSpinSettings>(SettingsPath);
            if (settings != null)
            {
                return settings;
            }

            settings = ScriptableObject.CreateInstance<WellSpinSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
            return settings;
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SceneLayout.SkyColor;
            go.AddComponent<AudioListener>();
        }

        private static void CreateRingUI(Transform canvas, WellSpinGame game, Sprite ringSprite)
        {
            var ring = new GameObject("Ring", typeof(RectTransform));
            var ringRect = (RectTransform)ring.transform;
            ringRect.SetParent(canvas, false);
            ringRect.sizeDelta = Vector2.one * RingDiameter;

            var track = new GameObject("Track", typeof(RectTransform), typeof(Image));
            StretchInParent(track, ringRect);
            var trackImage = track.GetComponent<Image>();
            trackImage.sprite = ringSprite;
            trackImage.color = TrackColor;

            // The same ring, filled only as far as the target is wide. The view sets its size and rotation.
            var targetObject = new GameObject("Target", typeof(RectTransform), typeof(Image));
            StretchInParent(targetObject, ringRect);
            var target = targetObject.GetComponent<Image>();
            target.sprite = ringSprite;
            target.color = TargetColor;
            target.type = Image.Type.Filled;
            target.fillMethod = Image.FillMethod.Radial360;
            target.fillOrigin = (int)Image.Origin360.Top;
            target.fillClockwise = true;

            // A stretched pivot rotates around the ring's center, carrying the bar around the band.
            var pivot = new GameObject("Indicator Pivot", typeof(RectTransform));
            RectTransform pivotRect = StretchInParent(pivot, ringRect);

            float outerRadius = RingDiameter / 2f;
            float bandWidth = outerRadius * (1f - RingInnerRadiusRatio);
            var bar = new GameObject("Indicator", typeof(RectTransform), typeof(Image));
            var barRect = (RectTransform)bar.transform;
            barRect.SetParent(pivotRect, false);
            barRect.sizeDelta = new Vector2(IndicatorWidth, bandWidth + 2f * IndicatorOverhang);
            barRect.anchoredPosition = new Vector2(0f, outerRadius - bandWidth / 2f);
            bar.GetComponent<Image>().color = IndicatorColor;

            TMP_Text score = CreateLabel("Score", ringRect, 120f, new Vector2(0f, 40f), new Vector2(300f, 150f));
            score.fontStyle = FontStyles.Bold;
            TMP_Text status = CreateLabel("Status", ringRect, 26f, new Vector2(0f, -70f), new Vector2(440f, 90f));

            TMP_Text hint = CreateLabel("Hint", canvas, 30f, new Vector2(0f, -RingDiameter / 2f - 60f), new Vector2(1000f, 50f));
            hint.text = "Press <b>Space</b> when the white marker is in the green area";

            SetReferences(ring.AddComponent<WellSpinRingView>(),
                ("game", game),
                ("target", target),
                ("indicatorPivot", pivotRect),
                ("scoreLabel", score),
                ("statusLabel", status));
        }

        private static TMP_Text CreateLabel(string objectName, Transform parent, float fontSize, Vector2 position, Vector2 size)
        {
            TMP_Text label = CreateText(objectName, fontSize, TextColor, TextAlignmentOptions.Center);
            var rect = (RectTransform)label.transform;
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return label;
        }
    }
}
