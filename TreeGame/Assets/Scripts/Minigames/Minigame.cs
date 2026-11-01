using System;
using UnityEngine;

namespace TreeGame.Minigames
{
    /// <summary>
    /// Base class for a minigame's main component, of which each minigame scene has one. A minigame starts by itself
    /// when its scene loads and reports back through <see cref="Finished"/>, so it can also be played directly from
    /// its own scene while developing it.
    /// </summary>
    public abstract class Minigame : MonoBehaviour
    {
        /// <summary>Raised when the player is done with the minigame.</summary>
        public event Action<MinigameResult> Finished;

        /// <summary>
        /// Reports the result to whoever started the minigame, then stops it. Returns false if nobody is listening
        /// because the scene is being played on its own, so the minigame can start over instead.
        /// </summary>
        protected bool ReportFinished(MinigameResult result)
        {
            if (Finished == null)
            {
                return false;
            }

            // Its scene takes a few frames to unload; a press in that time must not count for anything.
            enabled = false;
            Finished.Invoke(result);
            return true;
        }
    }
}
