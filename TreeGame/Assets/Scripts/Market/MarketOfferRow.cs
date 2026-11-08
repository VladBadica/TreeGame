using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.Market
{
    /// <summary>
    /// One offer in the market menu: its picture, name and what it does, the growth rate before and after, the price,
    /// and a Buy button. Keeps itself up to date while it shows, and asks the stall to buy when the button is pressed.
    /// </summary>
    public class MarketOfferRow : MonoBehaviour
    {
        [SerializeField] private MarketStall stall;
        [SerializeField] private MarketOffer offer;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text rateLabel;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyCaption;
        [Tooltip("The price's color when the player has too few coins.")]
        [SerializeField] private Color tooExpensiveColor = new Color(1f, 0.6f, 0.55f);

        private Color priceColor;

        private void Awake()
        {
            if (priceLabel != null)
            {
                priceColor = priceLabel.color;
            }
        }

        private void OnEnable()
        {
            if (stall == null || offer == null || icon == null || nameLabel == null || descriptionLabel == null ||
                rateLabel == null || priceLabel == null || buyButton == null || buyCaption == null)
            {
                Debug.LogWarning($"{nameof(MarketOfferRow)}: needs a stall, an offer and all of its labels.", this);
                enabled = false;
                return;
            }

            stall.Changed += Refresh;
            buyButton.onClick.AddListener(Buy);
            Refresh();
        }

        private void OnDisable()
        {
            if (stall != null)
            {
                stall.Changed -= Refresh;
            }

            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(Buy);
            }
        }

        private void Buy()
        {
            stall.TryBuy(offer);
        }

        private void Refresh()
        {
            icon.sprite = offer.Icon;
            nameLabel.text = offer.DisplayName;
            descriptionLabel.text = offer.Description;

            bool maxed = stall.IsMaxed(offer);
            int rate = stall.CurrentRate(offer);
            rateLabel.text = maxed
                ? $"Growth rate {rate}, the highest it goes"
                : $"Growth rate {rate}, then {stall.RateAfterBuying(offer)}";

            priceLabel.text = offer.Price == 1 ? "1 coin" : $"{offer.Price} coins";
            priceLabel.color = stall.CanAfford(offer) ? priceColor : tooExpensiveColor;

            buyButton.interactable = stall.CanBuy(offer);
            buyCaption.text = maxed ? "Max" : "Buy";
        }
    }
}
