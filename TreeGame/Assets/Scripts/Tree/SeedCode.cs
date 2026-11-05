using System;
using System.Text;

namespace TreeGame.Trees
{
    /// <summary>
    /// Turns a tree's seed into a short code people can read, share and type back in, such as "K3M7-QX2P", and back.
    /// Not a hash: the code holds the seed itself, so it always gives back the same seed. It also holds the tree's
    /// generation version, as a seed only grows the same tree while the rules that grow trees stay the same, and a check
    /// character, so a mistyped code is rejected instead of quietly growing a different tree.
    /// Uses Crockford's base 32 alphabet: digits and capitals without I, L, O and U, which are easily misread. Reading is
    /// forgiving: lower case, spaces and dashes are fine, and O, I and L are read as 0, 1 and 1.
    /// Plain C# with no Unity dependency, so it can be checked on its own.
    /// </summary>
    public static class SeedCode
    {
        /// <summary>The highest generation version a code can hold.</summary>
        public const int MaxVersion = 7;

        /// <summary>Characters in a code, not counting the dash.</summary>
        public const int Length = 8;

        private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        private const int BitsPerCharacter = 5;
        private const int PayloadCharacters = Length - 1;
        private const int DashAfter = 4;

        /// <summary>The code for a seed grown by this generation version (0 to <see cref="MaxVersion"/>).</summary>
        public static string Encode(int seed, int version)
        {
            if (version < 0 || version > MaxVersion)
            {
                throw new ArgumentOutOfRangeException(nameof(version), version, $"A code holds versions 0 to {MaxVersion}.");
            }

            // The version above the seed's 32 bits: 35 bits, exactly 7 characters.
            ulong payload = ((ulong)version << 32) | unchecked((uint)seed);
            var values = new int[Length];
            for (int i = PayloadCharacters - 1; i >= 0; i--)
            {
                values[i] = (int)(payload & 31);
                payload >>= BitsPerCharacter;
            }

            values[PayloadCharacters] = CheckValue(values);

            var code = new StringBuilder(Length + 1);
            for (int i = 0; i < Length; i++)
            {
                if (i == DashAfter)
                {
                    code.Append('-');
                }

                code.Append(Alphabet[values[i]]);
            }

            return code.ToString();
        }

        /// <summary>
        /// Reads a code back into its seed and generation version. False if it isn't a code, or if a character was
        /// mistyped: every single wrong character is caught, and most swaps of two neighbours.
        /// </summary>
        public static bool TryDecode(string code, out int seed, out int version)
        {
            seed = 0;
            version = 0;
            if (code == null)
            {
                return false;
            }

            var values = new int[Length];
            int count = 0;
            foreach (char typed in code)
            {
                if (typed == '-' || char.IsWhiteSpace(typed))
                {
                    continue;
                }

                int value = ValueOf(typed);
                if (value < 0 || count == Length)
                {
                    return false;
                }

                values[count++] = value;
            }

            if (count != Length || values[PayloadCharacters] != CheckValue(values))
            {
                return false;
            }

            ulong payload = 0;
            for (int i = 0; i < PayloadCharacters; i++)
            {
                payload = (payload << BitsPerCharacter) | (uint)values[i];
            }

            seed = unchecked((int)(uint)(payload & uint.MaxValue));
            version = (int)(payload >> 32);
            return true;
        }

        /// <summary>
        /// The check character for the first seven: a weighted sum of them. Odd weights, as an odd number has an inverse
        /// modulo 32, so changing any one character always changes the sum; and weights that differ between neighbours,
        /// so swapping two of them almost always does too.
        /// </summary>
        private static int CheckValue(int[] values)
        {
            int sum = 0;
            for (int i = 0; i < PayloadCharacters; i++)
            {
                sum += values[i] * (2 * i + 1);
            }

            return sum & 31;
        }

        private static int ValueOf(char typed)
        {
            char upper = char.ToUpperInvariant(typed);
            switch (upper)
            {
                case 'O':
                    return 0;
                case 'I':
                case 'L':
                    return 1;
                default:
                    return Alphabet.IndexOf(upper);
            }
        }
    }
}
