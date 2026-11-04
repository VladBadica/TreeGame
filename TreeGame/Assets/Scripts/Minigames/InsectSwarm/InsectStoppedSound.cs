using TreeGame.Audio;
using UnityEngine;

namespace TreeGame.Minigames.InsectSwarm
{
    /// <summary>Plays a slap each time a click shoos an insect away. Clicks that miss stay quiet.</summary>
    public class InsectStoppedSound : OneShotSound
    {
        [SerializeField] private InsectSwarmGame game;

        protected override bool TryConnect()
        {
            if (game == null)
            {
                return false;
            }

            game.Clicked += OnClicked;
            return true;
        }

        protected override void Disconnect()
        {
            if (game != null)
            {
                game.Clicked -= OnClicked;
            }
        }

        private void OnClicked(SwarmClick result)
        {
            if (result == SwarmClick.Stopped)
            {
                Play();
            }
        }
    }
}
