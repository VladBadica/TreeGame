using UnityEngine;
using UnityEngine.InputSystem;

namespace TreeGame.Menus
{
    /// <summary>Goes back to a menu's home panel on the cancel key (Esc). On the home panel itself, the key does nothing.</summary>
    public class MenuBackInput : MonoBehaviour
    {
        [Tooltip("A button action, e.g. UI/Cancel.")]
        [SerializeField] private InputActionReference backAction;
        [SerializeField] private MenuPanels panels;
        [SerializeField] private GameObject homePanel;

        private void OnEnable()
        {
            if (backAction == null || backAction.action == null || panels == null || homePanel == null)
            {
                Debug.LogWarning($"{nameof(MenuBackInput)}: needs a back action, panels and a home panel.", this);
                enabled = false;
                return;
            }

            // Not disabled in OnDisable: the UI input module shares this action.
            backAction.action.Enable();
        }

        private void Update()
        {
            if (backAction.action.WasPressedThisFrame() && panels.CurrentPanel != homePanel)
            {
                panels.Show(homePanel);
            }
        }
    }
}
