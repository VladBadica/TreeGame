using TreeGame.Core;
using UnityEngine;

namespace TreeGame.Trees
{
    /// <summary>
    /// Grows every part of the tree steadily over time, each at its current growth rate (see
    /// <see cref="TreeGrowthRates"/>), so it keeps growing without player input. A part with a rate above 7 also fills
    /// its extra growth, from the start rather than only once it is fully grown, and keeps it. Uses only the public APIs
    /// of the growth, rate and extra growth components; later rules (e.g. growth needing water) can gate or replace
    /// this component. Disable the component to pause passive growth.
    /// </summary>
    public class PassiveGrowth : MonoBehaviour, IAdvancesWithTime
    {
        [SerializeField] private TreeGrowth treeGrowth;
        [SerializeField] private TreeGrowthRates growthRates;
        [SerializeField] private TreeExtraGrowth extraGrowth;
        [SerializeField] private GameConfig config;

        private void OnEnable()
        {
            if (treeGrowth == null || growthRates == null || extraGrowth == null || config == null)
            {
                Debug.LogWarning($"{nameof(PassiveGrowth)}: needs a {nameof(TreeGrowth)}, {nameof(TreeGrowthRates)}, " +
                                 $"{nameof(TreeExtraGrowth)} and {nameof(GameConfig)}.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            // Scaled time, so pausing via Time.timeScale also pauses growth.
            Advance(Time.deltaTime);
        }

        public void Advance(float seconds)
        {
            // Called directly after a minigame too, so it must hold up to what it is given.
            if (!enabled || !(seconds > 0f) || !float.IsFinite(seconds))
            {
                return;
            }

            foreach (TreeGrowthPart part in TreeGrowth.Parts)
            {
                // The rate at this moment, including temporary changes such as overwatering: a change takes effect from
                // the next frame, so time is never counted twice.
                int rate = growthRates.GetEffectiveRate(part);
                treeGrowth.AddGrowth(part, seconds * GrowthRates.GrowthPerSecond(rate, config.SecondsToFullyGrow));
                extraGrowth.AddExtraGrowth(part, seconds * GrowthRates.ExtraGrowthPerSecond(rate, config.SecondsToFillExtraGrowth));
            }
        }
    }
}
