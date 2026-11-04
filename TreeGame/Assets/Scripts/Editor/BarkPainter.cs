using System;

namespace TreeGame.EditorTools
{
    /// <summary>
    /// Paints the placeholder bark: irregular plates, longer along the wood than around it, split by dark cracks where
    /// neighbouring plates meet, each plate a slightly different tone with a fine grain. The plates come from scattered
    /// points (each pixel belongs to the plate of its nearest point), so they never line up like bricks. Built from
    /// patterns that repeat a whole number of times across the texture, so it tiles without seams in both directions.
    /// Plain C# with no Unity dependency, so it can be previewed outside Unity.
    /// </summary>
    public static class BarkPainter
    {
        // How many plates fit around the wood and along it; whole numbers, so the texture tiles. Fewer along makes
        // each plate taller than it is wide, the way bark splits along the grain.
        private const int PlatesAround = 12;
        private const int PlatesAlong = 3;
        // How much the crack lines wobble, as a share of the texture, so plate edges aren't straight.
        private const float EdgeWobble = 0.012f;
        // How wide the cracks are, as a share of the texture, at their narrowest and widest.
        private const float NarrowestCrack = 0.0015f;
        private const float WidestCrack = 0.006f;
        // How far into a plate its edges are shaded darker, as if it curves down into the crack.
        private const float EdgeShading = 0.025f;

        private static readonly (byte r, byte g, byte b) CrackColor = (50, 34, 24);
        private static readonly (byte r, byte g, byte b) DarkBark = (82, 58, 40);
        private static readonly (byte r, byte g, byte b) LightBark = (130, 97, 69);

        /// <summary>A square texture's pixels, row by row from the bottom, as red, green, blue and alpha bytes.</summary>
        public static byte[] Paint(int size)
        {
            var pixels = new byte[size * size * 4];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float around = (float)x / size;
                    float along = (float)y / size;

                    // Pushing each position about a little before finding its plate makes the plates' edges wobble.
                    float wobbledAround = around + (FractalNoise(around, along, 8, 8, 3, salt: 1) - 0.5f) * 2f * EdgeWobble;
                    float wobbledAlong = along + (FractalNoise(around, along, 8, 8, 3, salt: 2) - 0.5f) * 2f * EdgeWobble;
                    (float toEdge, int plate) = NearestPlate(wobbledAround, wobbledAlong);
                    float crackWidth = Lerp(NarrowestCrack, WidestCrack, FractalNoise(around, along, 6, 6, 2, salt: 3));
                    float awayFromCrack = Math.Clamp((toEdge - crackWidth) / (crackWidth * 0.6f), 0f, 1f);

                    // Each plate its own tone, darker toward its edges, with a fine grain running along the wood.
                    float plateTone = Grain(plate, 0, salt: 4);
                    float rounding = Math.Clamp(toEdge / EdgeShading, 0f, 1f);
                    float streaks = FractalNoise(around, along, 64, 8, 2, salt: 5) - 0.5f;
                    float shade = Math.Clamp(0.1f + plateTone * 0.45f + rounding * 0.4f + streaks * 0.5f + (Grain(x, y, salt: 6) - 0.5f) * 0.15f, 0f, 1f);

                    (byte r, byte g, byte b) surface = Lerp(DarkBark, LightBark, shade);
                    (byte r, byte g, byte b) color = Lerp(CrackColor, surface, awayFromCrack);

                    int index = (y * size + x) * 4;
                    pixels[index] = color.r;
                    pixels[index + 1] = color.g;
                    pixels[index + 2] = color.b;
                    pixels[index + 3] = 255;
                }
            }

            return pixels;
        }

        /// <summary>
        /// Each plate has one point scattered within its own cell of a grid; a position belongs to the plate whose point is
        /// nearest. Nearness is judged in grid cells rather than texture units, so each plate takes its cell's tall shape.
        /// Returns how far the position is from the edge where its plate meets the next, in texture units so cracks are
        /// the same width whichever way they run, and which plate it is. The grid repeats across the texture, so the
        /// plates tile.
        /// </summary>
        private static (float toEdge, int plate) NearestPlate(float around, float along)
        {
            float x = around * PlatesAround;
            float y = along * PlatesAlong;
            int cellX = (int)MathF.Floor(x);
            int cellY = (int)MathF.Floor(y);

            float nearest = float.MaxValue;
            float secondNearest = float.MaxValue;
            (float x, float y) nearestPoint = (0f, 0f);
            (float x, float y) secondPoint = (0f, 0f);
            int plate = 0;

            // In grid cells, a point can only be nearest if its cell is this one or a neighbour.
            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    int neighbourX = cellX + offsetX;
                    int neighbourY = cellY + offsetY;
                    int wrappedX = Wrap(neighbourX, PlatesAround);
                    int wrappedY = Wrap(neighbourY, PlatesAlong);

                    // Kept away from the cell's edges, so no plate ends up a sliver.
                    float pointX = neighbourX + 0.15f + 0.7f * Grain(wrappedX, wrappedY, salt: 10);
                    float pointY = neighbourY + 0.15f + 0.7f * Grain(wrappedX, wrappedY, salt: 11);
                    float distance = (x - pointX) * (x - pointX) + (y - pointY) * (y - pointY);

                    if (distance < nearest)
                    {
                        secondNearest = nearest;
                        secondPoint = nearestPoint;
                        nearest = distance;
                        nearestPoint = (pointX, pointY);
                        plate = wrappedY * PlatesAround + wrappedX;
                    }
                    else if (distance < secondNearest)
                    {
                        secondNearest = distance;
                        secondPoint = (pointX, pointY);
                    }
                }
            }

            // The edge between the two plates is the line halfway between their points, square to the line joining them
            // in grid cells. Turned into texture units, it is square to (cellsAround² dx, cellsAlong² dy), so measure along that.
            float joinX = secondPoint.x - nearestPoint.x;
            float joinY = secondPoint.y - nearestPoint.y;
            float normalX = joinX * PlatesAround;
            float normalY = joinY * PlatesAlong;
            float normalLength = MathF.Sqrt(normalX * normalX + normalY * normalY);

            float fromMiddleX = (x - (nearestPoint.x + secondPoint.x) / 2f) / PlatesAround;
            float fromMiddleY = (y - (nearestPoint.y + secondPoint.y) / 2f) / PlatesAlong;
            float toEdge = MathF.Abs(fromMiddleX * normalX + fromMiddleY * normalY) / normalLength;

            return (toEdge, plate);
        }

        /// <summary>
        /// Several layers of smooth noise from 0 to 1, each twice as fine and half as strong as the last. Every layer
        /// repeats a whole number of times across the texture, so the result tiles.
        /// </summary>
        private static float FractalNoise(float u, float v, int cellsAcross, int cellsDown, int octaves, int salt)
        {
            float sum = 0f;
            float total = 0f;
            float strength = 0.5f;
            for (int octave = 0; octave < octaves; octave++)
            {
                int across = cellsAcross << octave;
                int down = cellsDown << octave;
                sum += strength * SmoothNoise(u * across, v * down, across, down, salt + octave * 31);
                total += strength;
                strength *= 0.5f;
            }

            return sum / total;
        }

        /// <summary>Random values on a grid that repeats every period cells, blended smoothly in between.</summary>
        private static float SmoothNoise(float x, float y, int periodX, int periodY, int salt)
        {
            int x0 = (int)MathF.Floor(x);
            int y0 = (int)MathF.Floor(y);

            // Easing the blend hides the square grid the noise is built on.
            float fx = Ease(x - x0);
            float fy = Ease(y - y0);

            float Corner(int cx, int cy) => Grain(Wrap(cx, periodX), Wrap(cy, periodY), salt);

            float bottom = Lerp(Corner(x0, y0), Corner(x0 + 1, y0), fx);
            float top = Lerp(Corner(x0, y0 + 1), Corner(x0 + 1, y0 + 1), fx);
            return Lerp(bottom, top, fy);
        }

        /// <summary>A fixed, random-looking value from 0 to 1 for each whole-number position.</summary>
        private static float Grain(int x, int y, int salt)
        {
            unchecked
            {
                uint hash = (uint)x * 374761393u + (uint)y * 668265263u + (uint)salt * 2246822519u;
                hash = (hash ^ (hash >> 13)) * 1274126177u;
                return (hash ^ (hash >> 16)) / (float)uint.MaxValue;
            }
        }

        private static int Wrap(int value, int period) => (value % period + period) % period;

        private static float Ease(float t) => t * t * (3f - 2f * t);

        private static float Lerp(float from, float to, float t) => from + (to - from) * t;

        private static (byte r, byte g, byte b) Lerp((byte r, byte g, byte b) from, (byte r, byte g, byte b) to, float t)
        {
            return ((byte)MathF.Round(Lerp(from.r, to.r, t)), (byte)MathF.Round(Lerp(from.g, to.g, t)), (byte)MathF.Round(Lerp(from.b, to.b, t)));
        }
    }
}
