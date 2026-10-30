using UnityEngine;

namespace TreeGame.Items
{
    /// <summary>
    /// An item lying in the world that an <see cref="ItemCollector"/> can pick up while standing in this object's
    /// trigger zone. Whatever doesn't fit in the inventory stays on the ground.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField, Min(1)] private int count = 1;

        /// <summary>Tries to move this pickup into the inventory; removes the pickup once all of it fits.</summary>
        public void CollectInto(Inventory inventory)
        {
            if (item == null)
            {
                Debug.LogWarning($"{nameof(ItemPickup)}: no item assigned.", this);
                return;
            }

            int leftOver = inventory.Add(item, count);
            if (leftOver == 0)
            {
                Destroy(gameObject);
            }
            else
            {
                count = leftOver;
            }
        }

        private void Reset()
        {
            // The zone only detects collectors; it should never block movement.
            GetComponent<Collider>().isTrigger = true;
        }
    }
}
