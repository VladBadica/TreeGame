using TreeGame.Core;
using TreeGame.Trees;
using UnityEngine;

namespace TreeGame.Care
{
    /// <summary>
    /// Slows the tree's growth while one of its resources (water or nutrients) is empty: every part's rate drops by the
    /// amount in <see cref="GameConfig"/>. One per resource, so with both empty the drops add up. Lifts as soon as the
    /// resource is above 0 again. Nothing to save: it follows the resource, which is saved. Connects resources and growth
    /// rates at this level, so neither knows about the other.
    /// </summary>
    public class EmptyResourcePenalty : MonoBehaviour
    {
        [SerializeField] private TreeResource resource;
        [SerializeField] private TreeGrowthRates growthRates;
        [SerializeField] private GameConfig config;

        // Listens from Awake to OnDestroy rather than only while enabled: during a minigame everything is switched off,
        // and a resource that runs out while time is caught up afterwards must still slow growth from that moment.
        private void Awake()
        {
            if (resource == null || growthRates == null || config == null)
            {
                Debug.LogWarning($"{nameof(EmptyResourcePenalty)}: needs a resource, the growth rates and a {nameof(GameConfig)}.", this);
                enabled = false;
                return;
            }

            resource.Changed += OnResourceChanged;
            Apply();
        }

        private void OnDestroy()
        {
            if (resource != null)
            {
                resource.Changed -= OnResourceChanged;
            }
        }

        private void OnResourceChanged(TreeResource changed)
        {
            Apply();
        }

        private void Apply()
        {
            // The same test the health damage uses: only an empty resource counts.
            bool isEmpty = resource.Normalized <= 0f;
            growthRates.SetTemporaryChange(this, isEmpty ? -config.EmptyResourceRatePenalty : 0);
        }
    }
}
