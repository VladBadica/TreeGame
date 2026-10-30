using System;
using TreeGame.Care;
using UnityEngine;

namespace TreeGame.Core
{
    /// <summary>
    /// Central gameplay tuning, stored as an asset so it can be edited in the Inspector without touching code.
    /// Components receive it through a serialized reference (no global access), so alternative configs
    /// (e.g. a fast one for testing) can be swapped in.
    /// Note: unlike scene objects, edits to this asset made during Play Mode are kept afterwards.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Tree Game/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Growth")]
        [Tooltip("Seconds of play time for the tree to grow from 0% to 100% on its own.")]
        [SerializeField, Min(1f)] private float secondsToFullyGrow = 300f;

        [Header("Resources")]
        [SerializeField] private ResourceSettings water = new ResourceSettings(maxAmount: 100f, secondsToEmpty: 120f);
        [SerializeField] private ResourceSettings nutrients = new ResourceSettings(maxAmount: 100f, secondsToEmpty: 240f);

        [Header("Player")]
        [Tooltip("Walking speed in world units per second.")]
        [SerializeField, Min(0.1f)] private float playerMoveSpeed = 4f;

        public float SecondsToFullyGrow => secondsToFullyGrow;

        public float PlayerMoveSpeed => playerMoveSpeed;

        public ResourceSettings GetResourceSettings(TreeResourceType type)
        {
            return type switch
            {
                TreeResourceType.Water => water,
                TreeResourceType.Nutrients => nutrients,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Add settings for this resource type to GameConfig."),
            };
        }
    }
}
