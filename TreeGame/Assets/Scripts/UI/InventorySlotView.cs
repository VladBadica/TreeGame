using TMPro;
using TreeGame.Items;
using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.UI
{
    /// <summary>
    /// Displays one inventory slot: its key number, the item's icon, the item's display text if it has any
    /// (e.g. a bucket's "40%"), and a short flash when used.
    /// </summary>
    public class InventorySlotView : MonoBehaviour
    {
        [SerializeField] private ImageFlash backgroundFlash;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text keyLabel;
        [Tooltip("Shows the item's display text; hidden when the item has none.")]
        [SerializeField] private TMP_Text displayTextLabel;
        [Tooltip("Background color at the start of the flash when the slot is used; it fades back to normal.")]
        [SerializeField] private Color activatedColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField, Min(0.01f)] private float flashDuration = 0.2f;

        public void SetKeyLabel(string text)
        {
            keyLabel.text = text;
        }

        public void ShowItem(Item item)
        {
            icon.sprite = item != null ? item.Icon : null;
            icon.enabled = icon.sprite != null;

            string displayText = item != null ? item.DisplayText : null;
            displayTextLabel.enabled = !string.IsNullOrEmpty(displayText);
            displayTextLabel.text = displayText ?? "";
        }

        public void PlayActivated()
        {
            backgroundFlash.Play(activatedColor, flashDuration);
        }
    }
}
