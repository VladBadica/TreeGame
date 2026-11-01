using System.Collections.Generic;
using TreeGame.Core;
using TreeGame.Items;
using UnityEngine;

namespace TreeGame.Trees
{
    /// <summary>
    /// Drops leaves on the ground around the tree at random intervals once it has grown enough
    /// (settings in <see cref="GameConfig"/>), up to a maximum lying around at once. Each leaf is a copy of a
    /// pickup prefab, so how a leaf looks and what it gives is edited on the prefab, not here.
    /// </summary>
    public class FallenLeafSpawner : MonoBehaviour
    {
        [SerializeField] private TreeGrowth treeGrowth;
        [SerializeField] private GameConfig config;
        [SerializeField] private ItemPickup leafPrefab;
        [Tooltip("Where spawned leaves are parented. Kept outside the tree so its generated parts stay separate.")]
        [SerializeField] private Transform container;
        [Tooltip("Depth range (world Z) leaves land in: between the ground's front edge and just behind the trunk.")]
        [SerializeField] private Vector2 depthRange = new Vector2(-3f, 0.5f);
        [Tooltip("Leaves get a random tint between these, so they don't all look identical.")]
        [SerializeField] private Color lightTint = Color.white;
        [SerializeField] private Color darkTint = new Color(0.75f, 0.7f, 0.65f);

        private readonly List<ItemPickup> leavesOnGround = new List<ItemPickup>();
        // Counted down rather than compared with the game clock, so it stops while the world is frozen for a minigame.
        private float secondsUntilNextLeaf;

        private void OnEnable()
        {
            if (treeGrowth == null || config == null || leafPrefab == null)
            {
                Debug.LogWarning($"{nameof(FallenLeafSpawner)}: needs a {nameof(TreeGrowth)}, a {nameof(GameConfig)} and a leaf prefab.", this);
                enabled = false;
            }
        }

        private void Start()
        {
            ScheduleNextLeaf();
        }

        private void Update()
        {
            secondsUntilNextLeaf -= Time.deltaTime;
            if (secondsUntilNextLeaf > 0f)
            {
                return;
            }

            ScheduleNextLeaf();
            FallenLeafSettings settings = config.FallenLeaves;
            if (treeGrowth.Growth < settings.MinGrowth)
            {
                return;
            }

            // Collected leaves are destroyed; forget them so new ones can fall.
            leavesOnGround.RemoveAll(leaf => leaf == null);
            if (leavesOnGround.Count < settings.MaxOnGround)
            {
                leavesOnGround.Add(SpawnLeaf(settings.SpreadFromTrunk));
            }
        }

        private void ScheduleNextLeaf()
        {
            secondsUntilNextLeaf = config.FallenLeaves.RandomSecondsBetweenLeaves();
        }

        private ItemPickup SpawnLeaf(float spread)
        {
            Vector3 trunk = transform.position;
            var position = new Vector3(
                trunk.x + Random.Range(-spread, spread),
                trunk.y,
                Random.Range(depthRange.x, depthRange.y));
            // A slight random tilt so leaves look scattered rather than lined up.
            Quaternion rotation = Quaternion.Euler(0f, 0f, Random.Range(-20f, 20f));

            ItemPickup leaf = Instantiate(leafPrefab, position, rotation, container);
            if (leaf.TryGetComponent(out SpriteRenderer sprite))
            {
                sprite.color = Color.Lerp(lightTint, darkTint, Random.value);
                sprite.flipX = Random.value < 0.5f;
            }

            return leaf;
        }
    }
}
