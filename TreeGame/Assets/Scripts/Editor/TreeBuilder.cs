using TreeGame.Care;
using TreeGame.Core;
using TreeGame.Interaction;
using TreeGame.Items;
using TreeGame.Trees;
using UnityEditor;
using UnityEngine;
using static TreeGame.EditorTools.BuilderHelpers;
using static TreeGame.EditorTools.SceneLayout;

namespace TreeGame.EditorTools
{
    /// <summary>Builds the tree and what belongs to it: its resources, caring for it, and its falling leaves.</summary>
    internal static class TreeBuilder
    {
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string FallenLeafPrefabPath = PrefabsFolder + "/FallenLeaf.prefab";

        public static GameObject CreateTree(GameConfig config, Material bark, Material leaves)
        {
            var go = new GameObject("Tree");
            var growth = go.AddComponent<TreeGrowth>();
            var visuals = go.AddComponent<TreeVisuals>();

            SetReferences(visuals,
                ("treeGrowth", growth),
                ("barkMaterial", bark),
                ("foliageMaterial", leaves));

            SetReferences(go.AddComponent<PassiveGrowth>(),
                ("treeGrowth", growth),
                ("config", config));

            SetReferences(go.AddComponent<TreeWindSway>(),
                ("visuals", visuals),
                ("config", config));

            return go;
        }

        /// <summary>A child object per resource keeps each resource easy to find in the Hierarchy.</summary>
        public static TreeResource CreateResource(Transform owner, TreeResourceType type, GameConfig config)
        {
            var go = new GameObject(type.ToString());
            go.transform.SetParent(owner, false);

            var resource = go.AddComponent<TreeResource>();
            SetReferences(resource, ("config", config));
            var serializedResource = new SerializedObject(resource);
            serializedResource.FindProperty("type").enumValueIndex = (int)type;
            serializedResource.ApplyModifiedPropertiesWithoutUndo();

            SetReferences(go.AddComponent<ResourceDepletion>(),
                ("resource", resource),
                ("config", config));

            return resource;
        }

        /// <summary>
        /// Lets the player water the tree (water bucket, hotbar slot 1) and feed it compost (compost bucket, slot 2)
        /// while standing at the trunk.
        /// </summary>
        public static void CreateTreeCare(Transform tree, TreeResource water, TreeResource nutrients)
        {
            // Both actions use the buckets' hotbar keys rather than the interact key, so the prompts name those keys.
            // Narrow, so the player has to stand almost at the trunk, leaving room for other interactables nearby.
            // The player's own trigger capsule (radius 0.3) adds to this, so it triggers within about 0.7 of the trunk.
            Interactable interactable = CreateInteractionZone(tree, width: 0.8f, promptHeight: 2.5f, "Water", promptKeyOverride: "1");
            var serializedInteractable = new SerializedObject(interactable);
            SerializedProperty extraPrompts = serializedInteractable.FindProperty("extraPrompts");
            extraPrompts.arraySize = 1;
            extraPrompts.GetArrayElementAtIndex(0).FindPropertyRelative("key").stringValue = "2";
            extraPrompts.GetArrayElementAtIndex(0).FindPropertyRelative("actionDescription").stringValue = "Feed";
            serializedInteractable.ApplyModifiedPropertiesWithoutUndo();

            SetReferences(interactable.gameObject.AddComponent<ResourceReceiver>(), ("resource", water));
            SetReferences(interactable.gameObject.AddComponent<ResourceReceiver>(), ("resource", nutrients));
        }

        /// <summary>Leaves fall around the tree once it has grown; they collect under their own root, not the tree.</summary>
        public static void CreateFallenLeafSpawner(GameObject tree, GameConfig config, ItemPickup leafPrefab)
        {
            // Outside the tree, so spawned leaves never mix with the tree's generated parts.
            var container = new GameObject("Fallen Leaves").transform;

            var spawner = tree.AddComponent<FallenLeafSpawner>();
            SetReferences(spawner,
                ("treeGrowth", tree.GetComponent<TreeGrowth>()),
                ("config", config),
                ("leafPrefab", leafPrefab),
                ("container", container));
            // From just inside the ground's front edge to just behind the trunk, so some land in front of the player.
            var serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("depthRange").vector2Value = new Vector2(GroundFrontZ + 0.5f, 0.5f);
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// The fallen leaf prefab: the leaf sprite plus a pickup zone giving one Brown Leaves.
        /// Reused if it exists, so edits to the prefab survive regenerating the scene.
        /// </summary>
        public static ItemPickup GetOrCreateFallenLeafPrefab(ItemDefinition leaves, Sprite sprite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ItemPickup>(FallenLeafPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            EnsureFolder(PrefabsFolder);

            var leaf = new GameObject("Fallen Leaf", typeof(SpriteRenderer), typeof(BoxCollider));
            leaf.GetComponent<SpriteRenderer>().sprite = sprite;

            // Narrow, so the player has to walk up to a leaf, but deep enough (Z) to reach the player's walking line
            // from wherever along the ground the leaf landed.
            var zone = leaf.GetComponent<BoxCollider>();
            zone.isTrigger = true;
            zone.center = new Vector3(0f, 0.5f, 0f);
            zone.size = new Vector3(0.5f, 1f, 8f);

            SetReferences(leaf.AddComponent<ItemPickup>(), ("item", leaves));

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(leaf, FallenLeafPrefabPath);
            Object.DestroyImmediate(leaf);
            return prefab.GetComponent<ItemPickup>();
        }
    }
}
