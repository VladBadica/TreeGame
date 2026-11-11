using TreeGame.Care;
using TreeGame.Core;
using TreeGame.Interaction;
using TreeGame.Items;
using TreeGame.Trees;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static TreeGame.EditorTools.BuilderHelpers;
using static TreeGame.EditorTools.SceneLayout;

namespace TreeGame.EditorTools
{
    /// <summary>Builds the tree and what belongs to it: its resources, caring for it, and its falling leaves.</summary>
    internal static class TreeBuilder
    {
        private const string PrefabsFolder = "Assets/Prefabs";
        private const string FallenLeafPrefabPath = PrefabsFolder + "/FallenLeaf.prefab";

        /// <summary>The game's tree: the tree itself, with its name, growing on its own over time and swaying in the wind.</summary>
        public static GameObject CreateTree(GameConfig config)
        {
            GameObject go = CreateTreeModel(config);
            go.AddComponent<TreeName>();

            var rates = go.AddComponent<TreeGrowthRates>();
            SetReferences(rates, ("config", config));

            SetReferences(go.AddComponent<PassiveGrowth>(),
                ("treeGrowth", go.GetComponent<TreeGrowth>()),
                ("growthRates", rates),
                ("extraGrowth", go.GetComponent<TreeExtraGrowth>()),
                ("config", config));
            return go;
        }

        /// <summary>
        /// The tree as it looks, swaying in the wind: its growth and extra growth, its seed and how it is drawn, but
        /// nothing that changes its growth. The game grows it; the main menu shows a saved tree with it.
        /// </summary>
        public static GameObject CreateTreeModel(GameConfig config)
        {
            Material bark = GetOrCreateMaterial("Bark", new Color(0.45f, 0.3f, 0.2f));
            ApplyBarkTexture(bark, PlaceholderArt.GetOrCreateBarkTexture());
            Material leaves = GetOrCreateMaterial("Leaves", new Color(0.36f, 0.62f, 0.3f));
            ApplyLeafTexture(leaves, PlaceholderArt.GetOrCreateLeafTexture());

            var go = new GameObject("Tree");
            var growth = go.AddComponent<TreeGrowth>();
            var extraGrowth = go.AddComponent<TreeExtraGrowth>();
            var seed = go.AddComponent<TreeSeed>();
            var visuals = go.AddComponent<TreeVisuals>();

            SetReferences(visuals,
                ("treeGrowth", growth),
                ("treeSeed", seed),
                ("extraGrowth", extraGrowth),
                ("barkMaterial", bark),
                ("foliageMaterial", leaves));

            SetReferences(go.AddComponent<TreeWindSway>(),
                ("visuals", visuals),
                ("config", config));

            return go;
        }

        /// <summary>
        /// Gives the bark material its texture, unless it already has one, so a texture chosen by hand isn't replaced.
        /// The texture carries the color, so the material's own tint goes back to white.
        /// </summary>
        public static void ApplyBarkTexture(Material bark, Texture2D texture)
        {
            const string baseMap = "_BaseMap";
            if (bark.GetTexture(baseMap) != null)
            {
                return;
            }

            bark.SetTexture(baseMap, texture);
            bark.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(bark);
        }

        /// <summary>How see-through a leaf pixel can be before it is cut away.</summary>
        internal const float LeafCutoff = 0.5f;

        /// <summary>
        /// Makes the leaf material show the leaf texture, unless it already has one, so a texture chosen by hand isn't
        /// replaced. Each leaf card is a single flat sheet, so the see-through parts around the leaves are cut away and
        /// both of its sides are drawn.
        /// </summary>
        public static void ApplyLeafTexture(Material leaves, Texture2D texture)
        {
            const string baseMap = "_BaseMap";
            if (leaves.GetTexture(baseMap) != null)
            {
                return;
            }

            leaves.SetTexture(baseMap, texture);
            leaves.SetColor("_BaseColor", Color.white);

            leaves.SetFloat("_AlphaClip", 1f);
            leaves.SetFloat("_Cutoff", LeafCutoff);
            leaves.EnableKeyword("_ALPHATEST_ON");
            leaves.renderQueue = (int)RenderQueue.AlphaTest;
            leaves.SetFloat("_Cull", (float)CullMode.Off);
            EditorUtility.SetDirty(leaves);
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

        /// <summary>On the health resource's object, so it catches up after water and nutrients have drained.</summary>
        public static void CreateHealthRegeneration(TreeResource health, TreeResource water, TreeResource nutrients, GameConfig config)
        {
            SetReferences(health.gameObject.AddComponent<HealthRegeneration>(),
                ("health", health),
                ("water", water),
                ("nutrients", nutrients),
                ("config", config));
        }

        /// <summary>
        /// On the tree itself, so after a minigame it catches up before water and nutrients drain. A resource that ran
        /// out partway through a minigame then costs nothing for that time, rather than counting as empty all along.
        /// </summary>
        public static void CreateNeglectDamage(GameObject tree, TreeResource health, TreeResource water, TreeResource nutrients, GameConfig config)
        {
            SetReferences(tree.AddComponent<NeglectDamage>(),
                ("health", health),
                ("water", water),
                ("nutrients", nutrients),
                ("config", config));
        }

        /// <summary>
        /// Lets the player water the tree (water bucket, hotbar slot 1) and feed it compost (compost bucket, slot 2)
        /// while standing at the trunk.
        /// </summary>
        /// <returns>The receiver for water, for anything that reacts to watering.</returns>
        /// <param name="nutrientsReceiver">The receiver for compost, for anything that reacts to feeding.</param>
        public static ResourceReceiver CreateTreeCare(Transform tree, TreeResource water, TreeResource nutrients,
            out ResourceReceiver nutrientsReceiver)
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

            var waterReceiver = interactable.gameObject.AddComponent<ResourceReceiver>();
            SetReferences(waterReceiver, ("resource", water));
            nutrientsReceiver = interactable.gameObject.AddComponent<ResourceReceiver>();
            SetReferences(nutrientsReceiver, ("resource", nutrients));
            return waterReceiver;
        }

        /// <summary>
        /// The penalty for pouring too much of a resource onto the tree at once (e.g. overwatering), for the resource the
        /// receiver takes. On the tree itself, next to its growth, so after a minigame the penalty is still in place while
        /// growth catches up.
        /// </summary>
        public static OverflowPenalty CreateOverflowPenalty(GameObject tree, ResourceReceiver receiver, GameConfig config)
        {
            var penalty = tree.AddComponent<OverflowPenalty>();
            SetReferences(penalty,
                ("receiver", receiver),
                ("growthRates", tree.GetComponent<TreeGrowthRates>()),
                ("config", config));
            return penalty;
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
