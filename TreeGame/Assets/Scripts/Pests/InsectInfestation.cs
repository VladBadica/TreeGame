using System;
using TreeGame.Care;
using TreeGame.Core;
using UnityEngine;

namespace TreeGame.Pests
{
    /// <summary>
    /// The insects currently on the tree, with timing and damage from <see cref="GameConfig"/>. It takes the damage
    /// they do from the tree's health. It doesn't know what brings insects or what removes them, and draws nothing:
    /// visuals listen to <see cref="CountChanged"/>. Disable the component to pause the insects; a catch-up after a
    /// minigame skips it too, so they do no damage for time spent paused.
    /// </summary>
    public class InsectInfestation : MonoBehaviour, IAdvancesWithTime
    {
        [SerializeField] private TreeResource health;
        [SerializeField] private GameConfig config;

        private InsectInfestationRules rules;

        /// <summary>Raised with the new number of insects when it changes.</summary>
        public event Action<int> CountChanged;

        public int Count => Rules.Count;

        // Created on first use, so other components can read the count regardless of initialization order.
        private InsectInfestationRules Rules
        {
            get
            {
                if (rules == null)
                {
                    InsectSettings settings = config.Insects;
                    rules = new InsectInfestationRules(settings.GraceSeconds, settings.DamageSeconds,
                        settings.DamagePerInsect, settings.MaxOnTree);
                }

                return rules;
            }
        }

        private void OnEnable()
        {
            if (health == null || config == null)
            {
                Debug.LogWarning($"{nameof(InsectInfestation)}: needs a health {nameof(TreeResource)} and a {nameof(GameConfig)}.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            // Scaled time, so pausing via Time.timeScale also pauses the insects.
            Advance(Time.deltaTime);
        }

        public void Advance(float seconds)
        {
            int countBefore = Rules.Count;
            float damage = Rules.Advance(seconds);

            if (damage > 0f)
            {
                health.Consume(damage);
            }

            if (Rules.Count != countBefore)
            {
                CountChanged?.Invoke(Rules.Count);
            }
        }

        /// <summary>Puts a new insect on the tree. Returns false if the tree already has as many as it can.</summary>
        public bool TryAddInsect()
        {
            if (!Rules.TryAddInsect())
            {
                return false;
            }

            CountChanged?.Invoke(Rules.Count);
            return true;
        }

        /// <summary>Removes a share of the insects (0 to 1), rounded up, oldest first.</summary>
        public void RemoveShare(float share)
        {
            int countBefore = Rules.Count;
            Rules.RemoveShare(share);

            if (Rules.Count != countBefore)
            {
                CountChanged?.Invoke(Rules.Count);
            }
        }
    }
}
