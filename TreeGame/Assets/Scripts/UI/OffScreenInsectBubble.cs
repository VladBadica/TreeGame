using TMPro;
using TreeGame.Pests;
using TreeGame.Trees;
using UnityEngine;

namespace TreeGame.UI
{
    /// <summary>
    /// Tells the player about insects on the tree while the tree is out of view: a bubble at the edge of the screen,
    /// on the side the tree is on, showing how many there are if more than one. It disappears once the tree is back
    /// in view. It is only a notice, so it takes no clicks; the player still has to walk back.
    /// </summary>
    public class OffScreenInsectBubble : MonoBehaviour
    {
        [SerializeField] private InsectInfestation infestation;
        [SerializeField] private TreeVisuals tree;
        [SerializeField] private Camera viewCamera;
        [Tooltip("The bubble, on a child object, so this component keeps running while it is hidden.")]
        [SerializeField] private RectTransform bubble;
        [SerializeField] private TMP_Text countLabel;
        [Tooltip("Distance from the edge of the screen.")]
        [SerializeField, Min(0f)] private float edgeInset = 24f;
        [Tooltip("How far above the middle of the screen the bubble sits.")]
        [SerializeField] private float heightAboveMiddle = 60f;
        [Tooltip("Share of the screen's width at each edge that doesn't count as seeing the tree, because insects on a " +
                 "trunk that close to the edge would be cut off.")]
        [SerializeField, Range(0f, 0.2f)] private float edgeMargin = 0.03f;

        private void OnEnable()
        {
            if (infestation == null || tree == null || viewCamera == null || bubble == null || countLabel == null)
            {
                Debug.LogWarning($"{nameof(OffScreenInsectBubble)}: needs an infestation, tree visuals, a camera, a bubble and a count label.", this);
                enabled = false;
                return;
            }

            infestation.CountChanged += ShowCount;
            ShowCount(infestation.Count);
        }

        private void OnDisable()
        {
            if (infestation != null)
            {
                infestation.CountChanged -= ShowCount;
            }
        }

        // After Update, because the camera moves during the frame too.
        private void LateUpdate()
        {
            if (infestation.Count == 0)
            {
                Hide();
                return;
            }

            float treeOnScreen = viewCamera.WorldToViewportPoint(tree.GetTrunkPoint(0.5f, 0f)).x;
            if (treeOnScreen < edgeMargin)
            {
                ShowOnSide(isLeft: true);
            }
            else if (treeOnScreen > 1f - edgeMargin)
            {
                ShowOnSide(isLeft: false);
            }
            else
            {
                Hide();
            }
        }

        private void ShowCount(int count)
        {
            countLabel.enabled = count > 1;
            countLabel.text = $"x{count}";
        }

        private void ShowOnSide(bool isLeft)
        {
            var anchor = new Vector2(isLeft ? 0f : 1f, 0.5f);

            // Only when it changes, so the UI isn't rebuilt every frame.
            if (bubble.anchorMin != anchor)
            {
                bubble.anchorMin = bubble.anchorMax = bubble.pivot = anchor;
                bubble.anchoredPosition = new Vector2(isLeft ? edgeInset : -edgeInset, heightAboveMiddle);
            }

            if (!bubble.gameObject.activeSelf)
            {
                bubble.gameObject.SetActive(true);
            }
        }

        private void Hide()
        {
            if (bubble.gameObject.activeSelf)
            {
                bubble.gameObject.SetActive(false);
            }
        }
    }
}
