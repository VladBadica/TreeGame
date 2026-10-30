using UnityEngine;

namespace TreeGame.Care
{
    /// <summary>
    /// Marks something that water can be poured onto (e.g. the tree) and passes poured water to its water resource.
    /// Pourers like the bucket only need to find this component; they don't need to know what receives the water.
    /// </summary>
    public class WaterReceiver : MonoBehaviour
    {
        [Tooltip("The resource that poured water goes into; liters map one to one.")]
        [SerializeField] private TreeResource water;

        private void OnEnable()
        {
            if (water == null)
            {
                Debug.LogWarning($"{nameof(WaterReceiver)}: no water {nameof(TreeResource)} assigned.", this);
                enabled = false;
            }
        }

        /// <summary>Adds poured water. Anything beyond the resource's maximum is lost, like overfilling a pot.</summary>
        public void ReceiveWater(float liters)
        {
            if (enabled)
            {
                water.Add(liters);
            }
        }
    }
}
