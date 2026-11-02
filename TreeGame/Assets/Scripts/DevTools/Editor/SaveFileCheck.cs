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
                treeGrowth = 0.42f,
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
            Expect(failures, read.treeGrowth == sample.treeGrowth, $"growth read back as {read.treeGrowth}");
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

        private static void CheckNoSave(List<string> failures, string folder)
        {
            var file = new SaveFile(Path.Combine(folder, "missing.json"));
            Expect(failures, !file.TryRead(out SaveData data) && data == null, "a missing file should read as no save");
            Expect(failures, !file.TryWrite(null), "saving nothing should be refused");
            Expect(failures, !File.Exists(Path.Combine(folder, "missing.json")), "saving nothing should not create a file");
        }

        private static void CheckUnusableFiles(List<string> failures, string folder)
        {
            string newerVersion = $"{{\"version\":{SaveData.CurrentVersion + 1},\"treeGrowth\":0.5}}";
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
                    second.treeGrowth = 0.99f;
                    Expect(failures, !file.TryWrite(second), "a write to a locked file should report failure");
                }
            }
            finally
            {
                Debug.unityLogger.logEnabled = true;
            }

            Expect(failures, file.TryRead(out SaveData read) && read.treeGrowth == first.treeGrowth, "a failed save should leave the previous one intact");
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
