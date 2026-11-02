using UnityEngine;

namespace TreeGame.Minigames.InsectSwarm
{
    /// <summary>
    /// The tuning of Insect Swarm as an asset, so it can be adjusted in the Inspector. Defaults come from
    /// <see cref="InsectSwarmTuning"/>, so they live in one place. Where the cracks and the patch are stays in the
    /// tuning. Changes apply from the next game, and like other assets, edits made during Play Mode are kept afterwards.
    /// </summary>
    [CreateAssetMenu(fileName = "InsectSwarmSettings", menuName = "Tree Game/Minigames/Insect Swarm Settings")]
    public class InsectSwarmSettings : ScriptableObject
    {
        private static readonly InsectSwarmTuning Defaults = new InsectSwarmTuning();

        [Header("Swarm")]
        [Tooltip("How many insects come in a game, while nothing else decides that.")]
        [SerializeField, Min(1)] private int swarmSize = 6;
        [Tooltip("The first insect appears at once and the last after this many seconds, with the others evenly spaced between.")]
        [SerializeField, Min(0f)] private float arrivalSeconds = Defaults.ArrivalSeconds;
        [Tooltip("How fast insects crawl, in field units per second. The field is 16 wide and 9 high.")]
        [SerializeField, Min(0.1f)] private float crawlSpeed = Defaults.CrawlSpeed;

        [Header("Game Length")]
        [Tooltip("A game also ends once this many seconds of play have passed.")]
        [SerializeField, Min(1f)] private float timeLimitSeconds = Defaults.TimeLimitSeconds;

        [Header("Reach")]
        [Tooltip("An insect has got through once it comes this close to the patch's center, in field units.")]
        [SerializeField, Min(0.1f)] private float patchRadius = Defaults.PatchRadius;
        [Tooltip("A click stops an insect this close to it, in field units.")]
        [SerializeField, Min(0.1f)] private float clickRadius = Defaults.ClickRadius;

        [Header("Flow")]
        [Tooltip("How long the results ignore clicks, so a click just after the game ends can't skip them.")]
        [SerializeField, Min(0f)] private float secondsBeforeResultsCanClose = 0.5f;

        public int SwarmSize => swarmSize;

        public float SecondsBeforeResultsCanClose => secondsBeforeResultsCanClose;

        public InsectSwarmTuning ToTuning()
        {
            return new InsectSwarmTuning
            {
                ArrivalSeconds = arrivalSeconds,
                CrawlSpeed = crawlSpeed,
                TimeLimitSeconds = timeLimitSeconds,
                PatchRadius = patchRadius,
                ClickRadius = clickRadius,
            };
        }
    }
}
