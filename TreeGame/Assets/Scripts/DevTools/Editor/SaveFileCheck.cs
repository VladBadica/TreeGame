using System;
using System.Collections.Generic;
using System.IO;
using TreeGame.Saving;
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace TreeGame.DevTools
{
    /// <summary>
    /// Checks saving to and reading from disk, always in a temporary folder and never on the real save. The atomic file
    /// has no Unity dependency, so that part also compiles and runs outside Unity; the JSON part needs Unity's
    /// serializer, so it only runs from the menu item.
    /// </summary>
    public static class SaveFileCheck
    {
        /// <summary>Runs the checks that don't need Unity. Returns a description of each failure; empty means all passed.</summary>
        public static List<string> RunAtomicFileChecks()
        {
            var failures = new List<string>();
            InTemporaryFolder(folder =>
            {
                CheckWriteAndRead(failures, folder);
                CheckOverwriting(failures, folder);
                CheckCreatesMissingFolders(failures, folder);
                CheckLeavesNoTemporaryFile(failures, folder);
                CheckIgnoresLeftoverTemporaryFile(failures, folder);
                CheckFailedWriteKeepsOldText(failures, folder);
                CheckBadInputIsRejected(failures, folder);
            });
            return failures;
        }

#if UNITY_EDITOR
        [MenuItem("Tree Game/Run Save File Check")]
        private static void RunFromMenu()
        {
            List<string> failures = RunAtomicFileChecks();

            // Bad files are meant to log warnings here, which would only be noise.
            LogType originalFilter = Debug.unityLogger.filterLogType;
            Debug.unityLogger.filterLogType = LogType.Error;
            try
            {
                InTemporaryFolder(folder =>
                {
                    CheckFullRoundTrip(failures, folder);
                    CheckUpgradeFromVersion1(failures, folder);
                    CheckUpgradeFromVersion2(failures, folder);
                    CheckNoSave(failures, folder);
                    CheckUnusableFiles(failures, folder);
                    CheckFailedSaveKeepsThePreviousOne(failures, folder);
                });
            }
            finally
            {
                Debug.unityLogger.filterLogType = originalFilter;
            }

            if (failures.Count == 0)
            {
                Debug.Log("Save file check PASSED.");
            }
            else
            {
                Debug.LogError("Save file check FAILED:\n- " + string.Join("\n- ", failures));
            }
        }

        private static SaveData NewSample()
        {
            return new SaveData
            {
                treeSeed = -123456789,
                trunkGrowth = 0.42f,
                branchGrowth = 0.3f,
                foliageGrowth = 0.55f,
                waterFill = 0.25f,
                nutrientsFill = 0.75f,
                healthFill = 0.5f,
                playerX = -3.5f,
                inventorySlots = new[]
                {
                    new SavedSlot { itemName = "WaterBucket", storedAmount = 40f },
                    new SavedSlot(),
                    new SavedSlot { itemName = "Coin", count = 7 },
                },
                composter = new SavedComposter { state = 1, workSeconds = 12.5f, workDuration = 30f, compostGrams = 200f },
                insectAges = new[] { 25f, 3.5f },
                fallenLeaves = new[] { new SavedLeaf { x = 1f, y = 0f, z = -2.5f } },
            };
        }

        private static void CheckFullRoundTrip(List<string> failures, string folder)
        {
            var file = new SaveFile(Path.Combine(folder, "round trip.json"));
            SaveData sample = NewSample();

            Expect(failures, file.TryWrite(sample), "writing a save failed");
            Expect(failures, sample.version == SaveData.CurrentVersion, "a written save should be stamped with the current version");

            if (!file.TryRead(out SaveData read))
            {
                failures.Add("a save that was just written couldn't be read");
                return;
            }

            Expect(failures, read.version == SaveData.CurrentVersion, $"version read back as {read.version}");
            Expect(failures, read.treeSeed == sample.treeSeed, $"the tree's seed read back as {read.treeSeed}");
            Expect(failures, read.trunkGrowth == sample.trunkGrowth && read.branchGrowth == sample.branchGrowth &&
                             read.foliageGrowth == sample.foliageGrowth, "the tree's growth changed");
            Expect(failures, read.waterFill == sample.waterFill, $"water read back as {read.waterFill}");
            Expect(failures, read.nutrientsFill == sample.nutrientsFill, $"nutrients read back as {read.nutrientsFill}");
            Expect(failures, read.healthFill == sample.healthFill, $"health read back as {read.healthFill}");
            Expect(failures, read.playerX == sample.playerX, $"player position read back as {read.playerX}");

            Expect(failures, read.inventorySlots.Length == 3, $"read back {read.inventorySlots.Length} slots, expected 3");
            if (read.inventorySlots.Length == 3)
            {
                Expect(failures, read.inventorySlots[0].itemName == "WaterBucket" && read.inventorySlots[0].storedAmount == 40f, "the bucket's slot changed");
                Expect(failures, read.inventorySlots[1].itemName == "", "the empty slot should stay empty");
                Expect(failures, read.inventorySlots[2].itemName == "Coin" && read.inventorySlots[2].count == 7, "the coins' slot changed");
            }

            Expect(failures, read.composter.state == 1 && read.composter.workSeconds == 12.5f &&
                             read.composter.workDuration == 30f && read.composter.compostGrams == 200f, "the composter changed");

            Expect(failures, read.insectAges.Length == 2 && read.insectAges[0] == 25f && read.insectAges[1] == 3.5f, "the insects changed");
            Expect(failures, read.fallenLeaves.Length == 1 && read.fallenLeaves[0].x == 1f && read.fallenLeaves[0].z == -2.5f, "the leaves changed");

            // A new game's empty lists must survive too, without coming back as nothing.
            var emptyFile = new SaveFile(Path.Combine(folder, "empty lists.json"));
            emptyFile.TryWrite(new SaveData());
            Expect(failures, emptyFile.TryRead(out SaveData empty) && empty.inventorySlots != null && empty.insectAges != null &&
                             empty.fallenLeaves != null && empty.composter != null, "empty lists didn't survive saving");
        }

        /// <summary>A save as version 1 of the game wrote it, with one growth value for the whole tree.</summary>
        private static void CheckUpgradeFromVersion1(List<string> failures, string folder)
        {
            const string version1Save =
                "{\"version\":1,\"treeGrowth\":0.4,\"waterFill\":0.5,\"nutrientsFill\":0.25,\"healthFill\":1.0,\"playerX\":-2.0," +
                "\"inventorySlots\":[{\"itemName\":\"Coin\",\"count\":3,\"storedAmount\":0.0}]," +
                "\"composter\":{\"state\":0,\"workSeconds\":0.0,\"workDuration\":0.0,\"compostGrams\":0.0}," +
                "\"insectAges\":[12.0],\"fallenLeaves\":[]}";

            string path = Path.Combine(folder, "version 1.json");
            File.WriteAllText(path, version1Save);

            if (!new SaveFile(path).TryRead(out SaveData read))
            {
                failures.Add("a version 1 save couldn't be read");
                return;
            }

            Expect(failures, read.version == SaveData.CurrentVersion, $"a version 1 save read as version {read.version}");
            Expect(failures, read.trunkGrowth == 0.4f && read.branchGrowth == 0.4f && read.foliageGrowth == 0.4f,
                $"a version 1 tree's growth of 0.4 became {read.trunkGrowth}, {read.branchGrowth} and {read.foliageGrowth}");
            Expect(failures, read.treeSeed == 12345, $"a version 1 tree should keep the seed every tree grew from then, not {read.treeSeed}");

            // Everything else carries over as it was.
            Expect(failures, read.waterFill == 0.5f && read.nutrientsFill == 0.25f && read.playerX == -2f, "upgrading changed the resources or player");
            Expect(failures, read.inventorySlots.Length == 1 && read.inventorySlots[0].itemName == "Coin" && read.inventorySlots[0].count == 3, "upgrading changed the inventory");
            Expect(failures, read.insectAges.Length == 1 && read.insectAges[0] == 12f, "upgrading changed the insects");
            Expect(failures, File.ReadAllText(path) == version1Save, "reading an old save should leave the file as it was, until the game next saves");
        }

        /// <summary>A save as version 2 of the game wrote it, before the tree's seed was saved.</summary>
        private static void CheckUpgradeFromVersion2(List<string> failures, string folder)
        {
            const string version2Save =
                "{\"version\":2,\"trunkGrowth\":0.3,\"branchGrowth\":0.6,\"foliageGrowth\":0.9,\"waterFill\":0.5," +
                "\"nutrientsFill\":0.25,\"healthFill\":1.0,\"playerX\":1.5,\"inventorySlots\":[]," +
                "\"composter\":{\"state\":0,\"workSeconds\":0.0,\"workDuration\":0.0,\"compostGrams\":0.0}," +
                "\"insectAges\":[],\"fallenLeaves\":[]}";

            string path = Path.Combine(folder, "version 2.json");
            File.WriteAllText(path, version2Save);

            if (!new SaveFile(path).TryRead(out SaveData read))
            {
                failures.Add("a version 2 save couldn't be read");
                return;
            }

            Expect(failures, read.version == SaveData.CurrentVersion, $"a version 2 save read as version {read.version}");
            Expect(failures, read.treeSeed == 12345, $"a version 2 tree should keep the seed every tree grew from then, not {read.treeSeed}");
            Expect(failures, read.trunkGrowth == 0.3f && read.branchGrowth == 0.6f && read.foliageGrowth == 0.9f &&
                             read.playerX == 1.5f, "upgrading a version 2 save changed the tree's growth or the player");
        }

        private static void CheckNoSave(List<string> failures, string folder)
        {
            var file = new SaveFile(Path.Combine(folder, "missing.json"));
            Expect(failures, !file.TryRead(out SaveData data) && data == null, "a missing file should read as no save");
            Expect(failures, !file.TryWrite(null), "saving nothing should be refused");
            Expect(failures, !File.Exists(Path.Combine(folder, "missing.json")), "saving nothing should not create a file");
        }

        private static void CheckUnusableFiles(List<string> failures, string folder)
        {
            string newerVersion = $"{{\"version\":{SaveData.CurrentVersion + 1},\"trunkGrowth\":0.5}}";
            var unusable = new (string description, string text)[]
            {
                ("an empty file", ""),
                ("text that isn't JSON", "this is not a save"),
                ("JSON cut off partway", "{\"version\":1,\"treeGrowth\":0."),
                ("JSON that isn't a save", "{\"somethingElse\":5}"),
                ("a version of 0", "{\"version\":0}"),
                ("a negative version", "{\"version\":-1}"),
                ("a save from a newer game", newerVersion),
            };

            foreach ((string description, string text) in unusable)
            {
                string path = Path.Combine(folder, "unusable.json");
                File.WriteAllText(path, text);
                var file = new SaveFile(path);

                Expect(failures, !file.TryRead(out SaveData data) && data == null, $"{description} should read as no save");
                Expect(failures, File.ReadAllText(path) == text, $"reading {description} should leave the file as it was");
            }
        }

        private static void CheckFailedSaveKeepsThePreviousOne(List<string> failures, string folder)
        {
            if (!IsWindows)
            {
                return;
            }

            string path = Path.Combine(folder, "locked save.json");
            var file = new SaveFile(path);
            SaveData first = NewSample();
            file.TryWrite(first);

            // A failed write is meant to log an error, which would only be noise here.
            Debug.unityLogger.logEnabled = false;
            try
            {
                using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    SaveData second = NewSample();
                    second.trunkGrowth = 0.99f;
                    Expect(failures, !file.TryWrite(second), "a write to a locked file should report failure");
                }
            }
            finally
            {
                Debug.unityLogger.logEnabled = true;
            }

            Expect(failures, file.TryRead(out SaveData read) && read.trunkGrowth == first.trunkGrowth, "a failed save should leave the previous one intact");
        }
#endif

        private static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;

        private static void InTemporaryFolder(Action<string> run)
        {
            string folder = Path.Combine(Path.GetTempPath(), "TreeGameSaveCheck-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                run(folder);
            }
            finally
            {
                Directory.Delete(folder, recursive: true);
            }
        }

        private static void Expect(List<string> failures, bool condition, string failure)
        {
            if (!condition)
            {
                failures.Add(failure);
            }
        }

        private static void CheckWriteAndRead(List<string> failures, string folder)
        {
            var file = new AtomicTextFile(Path.Combine(folder, "basic.txt"));
            Expect(failures, !file.Exists, "a file that was never written should not exist");

            // Not plain English, so a wrong text encoding would show up.
            const string text = "line one\nlínea dos ✓";
            file.Write(text);
            Expect(failures, file.Exists, "a written file should exist");
            Expect(failures, file.Read() == text, "a file should read back exactly as written");
        }

        private static void CheckOverwriting(List<string> failures, string folder)
        {
            var file = new AtomicTextFile(Path.Combine(folder, "overwrite.txt"));
            file.Write("a much longer first text than the second one");
            file.Write("short");
            Expect(failures, file.Read() == "short", "a new write should replace all of the old text");
        }

        private static void CheckCreatesMissingFolders(List<string> failures, string folder)
        {
            var file = new AtomicTextFile(Path.Combine(folder, "new", "nested", "file.txt"));
            file.Write("text");
            Expect(failures, file.Read() == "text", "writing should create missing folders");
        }

        private static void CheckLeavesNoTemporaryFile(List<string> failures, string folder)
        {
            string path = Path.Combine(folder, "tidy.txt");
            var file = new AtomicTextFile(path);

            file.Write("first");
            Expect(failures, !File.Exists(path + ".tmp"), "a first write left its temporary file behind");

            file.Write("second");
            Expect(failures, !File.Exists(path + ".tmp"), "an overwrite left its temporary file behind");
        }

        private static void CheckIgnoresLeftoverTemporaryFile(List<string> failures, string folder)
        {
            // As left by a crash partway through a write: the real file must be unaffected, and saving must still work.
            string path = Path.Combine(folder, "crashed.txt");
            var file = new AtomicTextFile(path);
            file.Write("good");
            File.WriteAllText(path + ".tmp", "half writ");

            Expect(failures, file.Read() == "good", "a leftover temporary file changed what was read");

            file.Write("newer");
            Expect(failures, file.Read() == "newer", "a leftover temporary file got in the way of writing");
        }

        private static void CheckFailedWriteKeepsOldText(List<string> failures, string folder)
        {
            // Holding a file open to block a write only works on Windows.
            if (!IsWindows)
            {
                return;
            }

            string path = Path.Combine(folder, "locked.txt");
            var file = new AtomicTextFile(path);
            file.Write("old");

            bool threw = false;
            using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                try
                {
                    file.Write("new");
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    threw = true;
                }
            }

            Expect(failures, threw, "a failed write should throw, so the caller knows");
            Expect(failures, file.Read() == "old", "a failed write should leave the old text in place");

            file.Write("later");
            Expect(failures, file.Read() == "later", "writing should work again once the problem has passed");

            // The text must reach the temporary file before the real one is touched, so blocking the temporary file
            // has to stop the write with the real one unchanged. This is what makes a crash partway through safe.
            threw = false;
            using (new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                try
                {
                    file.Write("written directly");
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    threw = true;
                }
            }

            File.Delete(path + ".tmp");
            Expect(failures, threw, "a write that can't use its temporary file should throw");
            Expect(failures, file.Read() == "later", "a write should go through its temporary file before touching the real one");
        }

        private static void CheckBadInputIsRejected(List<string> failures, string folder)
        {
            void ExpectRejected(string description, Action action)
            {
                try
                {
                    action();
                    failures.Add($"{description} should be rejected");
                }
                catch (ArgumentException)
                {
                    // Expected. ArgumentNullException is a kind of this.
                }
            }

            ExpectRejected("a null path", () => new AtomicTextFile(null));
            ExpectRejected("a blank path", () => new AtomicTextFile("  "));

            string path = Path.Combine(folder, "null text.txt");
            var file = new AtomicTextFile(path);
            file.Write("old");
            ExpectRejected("null text", () => file.Write(null));
            Expect(failures, file.Read() == "old", "rejected text should leave the old text in place");
        }
    }
}
