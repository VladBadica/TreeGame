using TMPro;
using TreeGame.Care;
using UnityEngine;

namespace TreeGame.UI
{
    /// <summary>
    /// Shows a penalty's badge while it is active, e.g. overwatering: a down arrow with the seconds left (its tooltip says
    /// what it means). Hidden otherwise. Only rewrites the text when the whole seconds left change.
    /// </summary>
    public class OverflowPenaltyStatus : MonoBehaviour
    {
        [SerializeField] private OverflowPenalty penalty;
        [Tooltip("Shown only while the penalty is active.")]
        [SerializeField] private GameObject badge;
        [SerializeField] private TMP_Text label;

        private int shownSeconds = -1;

        private void OnEnable()
        {
            if (penalty == null || badge == null || label == null)
            {
                Debug.LogWarning($"{nameof(OverflowPenaltyStatus)}: needs the penalty, a badge and a label.", this);
                enabled = false;
                return;
            }

            penalty.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            if (penalty != null)
            {
                penalty.Changed -= Refresh;
            }
        }

        private void Update()
        {
            if (penalty.IsActive)
            {
                ShowSeconds();
            }
        }

        private void Refresh()
        {
            badge.SetActive(penalty.IsActive);
            shownSeconds = -1;
            if (penalty.IsActive)
            {
                ShowSeconds();
            }
        }

        private void ShowSeconds()
        {
            int seconds = Mathf.CeilToInt(penalty.SecondsLeft);
            if (seconds == shownSeconds)
            {
                return;
            }

            shownSeconds = seconds;
            label.text = $"{seconds}s";
        }
    }
}
