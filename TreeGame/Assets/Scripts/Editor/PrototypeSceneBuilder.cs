using System.Linq;
using TreeGame.Trees;
using TreeGame.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreeGame.EditorTools
{
    /// <summary>
    /// Generates the prototype scene (camera, light, ground, tree and debug UI) with all references wired,
    /// so the setup is reproducible and nothing has to be assembled by hand.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/TreeGame.unity";
        private const string MaterialsFolder = "Assets/Materials";

        // Camera framing: looks slightly down at the tree for a 2.5D feel.
        private static readonly Vector3 CameraTarget = new Vector3(0f, 2.2f, 0f);
        private const float CameraPitch = 12f;
        private const float CameraDistance = 15f;
        private const float CameraOrthographicSize = 3f;

        [MenuItem("Tree Game/Create Prototype Scene")]
        private static void CreatePrototypeScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog("Create Prototype Scene", $"{ScenePath} already exists. Replace it?", "Replace", "Cancel"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            // Existing materials are reused so manual tweaks to them survive regenerating the scene.
            Material bark = GetOrCreateMaterial("Bark", new Color(0.45f, 0.3f, 0.2f));
            Material leaves = GetOrCreateMaterial("Leaves", new Color(0.36f, 0.62f, 0.3f));
            Material ground = GetOrCreateMaterial("Ground", new Color(0.58f, 0.72f, 0.42f));

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            ConfigureEnvironment();
            CreateSun();
            CreateCamera();
            CreateGround(ground);
            TreeGrowth treeGrowth = CreateTree(bark, leaves);
            CreateDebugUI(treeGrowth);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();

            Debug.Log($"Prototype scene created at {ScenePath}. Press Play and use the Growth panel.");
        }

        private static void ConfigureEnvironment()
        {
            // Flat gradient ambient instead of a skybox: soft, cozy and needs no lighting bake.
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.75f, 0.82f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.68f, 0.7f, 0.62f);
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.38f, 0.3f);
            RenderSettings.fog = false;
        }

        private static void CreateSun()
        {
            var go = new GameObject("Sun");
            go.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.94f, 0.84f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            Quaternion rotation = Quaternion.Euler(CameraPitch, 0f, 0f);
            go.transform.SetPositionAndRotation(CameraTarget - rotation * Vector3.forward * CameraDistance, rotation);

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = CameraOrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.78f, 0.88f, 0.95f);
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 50f;

            go.AddComponent<AudioListener>();
        }

        private static void CreateGround(Material material)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            ground.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static TreeGrowth CreateTree(Material bark, Material leaves)
        {
            var go = new GameObject("Tree");
            var growth = go.AddComponent<TreeGrowth>();
            var visuals = go.AddComponent<TreeVisuals>();

            SetReferences(visuals,
                ("treeGrowth", growth),
                ("barkMaterial", bark),
                ("foliageMaterial", leaves));

            return growth;
        }

        private static void CreateDebugUI(TreeGrowth treeGrowth)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            // The project uses the Input System package only, so the legacy StandaloneInputModule would not work.
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            var canvasGo = new GameObject("Debug UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            DefaultControls.Resources resources = GetUIResources();

            // Panel in the top-left corner, children stacked vertically.
            var panel = new GameObject("Growth Debug Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            var panelRect = (RectTransform)panel.transform;
            panelRect.SetParent(canvasGo.transform, false);
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -24f);
            panelRect.sizeDelta = new Vector2(420f, 160f);

            var panelImage = panel.GetComponent<Image>();
            panelImage.sprite = resources.standard;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = new Color(0f, 0f, 0f, 0.45f);

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 16);
            layout.spacing = 10f;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Text label = DefaultControls.CreateText(resources).GetComponent<Text>();
            label.name = "Growth Label";
            AddToLayout(label.gameObject, panel.transform, 32f);
            StyleText(label, 24, Color.white, TextAnchor.MiddleLeft);
            label.text = "Growth: 0%";

            Slider slider = DefaultControls.CreateSlider(resources).GetComponent<Slider>();
            slider.name = "Growth Slider";
            AddToLayout(slider.gameObject, panel.transform, 24f);
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = treeGrowth.Growth;

            var buttonRow = new GameObject("Buttons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            AddToLayout(buttonRow, panel.transform, 40f);
            var rowLayout = buttonRow.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 10f;
            rowLayout.childControlWidth = rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = rowLayout.childForceExpandHeight = true;

            Button addButton = CreateButton(resources, buttonRow.transform, "Add Growth Button", "Grow +5%");
            Button resetButton = CreateButton(resources, buttonRow.transform, "Reset Button", "Reset");

            var debugPanel = panel.AddComponent<GrowthDebugPanel>();
            SetReferences(debugPanel,
                ("treeGrowth", treeGrowth),
                ("growthSlider", slider),
                ("addGrowthButton", addButton),
                ("resetButton", resetButton),
                ("growthLabel", label));
        }

        private static Button CreateButton(DefaultControls.Resources resources, Transform parent, string objectName, string caption)
        {
            GameObject go = DefaultControls.CreateButton(resources);
            go.name = objectName;
            go.transform.SetParent(parent, false);

            Text text = go.GetComponentInChildren<Text>();
            StyleText(text, 20, new Color(0.2f, 0.2f, 0.2f), TextAnchor.MiddleCenter);
            text.text = caption;

            return go.GetComponent<Button>();
        }

        private static void AddToLayout(GameObject go, Transform parent, float preferredHeight)
        {
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
        }

        private static void StyleText(Text text, int fontSize, Color color, TextAnchor alignment)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
        }

        private static DefaultControls.Resources GetUIResources()
        {
            // The same built-in sprites Unity's GameObject > UI menu uses.
            return new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            };
        }

        private static Material GetOrCreateMaterial(string materialName, Color color)
        {
            string path = $"{MaterialsFolder}/{materialName}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogWarning("URP Lit shader not found; falling back to the render pipeline's default shader.");
                shader = GraphicsSettings.currentRenderPipeline != null
                    ? GraphicsSettings.currentRenderPipeline.defaultShader
                    : Shader.Find("Standard");
            }

            material = new Material(shader) { name = materialName };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.1f); // Matte look suits the soft, cozy style.

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void AddSceneToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>Assigns private [SerializeField] references the same way the Inspector would.</summary>
        private static void SetReferences(Object target, params (string propertyName, Object value)[] references)
        {
            var serializedObject = new SerializedObject(target);
            foreach ((string propertyName, Object value) in references)
            {
                SerializedProperty property = serializedObject.FindProperty(propertyName);
                if (property == null)
                {
                    Debug.LogError($"{target.GetType().Name} has no serialized field '{propertyName}'.", target);
                    continue;
                }

                property.objectReferenceValue = value;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
