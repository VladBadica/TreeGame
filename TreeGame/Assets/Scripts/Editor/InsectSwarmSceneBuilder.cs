using TMPro;
using TreeGame.Minigames;
using TreeGame.Minigames.InsectSwarm;
using TreeGame.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static TreeGame.EditorTools.BuilderHelpers;
using static TreeGame.EditorTools.UIFactory;

namespace TreeGame.EditorTools
{
    /// <summary>Generates the Insect Swarm minigame scene: a close-up of the bark, and the game running on it.</summary>
    public static class InsectSwarmSceneBuilder
    {
        private const string MinigamesFolder = "Assets/Minigames";
        private const string Folder = MinigamesFolder + "/InsectSwarm";
        internal const string ScenePath = Folder + "/InsectSwarm.unity";
        private const string SettingsPath = Folder + "/InsectSwarmSettings.asset";
        private const string DefinitionPath = Folder + "/InsectSwarmMinigame.asset";
        private const string InputActionsPath = Folder + "/InsectSwarm.inputactions";

        private static readonly Color BackgroundColor = new Color(0.2f, 0.13f, 0.09f);
        private static readonly Color PatchColor = new Color(0.45f, 0.82f, 0.36f, 0.85f);

        [MenuItem("Tree Game/Create Insect Swarm Scene", false, AllScenesBuilder.SingleSceneMenuPriority)]
        private static void CreateInsectSwarmScene()
        {
            if (ConfirmGenerating("Create Insect Swarm Scene", ScenePath))
            {
                Build();
            }
        }

        /// <summary>Generates the scene without asking; callers confirm first.</summary>
        internal static void Build()
        {
            Scene scene = StartNewScene();

            EnsureFolder(MinigamesFolder);
            EnsureFolder(Folder);
            var settings = GetOrCreateSettings<InsectSwarmSettings>(SettingsPath);
            Sprite bark = PlaceholderArt.GetOrCreateBarkSprite();
            Sprite crack = PlaceholderArt.GetOrCreateCrackSprite();
            Sprite insect = PlaceholderArt.GetOrCreateInsectSprite();
            Sprite disc = PlaceholderArt.GetOrCreateDiscSprite();
            AssetDatabase.SaveAssets();

            CreateUICamera(BackgroundColor);

            Transform canvas = CreateCanvas("Swarm UI", 0);

            var background = new GameObject("Bark", typeof(RectTransform), typeof(Image));
            StretchInParent(background, canvas);
            background.GetComponent<Image>().sprite = bark;

            RectTransform field = CreateField(canvas);

            var gameObject = new GameObject("Insect Swarm Game");
            var game = gameObject.AddComponent<InsectSwarmGame>();
            SetReferences(game,
                ("settings", settings),
                ("pressAction", FindActionReference("InsectSwarm", "Press", InputActionsPath)),
                ("pointAction", FindActionReference("InsectSwarm", "Point", InputActionsPath)),
                ("quitAction", FindActionReference("InsectSwarm", "Quit", InputActionsPath)),
                ("field", field));

            CreateView(canvas, field, game, crack, insect, disc);

            SaveGeneratedScene(scene, ScenePath);

            Debug.Log($"Insect Swarm scene created at {ScenePath}. Press Play in it to test the minigame on its own.");
        }

        /// <summary>The asset other scenes use to start this minigame.</summary>
        public static MinigameDefinition GetOrCreateDefinition()
        {
            EnsureFolder(MinigamesFolder);
            EnsureFolder(Folder);
            return GetOrCreateMinigameDefinition(DefinitionPath, ScenePath);
        }

        /// <summary>The largest area of the screen in the field's shape, so positions on it look the same on any screen.</summary>
        private static RectTransform CreateField(Transform canvas)
        {
            var defaults = new InsectSwarmTuning();

            var go = new GameObject("Field", typeof(RectTransform), typeof(AspectRatioFitter));
            RectTransform field = StretchInParent(go, canvas);
            var fitter = go.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = defaults.FieldWidth / defaults.FieldHeight;
            return field;
        }

        private static void CreateView(Transform canvas, RectTransform field, InsectSwarmGame game, Sprite crack, Sprite insect, Sprite disc)
        {
            // Layers, so cracks stay under the patch and insects over it, whatever order copies are made in at runtime.
            RectTransform cracks = CreateLayer("Cracks", field);
            var patch = new GameObject("Patch", typeof(RectTransform), typeof(Image));
            patch.transform.SetParent(field, false);
            var patchImage = patch.GetComponent<Image>();
            patchImage.sprite = disc;
            patchImage.color = PatchColor;
            RectTransform insects = CreateLayer("Insects", field);

            RectTransform crackTemplate = CreateTemplate("Crack Template", cracks, crack);
            RectTransform insectTemplate = CreateTemplate("Insect Template", insects, insect);

            GameObject messagePanel = CreatePanel(canvas, "Message Panel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 170f));
            TMP_Text message = CreateText("Message", 36f, Color.white, TextAlignmentOptions.Center);
            AddToLayout(message.gameObject, messagePanel.transform, 130f);

            TMP_Text hint = CreateHintBar(canvas, width: 900f);
            CountdownBar timer = CreateCountdownBar(canvas, Color.white);

            TMP_Text count = CreateText("Count", 30f, Color.white, TextAlignmentOptions.Center);
            var countRect = (RectTransform)count.transform;
            countRect.SetParent(canvas, false);
            countRect.anchorMin = countRect.anchorMax = countRect.pivot = new Vector2(0.5f, 1f);
            countRect.anchoredPosition = new Vector2(0f, -72f);
            countRect.sizeDelta = new Vector2(500f, 44f);
            count.fontStyle = FontStyles.Bold;

            var viewObject = new GameObject("Insect Swarm View");
            SetReferences(viewObject.AddComponent<InsectSwarmView>(),
                ("game", game),
                ("patch", patch.transform),
                ("crackTemplate", crackTemplate),
                ("insectTemplate", insectTemplate.GetComponent<Image>()),
                ("messagePanel", messagePanel),
                ("messageLabel", message),
                ("countLabel", count),
                ("hintLabel", hint),
                ("timer", timer),
                ("patchFlash", patch.AddComponent<ImageFlash>()));
        }

        private static RectTransform CreateLayer(string objectName, Transform field)
        {
            return StretchInParent(new GameObject(objectName, typeof(RectTransform)), field);
        }

        /// <summary>An inactive image the view copies at runtime, and places by its anchors.</summary>
        private static RectTransform CreateTemplate(string objectName, Transform parent, Sprite sprite)
        {
            var template = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            RectTransform rect = StretchInParent(template, parent);
            var image = template.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            template.SetActive(false);
            return rect;
        }
    }
}
