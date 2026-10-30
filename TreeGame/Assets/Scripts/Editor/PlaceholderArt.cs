using System.IO;
using UnityEditor;
using UnityEngine;

namespace TreeGame.EditorTools
{
    /// <summary>
    /// Paints simple placeholder sprites as PNG files, so the prototype needs no imported art.
    /// Existing files are never overwritten: replace a PNG with real art (same name) and the scene picks it up.
    /// </summary>
    public static class PlaceholderArt
    {
        private const string ArtFolder = "Assets/Art";
        private const string FenceNearPath = ArtFolder + "/FenceNear.png";
        private const string FenceFarPath = ArtFolder + "/FenceFar.png";
        private const float PixelsPerUnit = 100f;

        // The camera is orthographic, so depth has to be painted in. The fence is drawn in one-point perspective:
        // each point at perspective depth d is pulled toward the vanishing point by 1/d (d = 1 is the nearest post).
        // Coordinates are world units relative to the "crossing": where the fence line passes the player's
        // walking depth at ground level. Both sprites are pivoted there, which makes placing them simple.
        private const float CrossingDepth = 2.5f;
        private const float FarthestDepth = 14f;
        private const float PostDepthSpacing = 0.75f;
        // The fence starts one post in from d = 1: a post that close filled too much of the foreground.
        private const float NearestPostDepth = 1f + PostDepthSpacing;
        // Up toward the horizon and inward (toward the tree), as seen from a camera standing on the tree's side.
        private static readonly Vector2 VanishingPoint = new Vector2(-1.2f, 0.35f);
        private const float PostHeightAtCrossing = 1f;
        private const float PostWidthAtCrossing = 0.12f;
        private const float OutlineAtCrossing = 0.025f;
        // Rail bands as fractions of the post height (bottom, top).
        private static readonly (float bottom, float top)[] Rails = { (0.3f, 0.42f), (0.68f, 0.8f) };

        private static readonly Color32 PostColor = new Color32(222, 186, 134, 255);
        private static readonly Color32 RailColor = new Color32(194, 154, 106, 255);
        private static readonly Color32 OutlineColor = new Color32(128, 92, 62, 255);
        private static readonly Color32 Transparent = new Color32(0, 0, 0, 0);

        /// <summary>
        /// A wooden fence receding from near the camera into the distance, split into the part nearer than the
        /// player's walking line and the part beyond it, so each can be drawn at the right depth.
        /// Both sprites share one canvas and pivot (the crossing point), so they line up when placed together.
        /// </summary>
        public static void GetOrCreateFenceSprites(out Sprite near, out Sprite far)
        {
            if (!File.Exists(FenceNearPath) || !File.Exists(FenceFarPath))
            {
                new PerspectiveFencePainter().Paint(FenceNearPath, FenceFarPath);
            }

            near = AssetDatabase.LoadAssetAtPath<Sprite>(FenceNearPath);
            far = AssetDatabase.LoadAssetAtPath<Sprite>(FenceFarPath);
        }

        /// <summary>Rasterizes the fence back to front (painter's algorithm), remembering each pixel's depth.</summary>
        private sealed class PerspectiveFencePainter
        {
            // Where the fence line would meet the ground at d = 1; the reference the perspective is computed from.
            private readonly Vector2 baseAtDepthOne = VanishingPoint * (1f - CrossingDepth);
            private readonly Vector2 canvasMin;
            private readonly int width;
            private readonly int height;
            private readonly Color32[] pixels;
            private readonly float[] pixelDepths;

            public PerspectiveFencePainter()
            {
                // The nearest post is the lowest, tallest and outermost element; the farthest the most inward.
                Vector2 nearestPostBase = Base(NearestPostDepth);
                float nearestHalfWidth = PostWidthAtCrossing * Scale(NearestPostDepth) * 0.5f + Outline(NearestPostDepth);
                float farthestHalfWidth = PostWidthAtCrossing * Scale(FarthestDepth) * 0.5f + Outline(FarthestDepth);
                const float margin = 0.02f;

                canvasMin = new Vector2(Base(FarthestDepth).x - farthestHalfWidth - margin,
                    nearestPostBase.y - Outline(NearestPostDepth) - margin);
                var canvasMax = new Vector2(nearestPostBase.x + nearestHalfWidth + margin,
                    nearestPostBase.y + PostHeightAtCrossing * Scale(NearestPostDepth) + Outline(NearestPostDepth) + margin);

                width = Mathf.CeilToInt((canvasMax.x - canvasMin.x) * PixelsPerUnit);
                height = Mathf.CeilToInt((canvasMax.y - canvasMin.y) * PixelsPerUnit);
                pixels = new Color32[width * height];
                pixelDepths = new float[width * height];
            }

            public void Paint(string nearPath, string farPath)
            {
                // Rails first so posts sit on top of them; each element outline first, then fill.
                foreach ((float bottom, float top) in Rails)
                {
                    PaintRail(bottom, top, OutlineColor, outlined: true);
                    PaintRail(bottom, top, RailColor, outlined: false);
                }

                int postCount = Mathf.FloorToInt((FarthestDepth - NearestPostDepth) / PostDepthSpacing) + 1;
                for (int i = postCount - 1; i >= 0; i--)
                {
                    PaintPost(NearestPostDepth + i * PostDepthSpacing);
                }

                var nearPixels = new Color32[pixels.Length];
                var farPixels = new Color32[pixels.Length];
                for (int i = 0; i < pixels.Length; i++)
                {
                    bool isNear = pixelDepths[i] < CrossingDepth;
                    nearPixels[i] = isNear ? pixels[i] : Transparent;
                    farPixels[i] = isNear ? Transparent : pixels[i];
                }

                var pivot = new Vector2(-canvasMin.x * PixelsPerUnit / width, -canvasMin.y * PixelsPerUnit / height);
                SaveSprite(nearPath, width, height, nearPixels, pivot);
                SaveSprite(farPath, width, height, farPixels, pivot);
            }

            private void PaintRail(float bottomFraction, float topFraction, Color32 color, bool outlined)
            {
                // Sweep from far to near in steps small enough to move under half a pixel sideways each time.
                float horizontalSpread = Mathf.Abs(baseAtDepthOne.x - VanishingPoint.x);
                for (float d = FarthestDepth; d >= NearestPostDepth;)
                {
                    float postHeight = PostHeightAtCrossing * Scale(d);
                    float grow = outlined ? Outline(d) : 0f;
                    Vector2 basePoint = Base(d);
                    FillRect(basePoint.x - 0.01f, basePoint.x + 0.01f,
                        basePoint.y + postHeight * bottomFraction - grow,
                        basePoint.y + postHeight * topFraction + grow, color, d);

                    float step = 0.5f / PixelsPerUnit / horizontalSpread * d * d;
                    d -= Mathf.Max(step, 0.001f);
                }
            }

            private void PaintPost(float d)
            {
                Vector2 basePoint = Base(d);
                float halfWidth = PostWidthAtCrossing * Scale(d) * 0.5f;
                float top = basePoint.y + PostHeightAtCrossing * Scale(d);
                float outline = Outline(d);

                FillRect(basePoint.x - halfWidth - outline, basePoint.x + halfWidth + outline,
                    basePoint.y - outline, top + outline, OutlineColor, d);
                FillRect(basePoint.x - halfWidth, basePoint.x + halfWidth, basePoint.y, top, PostColor, d);
            }

            /// <summary>Fills a world-space rectangle; later calls overwrite earlier ones (nearer things are drawn later).</summary>
            private void FillRect(float xMin, float xMax, float yMin, float yMax, Color32 color, float depth)
            {
                int left = Mathf.Max(0, Mathf.FloorToInt((xMin - canvasMin.x) * PixelsPerUnit));
                int right = Mathf.Min(width - 1, Mathf.CeilToInt((xMax - canvasMin.x) * PixelsPerUnit) - 1);
                int bottom = Mathf.Max(0, Mathf.FloorToInt((yMin - canvasMin.y) * PixelsPerUnit));
                int top = Mathf.Min(height - 1, Mathf.CeilToInt((yMax - canvasMin.y) * PixelsPerUnit) - 1);

                for (int y = bottom; y <= top; y++)
                {
                    for (int x = left; x <= right; x++)
                    {
                        pixels[y * width + x] = color;
                        pixelDepths[y * width + x] = depth;
                    }
                }
            }

            private Vector2 Base(float d) => VanishingPoint + (baseAtDepthOne - VanishingPoint) / d;

            // Size relative to the crossing: 1 at the player's walking line, bigger nearer the camera.
            private static float Scale(float d) => CrossingDepth / d;

            // At least one pixel, so distant posts keep a visible outline.
            private static float Outline(float d) => Mathf.Max(OutlineAtCrossing * Scale(d), 1f / PixelsPerUnit);
        }

        private static void SaveSprite(string path, int width, int height, Color32[] pixels, Vector2 pivot)
        {
            if (!AssetDatabase.IsValidFolder(ArtFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Art");
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
