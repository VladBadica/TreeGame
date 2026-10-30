using UnityEngine;

namespace TreeGame.Items
{
    /// <summary>Settings shared by every water bucket.</summary>
    [CreateAssetMenu(fileName = "WaterBucket", menuName = "Tree Game/Items/Water Bucket")]
    public class WaterBucketDefinition : ContainerDefinition
    {
        [Tooltip("How much water the bucket holds, in liters.")]
        [SerializeField, Min(0.01f)] private float capacity = 100f;

        public override float Capacity => capacity;

        public override Item CreateItem() => new WaterBucket(this);
    }
}
