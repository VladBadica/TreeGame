using TreeGame.Core;
using UnityEngine;

namespace TreeGame.Care
{
    /// <summary>
    /// Slowly restores the tree's health while it is well cared for, meaning water and nutrients are both above the
    /// share set in <see cref="GameConfig"/>. Disable the component to pause it.
    /// </summary>
    public class HealthRegeneration : MonoBehaviour, IAdvancesWithTime
    {
        [SerializeField] private TreeResource health;
        [SerializeField] private TreeResource water;
        [SerializeField] private TreeResource nutrients;
        [SerializeField] private GameConfig config;

        private bool IsWellCaredFor =>
            water.Normalized > config.HealthRegenMinResourceShare && nutrients.Normalized > config.HealthRegenMinResourceShare;

        private void OnEnable()
        {
            if (health == null || water == null || nutrients == null || config == null)
            {
                Debug.LogWarning($"{nameof(HealthRegeneration)}: needs health, water, nutrients and a {nameof(GameConfig)}.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            // Scaled time, so pausing via Time.timeScale also pauses regeneration.
            Advance(Time.deltaTime);
        }

        public void Advance(float seconds)
        {
            if (health.Normalized < 1f && IsWellCaredFor)
            {
                health.Add(health.MaxAmount * seconds / config.SecondsToRegenFullHealth);
            }
        }
    }
}
