using System;
using System.Collections.Generic;
using TreeGame.Care;
using TreeGame.Items;
using TreeGame.Pests;
using TreeGame.Player;
using TreeGame.Trees;
using TreeGame.World;
using UnityEngine;

namespace TreeGame.Saving
{
    /// <summary>
    /// Connects the game's systems to the save format at this level, so none of them knows about saving and new state
    /// only has to be added here. It works with everything switched off too, as when the game closes during a minigame.
    /// </summary>
    public class WorldState : MonoBehaviour
    {
        [SerializeField] private TreeGrowth treeGrowth;
        [SerializeField] private TreeSeed treeSeed;
        [SerializeField] private TreeName treeName;
        [SerializeField] private TreeGrowthRates growthRates;
        [SerializeField] private TreeExtraGrowth extraGrowth;
        [SerializeField] private TreeResource water;
        [SerializeField] private TreeResource nutrients;
        [SerializeField] private TreeResource health;
        [SerializeField] private PlayerMovement player;
        [SerializeField] private Inventory inventory;
        [Tooltip("Every item that can be in the inventory. A save names its items, so this is how they are found again.")]
        [SerializeField] private ItemDefinition[] knownItems = new ItemDefinition[0];
        [SerializeField] private Composter composter;
        [SerializeField] private InsectInfestation insects;
        [SerializeField] private InsectSpawner insectSpawner;
        [SerializeField] private FallenLeafSpawner leaves;

        /// <summary>True if everything it reads is assigned.</summary>
        public bool IsComplete =>
            treeGrowth != null && treeSeed != null && treeName != null && growthRates != null && extraGrowth != null &&
            water != null && nutrients != null && health != null && player != null &&
            inventory != null && composter != null && insects != null && insectSpawner != null && leaves != null;

        /// <summary>The current state of the game, ready to write to a save.</summary>
        public SaveData Capture()
        {
            return new SaveData
            {
                treeSeed = treeSeed.Seed,
                treeName = treeName.Name,
                trunkGrowth = treeGrowth.GetGrowth(TreeGrowthPart.Trunk),
                branchGrowth = treeGrowth.GetGrowth(TreeGrowthPart.Branches),
                foliageGrowth = treeGrowth.GetGrowth(TreeGrowthPart.Foliage),
                trunkGrowthRate = growthRates.GetRate(TreeGrowthPart.Trunk),
                branchGrowthRate = growthRates.GetRate(TreeGrowthPart.Branches),
                foliageGrowthRate = growthRates.GetRate(TreeGrowthPart.Foliage),
                trunkExtraGrowth = extraGrowth.GetExtraGrowth(TreeGrowthPart.Trunk),
                branchExtraGrowth = extraGrowth.GetExtraGrowth(TreeGrowthPart.Branches),
                foliageExtraGrowth = extraGrowth.GetExtraGrowth(TreeGrowthPart.Foliage),
                waterFill = water.Normalized,
                nutrientsFill = nutrients.Normalized,
                healthFill = health.Normalized,
                playerX = player.transform.position.x,
                inventorySlots = CaptureInventory(),
                composter = new SavedComposter
                {
                    state = (int)composter.State,
                    workSeconds = composter.WorkSeconds,
                    workDuration = composter.WorkDuration,
                    compostGrams = composter.CompostGrams,
                },
                insectAges = new List<float>(insects.InsectAges).ToArray(),
                secondsUntilNextInsect = insectSpawner.SecondsUntilNextInsect,
                fallenLeaves = CaptureLeaves(),
            };
        }

        /// <summary>
        /// Puts the game into a saved state, replacing what's there. Each system checks what it is given, so a damaged
        /// value only affects that value.
        /// </summary>
        public void Restore(SaveData data)
        {
            treeSeed.SetSeed(data.treeSeed);
            treeName.SetName(data.treeName);
            treeGrowth.SetGrowth(TreeGrowthPart.Trunk, data.trunkGrowth);
            treeGrowth.SetGrowth(TreeGrowthPart.Branches, data.branchGrowth);
            treeGrowth.SetGrowth(TreeGrowthPart.Foliage, data.foliageGrowth);
            RestoreRate(TreeGrowthPart.Trunk, data.trunkGrowthRate);
            RestoreRate(TreeGrowthPart.Branches, data.branchGrowthRate);
            RestoreRate(TreeGrowthPart.Foliage, data.foliageGrowthRate);
            extraGrowth.SetExtraGrowth(TreeGrowthPart.Trunk, data.trunkExtraGrowth);
            extraGrowth.SetExtraGrowth(TreeGrowthPart.Branches, data.branchExtraGrowth);
            extraGrowth.SetExtraGrowth(TreeGrowthPart.Foliage, data.foliageExtraGrowth);
            water.SetAmount(data.waterFill * water.MaxAmount);
            nutrients.SetAmount(data.nutrientsFill * nutrients.MaxAmount);
            health.SetAmount(data.healthFill * health.MaxAmount);
            player.PlaceAt(data.playerX);
            RestoreInventory(data.inventorySlots);
            composter.RestoreState((CompostState)data.composter.state, data.composter.workSeconds,
                data.composter.workDuration, data.composter.compostGrams);
            insects.RestoreInsects(data.insectAges);
            insectSpawner.RestoreSecondsUntilNextInsect(data.secondsUntilNextInsect);
            RestoreLeaves(data.fallenLeaves);
        }

        /// <summary>A saved rate of 0 means none was saved: the part keeps the rate it starts a game at.</summary>
        private void RestoreRate(TreeGrowthPart part, int savedRate)
        {
            if (savedRate != 0)
            {
                growthRates.SetRate(part, savedRate);
            }
        }

        private void RestoreInventory(SavedSlot[] slots)
        {
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                inventory.SetItem(i, i < slots.Length ? CreateItem(slots[i]) : null);
            }
        }

        /// <summary>The item a slot held, or null if it held none or the item can't be found any more.</summary>
        private Item CreateItem(SavedSlot slot)
        {
            if (string.IsNullOrEmpty(slot.itemName))
            {
                return null;
            }

            ItemDefinition definition = Array.Find(knownItems, known => known != null && known.name == slot.itemName);
            if (definition == null)
            {
                Debug.LogWarning($"{nameof(WorldState)}: the save has an unknown item '{slot.itemName}', so its slot stays empty. Is it in the known items?", this);
                return null;
            }

            Item item = definition.CreateItem();
            if (item is StackableItem stack)
            {
                // A stack of nothing isn't a stack, and setting its count couldn't express that.
                if (slot.count <= 0)
                {
                    return null;
                }

                stack.SetCount(slot.count);
            }
            else if (item is ContainerItem container)
            {
                container.SetStoredAmount(slot.storedAmount);
            }

            return item;
        }

        private void RestoreLeaves(SavedLeaf[] saved)
        {
            var positions = new List<Vector3>(saved.Length);
            foreach (SavedLeaf leaf in saved)
            {
                positions.Add(new Vector3(leaf.x, leaf.y, leaf.z));
            }

            leaves.RestoreLeaves(positions);
        }

        private SavedSlot[] CaptureInventory()
        {
            var slots = new SavedSlot[inventory.SlotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                Item item = inventory.GetItem(i);
                slots[i] = new SavedSlot
                {
                    itemName = item != null ? item.Definition.name : "",
                    count = item is StackableItem stack ? stack.Count : 0,
                    storedAmount = item is ContainerItem container ? container.StoredAmount : 0f,
                };
            }

            return slots;
        }

        private SavedLeaf[] CaptureLeaves()
        {
            List<Vector3> positions = leaves.GetLeafPositions();
            var saved = new SavedLeaf[positions.Count];
            for (int i = 0; i < saved.Length; i++)
            {
                saved[i] = new SavedLeaf { x = positions[i].x, y = positions[i].y, z = positions[i].z };
            }

            return saved;
        }
    }
}
