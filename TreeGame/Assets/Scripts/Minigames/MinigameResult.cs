using System;

namespace TreeGame.Minigames
{
    /// <summary>
    /// How a minigame ended, in terms every minigame can share. Whoever started the minigame decides what this
    /// is worth; minigames know nothing about water, compost or any other reward.
    /// </summary>
    public readonly struct MinigameResult
    {
        public MinigameResult(bool completed, float score)
        {
            Completed = completed;
            Score = float.IsFinite(score) ? Math.Clamp(score, 0f, 1f) : 0f;
        }

        /// <summary>True if the player played it to the end, false if they left early.</summary>
        public bool Completed { get; }

        /// <summary>How well it went, from 0 to 1.</summary>
        public float Score { get; }
    }
}
