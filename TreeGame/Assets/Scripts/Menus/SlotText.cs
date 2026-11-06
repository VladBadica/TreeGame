using System;
using System.Globalization;

namespace TreeGame.Menus
{
    /// <summary>
    /// The words a save slot's card uses for how grown its tree is and when it was last played. Plain C# with no Unity
    /// dependency, so it can be checked on its own.
    /// </summary>
    public static class SlotText
    {
        /// <summary>How many days back "last played" counts in days; older than that shows the date.</summary>
        public const int DaysShownAsDays = 6;

        /// <summary>How grown the tree is overall: the average of its parts, as a whole percentage.</summary>
        public static string Growth(float trunk, float branches, float foliage)
        {
            float average = (Share(trunk) + Share(branches) + Share(foliage)) / 3f;
            return $"{(int)MathF.Round(average * 100f)}% grown";
        }

        /// <summary>
        /// How much extra growth (past full size) the tree has overall, as " +20% extra", the average of its parts; empty
        /// with none. Any at all shows as at least 1%, so a tree that has started on it never reads as having none.
        /// </summary>
        public static string Extra(float trunk, float branches, float foliage)
        {
            float average = (Share(trunk) + Share(branches) + Share(foliage)) / 3f;
            if (average <= 0f)
            {
                return "";
            }

            int percent = Math.Max(1, (int)MathF.Round(average * 100f));
            return $" +{percent}% extra";
        }

        /// <summary>
        /// When the tree was last played, in the player's own days: today, yesterday, some days ago, or the date. Both
        /// times are the player's local time, as "today" means their today.
        /// </summary>
        public static string LastPlayed(DateTime playedLocal, DateTime nowLocal)
        {
            int days = (nowLocal.Date - playedLocal.Date).Days;

            // A clock set back makes a save look like it's from the future; it can't be, so it was today.
            if (days <= 0)
            {
                return "Played today";
            }

            if (days == 1)
            {
                return "Played yesterday";
            }

            if (days <= DaysShownAsDays)
            {
                return $"Played {days} days ago";
            }

            return "Played " + playedLocal.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
        }

        private static float Share(float value)
        {
            return float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : 0f;
        }
    }
}
