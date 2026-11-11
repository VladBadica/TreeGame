using TreeGame.Care;
using TreeGame.Core;
using TreeGame.DevTools;
using TreeGame.Interaction;
using TreeGame.Items;
using TreeGame.Market;
using TreeGame.Menus;
using TreeGame.Minigames;
using TreeGame.Pests;
using TreeGame.Saving;
using TreeGame.Seasons;
using TreeGame.Trees;
using TreeGame.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TreeGame.EditorTools
{
    /// <summary>
    /// Generates the game scene with all references wired, so the setup is reproducible and nothing has to be
    /// assembled by hand. This is only the order of assembly; each part has its own builder.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        [MenuItem("Tree Game/Create Prototype Scene", false, AllScenesBuilder.SingleSceneMenuPriority)]
        private static void CreatePrototypeScene()
        {
            if (BuilderHelpers.ConfirmGenerating("Create Prototype Scene", BuildSceneList.TreeGamePath))
            {
                Build();
            }
        }

        /// <summary>Generates the scene without asking; callers confirm first.</summary>
        internal static void Build()
        {
            const string scenePath = BuildSceneList.TreeGamePath;
            Scene scene = BuilderHelpers.StartNewScene();

            // Existing assets are reused so manual tweaks to them survive regenerating the scene.
            GameConfig config = GameAssets.GetOrCreateConfig();
            var waterBucket = GameAssets.GetOrCreateItem<WaterBucketDefinition>("WaterBucket", "Water Bucket", PlaceholderArt.GetOrCreateWaterBucketIcon());
            var compostBucket = GameAssets.GetOrCreateItem<CompostBucketDefinition>("CompostBucket", "Compost Bucket", PlaceholderArt.GetOrCreateCompostBucketIcon());
            // Order matters: the water bucket goes into slot 1, which the tree's "Press 1 to Water" prompt relies on.
            GameAssets.GiveStartingItemsIfNone(config, waterBucket, compostBucket);
            Sprite brownLeaf = PlaceholderArt.GetOrCreateBrownLeafSprite();
            var brownLeaves = GameAssets.GetOrCreateItem<StackableItemDefinition>("BrownLeaves", "Brown Leaves", brownLeaf);
            ItemPickup fallenLeafPrefab = TreeBuilder.GetOrCreateFallenLeafPrefab(brownLeaves, brownLeaf);
            var coin = GameAssets.GetOrCreateItem<StackableItemDefinition>("Coin", "Coin", PlaceholderArt.GetOrCreateCoinIcon());
            Sprite insect = PlaceholderArt.GetOrCreateInsectSprite();
            Sprite notificationBubble = PlaceholderArt.GetOrCreateNotificationBubbleSprite();
            PlaceholderArt.GetOrCreateFenceSprites(out Sprite fenceNear, out Sprite fenceFar);
            MinigameDefinition wellMinigame = WellSpinSceneBuilder.GetOrCreateDefinition();
            MinigameDefinition insectMinigame = InsectSwarmSceneBuilder.GetOrCreateDefinition();
            MarketOffer[] marketOffers = MarketBuilder.GetOrCreateOffers();
            AssetDatabase.SaveAssets();

            Renderer ground = WorldBuilder.CreateStage();
            WorldBuilder.CreateFences(fenceNear, fenceFar);
            SeasonClock seasons = SeasonBuilder.CreateClock(config);
            MinigameRunner minigameRunner = CreateMinigameRunner();
            MusicBuilder.CreateGameMusic();
            WaterWell well = StationBuilder.CreateWell(config, minigameRunner, wellMinigame);
            Composter composter = StationBuilder.CreateCompostHeap(config, brownLeaves);
            GameObject tree = TreeBuilder.CreateTree(config);
            TreeResource water = TreeBuilder.CreateResource(tree.transform, TreeResourceType.Water, config);
            TreeResource nutrients = TreeBuilder.CreateResource(tree.transform, TreeResourceType.Nutrients, config);
            TreeResource health = TreeBuilder.CreateResource(tree.transform, TreeResourceType.Health, config);
            TreeBuilder.CreateHealthRegeneration(health, water, nutrients, config);
            TreeBuilder.CreateNeglectDamage(tree, health, water, nutrients, config);
            ResourceReceiver treeWaterReceiver = TreeBuilder.CreateTreeCare(tree.transform, water, nutrients,
                out ResourceReceiver treeNutrientsReceiver);
            OverflowPenalty overwatering = TreeBuilder.CreateOverflowPenalty(tree, treeWaterReceiver, config);
            OverflowPenalty overfeeding = TreeBuilder.CreateOverflowPenalty(tree, treeNutrientsReceiver, config);
            TreeBuilder.CreateEmptyResourcePenalty(tree, water, config);
            TreeBuilder.CreateEmptyResourcePenalty(tree, nutrients, config);
            StationBuilder.CreateRunningWaterSound(well, treeWaterReceiver);
            TreeBuilder.CreateFallenLeafSpawner(tree, config, fallenLeafPrefab);
            InsectInfestation insects = InsectBuilder.CreateInsects(tree, health, config, insect, out InsectVisuals insectVisuals);
            SeasonBuilder.ConnectToSeasons(seasons, tree, water, nutrients, composter);
            MarketStall marketStall = MarketBuilder.CreateStall(marketOffers, coin, tree.GetComponent<TreeGrowthRates>(),
                out Interactable marketInteractable);
            Transform player = PlayerBuilder.CreatePlayer(config, tree.transform);
            BuilderHelpers.SetReferences(well, ("inventory", player.GetComponent<Inventory>()));
            BuilderHelpers.SetReferences(marketStall, ("inventory", player.GetComponent<Inventory>()));
            InsectBuilder.CreateInsectDefense(insectVisuals, insects, config, minigameRunner, insectMinigame, player.GetComponent<Inventory>(), coin);
            Transform camera = WorldBuilder.CreateCamera(player);
            WorldBuilder.FitCameraToTree(camera, tree.GetComponent<TreeVisuals>());
            var viewCamera = camera.GetComponent<Camera>();
            PlayerBuilder.ConnectToCamera(player, viewCamera);
            Transform backdrop = WorldBuilder.CreateBackdrop(camera);
            SeasonBuilder.ColorScenery(seasons, ground, backdrop);

            UIFactory.CreateEventSystem();
            Transform hud = UIFactory.CreateCanvas("HUD", 0);
            HudBuilder.CreateResourceHud(hud, health, water, nutrients);
            HudBuilder.CreateStatusBadges(hud, water, nutrients, overwatering, overfeeding);
            HudBuilder.CreateInteractionPrompt(hud, player.GetComponent<Interactor>(), viewCamera);
            HudBuilder.CreateGatherPrompt(hud, player.GetComponent<ItemCollector>(), viewCamera);
            HudBuilder.CreateInventoryBar(hud, player.GetComponent<Inventory>());
            HudBuilder.CreateOffScreenInsectBubble(hud, insects, tree.GetComponent<TreeVisuals>(), viewCamera, insect, notificationBubble);
            SeasonBuilder.CreateSeasonHud(hud, seasons);
            // Own canvas, drawn on top, so all dev tooling can be removed by deleting one object.
            DebugPanelBuilder.CreateDebugPanel(UIFactory.CreateCanvas("Debug UI", 100), tree, seasons);
            PauseMenu pauseMenu = PauseMenuBuilder.CreatePauseMenu(player, minigameRunner, tree.GetComponent<TreeSeed>(), out Button quitToMenuButton);
            MarketBuilder.CreateMenu(marketStall, marketInteractable, player, pauseMenu);
            GameSaver saver = SaveBuilder.CreateSaveAndLoad(minigameRunner, pauseMenu, quitToMenuButton, tree, water, nutrients, health,
                overwatering, overfeeding, player, composter, insects, seasons, camera, waterBucket, compostBucket, brownLeaves, coin);
            HudBuilder.CreateSaveMessage(hud, saver);

            BuilderHelpers.SaveGeneratedScene(scene, scenePath);

            Debug.Log($"Prototype scene created at {scenePath}. Press Play and use the Growth panel.");
        }

        /// <summary>
        /// On its own root object, because the runner switches off all the others while a minigame plays.
        /// Its fade is a child, so it stays on with it and can cover the change.
        /// </summary>
        private static MinigameRunner CreateMinigameRunner()
        {
            var runnerObject = new GameObject("Minigame Runner");
            var runner = runnerObject.AddComponent<MinigameRunner>();
            BuilderHelpers.SetReferences(runner, ("screenFade", UIFactory.CreateScreenFade(runnerObject.transform)));
            return runner;
        }
    }
}
