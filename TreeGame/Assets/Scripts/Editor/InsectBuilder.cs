using TreeGame.Care;
using TreeGame.Core;
using TreeGame.Interaction;
using TreeGame.Items;
using TreeGame.Minigames;
using TreeGame.Pests;
using TreeGame.Trees;
using UnityEngine;
using static TreeGame.EditorTools.BuilderHelpers;

namespace TreeGame.EditorTools
{
    /// <summary>Builds the insects that come to the tree: their state and spawner on the tree, their visuals, and defending against them.</summary>
    internal static class InsectBuilder
    {
        // Bigger than the sprite, so a small target on a screen full of other things is easy to click.
        private static readonly Vector3 ClickAreaSize = new Vector3(0.5f, 0.5f, 0.2f);

        public static InsectInfestation CreateInsects(GameObject tree, TreeResource health, GameConfig config, Sprite insectSprite,
            out InsectVisuals insectVisuals)
        {
            var infestation = tree.AddComponent<InsectInfestation>();
            SetReferences(infestation,
                ("health", health),
                ("config", config));

            SetReferences(tree.AddComponent<InsectSpawner>(),
                ("infestation", infestation),
                ("config", config));

            // Outside the tree, so the insects never mix with the tree's generated parts.
            var visuals = new GameObject("Insects");
            var template = new GameObject("Insect Template", typeof(SpriteRenderer), typeof(BoxCollider), typeof(Clickable));
            template.transform.SetParent(visuals.transform, false);
            template.GetComponent<SpriteRenderer>().sprite = insectSprite;
            var clickArea = template.GetComponent<BoxCollider>();
            clickArea.isTrigger = true;
            clickArea.size = ClickAreaSize;
            template.SetActive(false);

            insectVisuals = visuals.AddComponent<InsectVisuals>();
            SetReferences(insectVisuals,
                ("infestation", infestation),
                ("tree", tree.GetComponent<TreeVisuals>()),
                ("config", config),
                ("insectTemplate", template.transform));

            return infestation;
        }

        /// <summary>Makes clicking an insect start the minigame. Separate, because it needs the player's inventory.</summary>
        public static void CreateInsectDefense(InsectVisuals visuals, InsectInfestation infestation, GameConfig config,
            MinigameRunner minigameRunner, MinigameDefinition minigame, Inventory inventory, ItemDefinition coin)
        {
            var defense = visuals.gameObject.AddComponent<InsectDefense>();
            SetReferences(defense,
                ("visuals", visuals),
                ("infestation", infestation),
                ("config", config),
                ("minigameRunner", minigameRunner),
                ("minigame", minigame),
                ("inventory", inventory),
                ("coin", coin));

            // Not placed in space: it rewards the player, wherever the insects were.
            SetReferences(AddSound<CoinRewardSound>(visuals.gameObject, volume: 0.8f, "Assets/Audio/Sounds/CoinReward.mp3"),
                ("defense", defense));
        }
    }
}
