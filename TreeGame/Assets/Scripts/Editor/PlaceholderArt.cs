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
        private const string CoinIconPath = ArtFolder + "/CoinIcon.png";
        private const string InsectPath = ArtFolder + "/Insect.png";
        private const string NotificationBubblePath = ArtFolder + "/NotificationBubble.png";
        private const string BarkPath = ArtFolder + "/Bark.png";
        private const string CrackPath = ArtFolder + "/Crack.png";
        private const string BarkTexturePath = ArtFolder + "/BarkTexture.png";
        private const string LeafTexturePath = ArtFolder + "/LeafTexture.png";
        private const string RingPath = ArtFolder + "/Ring.png";
        private const string DiscPath = ArtFolder + "/Disc.png";
        private const string CopyIconPath = ArtFolder + "/CopyIcon.png";
        private const string TickIconPath = ArtFolder + "/TickIcon.png";
        private const string TrunkTonicIconPath = ArtFolder + "/TrunkTonicIcon.png";
        private const string BranchTonicIconPath = ArtFolder + "/BranchTonicIcon.png";
        private const string LeafTonicIconPath = ArtFolder + "/LeafTonicIcon.png";
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

        /// <summary>The coin's inventory icon: a gold coin with a raised rim and a leaf stamped in its face.</summary>
        public static Sprite GetOrCreateCoinIcon()
        {
            if (!File.Exists(CoinIconPath))
            {
                PaintCoinIcon();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(CoinIconPath);
        }

        private static void PaintCoinIcon()
        {
            const int size = 128;
            const float center = size / 2f;
            const float outerRadius = 60f;
            const float rimRadius = 55f;
            const float faceRadius = 46f;
            const float leafHalfLength = 28f;
            const float leafHalfWidth = 15f;
            var outlineColor = new Color32(138, 94, 26, 255);
            var rimColor = new Color32(250, 208, 88, 255);
            var faceColor = new Color32(232, 176, 58, 255);
            var leafColor = new Color32(176, 124, 34, 255);

            // Pointed at both ends, like the fallen leaf: the half-width shrinks toward the tips like a parabola.
            bool IsLeaf(float dx, float dy)
            {
                float along = dy / leafHalfLength;
                return Mathf.Abs(along) < 1f && Mathf.Abs(dx) < leafHalfWidth * (1f - along * along);
            }

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - center;
                    float dy = y + 0.5f - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    Color32 color = distance >= rimRadius ? outlineColor
                        : distance >= faceRadius ? rimColor
                        : IsLeaf(dx, dy) ? leafColor
                        : faceColor;

                    // Fades over one pixel at the edge, which smooths the circle.
                    color.a = (byte)(Mathf.Clamp01(outerRadius - distance) * 255f);
                    pixels[y * size + x] = color;
                }
            }

            SaveSprite(CoinIconPath, size, size, pixels, new Vector2(0.5f, 0.5f));
        }

        // Where the symbol on a tonic bottle sits: the middle of the bottle's round body, below the liquid's surface.
        private static readonly Vector2 TonicSymbolCenter = new Vector2(64f, 42f);

        /// <summary>The trunk tonic's picture: a bottle of amber liquid marked with tree rings, as on the cut end of a log.</summary>
        public static Sprite GetOrCreateTrunkTonicIcon()
        {
            return GetOrCreateTonicIcon(TrunkTonicIconPath, new Color32(196, 120, 52, 255), (x, y) =>
            {
                float distance = Vector2.Distance(new Vector2(x, y), TonicSymbolCenter);
                return (distance >= 15f && distance < 19f) || (distance >= 8.5f && distance < 11.5f) || distance < 4.5f;
            });
        }

        /// <summary>The branch tonic's picture: a bottle of golden liquid marked with a forking branch.</summary>
        public static Sprite GetOrCreateBranchTonicIcon()
        {
            return GetOrCreateTonicIcon(BranchTonicIconPath, new Color32(222, 172, 48, 255), (x, y) =>
            {
                var point = new Vector2(x, y);
                Vector2 c = TonicSymbolCenter;
                Vector2 fork = c + new Vector2(0f, -2f);
                return SegmentDistance(point, c + new Vector2(0f, -18f), fork) < 3.5f ||
                       SegmentDistance(point, fork, c + new Vector2(-13f, 16f)) < 3f ||
                       SegmentDistance(point, fork, c + new Vector2(13f, 16f)) < 3f ||
                       SegmentDistance(point, c + new Vector2(-6f, 6f), c + new Vector2(-16f, 4f)) < 2.5f;
            });
        }

        /// <summary>The leaf tonic's picture: a bottle of green liquid marked with a leaf.</summary>
        public static Sprite GetOrCreateLeafTonicIcon()
        {
            return GetOrCreateTonicIcon(LeafTonicIconPath, new Color32(104, 176, 78, 255), (x, y) =>
            {
                // Pointed at both ends, like the coin's leaf: the half-width shrinks toward the tips like a parabola.
                float dx = x - TonicSymbolCenter.x;
                float along = (y - TonicSymbolCenter.y) / 19f;
                bool isBlade = Mathf.Abs(along) < 1f && Mathf.Abs(dx) < 11f * (1f - along * along);
                bool isVein = Mathf.Abs(dx) < 1.2f && Mathf.Abs(along) < 0.8f;
                return isBlade && !isVein;
            });
        }

        /// <summary>
        /// A round glass bottle with a cork, two thirds full of liquid in the given color, with a light symbol on the
        /// liquid saying what the tonic is for. <paramref name="isSymbol"/> takes pixel middles.
        /// </summary>
        private static Sprite GetOrCreateTonicIcon(string path, Color32 liquidColor, System.Func<float, float, bool> isSymbol)
        {
            if (!File.Exists(path))
            {
                PaintTonicIcon(path, liquidColor, isSymbol);
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void PaintTonicIcon(string path, Color32 liquidColor, System.Func<float, float, bool> isSymbol)
        {
            const int size = 128;
            const float centerX = size / 2f;
            var bodyCenter = new Vector2(centerX, 44f);
            const float bodyRadius = 38f;
            const float liquidTop = 62f;
            var glassColor = new Color32(214, 236, 240, 255);
            var corkColor = new Color32(176, 132, 90, 255);
            var outlineColor = new Color32(62, 68, 78, 255);
            var symbolColor = new Color32(255, 248, 230, 255);
            var shineColor = new Color32(255, 255, 255, 255);

            bool IsBody(int x, int y) => Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), bodyCenter) < bodyRadius;
            bool IsNeck(int x, int y) => y >= 70 && y < 104 && Mathf.Abs(x + 0.5f - centerX) < 11f;
            bool IsCork(int x, int y) => y >= 100 && y < 120 && Mathf.Abs(x + 0.5f - centerX) < 13f;
            bool IsBottle(int x, int y) => IsBody(x, y) || IsNeck(x, y);

            // A small curved glint on the glass, upper left, which makes it read as round glass.
            bool IsShine(int x, int y)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), bodyCenter);
                float angle = Mathf.Atan2(y + 0.5f - bodyCenter.y, x + 0.5f - bodyCenter.x) * Mathf.Rad2Deg;
                return distance > 27f && distance < 32f && angle > 110f && angle < 150f;
            }

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color32 color = Transparent;
                    if (IsCork(x, y))
                    {
                        color = IsNearEdge(x, y, IsCork, 4) ? outlineColor : corkColor;
                    }
                    else if (IsBottle(x, y))
                    {
                        if (IsNearEdge(x, y, IsBottle, 4))
                        {
                            color = outlineColor;
                        }
                        else if (IsShine(x, y))
                        {
                            color = shineColor;
                        }
                        else if (y + 0.5f >= liquidTop)
                        {
                            color = glassColor;
                        }
                        else
                        {
                            color = isSymbol(x + 0.5f, y + 0.5f) ? symbolColor : liquidColor;
                        }
                    }

                    pixels[y * size + x] = color;
                }
            }

            SaveSprite(path, size, size, pixels, new Vector2(0.5f, 0.5f));
        }

        /// <summary>A small red beetle seen from above, head up, about 0.25 world units across. Pivoted at its center.</summary>
        public static Sprite GetOrCreateInsectSprite()
        {
            if (!File.Exists(InsectPath))
            {
                PaintInsect();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(InsectPath);
        }

        private static void PaintInsect()
        {
            const int size = 64;
            const float centerX = size / 2f;
            const float legWidth = 1.5f;
            var shellColor = new Color32(186, 48, 54, 255);
            var darkColor = new Color32(44, 30, 34, 255);

            // One side's legs and feeler, as distances from the middle; the other side mirrors them.
            (Vector2 from, Vector2 to)[] limbs =
            {
                (new Vector2(5f, 53f), new Vector2(11f, 62f)),
                (new Vector2(11f, 37f), new Vector2(26f, 44f)),
                (new Vector2(10f, 26f), new Vector2(26f, 26f)),
                (new Vector2(11f, 14f), new Vector2(26f, 6f)),
            };

            bool IsShell(int x, int y)
            {
                float across = (x + 0.5f - centerX) / 15f;
                float along = (y + 0.5f - 25f) / 20f;
                return across * across + along * along < 1f;
            }

            bool IsHead(int x, int y) => Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(centerX, 50f)) < 8f;
            bool IsBody(int x, int y) => IsShell(x, y) || IsHead(x, y);

            bool IsLimb(int x, int y)
            {
                var point = new Vector2(Mathf.Abs(x + 0.5f - centerX), y + 0.5f);
                foreach ((Vector2 from, Vector2 to) in limbs)
                {
                    if (DistanceToSegment(point, from, to) < legWidth)
                    {
                        return true;
                    }
                }

                return false;
            }

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color32 color = Transparent;
                    if (IsShell(x, y) && !IsNearEdge(x, y, IsBody, 2))
                    {
                        // A dark line down the middle shows where the wing cases meet.
                        color = Mathf.Abs(x + 0.5f - centerX) < 1f ? darkColor : shellColor;
                    }
                    else if (IsBody(x, y) || IsLimb(x, y))
                    {
                        color = darkColor;
                    }

                    pixels[y * size + x] = color;
                }
            }

            SaveSprite(InsectPath, size, size, pixels, new Vector2(0.5f, 0.5f), pixelsPerUnit: 256f);
        }

        /// <summary>A close-up of tree bark, for filling the screen: brown with long dark streaks running up and down.</summary>
        public static Sprite GetOrCreateBarkSprite()
        {
            if (!File.Exists(BarkPath))
            {
                PaintBark();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(BarkPath);
        }

        private static void PaintBark()
        {
            const int width = 480;
            const int height = 270;
            // Noise that changes quickly across the bark and slowly along it, which stretches it into streaks.
            const float noiseAcross = 0.05f;
            const float noiseAlong = 0.006f;
            const float darkSeamBelow = 0.3f;
            var darkColor = new Color32(84, 56, 36, 255);
            var lightColor = new Color32(130, 92, 62, 255);
            var seamColor = new Color32(60, 38, 24, 255);

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float noise = Mathf.PerlinNoise(x * noiseAcross, y * noiseAlong);
                    pixels[y * width + x] = noise < darkSeamBelow ? seamColor : Color32.Lerp(darkColor, lightColor, noise);
                }
            }

            SaveSprite(BarkPath, width, height, pixels, new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Bark for wrapping around the trunk and branches, painted by <see cref="BarkPainter"/>. It tiles in both
        /// directions, so it can repeat along long branches without visible seams.
        /// </summary>
        public static Texture2D GetOrCreateBarkTexture()
        {
            if (!File.Exists(BarkTexturePath))
            {
                const int size = 256;
                SaveTexture(BarkTexturePath, size, size, ToPixels(BarkPainter.Paint(size)));
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(BarkTexturePath);
        }

        /// <summary>
        /// A sprig of leaves on a see-through background, painted by <see cref="LeafPainter"/>, for the cards the
        /// tree's leaves are made of.
        /// </summary>
        public static Texture2D GetOrCreateLeafTexture()
        {
            if (!File.Exists(LeafTexturePath))
            {
                const int size = 256;
                SaveTexture(LeafTexturePath, size, size, ToPixels(LeafPainter.Paint(size)), cutOut: true);
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(LeafTexturePath);
        }

        /// <summary>Turns red, green, blue and alpha bytes into Unity's pixel colors.</summary>
        private static Color32[] ToPixels(byte[] rgba)
        {
            var pixels = new Color32[rgba.Length / 4];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2], rgba[i * 4 + 3]);
            }

            return pixels;
        }

        /// <summary>A dark, jagged crack running up and down, narrowing to a point at both ends.</summary>
        public static Sprite GetOrCreateCrackSprite()
        {
            if (!File.Exists(CrackPath))
            {
                PaintCrack();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(CrackPath);
        }

        private static void PaintCrack()
        {
            const int width = 64;
            const int height = 128;
            const float centerX = width / 2f;
            const float widestHalfWidth = 13f;
            const float jaggedness = 5f;
            const float jagFrequency = 0.3f;
            var crackColor = new Color32(36, 22, 16, 255);

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float along = (y + 0.5f) / height * 2f - 1f;
                float halfWidth = widestHalfWidth * (1f - along * along);
                float middle = centerX + Mathf.Sin(y * jagFrequency) * jaggedness * (1f - along * along);

                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = Mathf.Abs(x + 0.5f - middle) < halfWidth ? crackColor : Transparent;
                }
            }

            SaveSprite(CrackPath, width, height, pixels, new Vector2(0.5f, 0.5f));
        }

        private static float DistanceToSegment(Vector2 point, Vector2 from, Vector2 to)
        {
            Vector2 segment = to - from;
            float along = Mathf.Clamp01(Vector2.Dot(point - from, segment) / segment.sqrMagnitude);
            return Vector2.Distance(point, from + segment * along);
        }

        /// <summary>
        /// A flat white ring with smooth edges, tinted by whatever displays it. The band's inner edge is at
        /// <paramref name="innerRadiusRatio"/> of the outer radius.
        /// </summary>
        public static Sprite GetOrCreateRingSprite(float innerRadiusRatio)
        {
            if (!File.Exists(RingPath))
            {
                PaintRing(RingPath, innerRadiusRatio);
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(RingPath);
        }

        private const int IconSize = 64;

        /// <summary>
        /// A white "copy" icon, tinted by whatever displays it: two overlapping rounded rectangles, the back one peeking
        /// out above and to the right of the front one, as copy buttons commonly show it.
        /// </summary>
        public static Sprite GetOrCreateCopyIcon()
        {
            if (!File.Exists(CopyIconPath))
            {
                const float half = 14f;
                const float corner = 5f;
                const float line = 4.5f;
                var front = new Vector2(27f, 27f);
                var back = new Vector2(37f, 37f);

                PaintIcon(CopyIconPath, (x, y) =>
                {
                    var point = new Vector2(x, y);
                    float frontDistance = RoundedBoxDistance(point - front, half, corner);
                    float backDistance = RoundedBoxDistance(point - back, half, corner);

                    // The back outline only where the front one doesn't cover it.
                    float frontInk = OutlineInk(frontDistance, line);
                    float backInk = OutlineInk(backDistance, line) * Mathf.Clamp01(frontDistance + 0.5f);
                    return Mathf.Max(frontInk, backInk);
                });
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(CopyIconPath);
        }

        /// <summary>A white tick, tinted by whatever displays it, shown once something has been copied.</summary>
        public static Sprite GetOrCreateTickIcon()
        {
            if (!File.Exists(TickIconPath))
            {
                const float line = 6f;
                var start = new Vector2(14f, 33f);
                var bend = new Vector2(27f, 20f);
                var end = new Vector2(50f, 45f);

                PaintIcon(TickIconPath, (x, y) =>
                {
                    var point = new Vector2(x, y);
                    float distance = Mathf.Min(SegmentDistance(point, start, bend), SegmentDistance(point, bend, end));
                    return Mathf.Clamp01(line * 0.5f - distance + 0.5f);
                });
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(TickIconPath);
        }

        /// <summary>A square white icon, its alpha at each pixel's middle given by <paramref name="inkAt"/> (0 to 1).</summary>
        private static void PaintIcon(string path, System.Func<float, float, float> inkAt)
        {
            var pixels = new Color32[IconSize * IconSize];
            for (int y = 0; y < IconSize; y++)
            {
                for (int x = 0; x < IconSize; x++)
                {
                    byte alpha = (byte)(Mathf.Clamp01(inkAt(x + 0.5f, y + 0.5f)) * 255f);
                    pixels[y * IconSize + x] = new Color32(255, 255, 255, alpha);
                }
            }

            SaveSprite(path, IconSize, IconSize, pixels, new Vector2(0.5f, 0.5f));
        }

        /// <summary>How far a point is outside a rounded square of this half-size centered on the origin; negative inside.</summary>
        private static float RoundedBoxDistance(Vector2 point, float half, float corner)
        {
            var q = new Vector2(Mathf.Abs(point.x) - (half - corner), Mathf.Abs(point.y) - (half - corner));
            var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
            return outside.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - corner;
        }

        /// <summary>How much of an outline of this width, just inside a shape's edge, covers a point; smooth over a pixel.</summary>
        private static float OutlineInk(float distance, float width)
        {
            float fromLineMiddle = Mathf.Abs(distance + width * 0.5f);
            return Mathf.Clamp01(width * 0.5f - fromLineMiddle + 0.5f);
        }

        private static float SegmentDistance(Vector2 point, Vector2 from, Vector2 to)
        {
            Vector2 along = to - from;
            float share = Mathf.Clamp01(Vector2.Dot(point - from, along) / along.sqrMagnitude);
            return Vector2.Distance(point, from + along * share);
        }

        /// <summary>A flat white circle with a smooth edge, tinted by whatever displays it.</summary>
        public static Sprite GetOrCreateDiscSprite()
        {
            if (!File.Exists(DiscPath))
            {
                PaintRing(DiscPath, innerRadiusRatio: 0f);
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(DiscPath);
        }

        /// <summary>
        /// A small flat white circle with a smooth edge, tinted by whatever displays it. Separate from the disc because
        /// that one is painted large for the minigame, and looks jagged when shrunk to the size of a notification.
        /// </summary>
        public static Sprite GetOrCreateNotificationBubbleSprite()
        {
            if (!File.Exists(NotificationBubblePath))
            {
                PaintRing(NotificationBubblePath, innerRadiusRatio: 0f, size: 128);
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(NotificationBubblePath);
        }

        private static void PaintRing(string path, float innerRadiusRatio, int size = 1024)
        {
            float center = size / 2f;
            float outerRadius = center - 2f; // Leaves a transparent margin, so the edge stays smooth.

            // A ratio of 0 means no hole; going below 0 keeps the very center fully solid too.
            float innerRadius = innerRadiusRatio > 0f ? outerRadius * innerRadiusRatio : -1f;

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));

                    // Fades over one pixel at each edge, which smooths the circle.
                    float coverage = Mathf.Clamp01(outerRadius - distance) * Mathf.Clamp01(distance - innerRadius);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(coverage * 255f));
                }
            }

            SaveSprite(path, size, size, pixels, new Vector2(0.5f, 0.5f));
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

        /// <summary>
        /// Saves a texture for 3D objects, with mipmaps so it stays smooth far away. By default it repeats at its edges,
        /// for wrapping round things. A cut-out texture, where see-through parts are cut away rather than blended,
        /// stops at its edges instead, and keeps its shapes as full in the smaller mipmaps, so distant leaves don't thin out.
        /// </summary>
        private static void SaveTexture(string path, int width, int height, Color32[] pixels, bool cutOut = false)
        {
            BuilderHelpers.EnsureFolder(ArtFolder);

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false);
            texture.SetPixels32(pixels);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = cutOut ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 2;
            if (cutOut)
            {
                importer.alphaIsTransparency = true;
                importer.mipMapsPreserveCoverage = true;
                importer.alphaTestReferenceValue = TreeBuilder.LeafCutoff;
            }

            importer.SaveAndReimport();
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
