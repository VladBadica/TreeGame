using TreeGame.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreeGame.Menus
{
    /// <summary>
    /// Makes a button replace the current scene with another, e.g. Play loading the game. The new scene starts fresh,
    /// unless the button is set to start it from the save, e.g. Continue.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LoadSceneButton : MonoBehaviour
    {
        [Tooltip("Must be listed in Build Profiles.")]
        [SerializeField] private string sceneName;
        [Tooltip("Starts the new scene from the save, rather than fresh.")]
        [SerializeField] private bool startFromSave;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(Load);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(Load);
        }

        private void Load()
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"{nameof(LoadSceneButton)}: scene '{sceneName}' isn't in Build Profiles.", this);
                return;
            }

            if (startFromSave)
            {
                PendingLoad.Request();
            }

            // Loading takes a moment; a second click must not start it again.
            button.interactable = false;
            SceneManager.LoadSceneAsync(sceneName);
        }
    }
}
