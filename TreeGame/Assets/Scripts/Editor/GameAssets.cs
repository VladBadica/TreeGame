using TreeGame.Core;
using TreeGame.Items;
using UnityEditor;
using UnityEngine;
using static TreeGame.EditorTools.BuilderHelpers;

namespace TreeGame.EditorTools
{
    /// <summary>
    /// The project assets the game scene needs. Existing ones are reused, so manual tweaks to them survive
    /// regenerating the scene.
    /// </summary>
    internal static class GameAssets
    {
        private const string ConfigFolder = "Assets/Config";
        private const string ConfigPath = ConfigFolder + "/GameConfig.asset";
        private const string ItemsFolder = "Assets/Items";

        public static GameConfig GetOrCreateConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config != null)
            {
                return config;
            }

            EnsureFolder(ConfigFolder);

            // Default values come from GameConfig's field initializers, so they live in one place.
            config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        /// <summary>
        /// An item definition asset in the Items folder, created with the given name and icon if it doesn't exist yet.
        /// Type-specific defaults (e.g. a bucket's capacity) come from the definition class itself.
        /// </summary>
        public static T GetOrCreateItem<T>(string assetName, string displayName, Sprite icon) where T : ItemDefinition
        {
            string path = $"{ItemsFolder}/{assetName}.asset";
            var item = AssetDatabase.LoadAssetAtPath<T>(path);
            if (item != null)
            {
                return item;
            }

            EnsureFolder(ItemsFolder);

            item = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(item, path);

            SetReferences(item, ("icon", icon));
            var serializedItem = new SerializedObject(item);
            serializedItem.FindProperty("displayName").stringValue = displayName;
            serializedItem.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        /// <summary>
        /// Only fills a starting list without any valid items, so items chosen in the config are never overwritten
        /// (entries whose asset was deleted count as empty).
        /// </summary>
        public static void GiveStartingItemsIfNone(GameConfig config, params ItemDefinition[] items)
        {
            var serializedConfig = new SerializedObject(config);
            SerializedProperty startingItems = serializedConfig.FindProperty("startingItems");
            for (int i = 0; i < startingItems.arraySize; i++)
            {
                if (startingItems.GetArrayElementAtIndex(i).objectReferenceValue != null)
                {
                    return;
                }
            }

            startingItems.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                startingItems.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }

            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }
    }
}
