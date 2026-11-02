using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.Menus
{
    /// <summary>
    /// Tabs within a menu screen: each button shows its page and hides the others. Starts on the first tab each time
    /// the screen opens, and tints the button of the tab showing so it's clear where the player is.
    /// </summary>
    public class MenuTabs : MonoBehaviour
    {
        [Tooltip("One button per page, in the same order.")]
        [SerializeField] private Button[] tabs = new Button[0];
        [SerializeField] private GameObject[] pages = new GameObject[0];
        [SerializeField] private Color shownTabColor = Color.white;
        [SerializeField] private Color otherTabColor = new Color(0.6f, 0.6f, 0.6f);

        private void OnEnable()
        {
            if (tabs.Length == 0 || tabs.Length != pages.Length)
            {
                Debug.LogWarning($"{nameof(MenuTabs)}: needs one button per page.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < tabs.Length; i++)
            {
                int tabIndex = i; // Captured per tab; the loop variable itself would be shared.
                tabs[i].onClick.AddListener(() => Show(tabIndex));
            }

            Show(0);
        }

        private void OnDisable()
        {
            foreach (Button tab in tabs)
            {
                tab.onClick.RemoveAllListeners();
            }
        }

        private void Show(int tabIndex)
        {
            for (int i = 0; i < tabs.Length; i++)
            {
                pages[i].SetActive(i == tabIndex);
                tabs[i].image.color = i == tabIndex ? shownTabColor : otherTabColor;
            }
        }
    }
}
