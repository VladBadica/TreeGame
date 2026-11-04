using TreeGame.Core;
using UnityEngine;

namespace TreeGame.Trees
{
    /// <summary>
    /// Grows every part of the tree steadily over time, each at its own rate from <see cref="GameConfig"/>, so it keeps
    /// growing without player input. Uses only TreeGrowth's public API; later rules (e.g. growth needing water) can gate
    /// or replace this component. Disable the component to pause passive growth.
    /// </summary>
    public class PassiveGrowth : MonoBehaviour, IAdvancesWithTime
    {
        [SerializeField] private TreeGrowth treeGrowth;
        [SerializeField] private GameConfig config;

        private void OnEnable()
        {
            if (treeGrowth == null || config == null)
            {
                Debug.LogWarning($"{nameof(PassiveGrowth)}: needs a {nameof(TreeGrowth)} and a {nameof(GameConfig)}.", this);
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
            foreach (TreeGrowthPart part in TreeGrowth.Parts)
            {
                treeGrowth.AddGrowth(part, seconds / config.SecondsToFullyGrow(part));
            }
        }
    }
}
