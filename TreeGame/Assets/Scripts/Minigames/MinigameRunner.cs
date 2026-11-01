using System;
using System.Collections;
using System.Collections.Generic;
using TreeGame.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TreeGame.Minigames
{
    /// <summary>
    /// Plays a minigame on top of the current scene. It fades out, loads the minigame's scene additively and switches
    /// off everything else in the current scene meanwhile, which freezes the world and takes its camera, HUD and input
    /// out of the way without losing any state. It has to sit on its own root object, so it stays on itself.
    /// </summary>
    public class MinigameRunner : MonoBehaviour
    {
        private const float Clear = 0f;
        private const float Covered = 1f;

        [Tooltip("Covers the screen during the change. Must be a child of this object, so it stays on with it.")]
        [SerializeField] private ScreenFade screenFade;
        [Tooltip("How long each fade takes, in seconds.")]
        [SerializeField, Min(0f)] private float fadeSeconds = 0.25f;

        public bool IsRunning { get; private set; }

        /// <summary>
        /// Starts a minigame. When it ends, the world is back on before <paramref name="onFinished"/> is called, so
        /// the caller can act on the result. Returns false if it couldn't start.
        /// </summary>
        public bool Launch(MinigameDefinition minigame, Action<MinigameResult> onFinished)
        {
            if (IsRunning || minigame == null)
            {
                return false;
            }

            if (screenFade == null)
            {
                Debug.LogError($"{nameof(MinigameRunner)}: no {nameof(ScreenFade)} assigned.", this);
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(minigame.SceneName))
            {
                Debug.LogError($"{nameof(MinigameRunner)}: scene '{minigame.SceneName}' isn't in Build Profiles.", this);
                return false;
            }

            StartCoroutine(Run(minigame.SceneName, onFinished));
            return true;
        }

        private IEnumerator Run(string sceneName, Action<MinigameResult> onFinished)
        {
            IsRunning = true;
            yield return screenFade.FadeTo(Covered, fadeSeconds);

            Scene world = gameObject.scene;
            List<GameObject> frozen = FreezeWorld(world);

            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            Scene minigameScene = SceneManager.GetSceneByName(sceneName);
            SceneManager.SetActiveScene(minigameScene); // So lighting comes from the minigame's own scene.

            yield return screenFade.FadeTo(Clear, fadeSeconds);

            // If the scene turns out to have no minigame, the world still has to come back.
            var result = new MinigameResult(completed: false, score: 0f);
            Minigame minigame = FindMinigame(minigameScene);
            if (minigame == null)
            {
                Debug.LogError($"{nameof(MinigameRunner)}: scene '{sceneName}' has no {nameof(Minigame)} component.", this);
            }
            else
            {
                bool finished = false;
                void OnFinished(MinigameResult minigameResult)
                {
                    result = minigameResult;
                    finished = true;
                }

                minigame.Finished += OnFinished;
                yield return new WaitUntil(() => finished);
                minigame.Finished -= OnFinished;
            }

            yield return screenFade.FadeTo(Covered, fadeSeconds);

            SceneManager.SetActiveScene(world);
            yield return SceneManager.UnloadSceneAsync(minigameScene);

            foreach (GameObject root in frozen)
            {
                root.SetActive(true);
            }

            // Reported while the screen is still covered, so the player sees the result as the world fades back in.
            onFinished?.Invoke(result);

            yield return screenFade.FadeTo(Clear, fadeSeconds);
            IsRunning = false;
        }

        /// <summary>Switches off the scene's root objects, returning those that were on so only they come back.</summary>
        private List<GameObject> FreezeWorld(Scene world)
        {
            var frozen = new List<GameObject>();
            foreach (GameObject root in world.GetRootGameObjects())
            {
                if (root != gameObject && root.activeSelf)
                {
                    root.SetActive(false);
                    frozen.Add(root);
                }
            }

            return frozen;
        }

        private static Minigame FindMinigame(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Minigame minigame = root.GetComponentInChildren<Minigame>(true);
                if (minigame != null)
                {
                    return minigame;
                }
            }

            return null;
        }
    }
}
