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
        private const string WaterBucketIconPath = ArtFolder + "/WaterBucketIcon.png";
        private const string CompostBucketIconPath = ArtFolder + "/CompostBucketIcon.png";
        private const string BrownLeafPath = ArtFolder + "/BrownLeaf.png";
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

        /// <summary>The water bucket's inventory icon: a blue bucket.</summary>
        public static Sprite GetOrCreateWaterBucketIcon()
        {
            return GetOrCreateBucketIcon(WaterBucketIconPath,
                body: new Color32(96, 156, 220, 255), rim: new Color32(66, 122, 188, 255));
        }

        /// <summary>The compost bucket's inventory icon: a brown bucket.</summary>
        public static Sprite GetOrCreateCompostBucketIcon()
        {
            return GetOrCreateBucketIcon(CompostBucketIconPath,
                body: new Color32(156, 108, 66, 255), rim: new Color32(120, 80, 48, 255));
        }

        /// <summary>A bucket icon (tapered body, darker rim and a handle, with an outline) in the given colors.</summary>
        private static Sprite GetOrCreateBucketIcon(string path, Color32 body, Color32 rim)
        {
            if (!File.Exists(path))
            {
                PaintBucketIcon(path, body, rim);
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void PaintBucketIcon(string path, Color32 bodyColor, Color32 rimColor)
        {
            const int size = 128;
            const int centerX = size / 2;
            // Handle and outline stay dark grey on every bucket, so only the body color tells them apart.
            var handleColor = new Color32(92, 100, 110, 255);
            var outlineColor = new Color32(62, 68, 78, 255);

            // Body narrows toward the bottom; the rim is a slightly wider band at the top.
            bool IsBody(int x, int y)
            {
                if (y < 14 || y >= 92)
                {
                    return false;
                }

                float halfWidth = Mathf.Lerp(34f, 46f, (y - 14) / 78f);
                return Mathf.Abs(x - centerX + 0.5f) < halfWidth;
            }

            bool IsRim(int x, int y) => y >= 86 && y < 98 && Mathf.Abs(x - centerX + 0.5f) < 50f;
            bool IsBucket(int x, int y) => IsBody(x, y) || IsRim(x, y);

            // An arch above the rim.
            bool IsHandle(int x, int y)
            {
                if (y < 96)
                {
                    return false;
                }

                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(centerX, 92f));
                return distance >= 38f && distance < 44f;
            }

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color32 color = Transparent;
                    if (IsBucket(x, y))
                    {
                        color = IsNearEdge(x, y, IsBucket, 4) ? outlineColor : IsRim(x, y) ? rimColor : bodyColor;
                    }
                    else if (IsHandle(x, y))
                    {
                        color = IsNearEdge(x, y, IsHandle, 2) ? outlineColor : handleColor;
                    }

                    pixels[y * size + x] = color;
                }
            }

            SaveSprite(path, size, size, pixels, new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// A fallen brown leaf, about 0.4 x 0.2 world units: a pointed oval with a center vein and a short stem,
        /// pivoted at its bottom center so it rests on the ground. Also used as the leaves' inventory icon.
        /// </summary>
        public static Sprite GetOrCreateBrownLeafSprite()
        {
            if (!File.Exists(BrownLeafPath))
            {
                PaintBrownLeaf();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(BrownLeafPath);
        }

        private static void PaintBrownLeaf()
        {
            const int width = 64;
            const int height = 32;
            const float centerX = 34f; // Shifted right to leave room for the stem on the left.
            const float centerY = height / 2f;
            const float halfLength = 28f;
            const float halfWidth = 13f;
            var leafColor = new Color32(176, 112, 58, 255);
            var veinColor = new Color32(118, 70, 36, 255);

            // Pointed at both ends: the half-width shrinks toward the tips like a parabola.
            bool IsBlade(int x, int y)
            {
                float along = (x + 0.5f - centerX) / halfLength;
                if (Mathf.Abs(along) >= 1f)
                {
                    return false;
                }

                return Mathf.Abs(y + 0.5f - centerY) < halfWidth * (1f - along * along);
            }

            bool IsStem(int x, int y) => x >= 1 && x < centerX - halfLength + 2 && Mathf.Abs(y + 0.5f - centerY) < 1.5f;

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color32 color = Transparent;
                    if (IsBlade(x, y))
                    {
                        bool isVein = Mathf.Abs(y + 0.5f - centerY) < 1f && Mathf.Abs(x + 0.5f - centerX) < halfLength * 0.8f;
                        color = isVein || IsNearEdge(x, y, IsBlade, 2) ? veinColor : leafColor;
                    }
                    else if (IsStem(x, y))
                    {
                        color = veinColor;
                    }

                    pixels[y * width + x] = color;
                }
            }

            SaveSprite(BrownLeafPath, width, height, pixels, new Vector2(0.5f, 0f), pixelsPerUnit: 160f);
        }

        /// <summary>True for pixels inside a shape but within <paramref name="distance"/> of its edge, used for outlines.</summary>
        private static bool IsNearEdge(int x, int y, System.Func<int, int, bool> isInside, int distance)
        {
            return !isInside(x - distance, y) || !isInside(x + distance, y) ||
                   !isInside(x, y - distance) || !isInside(x, y + distance);
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

        private static void SaveSprite(string path, int width, int height, Color32[] pixels, Vector2 pivot,
            float pixelsPerUnit = PixelsPerUnit)
        {
            BuilderHelpers.EnsureFolder(ArtFolder);

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
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
