using UnityEditor;
using UnityEngine;

namespace TreeGame.EditorTools
{
    /// <summary>Adds looping music to a scene. Each track's credit is in MainMenuBuilder and THIRD_PARTY_NOTICES.md.</summary>
    internal static class MusicBuilder
    {
        private const string GameMusicPath = "Assets/Audio/Music/Path to Lake Land.ogg";
        private const string MenuMusicPath = "Assets/Audio/Music/Menu_Town.mp3";
        private const float MusicVolume = 0.5f;

        /// <summary>
        /// On its own root object, so the minigame runner switches it off with the rest of the world: minigames are
        /// silent for now, until they get music of their own.
        /// </summary>
        public static void CreateGameMusic()
        {
            CreateMusic("Game Music", GameMusicPath);
        }

        public static void CreateMenuMusic()
        {
            CreateMusic("Menu Music", MenuMusicPath);
        }

        private static void CreateMusic(string objectName, string clipPath)
        {
            AudioClip clip = LoadStreamedClip(clipPath);
            if (clip == null)
            {
                Debug.LogError($"No music found at {clipPath}; the scene will be silent.");
            }

            var music = new GameObject(objectName, typeof(AudioSource));
            var source = music.GetComponent<AudioSource>();
            source.clip = clip;
            source.volume = MusicVolume;
            source.loop = true;
            source.playOnAwake = true;
        }

        /// <summary>Streaming reads a track from disk as it plays, instead of holding all of it in memory.</summary>
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
