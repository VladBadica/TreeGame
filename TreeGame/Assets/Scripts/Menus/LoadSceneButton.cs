using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreeGame.Menus
{
    /// <summary>
    /// Makes a button replace the current scene with another, e.g. Play loading the game. There is no save system,
    /// so everything in the scene being left is lost and the new scene starts fresh.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LoadSceneButton : MonoBehaviour
    {
        [Tooltip("Must be listed in Build Profiles.")]
        [SerializeField] private string sceneName;

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

            // Loading takes a moment; a second click must not start it again.
            button.interactable = false;
            SceneManager.LoadSceneAsync(sceneName);
        }
    }
}
