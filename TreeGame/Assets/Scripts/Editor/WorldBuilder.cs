using TreeGame.Cameras;
using TreeGame.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static TreeGame.EditorTools.BuilderHelpers;
using static TreeGame.EditorTools.SceneLayout;

namespace TreeGame.EditorTools
{
    /// <summary>Builds the stage of the game scene: lighting, camera, ground, fences and the layered backdrop.</summary>
    internal static class WorldBuilder
    {
        public static void ConfigureEnvironment()
        {
            // Flat gradient ambient instead of a skybox: soft, cozy and needs no lighting bake.
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.75f, 0.82f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.68f, 0.7f, 0.62f);
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.38f, 0.3f);
            RenderSettings.fog = false;
        }

        public static void CreateSun()
        {
            var go = new GameObject("Sun");
            go.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.94f, 0.84f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
        }

        public static Transform CreateCamera(Transform followTarget)
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            Quaternion rotation = Quaternion.Euler(CameraPitch, 0f, 0f);
            go.transform.SetPositionAndRotation(CameraTarget - rotation * Vector3.forward * CameraDistance, rotation);

            var camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = CameraOrthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SkyColor;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 100f;

            // No AudioListener here: it stays on with the background music, while this camera switches off for minigames.
            SetReferences(go.AddComponent<CameraFollow>(), ("target", followTarget));
            return go.transform;
        }

        public static void CreateGround(Material material)
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
        public static void CreateFences(Sprite near, Sprite far)
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
        public static void CreateBackdrop(Transform cameraTransform)
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
    }
}
