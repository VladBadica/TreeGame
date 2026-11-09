using System;
using System.Collections.Generic;
using TreeGame.Care;
using TreeGame.Items;
using TreeGame.Trees;
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
        [Tooltip("Seconds of play time for a part of the tree to grow from 0% to 100% on its own at growth rate 5. " +
                 "Other rates scale it: 10 takes half as long, 1 five times as long.")]
        [SerializeField, Min(1f)] private float secondsToFullyGrow = 1800f;
        [Tooltip("Seconds of play time at growth rate 10 for a part's extra growth to fill from 0% to 100%. Only rates " +
                 "above 7 fill it, slower the closer to 7: 9 takes 1.5 times as long, 8 three times as long.")]
        [SerializeField, Min(1f)] private float secondsToFillExtraGrowth = 600f;
        [Tooltip("The rate the trunk starts a new game growing at, from 1 (slow) to 10 (fast).")]
        [SerializeField, Range(GrowthRates.Min, GrowthRates.Max)] private int trunkGrowthRate = GrowthRates.Middle;
        [Tooltip("The rate the branches start a new game growing at, from 1 (slow) to 10 (fast).")]
        [SerializeField, Range(GrowthRates.Min, GrowthRates.Max)] private int branchGrowthRate = GrowthRates.Middle;
        [Tooltip("The rate the foliage starts a new game growing at, from 1 (slow) to 10 (fast).")]
        [SerializeField, Range(GrowthRates.Min, GrowthRates.Max)] private int foliageGrowthRate = GrowthRates.Middle;

        [Header("Resources")]
        [Tooltip("Water is measured in liters, the same unit as a bucket, so pouring moves liters one to one.")]
        [SerializeField] private ResourceSettings water = new ResourceSettings(maxAmount: 200f, secondsToEmpty: 300f);
        [Tooltip("Nutrients are measured in grams, the same unit as compost, so feeding moves grams one to one.")]
        [SerializeField] private ResourceSettings nutrients = new ResourceSettings(maxAmount: 500f, secondsToEmpty: 300f);
        [Tooltip("Health is a plain 0 to 100 score. It doesn't drain on its own; only damage to the tree lowers it.")]
        [SerializeField] private ResourceSettings health = new ResourceSettings(maxAmount: 100f, secondsToEmpty: 0f);
        [Tooltip("Health only regenerates while water and nutrients are both above this share of their maximum (0..1).")]
        [SerializeField, Range(0f, 1f)] private float healthRegenMinResourceShare = 0.5f;
        [Tooltip("Seconds of play time for health to regenerate from 0 to full. Slower than insects do damage on average, " +
                 "so ignoring them still costs something.")]
        [SerializeField, Min(1f)] private float secondsToRegenFullHealth = 1800f;
        [Tooltip("Seconds for health to go from full to 0 with one of water or nutrients empty. With both empty it takes half as long.")]
        [SerializeField, Min(1f)] private float secondsToLoseFullHealthPerEmptyResource = 420f;

        [Header("Player")]
        [Tooltip("Walking speed in world units per second.")]
        [SerializeField, Min(0.1f)] private float playerMoveSpeed = 4f;

        [Header("Inventory")]
        [Tooltip("Number of inventory slots. The number keys 1-9 can reach at most 9.")]
        [SerializeField, Range(1, 9)] private int inventorySlotCount = 9;
        [Tooltip("Items the player starts with, placed into slots in this order. Containers start empty.")]
        [SerializeField] private ItemDefinition[] startingItems = new ItemDefinition[0];

        [Header("Well")]
        [Tooltip("How much water a perfect minigame at the well adds to a bucket, as a fraction of the bucket's capacity. " +
                 "Lower scores add proportionally less.")]
        [SerializeField, Range(0f, 1f)] private float wellFillPerUse = 0.1f;

        [Header("Composter")]
        [Tooltip("How long a batch of dropped-in leaves takes to become compost, in seconds.")]
        [SerializeField, Min(0f)] private float secondsToCompost = 20f;
        [Tooltip("Grams of compost made from each dropped-in leaf.")]
        [SerializeField, Min(0f)] private float compostGramsPerLeaf = 30f;

        [Header("Fallen Leaves")]
        [SerializeField] private FallenLeafSettings fallenLeaves = new FallenLeafSettings();

        [Header("Insects")]
        [SerializeField] private InsectSettings insects = new InsectSettings();

        [Header("Wind")]
        [Tooltip("Shared by everything the wind moves, so it all sways together.")]
        [SerializeField] private WindSettings wind = new WindSettings();

        /// <summary>Seconds of play time for a part to grow from 0% to 100% at the middle growth rate.</summary>
        public float SecondsToFullyGrow => secondsToFullyGrow;

        /// <summary>Seconds of play time for a part's extra growth to fill from 0% to 100% at the top growth rate.</summary>
        public float SecondsToFillExtraGrowth => secondsToFillExtraGrowth;

        /// <summary>The growth rate a part starts a new game at.</summary>
        public int StartingGrowthRate(TreeGrowthPart part)
        {
            int rate = part switch
            {
                TreeGrowthPart.Trunk => trunkGrowthRate,
                TreeGrowthPart.Branches => branchGrowthRate,
                TreeGrowthPart.Foliage => foliageGrowthRate,
                _ => throw new ArgumentOutOfRangeException(nameof(part), part, "Add a growth rate for this part to GameConfig."),
            };

            // The Inspector keeps rates between 1 and 10, but the asset file itself could still hold anything.
            return GrowthRates.Clamp(rate);
        }

        public float HealthRegenMinResourceShare => healthRegenMinResourceShare;

        public float SecondsToRegenFullHealth => secondsToRegenFullHealth;

        public float SecondsToLoseFullHealthPerEmptyResource => secondsToLoseFullHealthPerEmptyResource;

        public float PlayerMoveSpeed => playerMoveSpeed;

        public int InventorySlotCount => inventorySlotCount;

        public IReadOnlyList<ItemDefinition> StartingItems => startingItems;

        public float WellFillPerUse => wellFillPerUse;

        public float SecondsToCompost => secondsToCompost;

        public float CompostGramsPerLeaf => compostGramsPerLeaf;

        public FallenLeafSettings FallenLeaves => fallenLeaves;

        public InsectSettings Insects => insects;

        public WindSettings Wind => wind;

        public ResourceSettings GetResourceSettings(TreeResourceType type)
        {
            return type switch
            {
                TreeResourceType.Water => water,
                TreeResourceType.Nutrients => nutrients,
                TreeResourceType.Health => health,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Add settings for this resource type to GameConfig."),
            };
        }
    }
}
