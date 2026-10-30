using System;
using UnityEngine;

namespace TreeGame.Core
{
    /// <summary>Tuning for leaves dropping from the tree onto the ground, edited as part of <see cref="GameConfig"/>.</summary>
    [Serializable]
    public class FallenLeafSettings
    {
        [Tooltip("Tree growth (0..1) from which leaves start falling.")]
        [SerializeField, Range(0f, 1f)] private float minGrowth = 0.1f;
        [Tooltip("Random wait between two falling leaves, in seconds (shortest, longest).")]
        [SerializeField] private Vector2 secondsBetweenLeaves = new Vector2(4f, 10f);
        [Tooltip("No more leaves fall while this many are lying on the ground.")]
        [SerializeField, Min(0)] private int maxOnGround = 20;
        [Tooltip("How far to either side of the trunk leaves can land, in world units.")]
        [SerializeField, Min(0f)] private float spreadFromTrunk = 2.5f;

        public float MinGrowth => minGrowth;

        public int MaxOnGround => maxOnGround;

        public float SpreadFromTrunk => spreadFromTrunk;

        /// <summary>A random wait until the next leaf, tolerating a range entered the wrong way round.</summary>
        public float RandomSecondsBetweenLeaves()
        {
            float shortest = Mathf.Max(0.1f, Mathf.Min(secondsBetweenLeaves.x, secondsBetweenLeaves.y));
            float longest = Mathf.Max(shortest, Mathf.Max(secondsBetweenLeaves.x, secondsBetweenLeaves.y));
            return UnityEngine.Random.Range(shortest, longest);
        }
    }
}
