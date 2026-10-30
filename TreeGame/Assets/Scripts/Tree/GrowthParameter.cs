using System;
using UnityEngine;

namespace TreeGame.Trees
{
    /// <summary>
    /// Maps a normalized progress value (0..1) to a value between <see cref="min"/> and <see cref="max"/>,
    /// shaped by a curve. Lets each visual aspect of the tree grow at its own pace.
    /// </summary>
    [Serializable]
    public class GrowthParameter
    {
        [SerializeField] private float min;
        [SerializeField] private float max;
        [Tooltip("X = progress (0..1), Y = blend between Min (0) and Max (1).")]
        [SerializeField] private AnimationCurve curve;

        public GrowthParameter(float min, float max, AnimationCurve curve = null)
        {
            this.min = min;
            this.max = max;
            this.curve = curve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }

        public float Evaluate(float progress)
        {
            float t = Mathf.Clamp01(progress);
            // An empty curve (e.g. cleared in the Inspector) falls back to linear instead of returning 0.
            float shaped = curve != null && curve.length > 0 ? curve.Evaluate(t) : t;
            return Mathf.LerpUnclamped(min, max, shaped);
        }
    }
}
