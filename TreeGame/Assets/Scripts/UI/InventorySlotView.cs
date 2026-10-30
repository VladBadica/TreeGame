using System.Collections;
using TMPro;
using TreeGame.Items;
using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.UI
{
    /// <summary>Displays one inventory slot: its key number, the item's icon, and a short flash when used.</summary>
    public class InventorySlotView : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text keyLabel;
        [Tooltip("Background color at the start of the flash when the slot is used; it fades back to normal.")]
        [SerializeField] private Color activatedColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField, Min(0.01f)] private float flashDuration = 0.2f;

        private Color normalColor;
        private Coroutine flash;

        private void Awake()
        {
            // The resting color is whatever the background was styled with, so it's defined in one place.
            normalColor = background.color;
        }

        private void OnDisable()
        {
            // A disabled object stops its coroutines; make sure the slot doesn't stay mid-flash.
            flash = null;
            background.color = normalColor;
        }

        public void SetKeyLabel(string text)
        {
            keyLabel.text = text;
        }

        public void ShowItem(ItemDefinition item)
        {
            icon.sprite = item != null ? item.Icon : null;
            icon.enabled = icon.sprite != null;
        }

        public void PlayActivated()
        {
            if (flash != null)
            {
                StopCoroutine(flash);
            }

            flash = StartCoroutine(Flash());
        }

        private IEnumerator Flash()
        {
            // Unscaled time, so feedback still plays if the game is paused.
            for (float elapsed = 0f; elapsed < flashDuration; elapsed += Time.unscaledDeltaTime)
            {
                background.color = Color.Lerp(activatedColor, normalColor, elapsed / flashDuration);
                yield return null;
            }

            background.color = normalColor;
            flash = null;
        }
    }
}
