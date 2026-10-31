using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.Menus
{
    /// <summary>Makes a button switch its menu to another panel, e.g. Settings, Credits or Back.</summary>
    [RequireComponent(typeof(Button))]
    public class OpenPanelButton : MonoBehaviour
    {
        [SerializeField] private MenuPanels menu;
        [SerializeField] private GameObject panel;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(Open);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(Open);
        }

        private void Open()
        {
            if (menu != null)
            {
                menu.Show(panel);
            }
        }
    }
}
