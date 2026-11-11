using System.Collections.Generic;
using TreeGame.Core;
using TreeGame.Items;
using TreeGame.Seasons;
using UnityEngine;

namespace TreeGame.Trees
{
    /// <summary>
    /// Drops leaves on the ground around the tree at random intervals once its foliage has grown enough
    /// (settings in <see cref="GameConfig"/>), up to a maximum lying around at once. They fall most in autumn and not at
    /// all in winter. Each leaf is a copy of a
    /// pickup prefab, so how a leaf looks and what it gives is edited on the prefab, not here.
    /// </summary>
    public class FallenLeafSpawner : MonoBehaviour, IAdvancesWithTime
    {
        [SerializeField] private TreeGrowth treeGrowth;
        [SerializeField] private GameConfig config;
        [Tooltip("Makes leaves fall more or less often with the season. Without one, they fall the same all year.")]
        [SerializeField] private SeasonClock seasons;
        [SerializeField] private ItemPickup leafPrefab;
        [Tooltip("Where spawned leaves are parented. Kept outside the tree so its generated parts stay separate.")]
        [SerializeField] private Transform container;
        [Tooltip("Depth range (world Z) leaves land in: between the ground's front edge and just behind the trunk.")]
        [SerializeField] private Vector2 depthRange = new Vector2(-3f, 0.5f);
        [Tooltip("Leaves get a random tint between these, so they don't all look identical.")]
        [SerializeField] private Color lightTint = Color.white;
        [SerializeField] private Color darkTint = new Color(0.75f, 0.7f, 0.65f);

        private readonly List<ItemPickup> leavesOnGround = new List<ItemPickup>();
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
            secondsUntilNextLeaf = config.FallenLeaves.RandomSecondsBetweenLeaves();
        }

        private void Update()
        {
            Advance(Time.deltaTime);
        }

        public void Advance(float seconds)
        {
            // The season makes the wait count down faster or slower; in winter it stands still, as the tree is bare.
            secondsUntilNextLeaf -= seconds * SeasonClock.MultiplierOf(seasons, SeasonEffect.LeafFall);

            // A loop, because catching up after a long time can mean several leaves are due at once.
            while (secondsUntilNextLeaf <= 0f)
            {
                DropLeaf();
                secondsUntilNextLeaf += config.FallenLeaves.RandomSecondsBetweenLeaves();
            }
        }

        private void DropLeaf()
        {
            FallenLeafSettings settings = config.FallenLeaves;
            if (treeGrowth.GetGrowth(TreeGrowthPart.Foliage) < settings.MinGrowth)
            {
                return;
            }

            // Collected leaves are destroyed; forget them so new ones can fall.
            leavesOnGround.RemoveAll(leaf => leaf == null);
            if (leavesOnGround.Count < settings.MaxOnGround)
            {
                leavesOnGround.Add(SpawnLeafAt(RandomLandingSpot(settings.SpreadFromTrunk)));
            }
        }

        /// <summary>Where each leaf lying on the ground is.</summary>
        public List<Vector3> GetLeafPositions()
        {
            leavesOnGround.RemoveAll(leaf => leaf == null);

            var positions = new List<Vector3>(leavesOnGround.Count);
            foreach (ItemPickup leaf in leavesOnGround)
            {
                positions.Add(leaf.transform.position);
            }

            return positions;
        }

        /// <summary>
        /// Replaces the leaves on the ground with leaves at these places, as when loading. Invalid places are skipped,
        /// and the usual maximum on the ground still applies.
        /// </summary>
        public void RestoreLeaves(IEnumerable<Vector3> positions)
        {
            foreach (ItemPickup leaf in leavesOnGround)
            {
                if (leaf != null)
                {
                    Destroy(leaf.gameObject);
                }
            }

            leavesOnGround.Clear();

            int maxOnGround = config.FallenLeaves.MaxOnGround;
            foreach (Vector3 position in positions)
            {
                if (leavesOnGround.Count >= maxOnGround)
                {
                    break;
                }

                if (float.IsFinite(position.x) && float.IsFinite(position.y) && float.IsFinite(position.z))
                {
                    leavesOnGround.Add(SpawnLeafAt(position));
                }
            }
        }

        private Vector3 RandomLandingSpot(float spread)
        {
            Vector3 trunk = transform.position;
            return new Vector3(
                trunk.x + Random.Range(-spread, spread),
                trunk.y,
                Random.Range(depthRange.x, depthRange.y));
        }

        private ItemPickup SpawnLeafAt(Vector3 position)
        {
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
