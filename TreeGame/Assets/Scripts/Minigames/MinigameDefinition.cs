using UnityEngine;

namespace TreeGame.Minigames
{
    /// <summary>Identifies a minigame that can be started, as an asset so it can be assigned in the Inspector.</summary>
    [CreateAssetMenu(fileName = "Minigame", menuName = "Tree Game/Minigames/Minigame")]
    public class MinigameDefinition : ScriptableObject
    {
        [Tooltip("The minigame's scene. Must be listed in Build Profiles.")]
        [SerializeField] private string sceneName;

        public string SceneName => sceneName;
    }
}
