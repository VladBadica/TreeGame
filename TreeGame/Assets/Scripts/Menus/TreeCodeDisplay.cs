using TMPro;
using TreeGame.Trees;
using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.Menus
{
    /// <summary>
    /// Shows the code for the seed this game's tree grows from, and copies it for sharing. Shown on a menu screen, so it
    /// reads the code each time the screen opens; the seed can't change while a menu is open.
    /// </summary>
    public class TreeCodeDisplay : MonoBehaviour
    {
        [SerializeField] private TreeSeed treeSeed;
        [SerializeField] private TMP_Text codeLabel;
        [SerializeField] private Button copyButton;
        [SerializeField] private TMP_Text copyButtonLabel;
        [SerializeField] private string copyCaption = "Copy Tree Code";
        [SerializeField] private string copiedCaption = "Copied!";

        private void OnEnable()
        {
            if (treeSeed == null || codeLabel == null || copyButton == null || copyButtonLabel == null)
            {
                Debug.LogWarning($"{nameof(TreeCodeDisplay)}: needs a tree seed, a code label and a copy button with its label.", this);
                enabled = false;
                return;
            }

            codeLabel.text = $"Tree code: {treeSeed.Code}";
            copyButtonLabel.text = copyCaption;
            copyButton.onClick.AddListener(Copy);
        }

        private void OnDisable()
        {
            if (copyButton != null)
            {
                copyButton.onClick.RemoveListener(Copy);
            }
        }

        private void Copy()
        {
            GUIUtility.systemCopyBuffer = treeSeed.Code;

            // Stays until the screen next opens, as the game is paused and time stands still.
            copyButtonLabel.text = copiedCaption;
        }
    }
}
