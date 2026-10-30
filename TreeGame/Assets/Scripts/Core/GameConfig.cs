using System;
using System.Collections.Generic;
using TreeGame.Care;
using TreeGame.Items;
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
        [Tooltip("Water is measured in liters, the same unit as a bucket, so pouring moves liters one to one.")]
        [SerializeField] private ResourceSettings water = new ResourceSettings(maxAmount: 200f, secondsToEmpty: 120f);
        [SerializeField] private ResourceSettings nutrients = new ResourceSettings(maxAmount: 100f, secondsToEmpty: 240f);

        [Header("Player")]
        [Tooltip("Walking speed in world units per second.")]
        [SerializeField, Min(0.1f)] private float playerMoveSpeed = 4f;

        [Header("Inventory")]
        [Tooltip("Number of inventory slots. The number keys 1-9 can reach at most 9.")]
        [SerializeField, Range(1, 9)] private int inventorySlotCount = 9;
        [Tooltip("Items the player starts with, placed into slots in this order. Containers start empty.")]
        [SerializeField] private ItemDefinition[] startingItems = new ItemDefinition[0];

        [Header("Well")]
        [Tooltip("How much water one use of the well adds to a bucket, as a fraction of the bucket's capacity.")]
        [SerializeField, Range(0f, 1f)] private float wellFillPerUse = 0.1f;

        [Header("Fallen Leaves")]
        [SerializeField] private FallenLeafSettings fallenLeaves = new FallenLeafSettings();

        [Header("Wind")]
        [Tooltip("Shared by everything the wind moves, so it all sways together.")]
        [SerializeField] private WindSettings wind = new WindSettings();

        public float SecondsToFullyGrow => secondsToFullyGrow;

        public float PlayerMoveSpeed => playerMoveSpeed;

        public int InventorySlotCount => inventorySlotCount;

        public IReadOnlyList<ItemDefinition> StartingItems => startingItems;

        public float WellFillPerUse => wellFillPerUse;

        public FallenLeafSettings FallenLeaves => fallenLeaves;

        public WindSettings Wind => wind;

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
