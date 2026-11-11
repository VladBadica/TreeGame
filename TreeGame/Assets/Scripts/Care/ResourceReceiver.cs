using System;
using UnityEngine;

namespace TreeGame.Care
{
    /// <summary>
    /// Marks something a resource can be poured onto (e.g. water or compost onto the tree) and passes it into a
    /// <see cref="TreeResource"/>, one unit to one unit. Add one per accepted resource. Pourers like buckets only
    /// need to find a receiver of the right type; they don't need to know what receives it.
    /// </summary>
    public class ResourceReceiver : MonoBehaviour
    {
        [Tooltip("The resource that poured amounts go into.")]
        [SerializeField] private TreeResource resource;

        /// <summary>Raised with the amount poured, whenever something is poured on, e.g. for a sound.</summary>
        public event Action<float> Received;

        /// <summary>Raised after a pour with how much of it didn't fit and was lost, if any did.</summary>
        public event Action<float> Overflowed;

        /// <summary>The resource poured amounts go into, e.g. to compare an overflow with its maximum.</summary>
        public TreeResource Resource => resource;

        public bool IsAvailable => isActiveAndEnabled && resource != null;

        public TreeResourceType ResourceType => resource.Type;

        private void OnEnable()
        {
            if (resource == null)
            {
                Debug.LogWarning($"{nameof(ResourceReceiver)}: no {nameof(TreeResource)} assigned.", this);
                enabled = false;
            }
        }

        /// <summary>Adds a poured amount. Anything beyond the resource's maximum is lost, like overfilling a pot.</summary>
        public void Receive(float amount)
        {
            if (IsAvailable && amount > 0f && float.IsFinite(amount))
            {
                float room = Mathf.Max(0f, resource.MaxAmount - resource.Amount);
                resource.Add(amount);
                Received?.Invoke(amount);

                if (amount > room)
                {
                    Overflowed?.Invoke(amount - room);
                }
            }
        }
    }
}
