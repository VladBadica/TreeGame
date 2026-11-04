using TreeGame.Audio;
using TreeGame.Care;
using UnityEngine;

namespace TreeGame.World
{
    /// <summary>
    /// Plays the sound of running water whenever water moves: when the well puts it in the bucket, and when it is
    /// poured on the tree.
    /// </summary>
    public class RunningWaterSound : OneShotSound
    {
        [SerializeField] private WaterWell well;
        [Tooltip("What water is poured on, i.e. the tree's water receiver.")]
        [SerializeField] private ResourceReceiver waterReceiver;

        protected override bool TryConnect()
        {
            if (well == null || waterReceiver == null)
            {
                return false;
            }

            well.WaterDrawn += OnWaterMoved;
            waterReceiver.Received += OnWaterMoved;
            return true;
        }

        protected override void Disconnect()
        {
            if (well != null)
            {
                well.WaterDrawn -= OnWaterMoved;
            }

            if (waterReceiver != null)
            {
                waterReceiver.Received -= OnWaterMoved;
            }
        }

        private void OnWaterMoved(float liters)
        {
            Play();
        }
    }
}
