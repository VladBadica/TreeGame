using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.Menus
{
    /// <summary>Makes a button close the game, or stop Play Mode when running in the Editor.</summary>
    [RequireComponent(typeof(Button))]
    public class QuitGameButton : MonoBehaviour
    {
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(Quit);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(Quit);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            // Application.Quit does nothing in the Editor.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
