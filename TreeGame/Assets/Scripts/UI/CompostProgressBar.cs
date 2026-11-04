using TreeGame.World;
using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.UI
{
    /// <summary>
    /// A progress bar for the <see cref="Composter"/>: visible while it is working (filling up, in one color) and when it
    /// is done (full, in another), hidden when idle. Visible regardless of where the player is. Only updates every frame
    /// while working.
    /// </summary>
    public class CompostProgressBar : MonoBehaviour
    {
        [SerializeField] private Composter composter;
        [SerializeField] private FillBar bar;
        [Tooltip("The bar's fill, which changes color when the compost is ready.")]
        [SerializeField] private Image fill;
        [Tooltip("What is shown and hidden, e.g. the bar's canvas.")]
        [SerializeField] private GameObject barRoot;
        [SerializeField] private Color workingColor = new Color(0.95f, 0.8f, 0.25f);
        [SerializeField] private Color doneColor = new Color(0.45f, 0.78f, 0.32f);

        private void OnEnable()
        {
            if (composter == null || bar == null || fill == null || barRoot == null)
            {
                Debug.LogWarning($"{nameof(CompostProgressBar)}: needs a composter, a bar, its fill and the bar's root object.", this);
                enabled = false;
                return;
            }

            composter.StateChanged += OnStateChanged;
            OnStateChanged(composter.State);
        }

        private void OnDisable()
        {
            if (composter != null)
            {
                composter.StateChanged -= OnStateChanged;
            }
        }

        private void Update()
        {
            if (composter.State == CompostState.Working)
            {
                bar.SetFill(composter.Progress);
            }
        }

        private void OnStateChanged(CompostState state)
        {
            barRoot.SetActive(state != CompostState.Idle);
            bar.SetFill(composter.Progress);
            fill.color = state == CompostState.Done ? doneColor : workingColor;
        }
    }
}
