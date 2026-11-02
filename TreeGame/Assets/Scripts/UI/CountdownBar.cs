using TMPro;
using UnityEngine;

namespace TreeGame.UI
{
    /// <summary>
    /// Shows the time left as a bar and a number. It moves in whole seconds, so the two always agree, and only touches
    /// the UI when the number changes.
    /// </summary>
    public class CountdownBar : MonoBehaviour
    {
        [SerializeField] private FillBar bar;
        [SerializeField] private TMP_Text label;

        private int shownSeconds = -1;

        public void Show(float secondsLeft, float totalSeconds)
        {
            int seconds = Mathf.CeilToInt(secondsLeft);
            if (seconds == shownSeconds)
            {
                return;
            }

            shownSeconds = seconds;
            label.text = $"{seconds}s";
            bar.SetFill(seconds / totalSeconds);
        }
    }
}
