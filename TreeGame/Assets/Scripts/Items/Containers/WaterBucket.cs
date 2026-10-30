using TreeGame.Care;
using TreeGame.Interaction;
using UnityEngine;

namespace TreeGame.Items
{
    /// <summary>A bucket of water, measured in liters. Using it pours all its water onto what the player stands at.</summary>
    public class WaterBucket : ContainerItem
    {
        public WaterBucket(WaterBucketDefinition definition) : base(definition)
        {
        }

        /// <summary>
        /// Pours all the water onto whatever the user is standing at, if it accepts water (e.g. the tree).
        /// Otherwise nothing happens and the bucket keeps its water.
        /// </summary>
        public override void OnUse(GameObject user)
        {
            if (IsEmpty || !user.TryGetComponent(out Interactor interactor) || interactor.Focused == null)
            {
                return;
            }

            if (interactor.Focused.TryGetComponent(out WaterReceiver receiver))
            {
                receiver.ReceiveWater(StoredAmount);
                SetStoredAmount(0f);
            }
        }
    }
}
