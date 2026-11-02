using System;
using System.IO;
using UnityEngine;

namespace TreeGame.Saving
{
    /// <summary>
    /// Reads and writes the game's one save as JSON. It never throws and never deletes anything: a file it can't read,
    /// because it is damaged or from a newer version of the game, is reported as no save and left where it is, so it
    /// can still be looked at or recovered.
    /// </summary>
    public sealed class SaveFile
    {
        private const string FileName = "save.json";

        private readonly AtomicTextFile file;

        public SaveFile(string path)
        {
            file = new AtomicTextFile(path);
        }

        /// <summary>The save this game uses, in the player's data folder.</summary>
        public static SaveFile ForThisGame()
        {
            return new SaveFile(Path.Combine(Application.persistentDataPath, FileName));
        }

        /// <summary>Writes a save, stamped with the current format's version. Returns false if it couldn't.</summary>
        public bool TryWrite(SaveData data)
        {
            if (data == null)
            {
                Debug.LogError($"{nameof(SaveFile)}: nothing to save.");
                return false;
            }

            data.version = SaveData.CurrentVersion;

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

            SaveData read;
            try
            {
                read = JsonUtility.FromJson<SaveData>(file.Read());
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

            data = read;
            return true;
        }
    }
}
