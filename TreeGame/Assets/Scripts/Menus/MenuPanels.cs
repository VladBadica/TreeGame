using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.Menus
{
    /// <summary>
    /// The screens of a menu, of which one shows at a time (e.g. main, settings, credits). Selects the first control
    /// of whichever one opens, so the menu can also be used with a keyboard or gamepad.
    /// </summary>
    public class MenuPanels : MonoBehaviour
    {
        [SerializeField] private GameObject[] panels = new GameObject[0];
        [Tooltip("The panel shown when the menu starts.")]
        [SerializeField] private GameObject startPanel;

        // In Start, because selecting a control needs the EventSystem to be enabled already.
        private void Start()
        {
            Show(startPanel);
        }

        public void Show(GameObject panel)
        {
            foreach (GameObject candidate in panels)
            {
                candidate.SetActive(candidate == panel);
            }

            Selectable firstControl = panel != null ? panel.GetComponentInChildren<Selectable>() : null;
            if (firstControl != null)
            {
                firstControl.Select();
            }
        }
    }
}
