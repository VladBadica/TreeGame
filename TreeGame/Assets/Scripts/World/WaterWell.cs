using TreeGame.Core;
using TreeGame.Interaction;
using TreeGame.Items;
using TreeGame.Minigames;
using UnityEngine;

namespace TreeGame.World
{
    /// <summary>
    /// Makes the well give water: using its <see cref="Interactable"/> starts a minigame, and its score decides how
    /// much the first water bucket that isn't full receives (a perfect score gives the share set in
    /// <see cref="GameConfig"/>). Keeps the prompt honest about a full bucket. Connects interaction, minigames and
    /// inventory at this level, so none of them knows about the others.
    /// </summary>
    public class WaterWell : MonoBehaviour
    {
        [SerializeField] private Interactable interactable;
        [SerializeField] private GameConfig config;
        [SerializeField] private MinigameRunner minigameRunner;
        [SerializeField] private MinigameDefinition minigame;
        [Tooltip("The inventory holding the bucket to fill, i.e. the player's.")]
        [SerializeField] private Inventory inventory;
        [Tooltip("Shown instead of the prompt when there is no room for more water.")]
        [SerializeField] private string bucketFullStatus = "Bucket is full";

        private void OnEnable()
        {
            if (interactable == null || config == null || minigameRunner == null || minigame == null || inventory == null)
            {
                Debug.LogWarning($"{nameof(WaterWell)}: needs an {nameof(Interactable)}, a {nameof(GameConfig)}, a minigame runner, a minigame and an {nameof(Inventory)}.", this);
                enabled = false;
                return;
            }

            interactable.Interacted += StartDrawingWater;
            inventory.SlotChanged += OnSlotChanged;
            UpdatePrompt();
        }

        private void OnDisable()
        {
            if (interactable != null)
            {
                interactable.Interacted -= StartDrawingWater;
            }

            if (inventory != null)
            {
                inventory.SlotChanged -= OnSlotChanged;
            }
        }

        private void StartDrawingWater(GameObject user)
        {
            WaterBucket bucket = FindBucketWithRoom();
            if (bucket == null)
            {
                return;
            }

            minigameRunner.Launch(minigame, result =>
            {
                bucket.AddAmount(bucket.Capacity * config.WellFillPerUse * result.Score);
            });
        }

        private void OnSlotChanged(int slot)
        {
            UpdatePrompt();
        }

        private void UpdatePrompt()
        {
            // Only "full" when there is a bucket and it is full; without one, the normal prompt stays.
            bool bucketIsFull = inventory.FindFirst<WaterBucket>() != null && FindBucketWithRoom() == null;
            interactable.SetStatusText(bucketIsFull ? bucketFullStatus : null);
        }

        private WaterBucket FindBucketWithRoom()
        {
            return inventory.FindFirst<WaterBucket>(candidate => !candidate.IsFull);
        }
    }
}
