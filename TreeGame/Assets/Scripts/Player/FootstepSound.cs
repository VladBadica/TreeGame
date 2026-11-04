using TreeGame.Audio;
using UnityEngine;

namespace TreeGame.Player
{
    /// <summary>Plays a footstep each time the character's foot comes down. Given two sounds, the feet alternate between them.</summary>
    public class FootstepSound : OneShotSound
    {
        [SerializeField] private CharacterWalkAnimation walkAnimation;

        protected override bool TryConnect()
        {
            if (walkAnimation == null)
            {
                return false;
            }

            walkAnimation.FootDown += OnFootDown;
            return true;
        }

        protected override void Disconnect()
        {
            if (walkAnimation != null)
            {
                walkAnimation.FootDown -= OnFootDown;
            }
        }

        private void OnFootDown(bool isFirstFoot)
        {
            Play();
        }
    }
}
