using TreeGame.Trees;
using UnityEngine;

namespace TreeGame.Market
{
    /// <summary>
    /// Something the market sells: a tonic that makes one part of the tree grow faster, raising that part's growth rate
    /// as soon as it is bought. One asset per offer, so names, prices and art can be changed in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "MarketOffer", menuName = "Tree Game/Market Offer")]
    public class MarketOffer : ScriptableObject
    {
        [SerializeField] private string displayName = "Tonic";
        [Tooltip("One short line under the name, saying what it does.")]
        [SerializeField] private string description = "";
        [SerializeField] private Sprite icon;
        [Tooltip("The part of the tree whose growth rate goes up.")]
        [SerializeField] private TreeGrowthPart part = TreeGrowthPart.Trunk;
        [Tooltip("How much the part's growth rate goes up per purchase. Rates stop at 10.")]
        [SerializeField, Min(1)] private int rateIncrease = 1;
        [Tooltip("Price in coins of the first purchase, while the part is at its starting rate. The price doubles for " +
                 "each step the part's rate is above that, e.g. 1, 2, 4, 8, 16.")]
        [SerializeField, Min(0)] private int price = 1;

        public string DisplayName => displayName;

        public string Description => description;

        public Sprite Icon => icon;

        public TreeGrowthPart Part => part;

        // The asset file could still hold values the Inspector wouldn't allow.
        public int RateIncrease => Mathf.Max(1, rateIncrease);

        /// <summary>The price of the first purchase; <see cref="MarketStall.PriceOf"/> gives the price right now.</summary>
        public int BasePrice => Mathf.Max(0, price);
    }
}
