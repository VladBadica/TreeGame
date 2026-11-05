using TMPro;
using TreeGame.Trees;
using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.Menus
{
    /// <summary>
    /// Shows the code for the seed this game's tree grows from, with a button beside it that copies the code for
    /// sharing; its icon turns into a tick once copied. Shown on a menu screen, so it reads the code each time the
    /// screen opens; the seed can't change while a menu is open.
    /// </summary>
    public class TreeCodeDisplay : MonoBehaviour
    {
        [SerializeField] private TreeSeed treeSeed;
        [SerializeField] private TMP_Text codeLabel;
        [SerializeField] private Button copyButton;
        [SerializeField] private Image copyIcon;
        [SerializeField] private Sprite copySprite;
        [Tooltip("Shown on the copy button once the code is copied.")]
        [SerializeField] private Sprite copiedSprite;

        private void OnEnable()
        {
            if (treeSeed == null || codeLabel == null || copyButton == null || copyIcon == null || copySprite == null ||
                copiedSprite == null)
            {
                Debug.LogWarning($"{nameof(TreeCodeDisplay)}: needs a tree seed, a code label, and a copy button with its icon and both sprites.", this);
                enabled = false;
                return;
            }

            codeLabel.text = $"Tree code: {treeSeed.Code}";
            copyIcon.sprite = copySprite;
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
            copyIcon.sprite = copiedSprite;
        }
    }
}
