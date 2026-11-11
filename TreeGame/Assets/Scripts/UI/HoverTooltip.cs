using UnityEngine;
using UnityEngine.EventSystems;

namespace TreeGame.UI
{
    /// <summary>
    /// Shows a tooltip while the pointer is over this UI element, and hides it otherwise. The element needs a graphic
    /// that takes raycasts (e.g. an Image) to notice the pointer.
    /// </summary>
    public class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private GameObject tooltip;

        private void OnEnable()
        {
            if (tooltip == null)
            {
                Debug.LogWarning($"{nameof(HoverTooltip)}: no tooltip assigned.", this);
                enabled = false;
                return;
            }

            tooltip.SetActive(false);
        }

        // Hidden when the element goes, too, so the tooltip isn't left showing when it comes back.
        private void OnDisable()
        {
            if (tooltip != null)
            {
                tooltip.SetActive(false);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (enabled)
            {
                tooltip.SetActive(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltip.SetActive(false);
        }
    }
}
