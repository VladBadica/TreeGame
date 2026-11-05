using System;
using System.IO;
using UnityEngine;

namespace TreeGame.Saving
{
    /// <summary>
    /// Reads and writes one save as JSON (see <see cref="SaveSlots"/> for where each is kept). Saves from earlier
    /// versions are upgraded as they are read, and are only rewritten in the current format the next time the game
    /// saves. It never throws and never destroys a save: a file it can't read, because it is damaged or from a newer
    /// version of the game, is reported as no save and left where it is, and deleting only moves the file aside, so
    /// either can still be looked at or recovered.
    /// </summary>
    public sealed class SaveFile
    {
        /// <summary>Added to a deleted save's name: deleting moves it here, replacing the one deleted before.</summary>
        public const string DeletedSuffix = ".deleted";

        private readonly string path;
        private readonly AtomicTextFile file;

        public SaveFile(string path)
        {
            this.path = path;
            file = new AtomicTextFile(path);
        }

        /// <summary>Whether there is a file here at all, readable or not.</summary>
        public bool Exists => file.Exists;

        /// <summary>Writes a save, stamped with the current format's version and the time. Returns false if it couldn't.</summary>
        public bool TryWrite(SaveData data)
        {
            if (data == null)
            {
                Debug.LogError($"{nameof(SaveFile)}: nothing to save.");
                return false;
            }

            data.version = SaveData.CurrentVersion;
            data.SetSavedAt(DateTime.UtcNow);

            try
            {
                file.Write(JsonUtility.ToJson(data, prettyPrint: true));
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                // The previous save is still intact, because a failed write never touches it.
                Debug.LogError($"{nameof(SaveFile)}: couldn't write the save: {exception.Message}");
                return false;
            }
        }

        /// <summary>Reads the save. Returns false if there is none, or it can't be used.</summary>
        public bool TryRead(out SaveData data)
        {
            data = null;
            if (!file.Exists)
            {
                return false;
            }

            string json;
            SaveData read;
            try
            {
                json = file.Read();
                read = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException ||
                                              exception is ArgumentException)
            {
                Debug.LogWarning($"{nameof(SaveFile)}: couldn't read the save: {exception.Message}");
                return false;
            }

            // Valid JSON can still be no save at all, which leaves the version at 0.
            if (read == null || read.version < 1)
            {
                Debug.LogWarning($"{nameof(SaveFile)}: the file isn't a save.");
                return false;
            }

            if (read.version > SaveData.CurrentVersion)
            {
                Debug.LogWarning($"{nameof(SaveFile)}: the save is from a newer version of the game ({read.version}, this one reads up to {SaveData.CurrentVersion}).");
                return false;
            }

            SaveUpgrades.Upgrade(read, json);

            // Saves from before the time was stored: when the file was last written is when it was saved.
            if (!read.TryGetSavedAt(out _))
            {
                read.SetSavedAt(File.GetLastWriteTimeUtc(path));
            }

            data = read;
            return true;
        }

        /// <summary>
        /// Empties the slot by moving the save aside (to its name plus <see cref="DeletedSuffix"/>) rather than erasing
        /// it, so a save deleted by mistake can still be recovered by hand. Returns false if it couldn't; true if there
        /// was nothing to delete.
        /// </summary>
        public bool TryDelete()
        {
            if (!file.Exists)
            {
                return true;
            }

            string deletedPath = path + DeletedSuffix;
            try
            {
                if (File.Exists(deletedPath))
                {
                    File.Delete(deletedPath);
                }

                File.Move(path, deletedPath);
                return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogError($"{nameof(SaveFile)}: couldn't delete the save: {exception.Message}");
                return false;
            }
        }
    }
}
