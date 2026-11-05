namespace TreeGame.Trees
{
    /// <summary>What a typed-in tree code means for growing a new tree.</summary>
    public enum TreeCodeStatus
    {
        /// <summary>Nothing typed: a random tree grows.</summary>
        Empty,

        /// <summary>A code this version of the game can grow.</summary>
        Valid,

        /// <summary>Fewer characters than a code has so far, so not wrong yet.</summary>
        Incomplete,

        /// <summary>A code with a mistake in it.</summary>
        Typo,

        /// <summary>A real code, from an earlier version that grew trees by other rules, so its tree can't be grown.</summary>
        OlderVersion,

        /// <summary>A real code, from a later version of the game.</summary>
        NewerVersion,
    }

    /// <summary>
    /// Reads a tree code as the player types it, to say whether a tree can grow from it. Plain C# with no Unity
    /// dependency, so it can be checked on its own.
    /// </summary>
    public static class TreeCodeEntry
    {
        /// <summary>What the typed text means, given the version of the rules this game grows trees by.</summary>
        public static TreeCodeStatus Read(string typed, int generationVersion, out int seed)
        {
            seed = 0;
            int characters = 0;
            foreach (char character in typed ?? "")
            {
                if (character != '-' && !char.IsWhiteSpace(character))
                {
                    characters++;
                }
            }

            if (characters == 0)
            {
                return TreeCodeStatus.Empty;
            }

            if (!SeedCode.TryDecode(typed, out int decodedSeed, out int version))
            {
                return characters < SeedCode.Length ? TreeCodeStatus.Incomplete : TreeCodeStatus.Typo;
            }

            if (version < generationVersion)
            {
                return TreeCodeStatus.OlderVersion;
            }

            if (version > generationVersion)
            {
                return TreeCodeStatus.NewerVersion;
            }

            seed = decodedSeed;
            return TreeCodeStatus.Valid;
        }

        /// <summary>Whether a tree can grow from what was typed: a random one if nothing, or the code's own.</summary>
        public static bool CanGrow(TreeCodeStatus status)
        {
            return status == TreeCodeStatus.Empty || status == TreeCodeStatus.Valid;
        }
    }
}
