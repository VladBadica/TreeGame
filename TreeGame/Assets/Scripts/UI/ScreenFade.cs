using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.UI
{
    /// <summary>
    /// A full-screen image that fades the view in and out, to cover changes such as loading a scene. Runs on unscaled
    /// time, so it works whether or not the game is paused. While visible it also blocks clicks on the UI behind it.
    /// </summary>
    public class ScreenFade : MonoBehaviour
    {
        // A slow frame, such as the one right after loading a scene, shouldn't make the fade jump.
        private const float LongestStep = 0.05f;

        [SerializeField] private Image image;

        /// <summary>Fades to an opacity, 0 (clear) to 1 (covered). Run it as a coroutine.</summary>
        public IEnumerator FadeTo(float opacity, float seconds)
        {
            image.enabled = true;
            float startOpacity = image.color.a;

            for (float elapsed = 0f; elapsed < seconds; elapsed += Mathf.Min(Time.unscaledDeltaTime, LongestStep))
            {
                SetOpacity(Mathf.Lerp(startOpacity, opacity, elapsed / seconds));
                yield return null;
            }

            SetOpacity(opacity);
            image.enabled = opacity > 0f;
        }

        private void SetOpacity(float opacity)
        {
            Color color = image.color;
            color.a = opacity;
            image.color = color;
        }
    }
}
