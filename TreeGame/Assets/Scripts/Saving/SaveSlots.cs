using System;
using System.IO;
using UnityEngine;

namespace TreeGame.Saving
{
    /// <summary>
    /// The game's save slots: each holds one tree's game, in a file of its own. Earlier versions had a single save; the
    /// first time the slots are looked at, that save moves into the first slot, so nobody loses their tree.
    /// </summary>
    public sealed class SaveSlots
    {
        public const int Count = 3;

        private const string SingleSaveFileName = "save.json";

        private readonly string folder;

        public SaveSlots(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
            {
                throw new ArgumentException("A folder is required.", nameof(folder));
            }

            this.folder = folder;
            MoveSingleSaveIntoFirstSlot();
        }

        /// <summary>The slots this game uses, in the player's data folder.</summary>
        public static SaveSlots ForThisGame()
        {
            return new SaveSlots(Application.persistentDataPath);
        }

        /// <summary>The save in a slot, numbered from 0.</summary>
        public SaveFile Slot(int index)
        {
            return new SaveFile(PathOf(index));
        }

        /// <summary>Where a slot's save is kept. Named from 1, as players count them.</summary>
        public string PathOf(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"There are {Count} slots, numbered from 0.");
            }

            return Path.Combine(folder, $"save {index + 1}.json");
        }

        /// <summary>
        /// Moves the one save of earlier versions into the first slot, as long as that slot is free. Moved rather than
        /// copied, so it can't later be moved again over a newer game; never over a game already in the slot.
        /// </summary>
        private void MoveSingleSaveIntoFirstSlot()
        {
            string single = Path.Combine(folder, SingleSaveFileName);
            string first = PathOf(0);
            if (!File.Exists(single) || File.Exists(first))
            {
                return;
            }

            try
            {
                File.Move(single, first);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                // The save stays where it was, to be moved next time.
                Debug.LogWarning($"{nameof(SaveSlots)}: couldn't move the earlier save into the first slot: {exception.Message}");
            }
        }
    }
}
