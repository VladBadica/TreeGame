using System;
using UnityEngine;

namespace TreeGame.Core
{
    /// <summary>
    /// Describes the world's wind and evaluates it at a position and time. Everything the wind moves samples
    /// this same function, so all of it sways consistently. The result is a signed horizontal strength:
    /// positive blows to the right, roughly 0..1 in magnitude; each consumer decides how strongly it reacts.
    /// Deterministic: the same position and time always give the same wind.
    /// </summary>
    [Serializable]
    public class WindSettings
    {
        // Fixed offsets so the gust and turbulence noise patterns differ from each other.
        private const float GustNoiseRow = 0.37f;
        private const float TurbulenceNoiseRow = 5.71f;

        [Tooltip("Which way the wind blows: 1 = to the right, -1 = to the left. Values in between weaken it.")]
        [SerializeField, Range(-1f, 1f)] private float direction = 1f;
        [Tooltip("Steady push that is always present. 0 = calm between gusts.")]
        [SerializeField, Range(0f, 1f)] private float baseStrength = 0.1f;
        [Tooltip("Extra strength added at the peak of a gust.")]
        [SerializeField, Range(0f, 1f)] private float gustStrength = 0.25f;
        [Tooltip("Roughly how many gusts pass per second. Lower = slower, rolling gusts.")]
        [SerializeField, Min(0f)] private float gustFrequency = 0.2f;
        [Tooltip("How fast gusts travel across the world (units per second), so things downwind react a moment later.")]
        [SerializeField, Min(0.1f)] private float gustTravelSpeed = 6f;
        [Tooltip("Small, quick variation on top of the gusts.")]
        [SerializeField, Range(0f, 1f)] private float turbulence = 0.05f;
        [Tooltip("How quickly the turbulence changes.")]
        [SerializeField, Min(0f)] private float turbulenceFrequency = 1.5f;

        /// <summary>Signed horizontal wind strength at a world X position and time (use scaled time so pausing stops it).</summary>
        public float Evaluate(float worldX, float time)
        {
            // Sampling a traveling wave: a point further downwind sees the same gust slightly later.
            float phase = time - worldX * Mathf.Sign(direction) / gustTravelSpeed;

            float gust = Mathf.Clamp01(Mathf.PerlinNoise(phase * gustFrequency, GustNoiseRow));
            float flutter = (Mathf.PerlinNoise(phase * turbulenceFrequency, TurbulenceNoiseRow) - 0.5f) * 2f * turbulence;

            return direction * (baseStrength + gustStrength * gust) + flutter;
        }
    }
}
