using TreeGame.Trees;
using UnityEngine;

namespace TreeGame.Seasons
{
    /// <summary>
    /// Shows the season on the tree: tells <see cref="TreeVisuals"/> how many leaves it has and what color they are, from
    /// the <see cref="SeasonClock"/>. Keeps the visuals independent of where the season comes from.
    /// </summary>
    public class TreeSeasonLook : MonoBehaviour
    {
        [SerializeField] private SeasonClock clock;
        [SerializeField] private TreeVisuals visuals;

        private void OnEnable()
        {
            if (clock == null || visuals == null)
            {
                Debug.LogWarning($"{nameof(TreeSeasonLook)}: needs a {nameof(SeasonClock)} and the {nameof(TreeVisuals)}.", this);
                enabled = false;
            }
        }

        // After Update, so a season loaded this frame shows at once.
        private void LateUpdate()
        {
            SeasonLook look = clock.Look;
            visuals.SetSeasonLook(look.LeafCover, look.Autumn, look.Fresh);
        }
    }
}
