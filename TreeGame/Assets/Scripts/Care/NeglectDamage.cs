using TreeGame.Core;
using UnityEngine;

namespace TreeGame.Care
{
    /// <summary>
    /// Slowly takes health from a tree that has run out of water or nutrients. Each empty resource counts, so with both
    /// empty the tree loses health twice as fast. The rate comes from <see cref="GameConfig"/>. Disable the component
    /// to pause it.
    /// </summary>
    public class NeglectDamage : MonoBehaviour, IAdvancesWithTime
    {
        [SerializeField] private TreeResource health;
        [SerializeField] private TreeResource water;
        [SerializeField] private TreeResource nutrients;
        [SerializeField] private GameConfig config;

        private int EmptyResourceCount => (water.Normalized <= 0f ? 1 : 0) + (nutrients.Normalized <= 0f ? 1 : 0);

        private void OnEnable()
        {
            if (health == null || water == null || nutrients == null || config == null)
            {
                Debug.LogWarning($"{nameof(NeglectDamage)}: needs health, water, nutrients and a {nameof(GameConfig)}.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            // Scaled time, so pausing via Time.timeScale also pauses the damage.
            Advance(Time.deltaTime);
        }

        public void Advance(float seconds)
        {
            int emptyResources = EmptyResourceCount;
            if (emptyResources > 0)
            {
                health.Consume(health.MaxAmount * seconds * emptyResources / config.SecondsToLoseFullHealthPerEmptyResource);
            }
        }
    }
}
