using System.Linq;
using TreeGame.Cameras;
using TreeGame.Care;
using TreeGame.Core;
using TreeGame.DevTools;
using TreeGame.Player;
using TreeGame.Trees;
using TreeGame.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreeGame.EditorTools
{
    /// <summary>
    /// Generates the prototype scene (camera, light, ground, tree, player, HUD and debug UI) with all references wired,
    /// so the setup is reproducible and nothing has to be assembled by hand.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/TreeGame.unity";
        private const string MaterialsFolder = "Assets/Materials";
        private const string ConfigFolder = "Assets/Config";
        private const string ConfigPath = ConfigFolder + "/GameConfig.asset";

        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

        // The ground is a shallow strip seen almost side-on, like a stage: wide along X, shallow in depth (Z).
        // It is thick so its front face always fills the bottom of the screen.
        private const float GroundWidth = 80f;
        private const float GroundThickness = 4f;
        private const float GroundFrontZ = -3.5f;
        private const float GroundBackZ = 1.5f;
        private const float WalkableHalfWidth = 13f;

        // The player walks in front of the tree (negative Z is toward the camera) and starts just left of it.
        private static readonly Vector3 PlayerStartPosition = new Vector3(-1.5f, 0.6f, -2f);
        private static readonly Vector3 PlayerSize = new Vector3(0.6f, 1.2f, 0.4f);

        // Camera framing: centered on the player, nearly side-on. A small pitch still shows a sliver of ground top.
        private static readonly Vector3 CameraTarget = new Vector3(PlayerStartPosition.x, 2f, 0f);
        private const float CameraPitch = 5f;
        private const float CameraDistance = 20f;
        private const float CameraOrthographicSize = 3f;

        // Background hills are generated across this X range on each side of the origin.
        private const float BackdropHalfWidth = 40f;

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
            Material playerMaterial = GetOrCreateMaterial("Player", new Color(0.86f, 0.46f, 0.36f));
            AssetDatabase.SaveAssets();

            ConfigureEnvironment();
            CreateSun();
            CreateGround(ground);
            GameObject tree = CreateTree(config, bark, leaves);
            TreeResource water = CreateResource(tree.transform, TreeResourceType.Water, config);
            TreeResource nutrients = CreateResource(tree.transform, TreeResourceType.Nutrients, config);
            Transform player = CreatePlayer(config, playerMaterial);
            Transform camera = CreateCamera(player);
            CreateBackdrop(camera);

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

        private static Transform CreateCamera(Transform followTarget)
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
            camera.farClipPlane = 100f;

            go.AddComponent<AudioListener>();

            SetReferences(go.AddComponent<CameraFollow>(), ("target", followTarget));
            return go.transform;
        }

        private static void CreateGround(Material material)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            float depth = GroundBackZ - GroundFrontZ;
            // Top face sits at y = 0.
            ground.transform.position = new Vector3(0f, -GroundThickness * 0.5f, GroundFrontZ + depth * 0.5f);
            ground.transform.localScale = new Vector3(GroundWidth, GroundThickness, depth);
            ground.GetComponent<Renderer>().sharedMaterial = material;
        }

        /// <summary>Flat, layered hills behind the play area; further layers are paler and drift more with the camera.</summary>
        private static void CreateBackdrop(Transform cameraTransform)
        {
            var root = new GameObject("Backdrop").transform;
            CreateHillLayer(root, cameraTransform, "Far Hills", depth: 30f, followFactor: 0.8f,
                new Color(0.74f, 0.85f, 0.84f), heightRange: new Vector2(4f, 6.5f), seed: 1);
            CreateHillLayer(root, cameraTransform, "Mid Hills", depth: 18f, followFactor: 0.55f,
                new Color(0.62f, 0.77f, 0.62f), heightRange: new Vector2(2.5f, 4f), seed: 2);
            CreateHillLayer(root, cameraTransform, "Near Hills", depth: 8f, followFactor: 0.25f,
                new Color(0.5f, 0.68f, 0.47f), heightRange: new Vector2(1.2f, 2.2f), seed: 3);
        }

        private static void CreateHillLayer(Transform parent, Transform cameraTransform, string layerName,
            float depth, float followFactor, Color color, Vector2 heightRange, int seed)
        {
            // Unlit flat colors read as painted cutouts rather than lit 3D shapes.
            Material material = GetOrCreateMaterial(layerName, color, unlit: true);

            var layer = new GameObject(layerName).transform;
            layer.SetParent(parent, false);
            // With the camera pitched down, distant objects appear higher on screen; lowering the layer by
            // the same amount lines its base up with the ground line.
            layer.position = new Vector3(0f, -depth * Mathf.Tan(CameraPitch * Mathf.Deg2Rad), depth);

            var random = new System.Random(seed);
            for (float x = -BackdropHalfWidth; x <= BackdropHalfWidth; x += random.NextFloat(5f, 9f))
            {
                float height = random.NextFloat(heightRange.x, heightRange.y);
                float width = height * random.NextFloat(2f, 3f);

                // A flattened sphere centered on the base line shows as a soft hill silhouette; the ground hides its lower half.
                GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hill.name = "Hill";
                hill.transform.SetParent(layer, false);
                hill.transform.localPosition = new Vector3(x, 0f, 0f);
                hill.transform.localScale = new Vector3(width, height * 2f, 0.1f);
                Object.DestroyImmediate(hill.GetComponent<Collider>());

                var renderer = hill.GetComponent<Renderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            var parallax = layer.gameObject.AddComponent<ParallaxLayer>();
            SetReferences(parallax, ("cameraTransform", cameraTransform));
            var serializedParallax = new SerializedObject(parallax);
            serializedParallax.FindProperty("followFactor").floatValue = followFactor;
            serializedParallax.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform CreatePlayer(GameConfig config, Material material)
        {
            // Placeholder body: a flat box reads as a rectangle from the side-on camera.
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Cube);
            player.name = "Player";
            player.transform.position = PlayerStartPosition;
            player.transform.localScale = PlayerSize;
            player.GetComponent<Renderer>().sharedMaterial = material;
            // Nothing uses physics yet; a collider can be added once interaction needs one.
            Object.DestroyImmediate(player.GetComponent<Collider>());

            var movement = player.AddComponent<PlayerMovement>();
            SetReferences(movement,
                ("moveAction", FindActionReference("Player", "Move")),
                ("config", config));
            var serializedMovement = new SerializedObject(movement);
            serializedMovement.FindProperty("walkableRangeX").vector2Value =
                new Vector2(-WalkableHalfWidth, WalkableHalfWidth);
            serializedMovement.ApplyModifiedPropertiesWithoutUndo();

            return player.transform;
        }

        /// <summary>Finds the reference asset Unity generates for an action inside the project's input actions.</summary>
        private static InputActionReference FindActionReference(string mapName, string actionName)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(InputActionsPath))
            {
                if (asset is InputActionReference reference && reference.action != null &&
                    reference.action.name == actionName && reference.action.actionMap.name == mapName)
                {
                    return reference;
                }
            }

            Debug.LogError($"No '{mapName}/{actionName}' action found in {InputActionsPath}.");
            return null;
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

            // Mouse-only: otherwise a selected slider would also react to the arrow keys used for walking.
            var noNavigation = new Navigation { mode = Navigation.Mode.None };
            slider.navigation = noNavigation;
            addButton.navigation = noNavigation;
            resetButton.navigation = noNavigation;

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

        private static Material GetOrCreateMaterial(string materialName, Color color, bool unlit = false)
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

            Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
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
