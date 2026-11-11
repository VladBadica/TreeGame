using TMPro;
using TreeGame.Seasons;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TreeGame.DevTools
{
    /// <summary>
    /// Development-only controls for the seasons: a slider through the current year and a button to skip to the next
    /// season. Only uses <see cref="SeasonClock"/>'s public API, so it can be removed without touching gameplay code.
    /// </summary>
    public class SeasonDebugPanel : MonoBehaviour
    {
        [SerializeField] private SeasonClock clock;
        [SerializeField] private TMP_Text label;
        [Tooltip("Where in the current year it is, from the start of spring to the end of winter.")]
        [SerializeField] private Slider yearSlider;
        [SerializeField] private Button nextSeasonButton;

        private UnityAction<float> sliderHandler;
        private string shownText;

        private void OnEnable()
        {
            if (clock == null || label == null || yearSlider == null || nextSeasonButton == null)
            {
                Debug.LogWarning($"{nameof(SeasonDebugPanel)}: needs a clock, a label, a slider and a button.", this);
                enabled = false;
                return;
            }

            yearSlider.minValue = 0f;
            yearSlider.maxValue = 1f;
            sliderHandler = MoveWithinYear;
            yearSlider.onValueChanged.AddListener(sliderHandler);
            nextSeasonButton.onClick.AddListener(clock.SkipToNextSeason);
        }

        private void OnDisable()
        {
            if (yearSlider != null && sliderHandler != null)
            {
                yearSlider.onValueChanged.RemoveListener(sliderHandler);
            }

            if (nextSeasonButton != null && clock != null)
            {
                nextSeasonButton.onClick.RemoveListener(clock.SkipToNextSeason);
            }
        }

        private void Update()
        {
            // Without notify, so the clock moving on doesn't echo back into it.
            yearSlider.SetValueWithoutNotify(clock.YearShare);

            string text = $"Season: {SeasonRules.Describe(clock.TotalSeconds, clock.SecondsPerSeason)}";
            if (text != shownText)
            {
                shownText = text;
                label.text = text;
            }
        }

        /// <summary>Moves to a point in the current year, quietly, as if the game had been loaded there.</summary>
        private void MoveWithinYear(float share)
        {
            float yearLength = clock.SecondsPerSeason * SeasonRules.SeasonsPerYear;
            float startOfYear = (clock.Year - 1) * yearLength;

            // Just short of the end, so the far right of the slider is still this year's winter.
            clock.SetTotalSeconds(startOfYear + Mathf.Min(share, 0.9999f) * yearLength);
        }
    }
}
