using TreeGame.Cameras;
using TreeGame.Care;
using TreeGame.Items;
using TreeGame.Menus;
using TreeGame.Minigames;
using TreeGame.Pests;
using TreeGame.Player;
using TreeGame.Saving;
using TreeGame.Trees;
using TreeGame.World;
using UnityEngine;
using UnityEngine.UI;
using static TreeGame.EditorTools.BuilderHelpers;

namespace TreeGame.EditorTools
{
    /// <summary>Builds what saves and loads the game. Everything else in the scene must exist first, because it reads all of it.</summary>
    internal static class SaveBuilder
    {
        /// <summary>On the minigame runner's object, because that is the one object that stays on during minigames.</summary>
        /// <param name="knownItems">Every item that can be in the inventory, so a load can find them by name.</param>
        public static GameSaver CreateSaveAndLoad(MinigameRunner minigameRunner, PauseMenu pauseMenu, Button quitToMenuButton,
            GameObject tree, TreeResource water, TreeResource nutrients, TreeResource health, Transform player,
            Composter composter, InsectInfestation insects, Transform camera, params ItemDefinition[] knownItems)
        {
            GameObject owner = minigameRunner.gameObject;

            var world = owner.AddComponent<WorldState>();
            SetReferences(world,
                ("treeGrowth", tree.GetComponent<TreeGrowth>()),
                ("treeSeed", tree.GetComponent<TreeSeed>()),
                ("treeName", tree.GetComponent<TreeName>()),
                ("water", water),
                ("nutrients", nutrients),
                ("health", health),
                ("player", player.GetComponent<PlayerMovement>()),
                ("inventory", player.GetComponent<Inventory>()),
                ("composter", composter),
                ("insects", insects),
                ("leaves", tree.GetComponent<FallenLeafSpawner>()));
            SetReferenceArray(world, "knownItems", knownItems);

            SetReferences(owner.AddComponent<GameLoader>(),
                ("world", world),
                ("quickLoadAction", FindActionReference("Player", "QuickLoad")),
                ("pauseMenu", pauseMenu),
                ("minigameRunner", minigameRunner),
                ("treeVisuals", tree.GetComponent<TreeVisuals>()),
                ("cameraFollow", camera.GetComponent<CameraFollow>()));

            var saver = owner.AddComponent<GameSaver>();
            SetReferences(saver,
                ("world", world),
                ("quickSaveAction", FindActionReference("Player", "QuickSave")),
                ("pauseMenu", pauseMenu),
                ("minigameRunner", minigameRunner),
                ("quitToMenuButton", quitToMenuButton));

            return saver;
        }
    }
}
