using System;

namespace TreeGame.EditorTools
{
    /// <summary>
    /// Paints the placeholder leaves: a sprig on a see-through background, with a thin stem up the middle, pairs of
    /// pointed leaves along it and one at its tip. Each leaf is darker at its base, lighter toward its tip, with a pale
    /// middle vein, and a slightly different green from the others. Plain C# with no Unity dependency, so it can be
    /// previewed outside Unity.
    /// </summary>
    public static class LeafPainter
    {
        // Where the stem runs, as shares of the texture, from the bottom middle up. It starts right at the bottom edge,
        // where the card meets its branch, so there is no gap.
        private const float StemX = 0.5f;
        private const float StemBottom = 0f;
        private const float StemTop = 0.6f;
        private const float StemWidth = 0.01f;
        // How wide a leaf is at its widest, as a share of its length.
        private const float LeafWidthRatio = 0.4f;
        private const float VeinWidth = 0.006f;

        private static readonly (byte r, byte g, byte b) StemColor = (92, 104, 46);
        private static readonly (byte r, byte g, byte b) LeafBase = (46, 96, 36);
        private static readonly (byte r, byte g, byte b) LeafTip = (104, 158, 58);
        private static readonly (byte r, byte g, byte b) VeinColor = (140, 178, 88);

        /// <summary>Where each leaf starts on the stem, which way it points (degrees, 90 is straight up) and how long it is.</summary>
        private static readonly (float baseY, float angle, float length)[] Leaves =
        {
            (0.16f, 140f, 0.36f),
            (0.2f, 40f, 0.35f),
            (0.32f, 145f, 0.33f),
            (0.36f, 35f, 0.33f),
            (0.47f, 135f, 0.29f),
            (0.5f, 45f, 0.29f),
            (0.58f, 90f, 0.38f),
        };

        /// <summary>A square texture's pixels, row by row from the bottom, as red, green, blue and alpha bytes.</summary>
        public static byte[] Paint(int size)
        {
            var pixels = new byte[size * size * 4];
            float pixel = 1f / size;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = (x + 0.5f) / size;
                    float py = (y + 0.5f) / size;

                    // See-through, but leaf-colored, so the blurred edges of distant leaves don't turn dark.
                    (byte r, byte g, byte b) color = LeafBase;
                    float coverage = 0f;

                    float stem = StemCoverage(px, py, pixel);
                    if (stem > 0f)
                    {
                        color = StemColor;
                        coverage = stem;
                    }

                    // Later leaves are drawn over earlier ones, so the tip leaf sits on top.
                    for (int i = 0; i < Leaves.Length; i++)
                    {
                        float leaf = LeafCoverage(i, px, py, pixel, out (byte r, byte g, byte b) leafColor);
                        if (leaf > 0f)
                        {
                            color = coverage > 0f ? Lerp(color, leafColor, leaf) : leafColor;
                            coverage = MathF.Max(coverage, leaf);
                        }
                    }

                    int index = (y * size + x) * 4;
                    pixels[index] = color.r;
                    pixels[index + 1] = color.g;
                    pixels[index + 2] = color.b;
                    pixels[index + 3] = (byte)MathF.Round(coverage * 255f);
                }
            }

            return pixels;
        }

        private static float StemCoverage(float px, float py, float pixel)
        {
            if (py < StemBottom || py > StemTop)
            {
                return 0f;
            }

            return Math.Clamp((StemWidth / 2f - MathF.Abs(px - StemX)) / pixel + 0.5f, 0f, 1f);
        }

        /// <summary>How much of this pixel the leaf covers, 0 to 1, with its edge softened over about a pixel.</summary>
        private static float LeafCoverage(int index, float px, float py, float pixel, out (byte r, byte g, byte b) color)
        {
            (float baseY, float angle, float length) = Leaves[index];
            float radians = angle * MathF.PI / 180f;
            float dirX = MathF.Cos(radians);
            float dirY = MathF.Sin(radians);

            // Into the leaf's own frame: how far along it from its base, and how far to one side of its middle.
            float fromBaseX = px - StemX;
            float fromBaseY = py - baseY;
            float along = (fromBaseX * dirX + fromBaseY * dirY) / length;
            float aside = -fromBaseX * dirY + fromBaseY * dirX;

            color = LeafBase;
            if (along <= 0f || along >= 1f)
            {
                return 0f;
            }

            // Pointed at both ends, widest a little before halfway, like a real leaf.
            float halfWidth = LeafWidthRatio * length / 2f * MathF.Pow(MathF.Sin(MathF.PI * MathF.Pow(along, 0.8f)), 0.9f);
            float coverage = Math.Clamp((halfWidth - MathF.Abs(aside)) / pixel + 0.5f, 0f, 1f);
            if (coverage <= 0f)
            {
                return 0f;
            }

            // Each leaf a slightly different green, lighter toward its tip, with a pale vein down the middle.
            float tone = 0.85f + 0.3f * Grain(index);
            color = Scale(Lerp(LeafBase, LeafTip, along * 0.8f), tone);
            if (MathF.Abs(aside) < VeinWidth / 2f && along < 0.9f)
            {
                color = Lerp(color, VeinColor, 0.6f);
            }

            return coverage;
        }

        private static float Grain(int value)
        {
            unchecked
            {
                uint hash = (uint)value * 2654435761u;
                hash = (hash ^ (hash >> 15)) * 2246822519u;
                return (hash ^ (hash >> 13)) / (float)uint.MaxValue;
            }
        }

        private static (byte r, byte g, byte b) Scale((byte r, byte g, byte b) color, float amount)
        {
            return ((byte)Math.Clamp(color.r * amount, 0f, 255f), (byte)Math.Clamp(color.g * amount, 0f, 255f), (byte)Math.Clamp(color.b * amount, 0f, 255f));
        }

        private static (byte r, byte g, byte b) Lerp((byte r, byte g, byte b) from, (byte r, byte g, byte b) to, float t)
        {
            return ((byte)MathF.Round(from.r + (to.r - from.r) * t), (byte)MathF.Round(from.g + (to.g - from.g) * t), (byte)MathF.Round(from.b + (to.b - from.b) * t));
        }
    }
}
