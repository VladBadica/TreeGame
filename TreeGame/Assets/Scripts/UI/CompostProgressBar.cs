using TreeGame.World;
using UnityEngine;

namespace TreeGame.UI
{
    /// <summary>
    /// A progress bar for the <see cref="Composter"/>: visible while it is working (filling up) and when it is done
    /// (full), hidden when idle. Visible regardless of where the player is. Only updates every frame while working.
    /// </summary>
    public class CompostProgressBar : MonoBehaviour
    {
        [SerializeField] private Composter composter;
        [SerializeField] private FillBar bar;
        [Tooltip("What is shown and hidden, e.g. the bar's canvas.")]
        [SerializeField] private GameObject barRoot;

        private void OnEnable()
        {
            if (composter == null || bar == null || barRoot == null)
            {
                Debug.LogWarning($"{nameof(CompostProgressBar)}: needs a composter, a bar and the bar's root object.", this);
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
        }
    }
}
