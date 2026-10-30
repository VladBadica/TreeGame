using TreeGame.Core;
using UnityEngine;

namespace TreeGame.Trees
{
    /// <summary>
    /// Feeds the shared wind from <see cref="GameConfig"/>, sampled at the tree's position, into <see cref="TreeVisuals"/>.
    /// Keeps the visuals independent of where the wind comes from. Disable the component for a calm tree.
    /// </summary>
    public class TreeWindSway : MonoBehaviour
    {
        [SerializeField] private TreeVisuals visuals;
        [SerializeField] private GameConfig config;

        private void OnEnable()
        {
            if (visuals == null || config == null)
            {
                Debug.LogWarning($"{nameof(TreeWindSway)}: needs a {nameof(TreeVisuals)} and a {nameof(GameConfig)}.", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            if (visuals != null)
            {
                visuals.SetWind(0f);
            }
        }

        private void Update()
        {
            // Scaled time, so pausing via Time.timeScale also stops the wind.
            visuals.SetWind(config.Wind.Evaluate(transform.position.x, Time.time));
        }
    }
}
