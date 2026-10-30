using UnityEngine;

namespace TreeGame.Items
{
    /// <summary>A bucket of compost, measured in grams.</summary>
    public class CompostBucket : ContainerItem
    {
        public CompostBucket(CompostBucketDefinition definition) : base(definition)
        {
        }

        public override void OnUse(GameObject user)
        {
            // Nothing yet: spreading compost (e.g. to restore the tree's nutrients) comes with the compost mechanic.
        }
    }
}
