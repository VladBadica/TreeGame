using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.Menus
{
    /// <summary>
    /// One save slot on the main menu: a button showing the slot's name and a line about its tree, highlighted while
    /// it is the chosen slot. It only shows what it is told; <see cref="SaveSlotMenu"/> decides what that is.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class SaveSlotCard : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image background;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.82f);
        [SerializeField] private Color chosenColor = new Color(0.82f, 0.95f, 0.74f, 1f);

        public Button Button => GetComponent<Button>();

        /// <summary>The slot's name in bold, with a smaller line about its tree under it.</summary>
        public void Show(string title, string detail)
        {
            if (label != null)
            {
                label.text = $"<b>{title}</b>\n<size=75%>{detail}</size>";
            }
        }

        public void SetChosen(bool chosen)
        {
            if (background != null)
            {
                background.color = chosen ? chosenColor : normalColor;
            }
        }
    }
}
