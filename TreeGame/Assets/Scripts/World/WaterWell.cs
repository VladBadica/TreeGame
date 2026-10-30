using TreeGame.Core;
using TreeGame.Interaction;
using TreeGame.Items;
using UnityEngine;

namespace TreeGame.World
{
    /// <summary>
    /// Makes the well give water: each time its <see cref="Interactable"/> is used, the user's first water bucket
    /// that isn't full gets a share of its capacity (set in <see cref="GameConfig"/>). Connects interaction and
    /// inventory at this level, so neither system knows about the other. Can later start a minigame instead.
    /// </summary>
    public class WaterWell : MonoBehaviour
    {
        [SerializeField] private Interactable interactable;
        [SerializeField] private GameConfig config;

        private void OnEnable()
        {
            if (interactable == null || config == null)
            {
                Debug.LogWarning($"{nameof(WaterWell)}: needs an {nameof(Interactable)} and a {nameof(GameConfig)}.", this);
                enabled = false;
                return;
            }

            interactable.Interacted += GiveWater;
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.Interacted -= GiveWater;
            }
        }

        private void GiveWater(GameObject user)
        {
            if (!user.TryGetComponent(out Inventory inventory))
            {
                return;
            }

            // No water bucket, or every one already full: nothing to fill.
            WaterBucket bucket = inventory.FindFirst<WaterBucket>(candidate => !candidate.IsFull);
            bucket?.AddAmount(bucket.Capacity * config.WellFillPerUse);
        }
    }
}
