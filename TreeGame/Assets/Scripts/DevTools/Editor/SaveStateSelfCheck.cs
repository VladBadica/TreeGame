using System.Collections.Generic;
using TreeGame.Care;
using TreeGame.Items;
using TreeGame.Pests;
using TreeGame.Player;
using TreeGame.Saving;
using TreeGame.Trees;
using TreeGame.World;
using UnityEditor;
using UnityEngine;

namespace TreeGame.DevTools
{
    /// <summary>
    /// Play Mode check that the game scene's systems can hand over their state and take it back. Puts everything back
    /// as it found it, except that restored leaves may look a little different.
    /// </summary>
    public static class SaveStateSelfCheck
    {
        private const string MenuPath = "Tree Game/Run Save State Self-Check (Play Mode)";

        [MenuItem(MenuPath, true)]
        private static bool CanRun() => Application.isPlaying;

        [MenuItem(MenuPath)]
        private static void Run()
        {
            var composter = Object.FindAnyObjectByType<Composter>();
            var insects = Object.FindAnyObjectByType<InsectInfestation>();
            var leaves = Object.FindAnyObjectByType<FallenLeafSpawner>();
            var player = Object.FindAnyObjectByType<PlayerMovement>();
            if (composter == null || insects == null || leaves == null || player == null)
            {
                Debug.LogError("Save state self-check: open the game scene first. It needs a composter, insects, a leaf spawner and a player.");
                return;
            }

            var failures = new List<string>();
            LogType originalFilter = Debug.unityLogger.filterLogType;
            try
            {
                // Silences the warnings that the deliberate invalid inputs below are expected to produce.
                Debug.unityLogger.filterLogType = LogType.Error;

                CheckComposter(failures, composter);
                CheckInsects(failures, insects);
                CheckInsectWait(failures);
                CheckOverflowPenalties(failures);
                CheckLeaves(failures, leaves);
                CheckPlayer(failures, player);
                CheckCapture(failures, composter, insects, leaves, player);
                CheckRestore(failures, composter, insects, leaves, player);
            }
            finally
            {
                Debug.unityLogger.filterLogType = originalFilter;
            }

            if (failures.Count == 0)
            {
                Debug.Log("Save state self-check PASSED.");
            }
            else
            {
                Debug.LogError("Save state self-check FAILED:\n- " + string.Join("\n- ", failures));
            }
        }

        private static void Expect(List<string> failures, bool condition, string failure)
        {
            if (!condition)
            {
                failures.Add(failure);
            }
        }

        /// <summary>Compares what would be saved with the game itself. It only reads, so nothing is written to disk.</summary>
        private static void CheckCapture(List<string> failures, Composter composter, InsectInfestation insects,
            FallenLeafSpawner leaves, PlayerMovement player)
        {
            var world = Object.FindAnyObjectByType<WorldState>();
            var growth = Object.FindAnyObjectByType<TreeGrowth>();
            var inventory = Object.FindAnyObjectByType<Inventory>();
            if (world == null || !world.IsComplete || growth == null || inventory == null)
            {
                failures.Add("the scene needs a complete world state, a tree and an inventory");
                return;
            }

            SaveData data = world.Capture();
            float FillOf(TreeResourceType type) =>
                System.Array.Find(Object.FindObjectsByType<TreeResource>(), resource => resource.Type == type).Normalized;

            Expect(failures, data.trunkGrowth == growth.GetGrowth(TreeGrowthPart.Trunk) &&
                             data.branchGrowth == growth.GetGrowth(TreeGrowthPart.Branches) &&
                             data.foliageGrowth == growth.GetGrowth(TreeGrowthPart.Foliage), "the captured growth differs from the tree's");
            var seed = Object.FindAnyObjectByType<TreeSeed>();
            Expect(failures, seed != null && data.treeSeed == seed.Seed, "the captured seed differs from the tree's");
            var name = Object.FindAnyObjectByType<TreeName>();
            Expect(failures, name != null && data.treeName == name.Name, "the captured name differs from the tree's");
            Expect(failures, data.waterFill == FillOf(TreeResourceType.Water), "the captured water differs from the tree's");
            Expect(failures, data.nutrientsFill == FillOf(TreeResourceType.Nutrients), "the captured nutrients differ from the tree's");
            Expect(failures, data.healthFill == FillOf(TreeResourceType.Health), "the captured health differs from the tree's");
            Expect(failures, data.playerX == player.transform.position.x, "the captured player position differs from the player's");
            Expect(failures, data.composter.state == (int)composter.State && data.composter.compostGrams == composter.CompostGrams, "the captured composter differs from the heap");
            Expect(failures, data.insectAges.Length == insects.Count, $"captured {data.insectAges.Length} insects, but there are {insects.Count}");
            Expect(failures, data.fallenLeaves.Length == leaves.GetLeafPositions().Count, "the captured leaves differ from those on the ground");

            Expect(failures, data.inventorySlots.Length == inventory.SlotCount, $"captured {data.inventorySlots.Length} slots, but there are {inventory.SlotCount}");
            for (int i = 0; i < data.inventorySlots.Length && i < inventory.SlotCount; i++)
            {
                SavedSlot slot = data.inventorySlots[i];
                Item item = inventory.GetItem(i);
                Expect(failures, slot.itemName == (item != null ? item.Definition.name : ""), $"slot {i} captured the wrong item: '{slot.itemName}'");

                if (item is StackableItem stack)
                {
                    Expect(failures, slot.count == stack.Count, $"slot {i} captured {slot.count} of a stack of {stack.Count}");
                }

                if (item is ContainerItem container)
                {
                    Expect(failures, slot.storedAmount == container.StoredAmount, $"slot {i} captured {slot.storedAmount} of {container.StoredAmount} stored");
                }
            }
        }

        /// <summary>
        /// Restores a different state and then the original one, in memory only, so the real save isn't touched. Going
        /// back must give exactly what was captured, which also proves every item in the game can be found again.
        /// </summary>
        private static void CheckRestore(List<string> failures, Composter composter, InsectInfestation insects,
            FallenLeafSpawner leaves, PlayerMovement player)
        {
            var world = Object.FindAnyObjectByType<WorldState>();
            var growth = Object.FindAnyObjectByType<TreeGrowth>();
            var inventory = Object.FindAnyObjectByType<Inventory>();
            if (world == null || !world.IsComplete || growth == null || inventory == null)
            {
                failures.Add("the scene needs a complete world state, a tree and an inventory");
                return;
            }

            SaveData original = world.Capture();

            // Every slot empty except a bucket in a slot of its own and an item the game doesn't know.
            string bucketName = FindItemNameOfType<ContainerItem>(original);
            var slots = new SavedSlot[inventory.SlotCount];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = new SavedSlot();
            }

            if (bucketName != null && slots.Length > 4)
            {
                slots[3] = new SavedSlot { itemName = bucketName, storedAmount = 5f };
                slots[4] = new SavedSlot { itemName = "No Such Item", count = 3 };
            }

            world.Restore(new SaveData
            {
                treeSeed = original.treeSeed + 1,
                treeName = "  Restored <i>Oak</i>  ",
                trunkGrowthRate = 9,
                branchGrowthRate = 0,
                foliageGrowthRate = 42,
                trunkExtraGrowth = 0.4f,
                branchExtraGrowth = float.NaN,
                foliageExtraGrowth = 3f,
                trunkGrowth = 0.5f,
                branchGrowth = 0.25f,
                foliageGrowth = 0.75f,
                waterFill = 0.25f,
                nutrientsFill = 0.75f,
                healthFill = 0.125f,
                overwateredSecondsLeft = 8f,
                overfedSecondsLeft = 3f,
                playerX = 0.5f,
                inventorySlots = slots,
                composter = new SavedComposter { state = (int)CompostState.Done, workSeconds = 30f, workDuration = 30f, compostGrams = 40f },
                insectAges = new[] { 12f },
                secondsUntilNextInsect = 12.5f,
                fallenLeaves = new[] { new SavedLeaf { x = 2f, y = 0f, z = -1f } },
            });

            Expect(failures, growth.GetGrowth(TreeGrowthPart.Trunk) == 0.5f && growth.GetGrowth(TreeGrowthPart.Branches) == 0.25f &&
                             growth.GetGrowth(TreeGrowthPart.Foliage) == 0.75f, "each part of the tree's growth should be restored separately");
            Expect(failures, player.transform.position.x == 0.5f, $"player restored at {player.transform.position.x}");
            Expect(failures, composter.State == CompostState.Done && composter.CompostGrams == 40f, "the composter wasn't restored");
            Expect(failures, insects.Count == 1 && insects.InsectAges[0] == 12f, "the insects weren't restored");
            Expect(failures, leaves.GetLeafPositions().Count == 1, "the leaves weren't restored");

            SaveData changed = world.Capture();
            Expect(failures, changed.treeSeed == unchecked(original.treeSeed + 1), $"the tree's seed restored as {changed.treeSeed}");
            Expect(failures, changed.treeName == "Restored iOak/i", $"a saved name should be restored cleaned, not as '{changed.treeName}'");
            Expect(failures, changed.trunkGrowthRate == 9, $"a saved rate of 9 restored as {changed.trunkGrowthRate}");
            Expect(failures, changed.branchGrowthRate == original.branchGrowthRate, "a rate saved as 0 (not saved) should leave the part's rate as it was");
            Expect(failures, changed.foliageGrowthRate == 10, $"a saved rate of 42 should be restored as 10, not {changed.foliageGrowthRate}");
            Expect(failures, Mathf.Approximately(changed.trunkExtraGrowth, 0.4f), $"extra growth of 0.4 restored as {changed.trunkExtraGrowth}");
            Expect(failures, changed.branchExtraGrowth == original.branchExtraGrowth, "a NaN extra growth should leave the part's extra growth as it was");
            Expect(failures, changed.foliageExtraGrowth == 1f, $"extra growth of 3 should be restored as 1, not {changed.foliageExtraGrowth}");
            Expect(failures, changed.overwateredSecondsLeft == 8f, $"8s left overwatered restored as {changed.overwateredSecondsLeft}");
            Expect(failures, changed.overfedSecondsLeft == 3f, $"3s left overfed restored as {changed.overfedSecondsLeft}");
            Expect(failures, changed.secondsUntilNextInsect == 12.5f, $"a wait of 12.5s for the next insect restored as {changed.secondsUntilNextInsect}");
            Expect(failures, Mathf.Approximately(changed.waterFill, 0.25f), $"water restored as {changed.waterFill}");
            Expect(failures, Mathf.Approximately(changed.nutrientsFill, 0.75f), $"nutrients restored as {changed.nutrientsFill}");
            Expect(failures, Mathf.Approximately(changed.healthFill, 0.125f), $"health restored as {changed.healthFill}");

            if (bucketName != null && slots.Length > 4)
            {
                Expect(failures, inventory.GetItem(0) == null, "a slot saved empty should be empty");
                Expect(failures, inventory.GetItem(3) is ContainerItem { StoredAmount: 5f }, "a bucket should come back in its slot with its amount");
                Expect(failures, inventory.GetItem(4) == null, "an unknown item should leave its slot empty");
            }

            world.Restore(original);
            SaveData restored = world.Capture();
            Expect(failures, restored.treeSeed == original.treeSeed, "the tree's seed didn't go back");
            Expect(failures, restored.treeName == original.treeName, "the tree's name didn't go back");
            Expect(failures, restored.trunkGrowthRate == original.trunkGrowthRate && restored.foliageGrowthRate == original.foliageGrowthRate,
                "the growth rates didn't go back");
            Expect(failures, restored.trunkExtraGrowth == original.trunkExtraGrowth && restored.foliageExtraGrowth == original.foliageExtraGrowth,
                "the extra growth didn't go back");

            // The resources may round by a tiny amount, as they are stored as shares of their maximum.
            Expect(failures, Mathf.Approximately(restored.waterFill, original.waterFill), "water didn't go back");
            Expect(failures, Mathf.Approximately(restored.nutrientsFill, original.nutrientsFill), "nutrients didn't go back");
            Expect(failures, Mathf.Approximately(restored.healthFill, original.healthFill), "health didn't go back");
            restored.waterFill = original.waterFill;
            restored.nutrientsFill = original.nutrientsFill;
            restored.healthFill = original.healthFill;

            string originalText = JsonUtility.ToJson(original);
            string restoredText = JsonUtility.ToJson(restored);
            Expect(failures, originalText == restoredText, $"restoring didn't give back what was captured:\n{originalText}\n{restoredText}");
        }

        private static string FindItemNameOfType<T>(SaveData data) where T : Item
        {
            var inventory = Object.FindAnyObjectByType<Inventory>();
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                if (inventory.GetItem(i) is T)
                {
                    return data.inventorySlots[i].itemName;
                }
            }

            return null;
        }

        private static void CheckComposter(List<string> failures, Composter composter)
        {
            CompostState originalState = composter.State;
            float originalSeconds = composter.WorkSeconds;
            float originalDuration = composter.WorkDuration;
            float originalGrams = composter.CompostGrams;

            composter.RestoreState(CompostState.Working, 15f, 30f, 200f);
            Expect(failures, composter.State == CompostState.Working, $"restoring a working heap gave {composter.State}");
            Expect(failures, composter.WorkSeconds == 15f && composter.WorkDuration == 30f && composter.CompostGrams == 200f, "a working heap's amounts changed");
            Expect(failures, Mathf.Approximately(composter.Progress, 0.5f), $"a heap 15s into 30s should be halfway, got {composter.Progress}");

            composter.Advance(14f);
            Expect(failures, composter.State == CompostState.Working, "a restored heap finished early");
            composter.Advance(2f);
            Expect(failures, composter.State == CompostState.Done, "a restored heap should finish after its remaining time");

            composter.RestoreState(CompostState.Done, 30f, 30f, 200f);
            Expect(failures, composter.State == CompostState.Done && composter.CompostGrams == 200f, "restoring a finished heap changed it");
            Expect(failures, composter.Progress == 1f, "a finished heap should show as full");

            composter.RestoreState(CompostState.Idle, 15f, 30f, 200f);
            Expect(failures, composter.State == CompostState.Idle && composter.CompostGrams == 0f, "an idle heap should be empty, whatever else was saved");

            void ExpectIdle(string description, CompostState state, float seconds, float duration, float grams)
            {
                composter.RestoreState(state, seconds, duration, grams);
                Expect(failures, composter.State == CompostState.Idle && composter.CompostGrams == 0f, $"{description} should leave the heap idle and empty");
            }

            ExpectIdle("an unknown state", (CompostState)99, 1f, 30f, 200f);
            ExpectIdle("working with no compost", CompostState.Working, 1f, 30f, 0f);
            ExpectIdle("done with negative compost", CompostState.Done, 30f, 30f, -5f);
            ExpectIdle("NaN compost", CompostState.Working, 1f, 30f, float.NaN);
            ExpectIdle("infinite compost", CompostState.Done, 1f, 30f, float.PositiveInfinity);
            ExpectIdle("NaN time worked", CompostState.Working, float.NaN, 30f, 200f);
            ExpectIdle("NaN duration", CompostState.Working, 1f, float.NaN, 200f);

            composter.RestoreState(CompostState.Working, -10f, 30f, 200f);
            Expect(failures, composter.WorkSeconds == 0f, $"negative time worked should count as none, got {composter.WorkSeconds}");

            composter.RestoreState(originalState, originalSeconds, originalDuration, originalGrams);
        }

        private static void CheckInsects(List<string> failures, InsectInfestation insects)
        {
            var original = new List<float>(insects.InsectAges);

            int reportedCount = -1;
            void OnCountChanged(int count) => reportedCount = count;
            insects.CountChanged += OnCountChanged;

            insects.RestoreInsects(new float[0]);
            reportedCount = -1;
            insects.RestoreInsects(new[] { 25f, 10f });
            Expect(failures, reportedCount == 2, $"restoring 2 insects should announce 2, announced {reportedCount}");
            Expect(failures, insects.Count == 2 && insects.InsectAges[0] == 25f && insects.InsectAges[1] == 10f, "restored insects should keep their ages");

            insects.CountChanged -= OnCountChanged;
            insects.RestoreInsects(original);
        }

        private static void CheckInsectWait(List<string> failures)
        {
            var spawner = Object.FindAnyObjectByType<InsectSpawner>();
            if (spawner == null)
            {
                failures.Add("the scene needs an insect spawner");
                return;
            }

            float original = spawner.SecondsUntilNextInsect;

            spawner.RestoreSecondsUntilNextInsect(20f);
            Expect(failures, spawner.SecondsUntilNextInsect == 20f, $"a saved wait of 20s restored as {spawner.SecondsUntilNextInsect}");

            spawner.RestoreSecondsUntilNextInsect(-1f);
            Expect(failures, spawner.SecondsUntilNextInsect > 0f, "an unsaved wait should be replaced by a fresh one");

            spawner.RestoreSecondsUntilNextInsect(float.NaN);
            Expect(failures, float.IsFinite(spawner.SecondsUntilNextInsect) && spawner.SecondsUntilNextInsect > 0f, "a NaN wait should be replaced by a fresh one");

            spawner.RestoreSecondsUntilNextInsect(1000000f);
            Expect(failures, spawner.SecondsUntilNextInsect < 1000000f, "a wait longer than the longest allowed should be cut");

            spawner.RestoreSecondsUntilNextInsect(original);
        }

        /// <summary>
        /// The overwatering and overfeeding rules, how restoring them changes growth rates, that the two add up, and that
        /// everything goes back. Expects the default settings: 10% allowed, then 10s plus 2s per percent past it.
        /// </summary>
        private static void CheckOverflowPenalties(List<string> failures)
        {
            OverflowPenalty[] penalties = Object.FindObjectsByType<OverflowPenalty>();
            OverflowPenalty overwatering = System.Array.Find(penalties, penalty => penalty.ResourceType == TreeResourceType.Water);
            OverflowPenalty overfeeding = System.Array.Find(penalties, penalty => penalty.ResourceType == TreeResourceType.Nutrients);
            var rates = Object.FindAnyObjectByType<TreeGrowthRates>();
            TreeResource[] resources = Object.FindObjectsByType<TreeResource>();
            if (overwatering == null || overfeeding == null || rates == null)
            {
                failures.Add("the scene needs overwatering, overfeeding and growth rates");
                return;
            }

            foreach (OverflowPenalty penalty in new[] { overwatering, overfeeding })
            {
                float max = System.Array.Find(resources, resource => resource.Type == penalty.ResourceType).MaxAmount;
                string name = penalty.ResourceType.ToString();

                // 10% of the maximum is allowed; 15% is 5 points past it: 10s + 5 x 2s = 20s.
                Expect(failures, penalty.SecondsFor(max * 0.1f) == 0f, $"{name}: overflowing exactly 10% shouldn't bring a penalty");
                Expect(failures, Mathf.Approximately(penalty.SecondsFor(max * 0.15f), 20f), $"{name}: overflowing 15% should last 20s, not {penalty.SecondsFor(max * 0.15f)}");
                Expect(failures, Mathf.Approximately(penalty.SecondsFor(max * 0.3f), 50f), $"{name}: overflowing 30% should last 50s, not {penalty.SecondsFor(max * 0.3f)}");
                Expect(failures, penalty.SecondsFor(float.NaN) == 0f && penalty.SecondsFor(-5f) == 0f, $"{name}: an invalid overflow shouldn't bring a penalty");
            }

            float originalWater = overwatering.SecondsLeft;
            float originalNutrients = overfeeding.SecondsLeft;
            overwatering.RestoreSecondsLeft(0f);
            overfeeding.RestoreSecondsLeft(0f);
            int noPenalty = rates.TemporaryChange;

            overwatering.RestoreSecondsLeft(5f);
            int oneDown = rates.TemporaryChange;
            Expect(failures, overwatering.IsActive && oneDown < noPenalty, "a restored overwatering should slow growth");
            overfeeding.RestoreSecondsLeft(10f);
            Expect(failures, rates.TemporaryChange < oneDown, "overfeeding as well should slow growth further");

            overwatering.Advance(6f);
            Expect(failures, !overwatering.IsActive && rates.TemporaryChange == oneDown, "overfeeding should still count once overwatering ends");
            overfeeding.Advance(5f);
            Expect(failures, !overfeeding.IsActive && rates.TemporaryChange == noPenalty, "growth should recover once both are over");

            overwatering.RestoreSecondsLeft(float.NaN);
            Expect(failures, !overwatering.IsActive, "a NaN time shouldn't bring a penalty");

            // An empty resource slows growth by 2, both empty by 4; filling them again lifts it.
            TreeResource water = System.Array.Find(resources, resource => resource.Type == TreeResourceType.Water);
            TreeResource nutrients = System.Array.Find(resources, resource => resource.Type == TreeResourceType.Nutrients);
            float originalWaterAmount = water.Amount;
            float originalNutrientsAmount = nutrients.Amount;
            water.SetAmount(water.MaxAmount);
            nutrients.SetAmount(nutrients.MaxAmount);
            int fed = rates.TemporaryChange;
            water.SetAmount(0f);
            Expect(failures, rates.TemporaryChange == fed - 2, $"no water should slow growth by 2, changed it by {rates.TemporaryChange - fed}");
            nutrients.SetAmount(0f);
            Expect(failures, rates.TemporaryChange == fed - 4, $"no water and no nutrients should slow growth by 4, changed it by {rates.TemporaryChange - fed}");
            water.SetAmount(1f);
            nutrients.SetAmount(1f);
            Expect(failures, rates.TemporaryChange == fed, "watering and feeding should lift the slowdown");
            water.SetAmount(originalWaterAmount);
            nutrients.SetAmount(originalNutrientsAmount);

            overwatering.RestoreSecondsLeft(originalWater);
            overfeeding.RestoreSecondsLeft(originalNutrients);
        }

        private static void CheckLeaves(List<string> failures, FallenLeafSpawner leaves)
        {
            List<Vector3> original = leaves.GetLeafPositions();

            var places = new List<Vector3> { new Vector3(1f, 0f, -1f), new Vector3(-2f, 0f, 0.25f) };
            leaves.RestoreLeaves(places);
            List<Vector3> read = leaves.GetLeafPositions();
            Expect(failures, read.Count == 2, $"restoring 2 leaves left {read.Count}");
            if (read.Count == 2)
            {
                Expect(failures, read[0] == places[0] && read[1] == places[1], "restored leaves should be where they were saved");
            }

            leaves.RestoreLeaves(new[] { new Vector3(float.NaN, 0f, 0f), new Vector3(0f, float.PositiveInfinity, 0f), new Vector3(3f, 0f, 0f) });
            read = leaves.GetLeafPositions();
            Expect(failures, read.Count == 1 && read[0].x == 3f, $"invalid leaf places should be skipped, left {read.Count} leaves");

            var tooMany = new List<Vector3>();
            for (int i = 0; i < 1000; i++)
            {
                tooMany.Add(new Vector3(i * 0.001f, 0f, 0f));
            }

            leaves.RestoreLeaves(tooMany);
            Expect(failures, leaves.GetLeafPositions().Count < tooMany.Count, "restoring leaves should respect the maximum on the ground");

            leaves.RestoreLeaves(original);
            Expect(failures, leaves.GetLeafPositions().Count == original.Count, "the original leaves weren't put back");
        }

        private static void CheckPlayer(List<string> failures, PlayerMovement player)
        {
            Vector3 original = player.transform.position;

            player.PlaceAt(1.25f);
            Expect(failures, player.transform.position.x == 1.25f, $"placing the player at 1.25 gave {player.transform.position.x}");
            Expect(failures, player.transform.position.y == original.y && player.transform.position.z == original.z, "placing the player should only change X");

            player.PlaceAt(100000f);
            Expect(failures, player.transform.position.x < 1000f, $"the player should stay within the walkable area, got {player.transform.position.x}");
            player.PlaceAt(-100000f);
            Expect(failures, player.transform.position.x > -1000f, $"the player should stay within the walkable area, got {player.transform.position.x}");

            player.PlaceAt(1.25f);
            player.PlaceAt(float.NaN);
            player.PlaceAt(float.PositiveInfinity);
            Expect(failures, player.transform.position.x == 1.25f, "an invalid position should be ignored");

            player.transform.position = original;
        }
    }
}
