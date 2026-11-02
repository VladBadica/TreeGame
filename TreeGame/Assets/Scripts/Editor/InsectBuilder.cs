using TreeGame.Care;
using TreeGame.Core;
using TreeGame.Pests;
using TreeGame.Trees;
using UnityEngine;
using static TreeGame.EditorTools.BuilderHelpers;

namespace TreeGame.EditorTools
{
    /// <summary>Builds the insects that come to the tree: their state and spawner on the tree, and their visuals.</summary>
    internal static class InsectBuilder
    {
        public static InsectInfestation CreateInsects(GameObject tree, TreeResource health, GameConfig config, Sprite insectSprite)
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
            var template = new GameObject("Insect Template", typeof(SpriteRenderer));
            template.transform.SetParent(visuals.transform, false);
            template.GetComponent<SpriteRenderer>().sprite = insectSprite;
            template.SetActive(false);

            SetReferences(visuals.AddComponent<InsectVisuals>(),
                ("infestation", infestation),
                ("tree", tree.GetComponent<TreeVisuals>()),
                ("config", config),
                ("insectTemplate", template.transform));

            return infestation;
        }
    }
}
