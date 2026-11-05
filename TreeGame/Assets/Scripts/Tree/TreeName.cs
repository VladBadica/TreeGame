using UnityEngine;

namespace TreeGame.Trees
{
    /// <summary>
    /// Owns the name the player gave their tree, if any. A new tree takes the name chosen for it before the game
    /// started; a loaded game sets the one it was saved with. It knows nothing about menus or saving.
    /// </summary>
    public class TreeName : MonoBehaviour
    {
        private string treeName = "";
        private bool chosen;

        /// <summary>The tree's name, or empty if it has none. Always clean (see <see cref="TreeNames.Clean"/>).</summary>
        public string Name
        {
            get
            {
                if (!chosen)
                {
                    chosen = true;
                    if (NewTreeChoices.Name.TryTake(out string requested))
                    {
                        treeName = TreeNames.Clean(requested);
                    }
                }

                return treeName;
            }
        }

        public void SetName(string value)
        {
            chosen = true;
            treeName = TreeNames.Clean(value);
        }
    }
}
