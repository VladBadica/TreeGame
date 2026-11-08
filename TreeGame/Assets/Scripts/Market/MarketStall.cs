using System;
using System.Collections.Generic;
using TreeGame.Items;
using TreeGame.Trees;
using UnityEngine;

namespace TreeGame.Market
{
    /// <summary>
    /// What the market sells and the rules for buying it: an offer is paid for with coins from the player's inventory
    /// and raises a growth rate of the tree at once. Connects the inventory and the tree's growth rates at this level, so
    /// neither knows about the market. It draws nothing; <see cref="MarketMenu"/> shows it.
    /// </summary>
    public class MarketStall : MonoBehaviour
    {
        [SerializeField] private MarketOffer[] offers = new MarketOffer[0];
        [Tooltip("The inventory that pays, i.e. the player's.")]
        [SerializeField] private Inventory inventory;
        [SerializeField] private ItemDefinition coin;
        [SerializeField] private TreeGrowthRates growthRates;

        /// <summary>Raised when anything that changes what can be bought changes: coins, growth rates or a purchase.</summary>
        public event Action Changed;

        /// <summary>Raised with the offer after a purchase, e.g. for a sound.</summary>
        public event Action<MarketOffer> Bought;

        public IReadOnlyList<MarketOffer> Offers => offers;

        public ItemDefinition Coin => coin;

        /// <summary>How many coins the player has to spend.</summary>
        public int Coins => inventory != null ? inventory.CountOf(coin) : 0;

        private void OnEnable()
        {
            if (inventory == null || coin == null || growthRates == null)
            {
                Debug.LogWarning($"{nameof(MarketStall)}: needs an inventory, a coin and the tree's growth rates.", this);
                enabled = false;
                return;
            }

            inventory.SlotChanged += OnSlotChanged;
            growthRates.RateChanged += OnRateChanged;
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.SlotChanged -= OnSlotChanged;
            }

            if (growthRates != null)
            {
                growthRates.RateChanged -= OnRateChanged;
            }
        }

        /// <summary>The growth rate of the part an offer speeds up.</summary>
        public int CurrentRate(MarketOffer offer)
        {
            return growthRates.GetRate(offer.Part);
        }

        /// <summary>The growth rate that part would have after buying the offer.</summary>
        public int RateAfterBuying(MarketOffer offer)
        {
            return GrowthRates.Clamp(CurrentRate(offer) + offer.RateIncrease);
        }

        /// <summary>True once the part already grows at the top rate, so buying would do nothing.</summary>
        public bool IsMaxed(MarketOffer offer)
        {
            return CurrentRate(offer) >= GrowthRates.Max;
        }

        public bool CanAfford(MarketOffer offer)
        {
            return offer != null && Coins >= offer.Price;
        }

        public bool CanBuy(MarketOffer offer)
        {
            return enabled && offer != null && !IsMaxed(offer) && CanAfford(offer);
        }

        /// <summary>Takes the price in coins and raises the part's growth rate. Returns false if it couldn't be bought.</summary>
        public bool TryBuy(MarketOffer offer)
        {
            if (!CanBuy(offer) || !inventory.TryRemove(coin, offer.Price))
            {
                return false;
            }

            growthRates.SetRate(offer.Part, RateAfterBuying(offer));
            Bought?.Invoke(offer);
            Changed?.Invoke();
            return true;
        }

        private void OnSlotChanged(int slot)
        {
            Changed?.Invoke();
        }

        private void OnRateChanged(TreeGrowthPart part, int rate)
        {
            Changed?.Invoke();
        }
    }
}
