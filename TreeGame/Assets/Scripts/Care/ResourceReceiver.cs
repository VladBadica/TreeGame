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
            if (IsAvailable)
            {
                resource.Add(amount);
            }
        }
    }
}
