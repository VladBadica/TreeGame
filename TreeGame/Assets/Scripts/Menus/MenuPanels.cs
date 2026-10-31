using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TreeGame.Menus
{
    /// <summary>
    /// The screens of a menu, of which at most one shows at a time (e.g. main, settings, credits). Selects the first
    /// control of whichever one opens, so the menu can also be used with a keyboard or gamepad.
    /// </summary>
    public class MenuPanels : MonoBehaviour
    {
        [SerializeField] private GameObject[] panels = new GameObject[0];
        [Tooltip("The panel shown when the menu starts. Empty = none, for menus opened later such as a pause menu.")]
        [SerializeField] private GameObject startPanel;

        /// <summary>The panel showing now, or null if none is.</summary>
        public GameObject CurrentPanel { get; private set; }

        // In Start, because selecting a control needs the EventSystem to be enabled already.
        private void Start()
        {
            Show(startPanel);
        }

        /// <summary>Shows one panel and hides the others; null hides them all.</summary>
        public void Show(GameObject panel)
        {
            CurrentPanel = panel;
            foreach (GameObject candidate in panels)
            {
                candidate.SetActive(candidate == panel);
            }

            Selectable firstControl = panel != null ? panel.GetComponentInChildren<Selectable>() : null;
            if (firstControl != null)
            {
                firstControl.Select();
            }
            else if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }
}
