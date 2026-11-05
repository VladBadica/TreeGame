using System.Text;

namespace TreeGame.Trees
{
    /// <summary>
    /// The rules for a name a player gives their tree. A name is shown as rich text in places, so characters that would
    /// be read as formatting are left out, as are invisible ones. Plain C# with no Unity dependency, so it can be checked
    /// on its own.
    /// </summary>
    public static class TreeNames
    {
        /// <summary>The longest a name can be, so it fits on a save slot's card.</summary>
        public const int MaxLength = 24;

        /// <summary>
        /// A name as it should be kept: without the characters rich text reads as formatting ('&lt;' and '&gt;') or
        /// control characters, with runs of spaces made single and none at either end, and no longer than
        /// <see cref="MaxLength"/>. Empty if nothing usable is left, which means the tree has no name.
        /// </summary>
        public static string Clean(string typed)
        {
            if (string.IsNullOrEmpty(typed))
            {
                return "";
            }

            var name = new StringBuilder(typed.Length);
            bool spaceBefore = false;
            foreach (char character in typed)
            {
                if (char.IsWhiteSpace(character))
                {
                    spaceBefore = name.Length > 0;
                    continue;
                }

                if (char.IsControl(character) || character == '<' || character == '>')
                {
                    continue;
                }

                if (spaceBefore)
                {
                    name.Append(' ');
                    spaceBefore = false;
                }

                name.Append(character);
            }

            // Cut to length after tidying, then once more for a space the cut may leave at the end.
            string cleaned = name.ToString();
            return cleaned.Length > MaxLength ? cleaned.Substring(0, MaxLength).TrimEnd() : cleaned;
        }

        /// <summary>What to call a tree: its name, or the slot's own name if it has none.</summary>
        public static string Display(string name, string unnamed)
        {
            string cleaned = Clean(name);
            return cleaned.Length > 0 ? cleaned : unnamed;
        }
    }
}
