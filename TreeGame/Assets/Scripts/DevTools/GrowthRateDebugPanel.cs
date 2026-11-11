using System;
using TMPro;
using TreeGame.Trees;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TreeGame.DevTools
{
    /// <summary>
    /// Development-only controls for each part's growth rate, with − and + buttons, showing the rate and the extra
    /// growth it has earned, a slider to set that extra growth directly, and a button to clear it all. Until something in the game changes rates, this is how
    /// to try extra growth. It only uses the public APIs and events of the rates and extra growth, so it can be removed
    /// without touching gameplay code.
    /// </summary>
    public class GrowthRateDebugPanel : MonoBehaviour
    {
        [SerializeField] private TreeGrowthRates growthRates;
        [SerializeField] private TreeExtraGrowth extraGrowth;
        [Tooltip("One per growth part, in the order of TreeGrowth.Parts.")]
        [SerializeField] private TMP_Text[] labels = new TMP_Text[0];
        [Tooltip("One per growth part, in the order of TreeGrowth.Parts.")]
        [SerializeField] private Button[] slowerButtons = new Button[0];
        [Tooltip("One per growth part, in the order of TreeGrowth.Parts.")]
        [SerializeField] private Button[] fasterButtons = new Button[0];
        [Tooltip("One per growth part, in the order of TreeGrowth.Parts: sets its extra growth directly, for testing.")]
        [SerializeField] private Slider[] extraSliders = new Slider[0];
        [SerializeField] private Button clearExtraButton;

        // Kept so exactly these listeners can be removed again.
        private UnityAction[] slowerHandlers = new UnityAction[0];
        private UnityAction[] fasterHandlers = new UnityAction[0];
        private UnityAction<float>[] extraHandlers = new UnityAction<float>[0];

        private void OnEnable()
        {
            int parts = TreeGrowth.Parts.Length;
            if (growthRates == null || extraGrowth == null || labels.Length != parts || slowerButtons.Length != parts ||
                fasterButtons.Length != parts || extraSliders.Length != parts || clearExtraButton == null)
            {
                Debug.LogWarning($"{nameof(GrowthRateDebugPanel)}: needs the rates, extra growth, and a label, both buttons and a slider per growth part.", this);
                enabled = false;
                return;
            }

            slowerHandlers = new UnityAction[parts];
            fasterHandlers = new UnityAction[parts];
            extraHandlers = new UnityAction<float>[parts];
            for (int i = 0; i < parts; i++)
            {
                TreeGrowthPart part = TreeGrowth.Parts[i];
                slowerHandlers[i] = () => growthRates.SetRate(part, growthRates.GetRate(part) - 1);
                fasterHandlers[i] = () => growthRates.SetRate(part, growthRates.GetRate(part) + 1);
                extraHandlers[i] = value => extraGrowth.SetExtraGrowth(part, value);
                slowerButtons[i].onClick.AddListener(slowerHandlers[i]);
                fasterButtons[i].onClick.AddListener(fasterHandlers[i]);
                extraSliders[i].minValue = 0f;
                extraSliders[i].maxValue = 1f;
                extraSliders[i].onValueChanged.AddListener(extraHandlers[i]);
            }

            clearExtraButton.onClick.AddListener(ClearExtraGrowth);
            growthRates.RateChanged += OnRateChanged;
            growthRates.TemporaryChangeChanged += RefreshAll;
            extraGrowth.ExtraGrowthChanged += OnExtraGrowthChanged;
            RefreshAll();
        }

        private void OnDisable()
        {
            for (int i = 0; i < slowerHandlers.Length; i++)
            {
                slowerButtons[i].onClick.RemoveListener(slowerHandlers[i]);
                fasterButtons[i].onClick.RemoveListener(fasterHandlers[i]);
                extraSliders[i].onValueChanged.RemoveListener(extraHandlers[i]);
            }

            if (clearExtraButton != null)
            {
                clearExtraButton.onClick.RemoveListener(ClearExtraGrowth);
            }

            if (growthRates != null)
            {
                growthRates.RateChanged -= OnRateChanged;
                growthRates.TemporaryChangeChanged -= RefreshAll;
            }

            if (extraGrowth != null)
            {
                extraGrowth.ExtraGrowthChanged -= OnExtraGrowthChanged;
            }
        }

        private void OnRateChanged(TreeGrowthPart part, int rate)
        {
            Refresh(part);
        }

        private void OnExtraGrowthChanged(TreeGrowthPart part, float extra)
        {
            Refresh(part);
        }

        private void ClearExtraGrowth()
        {
            foreach (TreeGrowthPart part in TreeGrowth.Parts)
            {
                extraGrowth.SetExtraGrowth(part, 0f);
            }
        }

        private void RefreshAll()
        {
            foreach (TreeGrowthPart part in TreeGrowth.Parts)
            {
                Refresh(part);
            }
        }

        private void Refresh(TreeGrowthPart part)
        {
            int index = Array.IndexOf(TreeGrowth.Parts, part);
            int rate = growthRates.GetRate(part);
            int effectiveRate = growthRates.GetEffectiveRate(part);
            float extra = extraGrowth.GetExtraGrowth(part);
            string filling = effectiveRate > GrowthRates.ExtraGrowthAbove ? " (filling)" : "";
            // While something temporary (e.g. overwatering) changes it, the rate the part actually grows at shows too.
            string rateText = effectiveRate == rate ? $"{rate}" : $"{rate} (now {effectiveRate})";
            labels[index].text = $"{part} rate {rateText} · extra {extra * 100f:0}%{filling}";

            // Without notify, so extra growth changes from other sources don't echo back into it.
            extraSliders[index].SetValueWithoutNotify(extra);
            slowerButtons[index].interactable = rate > GrowthRates.Min;
            fasterButtons[index].interactable = rate < GrowthRates.Max;
        }
    }
}
