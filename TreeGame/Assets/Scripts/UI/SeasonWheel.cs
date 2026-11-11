using TMPro;
using TreeGame.Seasons;
using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.UI
{
    /// <summary>
    /// The season wheel in the corner of the HUD: a dial of the four seasons with a marker gliding slowly around it as the
    /// year passes, the current season lit, and next to it the season ("Late Summer") and a gentle word on when the next
    /// one comes ("Autumn in about 2 min"). Never a ticking countdown; the exact time is only in the tooltip on hover.
    /// While the next season is close, a soft glow in its color shows around the dial. It only shows the clock.
    /// </summary>
    public class SeasonWheel : MonoBehaviour
    {
        private const int SeasonCount = SeasonRules.SeasonsPerYear;

        [SerializeField] private SeasonClock clock;
        [Tooltip("Turned to show how far through the year it is; the marker sits at its top. Clockwise from the top: spring, summer, autumn, winter.")]
        [SerializeField] private RectTransform markerPivot;
        [Tooltip("One per season, in the order spring, summer, autumn, winter.")]
        [SerializeField] private Image[] quarters = new Image[SeasonCount];
        [Tooltip("One per season, in the order spring, summer, autumn, winter.")]
        [SerializeField] private Image[] icons = new Image[SeasonCount];
        [Tooltip("Behind the dial, glowing in the next season's color while it is close.")]
        [SerializeField] private Image glow;
        [SerializeField] private TMP_Text seasonLabel;
        [SerializeField] private TMP_Text timeLeftLabel;
        [Tooltip("The text in the tooltip shown on hover.")]
        [SerializeField] private TMP_Text tooltipLabel;

        [Header("Look")]
        [Tooltip("Each season's color, in the order spring, summer, autumn, winter.")]
        [SerializeField] private Color[] seasonColors =
        {
            new Color(0.62f, 0.82f, 0.46f),
            new Color(0.98f, 0.8f, 0.36f),
            new Color(0.9f, 0.5f, 0.26f),
            new Color(0.74f, 0.86f, 0.96f),
        };
        [Tooltip("How visible the quarters of the other seasons are, next to the current one's full color.")]
        [SerializeField, Range(0f, 1f)] private float otherSeasonsAlpha = 0.4f;
        [Tooltip("How strongly the glow shows at its brightest.")]
        [SerializeField, Range(0f, 1f)] private float glowAlpha = 0.75f;
        [Tooltip("Seconds for the glow to brighten and soften once.")]
        [SerializeField, Min(0.1f)] private float glowPulseSeconds = 2.5f;

        [Header("Words")]
        [Tooltip("A line in the tooltip on what each season asks of the player, in the order spring, summer, autumn, winter. " +
                 "The tooltip shows the one for the season coming next.")]
        [SerializeField] private string[] seasonHints =
        {
            "Spring: new leaves make the tree hungry, so have compost ready.",
            "Summer: hot days mean thirsty roots, and more insects.",
            "Autumn: lots of falling leaves to collect for compost.",
            "Winter: the tree rests and needs very little.",
        };

        // What the texts show, so they are only rebuilt when something in them changes.
        private Season shownSeason = (Season)(-1);
        private SeasonPart shownPart;
        private string shownTimeLeft;
        private int shownExactSeconds = -1;
        private float glowTime;

        private void OnEnable()
        {
            if (clock == null || markerPivot == null || seasonLabel == null || timeLeftLabel == null ||
                quarters.Length != SeasonCount || icons.Length != SeasonCount || seasonColors.Length != SeasonCount)
            {
                Debug.LogWarning($"{nameof(SeasonWheel)}: needs a clock, a marker, labels, and a quarter, icon and color per season.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            // Clockwise from the top, as a clock's hand moves.
            markerPivot.localRotation = Quaternion.Euler(0f, 0f, -clock.YearShare * 360f);

            Season season = clock.Season;
            SeasonPart part = clock.Part;
            if (season != shownSeason || part != shownPart)
            {
                shownSeason = season;
                shownPart = part;
                seasonLabel.text = $"{part} {season}";
                LightUp(season);
            }

            float secondsLeft = clock.SecondsLeftInSeason;
            string timeLeft = SeasonRules.TimeLeftGently(clock.NextSeason, secondsLeft);
            if (timeLeft != shownTimeLeft)
            {
                shownTimeLeft = timeLeft;
                timeLeftLabel.text = timeLeft;
            }

            UpdateTooltip(secondsLeft);
            UpdateGlow();
        }

        /// <summary>The current season's quarter and icon in full color, the others faded.</summary>
        private void LightUp(Season current)
        {
            for (int i = 0; i < SeasonCount; i++)
            {
                Color color = seasonColors[i];
                color.a = i == (int)current ? 1f : otherSeasonsAlpha;
                if (quarters[i] != null)
                {
                    quarters[i].color = color;
                }

                if (icons[i] != null)
                {
                    Color iconColor = icons[i].color;
                    iconColor.a = i == (int)current ? 1f : otherSeasonsAlpha;
                    icons[i].color = iconColor;
                }
            }
        }

        /// <summary>The exact time, the year and what the next season asks for. Only while the tooltip shows.</summary>
        private void UpdateTooltip(float secondsLeft)
        {
            if (tooltipLabel == null || !tooltipLabel.gameObject.activeInHierarchy)
            {
                shownExactSeconds = -1;
                return;
            }

            int exactSeconds = Mathf.CeilToInt(secondsLeft);
            if (exactSeconds == shownExactSeconds)
            {
                return;
            }

            shownExactSeconds = exactSeconds;
            Season next = clock.NextSeason;
            string hint = (int)next < seasonHints.Length ? seasonHints[(int)next] : "";
            tooltipLabel.text = $"{SeasonRules.TimeLeftExactly(next, secondsLeft)} · Year {clock.Year}\n<size=85%>{hint}</size>";
        }

        /// <summary>A slow, soft pulse in the next season's color while it is close; nothing otherwise.</summary>
        private void UpdateGlow()
        {
            if (glow == null)
            {
                return;
            }

            if (!clock.IsNextSeasonComingUp)
            {
                glowTime = 0f;
                glow.enabled = false;
                return;
            }

            glowTime += Time.deltaTime;
            float pulse = 0.5f - 0.5f * Mathf.Cos(glowTime * 2f * Mathf.PI / glowPulseSeconds);
            Color color = seasonColors[(int)clock.NextSeason];
            color.a = glowAlpha * Mathf.Lerp(0.35f, 1f, pulse);
            glow.color = color;
            glow.enabled = true;
        }
    }
}
