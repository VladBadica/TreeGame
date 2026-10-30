using System.Linq;
using TreeGame.Care;
using TreeGame.Core;
using TreeGame.DevTools;
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
    /// Generates the prototype scene (camera, light, ground, tree, HUD and debug UI) with all references wired,
    /// so the setup is reproducible and nothing has to be assembled by hand.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/TreeGame.unity";
        private const string MaterialsFolder = "Assets/Materials";
        private const string ConfigFolder = "Assets/Config";
        private const string ConfigPath = ConfigFolder + "/GameConfig.asset";

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

            // Opening a new scene unloads unused assets, so assets must be loaded or created after this point;
            // otherwise freshly created ones are gone by the time they are assigned.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Existing assets are reused so manual tweaks to them survive regenerating the scene.
            GameConfig config = GetOrCreateConfig();
            Material bark = GetOrCreateMaterial("Bark", new Color(0.45f, 0.3f, 0.2f));
            Material leaves = GetOrCreateMaterial("Leaves", new Color(0.36f, 0.62f, 0.3f));
            Material ground = GetOrCreateMaterial("Ground", new Color(0.58f, 0.72f, 0.42f));
            AssetDatabase.SaveAssets();

            ConfigureEnvironment();
            CreateSun();
            CreateCamera();
            CreateGround(ground);
            GameObject tree = CreateTree(config, bark, leaves);
            TreeResource water = CreateResource(tree.transform, TreeResourceType.Water, config);
            TreeResource nutrients = CreateResource(tree.transform, TreeResourceType.Nutrients, config);

            CreateEventSystem();
            CreateResourceHud(CreateCanvas("HUD", 0), water, nutrients);
            // Own canvas, drawn on top, so all dev tooling can be removed by deleting one object.
            CreateDebugPanel(CreateCanvas("Debug UI", 100), tree.GetComponent<TreeGrowth>());

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

        private static GameObject CreateTree(GameConfig config, Material bark, Material leaves)
        {
            var go = new GameObject("Tree");
            var growth = go.AddComponent<TreeGrowth>();
            var visuals = go.AddComponent<TreeVisuals>();

            SetReferences(visuals,
                ("treeGrowth", growth),
                ("barkMaterial", bark),
                ("foliageMaterial", leaves));

            SetReferences(go.AddComponent<PassiveGrowth>(),
                ("treeGrowth", growth),
                ("config", config));

            return go;
        }

        /// <summary>A child object per resource keeps each resource easy to find in the Hierarchy.</summary>
        private static TreeResource CreateResource(Transform owner, TreeResourceType type, GameConfig config)
        {
            var go = new GameObject(type.ToString());
            go.transform.SetParent(owner, false);

            var resource = go.AddComponent<TreeResource>();
            SetReferences(resource, ("config", config));
            var serializedResource = new SerializedObject(resource);
            serializedResource.FindProperty("type").enumValueIndex = (int)type;
            serializedResource.ApplyModifiedPropertiesWithoutUndo();

            SetReferences(go.AddComponent<ResourceDepletion>(),
                ("resource", resource),
                ("config", config));

            return resource;
        }

        private static void CreateEventSystem()
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            // The project uses the Input System package only, so the legacy StandaloneInputModule would not work.
            eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static Transform CreateCanvas(string objectName, int sortingOrder)
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

        private static void CreateDebugPanel(Transform canvas, TreeGrowth treeGrowth)
        {
            DefaultControls.Resources resources = GetUIResources();
            GameObject panel = CreatePanel(canvas, "Growth Debug Panel", new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(420f, 160f));

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

            // The panel sets the real caption from its growth step at runtime.
            Button addButton = CreateButton(resources, buttonRow.transform, "Add Growth Button", "Grow");
            Button resetButton = CreateButton(resources, buttonRow.transform, "Reset Button", "Reset");

            var debugPanel = panel.AddComponent<GrowthDebugPanel>();
            SetReferences(debugPanel,
                ("treeGrowth", treeGrowth),
                ("growthSlider", slider),
                ("addGrowthButton", addButton),
                ("resetButton", resetButton),
                ("growthLabel", label));
        }

        private static void CreateResourceHud(Transform canvas, TreeResource water, TreeResource nutrients)
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

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(Shadow));
            StretchInParent(labelGo, bar.transform);
            var label = labelGo.GetComponent<Text>();
            StyleText(label, 20, Color.white, TextAnchor.MiddleCenter);
            label.text = resource.Type.ToString(); // Placeholder; the bar formats the value at runtime.

            var resourceBar = bar.AddComponent<ResourceBar>();
            SetReferences(resourceBar,
                ("resource", resource),
                ("fill", fillRect),
                ("label", label));
        }

        /// <summary>Semi-transparent, vertically stacking panel anchored to a screen corner.</summary>
        private static GameObject CreatePanel(Transform canvas, string objectName, Vector2 corner, Vector2 offset, Vector2 size)
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

        private static RectTransform StretchInParent(GameObject go, Transform parent)
        {
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
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

        private static void StyleImage(Image image, Color color)
        {
            image.sprite = GetUIResources().standard;
            image.type = Image.Type.Sliced;
            image.color = color;
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

        private static GameConfig GetOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config != null)
            {
                return config;
            }

            if (!AssetDatabase.IsValidFolder(ConfigFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Config");
            }

            // Default values come from GameConfig's field initializers, so they live in one place.
            config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
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

                if (value == null)
                {
                    Debug.LogError($"{target.GetType().Name}.{propertyName}: tried to assign a missing object.", target);
                }

                property.objectReferenceValue = value;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
