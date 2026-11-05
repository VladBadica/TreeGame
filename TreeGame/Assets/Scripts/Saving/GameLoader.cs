using TreeGame.Cameras;
using TreeGame.Menus;
using TreeGame.Minigames;
using TreeGame.Trees;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TreeGame.Saving
{
    /// <summary>
    /// Loads the save. The quick load key reloads this scene and restores the save into the fresh copy, so nothing can
    /// be left over from before the load. Reloading doesn't count as exiting, so it doesn't save over what it is about
    /// to load. The key is ignored while paused, in a minigame, or with no usable save. Nothing catches up on load:
    /// time doesn't pass while a game isn't being played.
    /// </summary>
    // Restores after every other Start, because the camera and backdrop record where they start from in theirs, and
    // moving things before that would throw them off for the rest of the session.
    [DefaultExecutionOrder(1000)]
    public class GameLoader : MonoBehaviour
    {
        [SerializeField] private WorldState world;
        [Tooltip("A button action, e.g. Player/QuickLoad.")]
        [SerializeField] private InputActionReference quickLoadAction;
        [SerializeField] private PauseMenu pauseMenu;
        [SerializeField] private MinigameRunner minigameRunner;
        [SerializeField] private TreeVisuals treeVisuals;
        [SerializeField] private CameraFollow cameraFollow;

        private SaveFile saveFile;

        private void Awake()
        {
            saveFile = CurrentSaveSlot.File();
        }

        private void OnEnable()
        {
            if (world == null || !world.IsComplete || quickLoadAction == null || quickLoadAction.action == null ||
                pauseMenu == null || minigameRunner == null || treeVisuals == null || cameraFollow == null)
            {
                Debug.LogWarning($"{nameof(GameLoader)}: needs a complete world state, a quick load action, a pause menu, a minigame runner, tree visuals and a camera.", this);
                enabled = false;
                return;
            }

            quickLoadAction.action.Enable();
        }

        private void OnDisable()
        {
            if (quickLoadAction != null && quickLoadAction.action != null)
            {
                quickLoadAction.action.Disable();
            }
        }

        private void Start()
        {
            if (PendingLoad.TakeRequest() && saveFile.TryRead(out SaveData data))
            {
                world.Restore(data);

                // Straight to the restored state, rather than easing there from a new game's.
                treeVisuals.SnapToTarget();
                cameraFollow.SnapToTarget();
            }
        }

        private void Update()
        {
            if (quickLoadAction.action.WasPressedThisFrame() && !pauseMenu.IsPaused && !minigameRunner.IsRunning &&
                saveFile.TryRead(out _))
            {
                PendingLoad.Request();
                SceneManager.LoadScene(gameObject.scene.name);
            }
        }
    }
}
