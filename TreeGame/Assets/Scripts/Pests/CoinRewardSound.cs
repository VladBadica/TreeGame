using TreeGame.Audio;
using UnityEngine;

namespace TreeGame.Pests
{
    /// <summary>Plays a reward sound when defending the tree earns coins.</summary>
    public class CoinRewardSound : OneShotSound
    {
        [SerializeField] private InsectDefense defense;

        protected override bool TryConnect()
        {
            if (defense == null)
            {
                return false;
            }

            defense.CoinsAwarded += OnCoinsAwarded;
            return true;
        }

        protected override void Disconnect()
        {
            if (defense != null)
            {
                defense.CoinsAwarded -= OnCoinsAwarded;
            }
        }

        private void OnCoinsAwarded(int coins)
        {
            Play();
        }
    }
}
