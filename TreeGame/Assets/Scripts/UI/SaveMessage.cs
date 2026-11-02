using TMPro;
using TreeGame.Saving;
using UnityEngine;

namespace TreeGame.UI
{
    /// <summary>Shows for a moment whether the game just saved.</summary>
    public class SaveMessage : MonoBehaviour
    {
        [SerializeField] private GameSaver saver;
        [Tooltip("The message's panel, on a child object, so this component keeps running while it is hidden.")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text label;
        [SerializeField, Min(0f)] private float secondsShown = 2f;
        [SerializeField] private string savedText = "Game saved";
        [SerializeField] private string failedText = "Couldn't save the game";

        private float hideTime;

        private void OnEnable()
        {
            if (saver == null || panel == null || label == null)
            {
                Debug.LogWarning($"{nameof(SaveMessage)}: needs a saver, a panel and a label.", this);
                enabled = false;
                return;
            }

            saver.Saved += Show;
        }

        private void OnDisable()
        {
            if (saver != null)
            {
                saver.Saved -= Show;
            }
        }

        private void Update()
        {
            if (panel.activeSelf && Time.unscaledTime >= hideTime)
            {
                panel.SetActive(false);
            }
        }

        private void Show(bool succeeded)
        {
            label.text = succeeded ? savedText : failedText;
            panel.SetActive(true);
            hideTime = Time.unscaledTime + secondsShown;
        }
    }
}
