using TreeGame.Core;
using TreeGame.Seasons;
using UnityEngine;

namespace TreeGame.Care
{
    /// <summary>
    /// Drains a <see cref="TreeResource"/> at a steady rate, giving the player a reason to replenish it.
    /// The rate comes from <see cref="GameConfig"/> as a duration, so it stays meaningful if the maximum changes.
    /// A duration of 0 means the resource doesn't drain on its own. The season makes water and nutrients drain faster or
    /// slower. Disable the component to pause depletion.
    /// </summary>
    public class ResourceDepletion : MonoBehaviour, IAdvancesWithTime
    {
        [SerializeField] private TreeResource resource;
        [SerializeField] private GameConfig config;
        [Tooltip("Speeds draining up or slows it down with the season. Without one, it drains the same all year.")]
        [SerializeField] private SeasonClock seasons;

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
            if (secondsToEmpty > 0f)
            {
                resource.Consume(resource.MaxAmount * seconds * SeasonMultiplier() / secondsToEmpty);
            }
        }

        /// <summary>How much faster or slower the season makes this resource drain; 1 for one the seasons don't change.</summary>
        private float SeasonMultiplier()
        {
            return resource.Type switch
            {
                TreeResourceType.Water => SeasonClock.MultiplierOf(seasons, SeasonEffect.WaterDrain),
                TreeResourceType.Nutrients => SeasonClock.MultiplierOf(seasons, SeasonEffect.NutrientDrain),
                _ => 1f,
            };
        }
    }
}
