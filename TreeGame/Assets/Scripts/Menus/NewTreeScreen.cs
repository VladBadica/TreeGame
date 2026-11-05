using TMPro;
using TreeGame.Saving;
using TreeGame.Trees;
using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.Menus
{
    /// <summary>
    /// The New Tree screen: an optional name for the tree, and grows a random tree or the tree of a code typed or
    /// pasted in. Says as the code is typed
    /// whether it can grow, and only lets Grow be pressed when it can. The Grow button loads the game itself (a
    /// <see cref="LoadSceneButton"/>); this only hands the code's seed to the new game.
    /// </summary>
    public class NewTreeScreen : MonoBehaviour
    {
        [SerializeField] private TMP_InputField nameField;
        [SerializeField] private TMP_InputField codeField;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button growButton;

        [Header("Messages")]
        [SerializeField] private string randomMessage = "A random tree will grow.";
        [SerializeField] private string validMessage = "Valid code: this tree will grow.";
        [SerializeField] private string incompleteMessage = "A code has 8 characters.";
        [SerializeField] private string typoMessage = "That code has a typo.";
        [SerializeField] private string olderVersionMessage = "That code is from an older version of the game.";
        [SerializeField] private string newerVersionMessage = "That code is from a newer version of the game.";

        private TreeCodeStatus status;
        private int codeSeed;

        private void OnEnable()
        {
            if (nameField == null || codeField == null || statusLabel == null || growButton == null)
            {
                Debug.LogWarning($"{nameof(NewTreeScreen)}: needs a name field, a code field, a status label and a grow button.", this);
                enabled = false;
                return;
            }

            // Every visit starts empty, so a name or code from last time can't end up on a tree by surprise.
            nameField.text = "";
            codeField.text = "";
            NewTreeChoices.Seed.Clear();
            NewTreeChoices.Name.Clear();

            // The hint is what the tree is called if left unnamed: its slot's name.
            if (nameField.placeholder is TMP_Text hint)
            {
                hint.text = $"Tree {CurrentSaveSlot.Index + 1}";
            }

            codeField.onValueChanged.AddListener(OnCodeChanged);
            codeField.onSubmit.AddListener(OnCodeSubmitted);
            growButton.onClick.AddListener(OnGrow);
            OnCodeChanged(codeField.text);
        }

        private void OnDisable()
        {
            if (codeField != null)
            {
                codeField.onValueChanged.RemoveListener(OnCodeChanged);
                codeField.onSubmit.RemoveListener(OnCodeSubmitted);
            }

            if (growButton != null)
            {
                growButton.onClick.RemoveListener(OnGrow);
            }
        }

        private void OnCodeChanged(string typed)
        {
            status = TreeCodeEntry.Read(typed, TreeSeed.GenerationVersion, out codeSeed);
            statusLabel.text = MessageFor(status);
            growButton.interactable = TreeCodeEntry.CanGrow(status);
        }

        /// <summary>
        /// Enter in the field grows the code's tree. Not with the field empty: the field is selected when the screen
        /// opens, and a stray Enter shouldn't start a new game over a save.
        /// </summary>
        private void OnCodeSubmitted(string typed)
        {
            if (status == TreeCodeStatus.Valid && growButton.interactable)
            {
                growButton.onClick.Invoke();
            }
        }

        private void OnGrow()
        {
            // The game loads in the background and starts on a later frame, so the seed is in place in time whichever
            // of the Grow button's listeners runs first.
            if (status == TreeCodeStatus.Valid)
            {
                NewTreeChoices.Seed.Request(codeSeed);
            }

            string treeName = TreeNames.Clean(nameField.text);
            if (treeName.Length > 0)
            {
                NewTreeChoices.Name.Request(treeName);
            }
        }

        private string MessageFor(TreeCodeStatus codeStatus)
        {
            return codeStatus switch
            {
                TreeCodeStatus.Empty => randomMessage,
                TreeCodeStatus.Valid => validMessage,
                TreeCodeStatus.Incomplete => incompleteMessage,
                TreeCodeStatus.Typo => typoMessage,
                TreeCodeStatus.OlderVersion => olderVersionMessage,
                TreeCodeStatus.NewerVersion => newerVersionMessage,
                _ => "",
            };
        }
    }
}
