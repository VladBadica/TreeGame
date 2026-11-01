using TreeGame.Core;
using UnityEngine;

namespace TreeGame.Care
{
    /// <summary>
    /// Drains a <see cref="TreeResource"/> at a steady rate, giving the player a reason to replenish it.
    /// The rate comes from <see cref="GameConfig"/> as a duration, so it stays meaningful if the maximum changes.
    /// Disable the component to pause depletion.
    /// </summary>
    public class ResourceDepletion : MonoBehaviour, IAdvancesWithTime
    {
        [SerializeField] private TreeResource resource;
        [SerializeField] private GameConfig config;

        private void OnEnable()
        {
            if (resource == null || config == null)
            {
                Debug.LogWarning($"{nameof(ResourceDepletion)}: needs a {nameof(TreeResource)} and a {nameof(GameConfig)}.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            // Scaled time, so pausing via Time.timeScale also pauses depletion.
            Advance(Time.deltaTime);
        }

        public void Advance(float seconds)
        {
            float secondsToEmpty = config.GetResourceSettings(resource.Type).SecondsToEmpty;
            resource.Consume(resource.MaxAmount * seconds / secondsToEmpty);
        }
    }
}
