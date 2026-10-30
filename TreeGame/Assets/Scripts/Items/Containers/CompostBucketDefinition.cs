using TreeGame.Care;
using UnityEngine;

namespace TreeGame.Items
{
    /// <summary>Settings shared by every compost bucket.</summary>
    [CreateAssetMenu(fileName = "CompostBucket", menuName = "Tree Game/Items/Compost Bucket")]
    public class CompostBucketDefinition : ContainerDefinition
    {
        [Tooltip("How much compost the bucket holds, in grams.")]
        [SerializeField, Min(0.01f)] private float capacity = 1000f;

        public override float Capacity => capacity;

        // Compost restores the tree's nutrients.
        public override TreeResourceType PouredResource => TreeResourceType.Nutrients;

        public override Item CreateItem() => new CompostBucket(this);
    }
}
