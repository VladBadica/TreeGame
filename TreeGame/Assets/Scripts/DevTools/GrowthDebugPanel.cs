using System;
using TMPro;
using TreeGame.Trees;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TreeGame.DevTools
{
    /// <summary>
    /// Development-only controls for driving <see cref="TreeGrowth"/> by hand: a slider per growth part, plus buttons to
    /// grow every part a step or reset them all. It only uses TreeGrowth's public API and event, so it can be removed
    /// without touching gameplay code.
    /// </summary>
    public class GrowthDebugPanel : MonoBehaviour
    {
        [SerializeField] private TreeGrowth treeGrowth;
        [Tooltip("One per growth part, in the order of TreeGrowth.Parts.")]
        [SerializeField] private Slider[] sliders = new Slider[0];
        [Tooltip("One per growth part, in the order of TreeGrowth.Parts.")]
        [SerializeField] private TMP_Text[] labels = new TMP_Text[0];
        [SerializeField] private Button addGrowthButton;
        [SerializeField] private Button resetButton;
        [SerializeField, Range(0.01f, 0.5f)] private float growthStep = 0.05f;

        // Kept so exactly these listeners can be removed again.
        private UnityAction<float>[] sliderHandlers;

        private void OnEnable()
        {
            if (treeGrowth == null || sliders.Length != TreeGrowth.Parts.Length || labels.Length != TreeGrowth.Parts.Length)
            {
                Debug.LogWarning($"{nameof(GrowthDebugPanel)}: needs a {nameof(TreeGrowth)}, and a slider and label per growth part.", this);
                enabled = false;
                return;
            }

            sliderHandlers = new UnityAction<float>[sliders.Length];
            for (int i = 0; i < sliders.Length; i++)
            {
                TreeGrowthPart part = TreeGrowth.Parts[i];
                sliders[i].minValue = treeGrowth.MinimumGrowth(part);
                sliders[i].maxValue = 1f;
                sliderHandlers[i] = value => treeGrowth.SetGrowth(part, value);
                sliders[i].onValueChanged.AddListener(sliderHandlers[i]);
            }

            if (addGrowthButton != null)
            {
                addGrowthButton.onClick.AddListener(AddGrowthStep);

                // Caption is derived from the step so the two can't drift apart.
                TMP_Text caption = addGrowthButton.GetComponentInChildren<TMP_Text>();
                if (caption != null)
                {
                    caption.text = $"Grow all +{growthStep * 100f:0}%";
                }
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ResetGrowth);
            }

            treeGrowth.GrowthChanged += Refresh;
            foreach (TreeGrowthPart part in TreeGrowth.Parts)
            {
                Refresh(part, treeGrowth.GetGrowth(part));
            }
        }

        private void OnDisable()
        {
            if (treeGrowth == null || sliderHandlers == null)
            {
                return;
            }

            for (int i = 0; i < sliders.Length; i++)
            {
                sliders[i].onValueChanged.RemoveListener(sliderHandlers[i]);
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

        private void AddGrowthStep()
        {
            foreach (TreeGrowthPart part in TreeGrowth.Parts)
            {
                treeGrowth.AddGrowth(part, growthStep);
            }
        }

        private void ResetGrowth()
        {
            foreach (TreeGrowthPart part in TreeGrowth.Parts)
            {
                treeGrowth.SetGrowth(part, 0f);
            }
        }

        private void Refresh(TreeGrowthPart part, float growth)
        {
            int index = Array.IndexOf(TreeGrowth.Parts, part);

            // Without notify, so growth changes from other sources don't echo back into SetGrowth.
            sliders[index].SetValueWithoutNotify(growth);
            labels[index].text = $"{part}: {growth * 100f:0}%";
        }
    }
}
