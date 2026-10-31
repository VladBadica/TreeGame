using TreeGame.Care;
using TreeGame.Core;
using TreeGame.DevTools;
using TreeGame.Interaction;
using TreeGame.Items;
using TreeGame.Trees;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TreeGame.EditorTools
{
    /// <summary>
    /// Generates the game scene with all references wired, so the setup is reproducible and nothing has to be
    /// assembled by hand. This is only the order of assembly; each part has its own builder.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        [MenuItem("Tree Game/Create Prototype Scene")]
        private static void CreatePrototypeScene()
        {
            const string scenePath = BuildSceneList.TreeGamePath;

            if (!BuilderHelpers.TryStartNewScene(scenePath, "Create Prototype Scene", out Scene scene))
            {
                return;
            }

            // Existing assets are reused so manual tweaks to them survive regenerating the scene.
            GameConfig config = GameAssets.GetOrCreateConfig();
            // Order matters: the water bucket goes into slot 1, which the tree's "Press 1 to Water" prompt relies on.
            GameAssets.GiveStartingItemsIfNone(config,
                GameAssets.GetOrCreateItem<WaterBucketDefinition>("WaterBucket", "Water Bucket", PlaceholderArt.GetOrCreateWaterBucketIcon()),
                GameAssets.GetOrCreateItem<CompostBucketDefinition>("CompostBucket", "Compost Bucket", PlaceholderArt.GetOrCreateCompostBucketIcon()));
            Sprite brownLeaf = PlaceholderArt.GetOrCreateBrownLeafSprite();
            var brownLeaves = GameAssets.GetOrCreateItem<StackableItemDefinition>("BrownLeaves", "Brown Leaves", brownLeaf);
            ItemPickup fallenLeafPrefab = TreeBuilder.GetOrCreateFallenLeafPrefab(brownLeaves, brownLeaf);
            Material bark = BuilderHelpers.GetOrCreateMaterial("Bark", new Color(0.45f, 0.3f, 0.2f));
            Material leaves = BuilderHelpers.GetOrCreateMaterial("Leaves", new Color(0.36f, 0.62f, 0.3f));
            Material ground = BuilderHelpers.GetOrCreateMaterial("Ground", new Color(0.58f, 0.72f, 0.42f));
            PlaceholderArt.GetOrCreateFenceSprites(out Sprite fenceNear, out Sprite fenceFar);
            AssetDatabase.SaveAssets();

            WorldBuilder.ConfigureEnvironment();
            WorldBuilder.CreateSun();
            WorldBuilder.CreateGround(ground);
            WorldBuilder.CreateFences(fenceNear, fenceFar);
            StationBuilder.CreateWell(config);
            StationBuilder.CreateCompostHeap(config, brownLeaves);
            GameObject tree = TreeBuilder.CreateTree(config, bark, leaves);
            TreeResource water = TreeBuilder.CreateResource(tree.transform, TreeResourceType.Water, config);
            TreeResource nutrients = TreeBuilder.CreateResource(tree.transform, TreeResourceType.Nutrients, config);
            TreeBuilder.CreateTreeCare(tree.transform, water, nutrients);
            TreeBuilder.CreateFallenLeafSpawner(tree, config, fallenLeafPrefab);
            Transform player = PlayerBuilder.CreatePlayer(config, tree.transform);
            Transform camera = WorldBuilder.CreateCamera(player);
            var viewCamera = camera.GetComponent<Camera>();
            PlayerBuilder.ConnectToCamera(player, viewCamera);
            WorldBuilder.CreateBackdrop(camera);

            UIFactory.CreateEventSystem();
            Transform hud = UIFactory.CreateCanvas("HUD", 0);
            HudBuilder.CreateResourceHud(hud, water, nutrients);
            HudBuilder.CreateInteractionPrompt(hud, player.GetComponent<Interactor>(), viewCamera);
            HudBuilder.CreateGatherPrompt(hud, player.GetComponent<ItemCollector>(), viewCamera);
            HudBuilder.CreateInventoryBar(hud, player.GetComponent<Inventory>());
            // Own canvas, drawn on top, so all dev tooling can be removed by deleting one object.
            DebugPanelBuilder.CreateDebugPanel(UIFactory.CreateCanvas("Debug UI", 100), tree.GetComponent<TreeGrowth>());

            BuilderHelpers.SaveGeneratedScene(scene, scenePath);

            Debug.Log($"Prototype scene created at {scenePath}. Press Play and use the Growth panel.");
        }
    }
}
