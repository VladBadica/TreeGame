using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.UI
{
    /// <summary>
    /// Flashes an image to a color that fades back to the image's own color, as quick feedback that something
    /// happened. Runs on unscaled time, so it still plays if the game is paused.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class ImageFlash : MonoBehaviour
    {
        private Image image;
        private Color restColor;
        private Coroutine flash;

        private void Awake()
        {
            // The resting color is whatever the image was styled with, so it's defined in one place.
            image = GetComponent<Image>();
            restColor = image.color;
        }

        private void OnDisable()
        {
            // A disabled object stops its coroutines; make sure it doesn't stay mid-flash.
            flash = null;
            image.color = restColor;
        }

        public void Play(Color color, float seconds)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (flash != null)
            {
                StopCoroutine(flash);
            }

            flash = StartCoroutine(Flash(color, seconds));
        }

        private IEnumerator Flash(Color color, float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                image.color = Color.Lerp(color, restColor, elapsed / seconds);
                yield return null;
            }

            image.color = restColor;
            flash = null;
        }
    }
}
