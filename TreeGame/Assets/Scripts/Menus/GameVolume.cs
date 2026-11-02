using UnityEngine;

namespace TreeGame.Menus
{
    /// <summary>
    /// The game's overall volume, from 0 to 1. It sets Unity's listener volume, which applies to every sound in every
    /// scene, and is remembered between runs. Applied before the first scene loads, so nothing plays at the wrong volume.
    /// </summary>
    public static class GameVolume
    {
        private const string SaveKey = "Settings.Volume";
        private const float DefaultVolume = 1f;

        public static float Current => AudioListener.volume;

        /// <summary>Changes the volume, clamped to 0..1. Invalid values are ignored.</summary>
        public static void Set(float volume)
        {
            if (!float.IsFinite(volume))
            {
                return;
            }

            AudioListener.volume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(SaveKey, AudioListener.volume);
        }

        /// <summary>Writes the volume to disk, for calling when a change is finished rather than on every step of it.</summary>
        public static void Save()
        {
            PlayerPrefs.Save();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySavedVolume()
        {
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(SaveKey, DefaultVolume));
        }
    }
}
