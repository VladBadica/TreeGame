using UnityEditor;
using UnityEngine;

namespace TreeGame.EditorTools
{
    /// <summary>Adds the game's background music to a scene. Its credit is in MainMenuBuilder and THIRD_PARTY_NOTICES.md.</summary>
    internal static class MusicBuilder
    {
        private const string BackgroundMusicPath = "Assets/Audio/Music/Path to Lake Land.ogg";
        private const float BackgroundMusicVolume = 0.5f;

        /// <summary>
        /// Puts the music under an object that stays on during minigames, so it keeps playing through them. The scene's
        /// listener goes with it, because a listener on the camera would switch off with the world and cut the music
        /// while the minigame's scene loads.
        /// </summary>
        public static void CreateBackgroundMusic(Transform parent)
        {
            AudioClip clip = LoadStreamedClip(BackgroundMusicPath);
            if (clip == null)
            {
                Debug.LogError($"No music found at {BackgroundMusicPath}; the scene will be silent.");
            }

            var music = new GameObject("Background Music", typeof(AudioSource), typeof(AudioListener));
            music.transform.SetParent(parent, false);
            var source = music.GetComponent<AudioSource>();
            source.clip = clip;
            source.volume = BackgroundMusicVolume;
            source.loop = true;
            source.playOnAwake = true;
        }

        /// <summary>Streaming reads a long track from disk as it plays, instead of holding all of it in memory.</summary>
        private static AudioClip LoadStreamedClip(string path)
        {
            if (AssetImporter.GetAtPath(path) is AudioImporter importer &&
                importer.defaultSampleSettings.loadType != AudioClipLoadType.Streaming)
            {
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.Streaming;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
    }
}
