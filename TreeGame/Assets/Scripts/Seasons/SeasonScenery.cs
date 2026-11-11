using UnityEngine;

namespace TreeGame.Seasons
{
    /// <summary>
    /// Colors part of the world for the season, e.g. the ground white with snow in winter or the hills golden in autumn,
    /// easing from one season's color into the next's as the season ends. The renderers share one copy of their
    /// material made at runtime, so the material asset itself is never changed. Without a clock it leaves them as they are.
    /// </summary>
    public class SeasonScenery : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField] private SeasonClock clock;
        [Tooltip("Everything colored together. They should share a material, as they all get the first one's copy.")]
        [SerializeField] private Renderer[] renderers = new Renderer[0];
        [SerializeField] private Color spring = Color.white;
        [SerializeField] private Color summer = Color.white;
        [SerializeField] private Color autumn = Color.white;
        [SerializeField] private Color winter = Color.white;
        [Tooltip("Seconds the colors take to ease into the next season's, at the end of each season.")]
        [SerializeField, Min(0f)] private float blendSeconds = 45f;

        private Material material;

        private void Start()
        {
            if (clock == null || renderers.Length == 0 || renderers[0] == null || renderers[0].sharedMaterial == null)
            {
                enabled = false;
                return;
            }

            material = new Material(renderers[0].sharedMaterial) { name = $"{renderers[0].sharedMaterial.name} (seasons)" };
            foreach (Renderer part in renderers)
            {
                if (part != null)
                {
                    part.sharedMaterial = material;
                }
            }
        }

        private void OnDestroy()
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        private void LateUpdate()
        {
            SeasonRules.Transition(clock.TotalSeconds, clock.SecondsPerSeason, blendSeconds,
                out Season from, out Season to, out float toShare);
            material.SetColor(BaseColor, Color.Lerp(ColorOf(from), ColorOf(to), toShare));
        }

        private Color ColorOf(Season season)
        {
            return season switch
            {
                Season.Spring => spring,
                Season.Summer => summer,
                Season.Autumn => autumn,
                _ => winter,
            };
        }
    }
}
