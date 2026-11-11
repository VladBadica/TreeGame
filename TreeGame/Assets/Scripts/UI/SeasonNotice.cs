using TMPro;
using TreeGame.Seasons;
using UnityEngine;

namespace TreeGame.UI
{
    /// <summary>
    /// Tells the player about the seasons without getting in the way: a small note that slides out under the season wheel
    /// when the next season is close, with one tip on getting ready for it, and a banner when a season arrives
    /// ("Spring has arrived · Year 2"). Both fade in, stay a few seconds and fade away on their own; nothing needs a click
    /// or pauses the game. All the words can be edited in the Inspector.
    /// </summary>
    public class SeasonNotice : MonoBehaviour
    {
        [SerializeField] private SeasonClock clock;

        [Header("Coming up note")]
        [SerializeField] private CanvasGroup note;
        [SerializeField] private TMP_Text noteTitle;
        [SerializeField] private TMP_Text noteBody;
        [SerializeField, Min(0.1f)] private float noteSeconds = 7f;
        [Tooltip("How far the note slides out as it appears, in canvas units.")]
        [SerializeField] private float noteSlide = 16f;
        [Tooltip("The note's title for each coming season, in the order spring, summer, autumn, winter.")]
        [SerializeField] private string[] comingUpTitles =
        {
            "Spring is near",
            "Summer is coming",
            "Autumn is on its way",
            "Winter is coming",
        };
        [Tooltip("One tip for getting ready for each coming season, in the order spring, summer, autumn, winter.")]
        [SerializeField] private string[] comingUpTips =
        {
            "The tree will be hungry for new leaves, so have compost ready.",
            "Hot days mean thirsty roots. Fill your water bucket.",
            "Leaves will start falling a lot. Time to make compost.",
            "The tree will rest soon. A full compost bucket now means a good spring.",
        };

        [Header("Arrival banner")]
        [SerializeField] private CanvasGroup banner;
        [SerializeField] private TMP_Text bannerLabel;
        [SerializeField, Min(0.1f)] private float bannerSeconds = 3.5f;
        [Tooltip("{0} is the season and {1} the year.")]
        [SerializeField] private string arrivedText = "{0} has arrived · Year {1}";

        [Header("Fading")]
        [SerializeField, Min(0.01f)] private float fadeSeconds = 0.6f;

        private RectTransform noteRect;
        private Vector2 noteRestPosition;
        private float noteTime = -1f;
        private float bannerTime = -1f;

        // The season it last saw; none until it has run, so loading a game doesn't announce the season it was saved in.
        private Season? seenSeason;

        private void Awake()
        {
            if (note != null)
            {
                noteRect = (RectTransform)note.transform;
                noteRestPosition = noteRect.anchoredPosition;
            }
        }

        private void OnEnable()
        {
            if (clock == null || note == null || noteTitle == null || noteBody == null || banner == null || bannerLabel == null)
            {
                Debug.LogWarning($"{nameof(SeasonNotice)}: needs a clock, a note with a title and body, and a banner with a label.", this);
                enabled = false;
                return;
            }

            clock.NextSeasonComingUp += ShowComingUp;
            clock.SeasonChanged += ShowArrived;
            Hide(note);
            Hide(banner);

            // A season that began while the HUD was switched off, e.g. during a minigame, still gets its banner.
            if (seenSeason.HasValue && seenSeason.Value != clock.Season)
            {
                ShowArrived(clock.Season);
            }
        }

        private void OnDisable()
        {
            if (clock != null)
            {
                clock.NextSeasonComingUp -= ShowComingUp;
                clock.SeasonChanged -= ShowArrived;
            }
        }

        private void Update()
        {
            seenSeason = clock.Season;

            // Scaled time, so the notes wait while the game is paused.
            float alpha = Advance(ref noteTime, noteSeconds, note);
            if (noteRect != null)
            {
                noteRect.anchoredPosition = noteRestPosition + Vector2.up * (noteSlide * (1f - alpha));
            }

            Advance(ref bannerTime, bannerSeconds, banner);
        }

        private void ShowComingUp(Season next)
        {
            noteTitle.text = Pick(comingUpTitles, next, $"{next} is coming");
            noteBody.text = Pick(comingUpTips, next, "");
            noteTime = 0f;
        }

        private void ShowArrived(Season season)
        {
            // A new season makes any note about it old news.
            noteTime = -1f;
            Hide(note);

            bannerLabel.text = string.Format(arrivedText, season, clock.Year);
            bannerTime = 0f;
        }

        /// <summary>Moves a notice on through fading in, staying and fading out. Returns how visible it is now, 0..1.</summary>
        private float Advance(ref float time, float duration, CanvasGroup group)
        {
            if (time < 0f)
            {
                return 0f;
            }

            time += Time.deltaTime;
            if (time >= duration)
            {
                time = -1f;
                Hide(group);
                return 0f;
            }

            float alpha = Mathf.Clamp01(Mathf.Min(time, duration - time) / fadeSeconds);
            group.alpha = alpha;
            group.gameObject.SetActive(true);
            return alpha;
        }

        private static void Hide(CanvasGroup group)
        {
            group.alpha = 0f;
            group.gameObject.SetActive(false);
        }

        private static string Pick(string[] texts, Season season, string fallback)
        {
            int index = (int)season;
            return texts != null && index < texts.Length && !string.IsNullOrEmpty(texts[index]) ? texts[index] : fallback;
        }
    }
}
