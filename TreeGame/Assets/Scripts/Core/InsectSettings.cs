using System;
using UnityEngine;

namespace TreeGame.Core
{
    /// <summary>Tuning for insects coming to the tree and damaging it, edited as part of <see cref="GameConfig"/>.</summary>
    [Serializable]
    public class InsectSettings
    {
        [Tooltip("Random wait between two insects arriving, in seconds (shortest, longest).")]
        [SerializeField] private Vector2 secondsBetweenInsects = new Vector2(5f, 10f);
        [Tooltip("No more insects arrive while this many are on the tree.")]
        [SerializeField, Min(0)] private int maxOnTree = 5;
        [Tooltip("Seconds an insect sits on the tree before it starts doing damage.")]
        [SerializeField, Min(0f)] private float graceSeconds = 20f;
        [Tooltip("Seconds it then spends doing damage, before it flies off.")]
        [SerializeField, Min(1f)] private float damageSeconds = 40f;
        [Tooltip("Health one insect takes in total if it is left alone for its whole stay.")]
        [SerializeField, Min(0f)] private float damagePerInsect = 5f;

        public int MaxOnTree => maxOnTree;

        public float GraceSeconds => graceSeconds;

        public float DamageSeconds => damageSeconds;

        public float DamagePerInsect => damagePerInsect;

        public float RandomSecondsBetweenInsects() => RandomInterval.Pick(secondsBetweenInsects);
    }
}
