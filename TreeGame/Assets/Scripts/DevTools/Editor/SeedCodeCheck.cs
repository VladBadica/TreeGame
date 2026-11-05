using System;
using System.Collections.Generic;
using TreeGame.Trees;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace TreeGame.DevTools
{
    /// <summary>
    /// Checks the codes that tree seeds are shared as, without a scene or Play Mode. The code has no Unity dependency, so
    /// this also compiles and runs outside Unity; only the menu item is Editor-specific.
    /// </summary>
    public static class SeedCodeCheck
    {
        private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        /// <summary>Runs every check. Returns a description of each failure; empty means all passed.</summary>
        public static List<string> Run()
        {
            var failures = new List<string>();

            void Expect(bool condition, string failure)
            {
                if (!condition)
                {
                    failures.Add(failure);
                }
            }

            // Every seed comes back exactly, with its version, from the edges of the range to a spread of others.
            var seeds = new List<int> { 0, 1, -1, 12345, int.MaxValue, int.MinValue, int.MaxValue - 1, int.MinValue + 1 };
            var random = new System.Random(7);
            for (int i = 0; i < 5000; i++)
            {
                seeds.Add(random.Next(int.MinValue, int.MaxValue));
            }

            var codesSeen = new HashSet<string>();
            foreach (int seed in seeds)
            {
                for (int version = 0; version <= SeedCode.MaxVersion; version++)
                {
                    string code = SeedCode.Encode(seed, version);
                    bool read = SeedCode.TryDecode(code, out int readSeed, out int readVersion);
                    if (!read || readSeed != seed || readVersion != version)
                    {
                        failures.Add($"seed {seed} version {version} became '{code}', which read back as {(read ? $"seed {readSeed} version {readVersion}" : "not a code")}");
                        return failures;
                    }

                    codesSeen.Add(code);
                }
            }

            // Different seeds or versions never share a code.
            Expect(codesSeen.Count == new HashSet<int>(seeds).Count * (SeedCode.MaxVersion + 1), "two different seeds or versions gave the same code");

            // The shape people see: four characters, a dash, four more, all from the readable alphabet.
            string sample = SeedCode.Encode(12345, 1);
            Expect(sample.Length == SeedCode.Length + 1 && sample[4] == '-', $"'{sample}' isn't four characters, a dash and four more");
            foreach (char character in sample.Replace("-", ""))
            {
                Expect(Alphabet.IndexOf(character) >= 0, $"'{sample}' has '{character}', which isn't in the readable alphabet");
            }

            // Forgiving to read: lower case, no dash, spaces, and the letters easily typed for 0 and 1.
            Expect(Reads(sample.ToLowerInvariant(), 12345, 1), "a code in lower case should read");
            Expect(Reads(sample.Replace("-", ""), 12345, 1), "a code without its dash should read");
            Expect(Reads($"  {sample.Substring(0, 4)} {sample.Substring(5)} ", 12345, 1), "a code with spaces should read");
            string withZerosAndOnes = SeedCode.Encode(0, 0);
            Expect(Reads(withZerosAndOnes.Replace('0', 'O'), 0, 0), "O typed for 0 should read as 0");
            string withOne = FindCodeWith('1');
            Expect(withOne == null || (Reads(withOne.Replace('1', 'I'), Seed(withOne), 0) && Reads(withOne.Replace('1', 'l'), Seed(withOne), 0)),
                "I or l typed for 1 should read as 1");

            // Mistyped codes are rejected: every single wrong character, in every place.
            int singleMissed = 0;
            foreach (int seed in new[] { 12345, -98765, int.MaxValue, 424242 })
            {
                char[] code = SeedCode.Encode(seed, 1).Replace("-", "").ToCharArray();
                for (int place = 0; place < code.Length; place++)
                {
                    char original = code[place];
                    foreach (char wrong in Alphabet)
                    {
                        if (wrong == original)
                        {
                            continue;
                        }

                        code[place] = wrong;
                        singleMissed += SeedCode.TryDecode(new string(code), out _, out _) ? 1 : 0;
                    }

                    code[place] = original;
                }
            }

            Expect(singleMissed == 0, $"{singleMissed} codes with one wrong character were accepted");

            // Most swaps of two neighbouring characters are rejected too.
            int swaps = 0;
            int swapsMissed = 0;
            for (int i = 0; i < 500; i++)
            {
                char[] code = SeedCode.Encode(random.Next(int.MinValue, int.MaxValue), 1).Replace("-", "").ToCharArray();
                for (int place = 0; place + 1 < code.Length; place++)
                {
                    if (code[place] == code[place + 1])
                    {
                        continue;
                    }

                    (code[place], code[place + 1]) = (code[place + 1], code[place]);
                    swaps++;
                    swapsMissed += SeedCode.TryDecode(new string(code), out _, out _) ? 1 : 0;
                    (code[place], code[place + 1]) = (code[place + 1], code[place]);
                }
            }

            Expect(swapsMissed < swaps * 0.1f, $"{swapsMissed} of {swaps} codes with two neighbours swapped were accepted");

            // Things that aren't codes, including a real code with more after it, and with a letter from outside the
            // alphabet in place of a Z (which, if it were counted as a value, could still add up).
            string withZ = FindCodeWith('Z');
            var notCodes = new List<string> { null, "", "-", "K3M7-QX2", "K3M7-QX2PP", "K3M7-QX2*", "UUUU-UUUU", "K3M7_QX2P", sample + "0", sample + "-" + sample };
            if (withZ != null)
            {
                notCodes.Add(withZ.Replace('Z', 'U'));
            }

            foreach (string notACode in notCodes)
            {
                Expect(!SeedCode.TryDecode(notACode, out _, out _), $"'{notACode ?? "null"}' was read as a code");
            }

            CheckTypedCodes(failures);

            // Versions a code can't hold are refused when making one.
            foreach (int badVersion in new[] { -1, SeedCode.MaxVersion + 1 })
            {
                try
                {
                    SeedCode.Encode(1, badVersion);
                    failures.Add($"a code was made for version {badVersion}, which a code can't hold");
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Expected.
                }
            }

            return failures;
        }

        /// <summary>What a code typed in for a new tree means, as the player types it.</summary>
        private static void CheckTypedCodes(List<string> failures)
        {
            const int version = 2;
            string code = SeedCode.Encode(-4242, version);

            void ExpectStatus(string typed, TreeCodeStatus expected, bool canGrow)
            {
                TreeCodeStatus status = TreeCodeEntry.Read(typed, version, out _);
                if (status != expected || TreeCodeEntry.CanGrow(status) != canGrow)
                {
                    failures.Add($"typing '{typed ?? "null"}' read as {status} (can grow: {TreeCodeEntry.CanGrow(status)}), expected {expected} (can grow: {canGrow})");
                }
            }

            ExpectStatus(null, TreeCodeStatus.Empty, true);
            ExpectStatus("", TreeCodeStatus.Empty, true);
            ExpectStatus("  - ", TreeCodeStatus.Empty, true);
            ExpectStatus(code, TreeCodeStatus.Valid, true);
            ExpectStatus($" {code.ToLowerInvariant()} ", TreeCodeStatus.Valid, true);

            // Partway through typing isn't a mistake yet, but can't grow either.
            ExpectStatus(code.Substring(0, 1), TreeCodeStatus.Incomplete, false);
            ExpectStatus(code.Substring(0, code.Length - 1), TreeCodeStatus.Incomplete, false);

            // A full code with a wrong character, or with too many, is a typo.
            char[] wrong = code.ToCharArray();
            wrong[0] = wrong[0] == 'A' ? 'B' : 'A';
            ExpectStatus(new string(wrong), TreeCodeStatus.Typo, false);
            ExpectStatus(code + "7", TreeCodeStatus.Typo, false);

            // Real codes from other versions of the rules can't grow here.
            ExpectStatus(SeedCode.Encode(-4242, version - 1), TreeCodeStatus.OlderVersion, false);
            ExpectStatus(SeedCode.Encode(-4242, version + 1), TreeCodeStatus.NewerVersion, false);

            // A valid code gives its seed; anything else gives none.
            Expect(failures, TreeCodeEntry.Read(code, version, out int seed) == TreeCodeStatus.Valid && seed == -4242, $"a valid code gave seed {seed}, expected -4242");
            TreeCodeEntry.Read(SeedCode.Encode(-4242, version - 1), version, out int olderSeed);
            Expect(failures, olderSeed == 0, "a code from an older version shouldn't give a seed to grow");
        }

        private static void Expect(List<string> failures, bool condition, string failure)
        {
            if (!condition)
            {
                failures.Add(failure);
            }
        }

        private static bool Reads(string code, int seed, int version)
        {
            return SeedCode.TryDecode(code, out int readSeed, out int readVersion) && readSeed == seed && readVersion == version;
        }

        private static int Seed(string code)
        {
            SeedCode.TryDecode(code, out int seed, out _);
            return seed;
        }

        /// <summary>
        /// A version 0 code with this character among the seven before its check character, or null if none of the
        /// seeds tried has one.
        /// </summary>
        private static string FindCodeWith(char character)
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                string code = SeedCode.Encode(seed, 0);
                if (code.Substring(0, code.Length - 1).IndexOf(character) >= 0)
                {
                    return code;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        [MenuItem("Tree Game/Run Seed Code Check")]
        private static void RunFromMenu()
        {
            List<string> failures = Run();
            if (failures.Count == 0)
            {
                Debug.Log("Seed code check PASSED.");
            }
            else
            {
                Debug.LogError("Seed code check FAILED:\n- " + string.Join("\n- ", failures));
            }
        }
#endif
    }
}
