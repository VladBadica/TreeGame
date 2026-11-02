using System;
using UnityEngine;

namespace TreeGame.Core
{
    /// <summary>Tuning for insects coming to the tree and damaging it, edited as part of <see cref="GameConfig"/>.</summary>
    [Serializable]
    public class InsectSettings
    {
        [Tooltip("Random wait between two insects arriving, in seconds (shortest, longest).")]
        [SerializeField] private Vector2 secondsBetweenInsects = new Vector2(45f, 90f);
        [Tooltip("No more insects arrive while this many are on the tree.")]
        [SerializeField, Min(0)] private int maxOnTree = 5;
        [Tooltip("Seconds an insect sits on the tree before it starts doing damage.")]
        [SerializeField, Min(0f)] private float graceSeconds = 20f;
        [Tooltip("Seconds it then spends doing damage, before it flies off.")]
        [SerializeField, Min(1f)] private float damageSeconds = 40f;
        [Tooltip("Health one insect takes in total if it is left alone for its whole stay.")]
        [SerializeField, Min(0f)] private float damagePerInsect = 5f;

        [Tooltip("How many insects come in the minigame, before adding those for the insects on the tree.")]
        [SerializeField, Min(0)] private int baseSwarmSize = 6;
        [Tooltip("How many more insects come in the minigame for each insect on the tree.")]
        [SerializeField, Min(0)] private int swarmSizePerInsect = 2;
        [Tooltip("The share of the swarm (0..1) the player must stop for a good score. A good score clears every insect on the tree.")]
        [SerializeField, Range(0f, 1f)] private float goodScore = 0.7f;
        [Tooltip("The share of the insects on the tree (0..1) that a bad score still clears, rounded up.")]
        [SerializeField, Range(0f, 1f)] private float shareClearedByBadScore = 0.5f;
        [Tooltip("Coins given for a good score. A bad score gives none.")]
        [SerializeField, Min(0)] private int coinsForGoodScore = 1;

        public int MaxOnTree => maxOnTree;

        public float GraceSeconds => graceSeconds;

        public float DamageSeconds => damageSeconds;

        public float DamagePerInsect => damagePerInsect;

        public float GoodScore => goodScore;

        public float ShareClearedByBadScore => shareClearedByBadScore;

        public int CoinsForGoodScore => coinsForGoodScore;

        public float RandomSecondsBetweenInsects() => RandomInterval.Pick(secondsBetweenInsects);

        /// <summary>How many insects come in the minigame when this many are on the tree. Always at least 1.</summary>
        public int SwarmSizeFor(int insectsOnTree) => Mathf.Max(1, baseSwarmSize + swarmSizePerInsect * insectsOnTree);
    }
}
