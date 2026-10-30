using UnityEngine;

namespace TreeGame.UI
{
    /// <summary>
    /// A horizontal bar filled from the left. The fill is a child stretched by its right anchor,
    /// so it works with any sprite (or none) and on any canvas, screen or world space.
    /// </summary>
    public class FillBar : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;

        /// <summary>Sets how full the bar is, 0..1.</summary>
        public void SetFill(float amount)
        {
            if (fill == null)
            {
                return;
            }

            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
        }
    }
}
