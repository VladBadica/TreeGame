using TMPro;
using UnityEngine;

namespace TreeGame.UI
{
    /// <summary>
    /// A small text popup on a screen-space canvas that sits above a point in the world and follows it on screen.
    /// It only displays; prompt components decide what it says and when it shows.
    /// </summary>
    public class WorldPopup : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [Tooltip("The panel that is shown and hidden; its pivot is placed at the target point.")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text label;

        private Transform target;
        private Vector3 offset;

        // Not hiding here: a prompt may already have shown the popup before this Awake runs.
        // The panel is saved inactive instead.
        private void Awake()
        {
            if (viewCamera == null || panel == null || label == null)
            {
                Debug.LogWarning($"{nameof(WorldPopup)}: needs a camera, panel and label.", this);
                enabled = false;
            }
        }

        // Only does work while the popup is visible.
        private void LateUpdate()
        {
            if (target != null)
            {
                FollowTarget();
            }
            else if (panel.gameObject.activeSelf)
            {
                Hide(); // The target was destroyed while shown, e.g. a collected leaf.
            }
        }

        /// <summary>Shows the text above a target, offset in world units (e.g. up from a leaf on the ground).</summary>
        public void Show(Transform newTarget, string text, Vector3 worldOffset = default)
        {
            if (viewCamera == null || newTarget == null)
            {
                Hide();
                return;
            }

            target = newTarget;
            offset = worldOffset;
            label.text = text;
            panel.gameObject.SetActive(true);
            FollowTarget();
        }

        public void Hide()
        {
            target = null;
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }
        }

        private void FollowTarget()
        {
            // On a screen-space overlay canvas, positions are in screen pixels.
            panel.position = viewCamera.WorldToScreenPoint(target.position + offset);
        }
    }
}
