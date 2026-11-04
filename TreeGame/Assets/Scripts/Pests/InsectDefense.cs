using System;
using TreeGame.Core;
using TreeGame.Items;
using TreeGame.Minigames;
using TreeGame.Minigames.InsectSwarm;
using UnityEngine;

namespace TreeGame.Pests
{
    /// <summary>
    /// Makes clicking an insect on the tree start the swarm minigame, sized by how many insects there are, and turns
    /// its score into insects cleared and coins (rules in <see cref="GameConfig"/>). Connects insects, minigames and
    /// inventory at this level, so none of them knows about the others.
    /// </summary>
    public class InsectDefense : MonoBehaviour
    {
        [SerializeField] private InsectVisuals visuals;
        [SerializeField] private InsectInfestation infestation;
        [SerializeField] private GameConfig config;
        [SerializeField] private MinigameRunner minigameRunner;
        [SerializeField] private MinigameDefinition minigame;
        [Tooltip("The inventory that receives the coins, i.e. the player's.")]
        [SerializeField] private Inventory inventory;
        [SerializeField] private ItemDefinition coin;

        /// <summary>Raised with how many coins went into the inventory after a good score, e.g. for a sound.</summary>
        public event Action<int> CoinsAwarded;

        private void OnEnable()
        {
            if (visuals == null || infestation == null || config == null || minigameRunner == null ||
                minigame == null || inventory == null || coin == null)
            {
                Debug.LogWarning($"{nameof(InsectDefense)}: needs insect visuals, an infestation, a config, a minigame runner, a minigame, an inventory and a coin.", this);
                enabled = false;
                return;
            }

            visuals.InsectClicked += StartDefending;
        }

        private void OnDisable()
        {
            if (visuals != null)
            {
                visuals.InsectClicked -= StartDefending;
            }
        }

        private void StartDefending()
        {
            int swarmSize = config.Insects.SwarmSizeFor(infestation.Count);

            bool started = minigameRunner.Launch(minigame, OnFinished, loaded =>
            {
                if (loaded is InsectSwarmGame game)
                {
                    game.UseSwarmSize(swarmSize);
                }
            });

            // The player is dealing with them, so the insects on the tree do no damage in the meantime.
            if (started)
            {
                infestation.enabled = false;
            }
        }

        private void OnFinished(MinigameResult result)
        {
            infestation.enabled = true;

            // Leaving early changes nothing.
            if (!result.Completed)
            {
                return;
            }

            InsectSettings settings = config.Insects;
            if (result.Score >= settings.GoodScore)
            {
                infestation.RemoveShare(1f);

                int coinsAwarded = settings.CoinsForGoodScore - inventory.Add(coin, settings.CoinsForGoodScore);
                if (coinsAwarded > 0)
                {
                    CoinsAwarded?.Invoke(coinsAwarded);
                }
            }
            else
            {
                infestation.RemoveShare(settings.ShareClearedByBadScore);
            }
        }
    }
}
