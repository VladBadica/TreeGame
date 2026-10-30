using System.Linq;
using TMPro;
using TreeGame.Cameras;
using TreeGame.Care;
using TreeGame.Core;
using TreeGame.DevTools;
using TreeGame.Interaction;
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

        // How far the player may walk to either side of the tree (which sits at the origin).
        // Ground and backdrop are sized from this so the world never ends on screen.
        private const float MaxDistanceFromTree = 8f;
        // Extra world beyond the walk limit, wider than half the camera view, so no edge is ever visible.
        private const float WorldEdgeMargin = 20f;
        // Space between the walk limit and a fence's inner edge, about half the character's width,
        // so the player stops just in front of the fence instead of inside it.
        private const float FenceClearance = 0.35f;

        // The well stands just inside the right fence, a little behind the player's walking line (level with the tree),
        // so the player walks past in front of it.
        private static readonly Vector3 WellPosition = new Vector3(MaxDistanceFromTree - 1.8f, 0f, 0.3f);

        // The compost heap sits just left of the tree, at the same depth as the well, clear of the trunk and lowest branches.
        private static readonly Vector3 CompostPosition = new Vector3(-2.5f, 0f, 0.3f);

        // The ground is a shallow strip seen almost side-on, like a stage: wide along X, shallow in depth (Z).
        // It is thick so its front face always fills the bottom of the screen.
        private const float GroundWidth = 2f * (MaxDistanceFromTree + WorldEdgeMargin);
        private const float GroundThickness = 4f;
        private const float GroundFrontZ = -3.5f;
        private const float GroundBackZ = 1.5f;

        // The player walks in front of the tree (negative Z is toward the camera) and starts just left of it.
        // The player's origin is at its feet.
        private static readonly Vector3 PlayerStartPosition = new Vector3(-1.5f, 0f, -2f);

        // Camera framing: centered on the player, nearly side-on. A small pitch still shows a sliver of ground top.
        private static readonly Vector3 CameraTarget = new Vector3(PlayerStartPosition.x, 2f, 0f);
        private const float CameraPitch = 5f;
        private const float CameraDistance = 20f;
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
            PlaceholderArt.GetOrCreateFenceSprites(out Sprite fenceNear, out Sprite fenceFar);
            AssetDatabase.SaveAssets();

            ConfigureEnvironment();
            CreateSun();
            CreateGround(ground);
            CreateFences(fenceNear, fenceFar);
            CreateWell();
            CreateCompostHeap();
            GameObject tree = CreateTree(config, bark, leaves);
            TreeResource water = CreateResource(tree.transform, TreeResourceType.Water, config);
            TreeResource nutrients = CreateResource(tree.transform, TreeResourceType.Nutrients, config);
            Transform player = CreatePlayer(config, tree.transform);
            Transform camera = CreateCamera(player);
            var viewCamera = camera.GetComponent<Camera>();
            SetReferences(player.GetComponent<CharacterHeadLook>(), ("viewCamera", viewCamera));
            CreateBackdrop(camera);

            CreateEventSystem();
            Transform hud = CreateCanvas("HUD", 0);
            CreateResourceHud(hud, water, nutrients);
            CreateInteractionPrompt(hud, player.GetComponent<Interactor>(), viewCamera);
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

        /// <summary>
        /// A fence at each end of the walkable area that recedes from near the camera into the distance.
        /// The perspective is painted into two sprites (see PlaceholderArt), both pivoted where the fence crosses
        /// the player's walking line, which is placed at the walk limit.
        /// </summary>
        private static void CreateFences(Sprite near, Sprite far)
        {
            if (near == null || far == null)
            {
                Debug.LogError("Fence sprites missing; fences were not created.");
                return;
            }

            // The far half sits just behind the player so the player is drawn in front of it.
            float farZ = PlayerStartPosition.z + 0.1f;
            // The near half sits in front of the ground's front edge, otherwise the ground would hide its lower part.
            float nearZ = GroundFrontZ - 0.1f;
            // Moving it toward the camera along the view direction keeps it on the same spot on screen;
            // with the camera pitched down, that means raising it slightly.
            float nearLift = (farZ - nearZ) * Mathf.Tan(CameraPitch * Mathf.Deg2Rad);

            var root = new GameObject("Fences").transform;
            foreach ((string fenceName, float side) in new[] { ("Left Fence", -1f), ("Right Fence", 1f) })
            {
                var fence = new GameObject(fenceName).transform;
                fence.SetParent(root, false);
                float x = side * (MaxDistanceFromTree + FenceClearance);

                // The art recedes toward the tree as drawn for the right side; the left side is mirrored.
                bool mirrored = side < 0f;
                CreateFenceHalf("Near", near, fence, new Vector3(x, nearLift, nearZ), mirrored);
                CreateFenceHalf("Far", far, fence, new Vector3(x, 0f, farZ), mirrored);
            }
        }

        /// <summary>Placeholder compost heap: a single box of dark soil resting on the ground.</summary>
        private static void CreateCompostHeap()
        {
            Material compost = GetOrCreateMaterial("Compost", new Color(0.36f, 0.26f, 0.18f));

            var heap = new GameObject("Compost Heap").transform;
            heap.position = CompostPosition;
            // Raised by half its height so the box rests on the ground.
            CreatePart(PrimitiveType.Cube, "Heap", heap, new Vector3(0f, 0.3f, 0f), new Vector3(1f, 0.6f, 0.8f), compost);
        }

        /// <summary>Placeholder well built from boxes, about 2.2 units tall with its base at ground level.</summary>
        private static void CreateWell()
        {
            Material stone = GetOrCreateMaterial("Well Stone", new Color(0.62f, 0.6f, 0.58f));
            Material rim = GetOrCreateMaterial("Well Rim", new Color(0.5f, 0.48f, 0.47f));
            Material wood = GetOrCreateMaterial("Well Wood", new Color(0.55f, 0.38f, 0.24f));
            Material roof = GetOrCreateMaterial("Well Roof", new Color(0.66f, 0.3f, 0.24f));
            Material rope = GetOrCreateMaterial("Well Rope", new Color(0.85f, 0.76f, 0.56f));

            var well = new GameObject("Well").transform;
            well.position = WellPosition;

            // Cube primitives are 1 unit, so scale is size and position is the box's center.
            CreatePart(PrimitiveType.Cube, "Base", well, new Vector3(0f, 0.35f, 0f), new Vector3(1.2f, 0.7f, 0.8f), stone);
            CreatePart(PrimitiveType.Cube, "Rim", well, new Vector3(0f, 0.75f, 0f), new Vector3(1.3f, 0.1f, 0.9f), rim);

            // Posts end where they meet the underside of the roof.
            CreatePart(PrimitiveType.Cube, "Left Post", well, new Vector3(-0.5f, 1.3f, 0f), new Vector3(0.1f, 1f, 0.1f), wood);
            CreatePart(PrimitiveType.Cube, "Right Post", well, new Vector3(0.5f, 1.3f, 0f), new Vector3(0.1f, 1f, 0.1f), wood);
            CreatePart(PrimitiveType.Cube, "Crossbeam", well, new Vector3(0f, 1.55f, 0f), new Vector3(1.1f, 0.08f, 0.08f), wood);

            CreatePart(PrimitiveType.Cube, "Rope", well, new Vector3(0f, 1.33f, 0f), new Vector3(0.02f, 0.36f, 0.02f), rope);
            CreatePart(PrimitiveType.Cube, "Bucket", well, new Vector3(0f, 1.05f, 0f), new Vector3(0.2f, 0.2f, 0.2f), wood);

            // Two panels tilted 35 degrees meet at a ridge above the center; their outer ends overhang the posts.
            const float roofPitch = 35f;
            Transform leftRoof = CreatePart(PrimitiveType.Cube, "Left Roof", well,
                new Vector3(-0.33f, 1.92f, 0f), new Vector3(0.8f, 0.06f, 1f), roof);
            leftRoof.localRotation = Quaternion.Euler(0f, 0f, roofPitch);
            Transform rightRoof = CreatePart(PrimitiveType.Cube, "Right Roof", well,
                new Vector3(0.33f, 1.92f, 0f), new Vector3(0.8f, 0.06f, 1f), roof);
            rightRoof.localRotation = Quaternion.Euler(0f, 0f, -roofPitch);

            // Interaction zone: a little wider than the well, and deep enough to reach the player's walking line.
            var zone = new GameObject("Interaction Zone", typeof(BoxCollider));
            zone.transform.SetParent(well, false);
            var zoneCollider = zone.GetComponent<BoxCollider>();
            zoneCollider.isTrigger = true;
            zoneCollider.center = new Vector3(0f, 1f, 0f);
            zoneCollider.size = new Vector3(2.4f, 2f, 6f);

            Transform promptAnchor = CreatePivot("Prompt Anchor", well, new Vector3(0f, 2.5f, 0f));
            var interactable = zone.AddComponent<Interactable>();
            SetReferences(interactable, ("promptAnchor", promptAnchor));
            var serializedInteractable = new SerializedObject(interactable);
            serializedInteractable.FindProperty("actionDescription").stringValue = "get water";
            serializedInteractable.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateFenceHalf(string objectName, Sprite sprite, Transform parent, Vector3 position, bool mirrored)
        {
            var half = new GameObject(objectName, typeof(SpriteRenderer));
            half.transform.SetParent(parent, false);
            half.transform.position = position;

            var spriteRenderer = half.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            // Flips around the pivot (the crossing point), so the mirrored fence still meets the walk limit.
            spriteRenderer.flipX = mirrored;
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
            // A layer drifts with the camera, so it only needs to cover the part of the walk range it doesn't follow.
            float halfWidth = MaxDistanceFromTree * (1f - followFactor) + WorldEdgeMargin;
            for (float x = -halfWidth; x <= halfWidth; x += random.NextFloat(5f, 9f))
            {
                float height = random.NextFloat(heightRange.x, heightRange.y);
                float width = height * random.NextFloat(2f, 3f);

                // A flattened sphere centered on the base line shows as a soft hill silhouette; the ground hides its lower half.
                Transform hill = CreatePart(PrimitiveType.Sphere, "Hill", layer,
                    new Vector3(x, 0f, 0f), new Vector3(width, height * 2f, 0.1f), material);

                var renderer = hill.GetComponent<Renderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            var parallax = layer.gameObject.AddComponent<ParallaxLayer>();
            SetReferences(parallax, ("cameraTransform", cameraTransform));
            var serializedParallax = new SerializedObject(parallax);
            serializedParallax.FindProperty("followFactor").floatValue = followFactor;
            serializedParallax.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform CreatePlayer(GameConfig config, Transform tree)
        {
            var player = new GameObject("Player");
            player.transform.position = PlayerStartPosition;

            CharacterRig rig = CreateCharacterModel(player.transform);

            var walkAnimation = player.AddComponent<CharacterWalkAnimation>();
            SetReferences(walkAnimation,
                ("body", rig.Body),
                ("leftLeg", rig.LeftLeg),
                ("rightLeg", rig.RightLeg),
                ("leftArm", rig.LeftArm),
                ("rightArm", rig.RightArm));

            // The camera is assigned once it exists; see CreatePrototypeScene.
            SetReferences(player.AddComponent<CharacterHeadLook>(),
                ("pointAction", FindActionReference("UI", "Point")),
                ("head", rig.Head),
                ("walkAnimation", walkAnimation));

            // Trigger capsule + kinematic body: detects interaction zones without any physical collisions.
            var body = player.AddComponent<CapsuleCollider>();
            body.isTrigger = true;
            body.center = new Vector3(0f, 0.9f, 0f);
            body.height = 1.8f;
            body.radius = 0.3f;
            var rigidbody = player.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            SetReferences(player.AddComponent<Interactor>(), ("interactAction", FindActionReference("Player", "Interact")));

            var movement = player.AddComponent<PlayerMovement>();
            SetReferences(movement,
                ("moveAction", FindActionReference("Player", "Move")),
                ("config", config),
                ("walkAreaCenter", tree));
            var serializedMovement = new SerializedObject(movement);
            serializedMovement.FindProperty("maxDistanceFromCenter").floatValue = MaxDistanceFromTree;
            serializedMovement.ApplyModifiedPropertiesWithoutUndo();

            return player.transform;
        }

        /// <summary>The animated pivots of the placeholder character.</summary>
        private readonly struct CharacterRig
        {
            public readonly Transform Body;
            public readonly Transform Head;
            public readonly Transform LeftLeg;
            public readonly Transform RightLeg;
            public readonly Transform LeftArm;
            public readonly Transform RightArm;

            public CharacterRig(Transform body, Transform head, Transform leftLeg, Transform rightLeg,
                Transform leftArm, Transform rightArm)
            {
                Body = body;
                Head = head;
                LeftLeg = leftLeg;
                RightLeg = rightLeg;
                LeftArm = leftArm;
                RightArm = rightArm;
            }
        }

        /// <summary>
        /// Placeholder gardener built from primitives, about 1.8 units tall with its feet at the origin.
        /// The front faces local -Z (toward the camera); the walk animation turns it toward the walking direction.
        /// Limbs and head hang from empty pivots at the joints (hips, shoulders, neck) so they rotate from there.
        /// </summary>
        private static CharacterRig CreateCharacterModel(Transform parent)
        {
            Material skin = GetOrCreateMaterial("Player Skin", new Color(0.96f, 0.8f, 0.66f));
            Material shirt = GetOrCreateMaterial("Player Shirt", new Color(0.86f, 0.46f, 0.36f));
            Material pants = GetOrCreateMaterial("Player Pants", new Color(0.33f, 0.38f, 0.52f));
            Material hat = GetOrCreateMaterial("Player Hat", new Color(0.88f, 0.75f, 0.45f));
            Material eyes = GetOrCreateMaterial("Player Eyes", new Color(0.12f, 0.1f, 0.1f));

            Transform body = new GameObject("Body").transform;
            body.SetParent(parent, false);

            // Unity's capsule and cylinder are 2 units tall at scale 1, so a Y scale of h/2 gives height h.
            Transform leftLeg = CreatePivot("Left Leg", body, new Vector3(-0.1f, 0.5f, 0f));
            Transform rightLeg = CreatePivot("Right Leg", body, new Vector3(0.1f, 0.5f, 0f));
            CreatePart(PrimitiveType.Capsule, "Leg", leftLeg, new Vector3(0f, -0.25f, 0f), new Vector3(0.16f, 0.25f, 0.16f), pants);
            CreatePart(PrimitiveType.Capsule, "Leg", rightLeg, new Vector3(0f, -0.25f, 0f), new Vector3(0.16f, 0.25f, 0.16f), pants);

            CreatePart(PrimitiveType.Capsule, "Torso", body, new Vector3(0f, 0.85f, 0f), new Vector3(0.42f, 0.38f, 0.34f), shirt);

            Transform leftArm = CreatePivot("Left Arm", body, new Vector3(-0.27f, 1.12f, 0f));
            Transform rightArm = CreatePivot("Right Arm", body, new Vector3(0.27f, 1.12f, 0f));
            CreatePart(PrimitiveType.Capsule, "Arm", leftArm, new Vector3(0f, -0.2f, 0f), new Vector3(0.12f, 0.2f, 0.12f), skin);
            CreatePart(PrimitiveType.Capsule, "Arm", rightArm, new Vector3(0f, -0.2f, 0f), new Vector3(0.12f, 0.2f, 0.12f), skin);

            // Eyes and hat are children of the neck pivot so they turn with the head.
            Transform head = CreatePivot("Head", body, new Vector3(0f, 1.28f, 0f));
            CreatePart(PrimitiveType.Sphere, "Skull", head, new Vector3(0f, 0.19f, 0f), Vector3.one * 0.42f, skin);
            CreatePart(PrimitiveType.Sphere, "Left Eye", head, new Vector3(-0.08f, 0.22f, -0.19f), Vector3.one * 0.06f, eyes);
            CreatePart(PrimitiveType.Sphere, "Right Eye", head, new Vector3(0.08f, 0.22f, -0.19f), Vector3.one * 0.06f, eyes);
            CreatePart(PrimitiveType.Cylinder, "Hat Brim", head, new Vector3(0f, 0.38f, 0f), new Vector3(0.56f, 0.015f, 0.56f), hat);
            CreatePart(PrimitiveType.Cylinder, "Hat Crown", head, new Vector3(0f, 0.46f, 0f), new Vector3(0.32f, 0.08f, 0.32f), hat);

            return new CharacterRig(body, head, leftLeg, rightLeg, leftArm, rightArm);
        }

        private static Transform CreatePivot(string objectName, Transform parent, Vector3 localPosition)
        {
            var pivot = new GameObject(objectName).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = localPosition;
            return pivot;
        }

        /// <summary>A collider-free primitive; nothing uses physics yet, colliders can be added when interaction needs them.</summary>
        private static Transform CreatePart(PrimitiveType type, string objectName, Transform parent,
            Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = objectName;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            return part.transform;
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

            SetReferences(go.AddComponent<TreeWindSway>(),
                ("visuals", visuals),
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

            TMP_Text label = CreateText("Growth Label", 24f, Color.white, TextAlignmentOptions.MidlineLeft);
            AddToLayout(label.gameObject, panel.transform, 32f);
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

        /// <summary>A small rounded popup that the prompt component moves above whatever the player can interact with.</summary>
        private static void CreateInteractionPrompt(Transform canvas, Interactor interactor, Camera viewCamera)
        {
            // The component lives on an always-active object; it shows and hides the popup child.
            var promptRoot = new GameObject("Interaction Prompt", typeof(RectTransform));
            StretchInParent(promptRoot, canvas);

            var popup = new GameObject("Popup", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            var popupRect = (RectTransform)popup.transform;
            popupRect.SetParent(promptRoot.transform, false);
            // Anchored at one point and pivoted at its bottom center, so it sits just above the target position.
            popupRect.anchorMin = popupRect.anchorMax = Vector2.zero;
            popupRect.pivot = new Vector2(0.5f, 0f);
            StyleImage(popup.GetComponent<Image>(), new Color(0f, 0f, 0f, 0.6f));

            var layout = popup.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 8, 8);
            layout.childControlWidth = layout.childControlHeight = true;
            var fitter = popup.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TMP_Text label = CreateText("Label", 24f, Color.white, TextAlignmentOptions.Center);
            label.transform.SetParent(popupRect, false);
            label.text = "Press F to interact"; // Placeholder; the prompt builds the real text from the binding.
            popup.SetActive(false);

            SetReferences(promptRoot.AddComponent<InteractionPrompt>(),
                ("interactor", interactor),
                ("viewCamera", viewCamera),
                ("popup", popupRect),
                ("label", label));
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
            // TMP's variant of the standard button, so its caption is a TextMeshPro label.
            GameObject go = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources { standard = resources.standard });
            go.name = objectName;
            go.transform.SetParent(parent, false);

            TMP_Text text = go.GetComponentInChildren<TMP_Text>();
            StyleText(text, 20f, new Color(0.2f, 0.2f, 0.2f), TextAlignmentOptions.Center);
            text.text = caption;

            return go.GetComponent<Button>();
        }

        /// <summary>A TextMeshPro label; TMP renders crisp text at any size and resolution.</summary>
        private static TMP_Text CreateText(string objectName, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            TMP_Text text = go.GetComponent<TMP_Text>();
            StyleText(text, fontSize, color, alignment);
            return text;
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

        private static void StyleText(TMP_Text text, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            // The font comes from TMP's default (set up by Window > TextMeshPro > Import TMP Essential Resources).
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
