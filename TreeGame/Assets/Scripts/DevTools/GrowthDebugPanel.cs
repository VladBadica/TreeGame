using TreeGame.Trees;
using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.DevTools
{
    /// <summary>
    /// Development-only controls for driving <see cref="TreeGrowth"/> by hand.
    /// It only uses TreeGrowth's public API and event, so it can be removed without touching gameplay code.
    /// </summary>
    public class GrowthDebugPanel : MonoBehaviour
    {
        [SerializeField] private TreeGrowth treeGrowth;
        [SerializeField] private Slider growthSlider;
        [SerializeField] private Button addGrowthButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Text growthLabel;
        [SerializeField, Range(0.01f, 0.5f)] private float growthStep = 0.05f;

        private void OnEnable()
        {
            if (treeGrowth == null)
            {
                Debug.LogWarning($"{nameof(GrowthDebugPanel)}: no {nameof(TreeGrowth)} assigned.", this);
                return;
            }

            if (growthSlider != null)
            {
                growthSlider.minValue = 0f;
                growthSlider.maxValue = 1f;
                growthSlider.onValueChanged.AddListener(treeGrowth.SetGrowth);
            }

            if (addGrowthButton != null)
            {
                addGrowthButton.onClick.AddListener(AddGrowthStep);

                // Caption is derived from the step so the two can't drift apart.
                Text caption = addGrowthButton.GetComponentInChildren<Text>();
                if (caption != null)
                {
                    caption.text = $"Grow +{growthStep * 100f:0}%";
                }
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ResetGrowth);
            }

            treeGrowth.GrowthChanged += Refresh;
            Refresh(treeGrowth.Growth);
        }

        private void OnDisable()
        {
            if (treeGrowth == null)
            {
                return;
            }

            if (growthSlider != null)
            {
                growthSlider.onValueChanged.RemoveListener(treeGrowth.SetGrowth);
            }

            if (addGrowthButton != null)
            {
                addGrowthButton.onClick.RemoveListener(AddGrowthStep);
            }

            if (resetButton != null)
            {
                resetButton.onClick.RemoveListener(ResetGrowth);
            }

            treeGrowth.GrowthChanged -= Refresh;
        }

        private void AddGrowthStep() => treeGrowth.AddGrowth(growthStep);

        private void ResetGrowth() => treeGrowth.SetGrowth(0f);

        private void Refresh(float growth)
        {
            // Without notify, so growth changes from other sources don't echo back into SetGrowth.
            if (growthSlider != null)
            {
                growthSlider.SetValueWithoutNotify(growth);
            }

            if (growthLabel != null)
            {
                growthLabel.text = $"Growth: {growth * 100f:0}%";
            }
        }
    }
}
