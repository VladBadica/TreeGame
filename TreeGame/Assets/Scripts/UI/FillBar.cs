using UnityEngine;

namespace TreeGame.UI
{
    /// <summary>
    /// A horizontal bar showing an amount. The fill is a child stretched by its anchors, so it works with any sprite
    /// (or none) and on any canvas, screen or world space.
    /// </summary>
    public class FillBar : MonoBehaviour
    {
        [SerializeField] private RectTransform fill;
        [Tooltip("Keeps the fill against the right edge, so it shrinks away from the left. Otherwise it fills from the left.")]
        [SerializeField] private bool anchoredRight;

        /// <summary>Sets how full the bar is, 0..1.</summary>
        public void SetFill(float amount)
        {
            if (fill == null)
            {
                return;
            }

            float clamped = Mathf.Clamp01(amount);
            fill.anchorMin = new Vector2(anchoredRight ? 1f - clamped : 0f, 0f);
            fill.anchorMax = new Vector2(anchoredRight ? 1f : clamped, 1f);
        }
    }
}
