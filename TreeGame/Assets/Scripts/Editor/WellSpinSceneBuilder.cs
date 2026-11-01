using System.IO;
using TMPro;
using TreeGame.Minigames;
using TreeGame.Minigames.WellSpin;
using TreeGame.UI;
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
        private const string DefinitionPath = Folder + "/WellSpinMinigame.asset";
        private const string InputActionsPath = Folder + "/WellSpin.inputactions";

        // The ring's size on the canvas. The band takes the outer fifth, which the indicator is sized to fit.
        private const float RingDiameter = 620f;
        private const float RingInnerRadiusRatio = 0.8f;
        private const float IndicatorWidth = 12f;
        private const float IndicatorOverhang = 12f;

        // Straight-on framing with the well filling most of the screen: the roof shows above the ring, the base
        // below it, and the posts, rope and bucket through its middle.
        private const float CameraOrthographicSize = 1.3f;
        private const float CameraHeight = 1.05f;
        private const float CameraDistance = 10f;

        private static readonly Color TrackColor = new Color(0.2f, 0.25f, 0.3f, 0.85f);
        // Lets the well show through while keeping the dark score readable on top of it.
        private static readonly Color DiscColor = new Color(1f, 1f, 1f, 0.6f);
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
            Sprite disc = PlaceholderArt.GetOrCreateDiscSprite();
            Material ground = GetOrCreateMaterial("Ground", new Color(0.58f, 0.72f, 0.42f));
            AssetDatabase.SaveAssets();

            WorldBuilder.ConfigureEnvironment();
            WorldBuilder.CreateSun();
            WorldBuilder.CreateGround(ground);
            StationBuilder.CreateWellModel(new GameObject("Well").transform);
            CreateCamera();

            var gameObject = new GameObject("Well Spin Game");
            var game = gameObject.AddComponent<WellSpinGame>();
            SetReferences(game,
                ("settings", settings),
                ("pressAction", FindActionReference("WellSpin", "Press", InputActionsPath)),
                ("quitAction", FindActionReference("WellSpin", "Quit", InputActionsPath)));

            CreateRingUI(CreateCanvas("Ring UI", 0), game, ring, disc);

            SaveGeneratedScene(scene, ScenePath);

            Debug.Log($"Well Spin scene created at {ScenePath}. Press Play in it to test the minigame on its own.");
        }

        /// <summary>The asset other scenes use to start this minigame.</summary>
        public static MinigameDefinition GetOrCreateDefinition()
        {
            var definition = AssetDatabase.LoadAssetAtPath<MinigameDefinition>(DefinitionPath);
            if (definition != null)
            {
                return definition;
            }

            EnsureFolder(MinigamesFolder);
            EnsureFolder(Folder);

            definition = ScriptableObject.CreateInstance<MinigameDefinition>();
            AssetDatabase.CreateAsset(definition, DefinitionPath);

            var serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("sceneName").stringValue = Path.GetFileNameWithoutExtension(ScenePath);
            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            return definition;
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
            go.transform.position = new Vector3(0f, CameraHeight, -CameraDistance);
            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = CameraOrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SceneLayout.SkyColor;
            go.AddComponent<AudioListener>();
        }

        private static void CreateRingUI(Transform canvas, WellSpinGame game, Sprite ringSprite, Sprite discSprite)
        {
            var ring = new GameObject("Ring", typeof(RectTransform));
            var ringRect = (RectTransform)ring.transform;
            ringRect.SetParent(canvas, false);
            ringRect.sizeDelta = Vector2.one * RingDiameter;

            var disc = new GameObject("Disc", typeof(RectTransform), typeof(Image));
            var discRect = (RectTransform)disc.transform;
            discRect.SetParent(ringRect, false);
            discRect.sizeDelta = Vector2.one * (RingDiameter * RingInnerRadiusRatio);
            var discImage = disc.GetComponent<Image>();
            discImage.sprite = discSprite;
            discImage.color = DiscColor;

            var track = new GameObject("Track", typeof(RectTransform), typeof(Image));
            StretchInParent(track, ringRect);
            var trackImage = track.GetComponent<Image>();
            trackImage.sprite = ringSprite;
            trackImage.color = TrackColor;
            var trackFlash = track.AddComponent<ImageFlash>();

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

            // In its own dark panel at the bottom of the screen, so it reads clearly over the well and the ground.
            GameObject hintPanel = CreatePanel(canvas, "Hint Panel", new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1100f, 72f));
            TMP_Text hint = CreateText("Hint", 30f, Color.white, TextAlignmentOptions.Center);
            AddToLayout(hint.gameObject, hintPanel.transform, 44f);

            SetReferences(ring.AddComponent<WellSpinRingView>(),
                ("game", game),
                ("target", target),
                ("indicatorPivot", pivotRect),
                ("scoreLabel", score),
                ("statusLabel", status),
                ("hintLabel", hint),
                ("trackFlash", trackFlash));
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
