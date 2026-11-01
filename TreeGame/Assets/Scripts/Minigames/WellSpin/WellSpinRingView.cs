using TMPro;
using TreeGame.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.Minigames.WellSpin
{
    /// <summary>
    /// Draws the state of a <see cref="WellSpinGame"/> as a flat ring: it only reads the game and changes nothing,
    /// so this can later be replaced by a real wheel without touching the gameplay.
    /// </summary>
    public class WellSpinRingView : MonoBehaviour
    {
        private const float FullCircle = 360f;

        [SerializeField] private WellSpinGame game;
        [Tooltip("A radial-fill image of the ring starting at the top, showing the target area.")]
        [SerializeField] private Image target;
        [Tooltip("Rotates around the ring's center; the visible indicator is its child.")]
        [SerializeField] private RectTransform indicatorPivot;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private TMP_Text hintLabel;

        [Header("Timer")]
        [SerializeField] private FillBar timerBar;
        [SerializeField] private TMP_Text timerLabel;

        [Header("Press Feedback")]
        [Tooltip("Flashes the ring's track when a press is judged.")]
        [SerializeField] private ImageFlash trackFlash;
        [SerializeField] private Color hitColor = new Color(0.55f, 1f, 0.45f);
        [SerializeField] private Color missColor = new Color(0.95f, 0.3f, 0.25f);
        [SerializeField, Min(0.01f)] private float flashSeconds = 0.3f;

        private int shownSecondsLeft = -1;

        private void OnEnable()
        {
            if (game == null || target == null || indicatorPivot == null || scoreLabel == null ||
                statusLabel == null || hintLabel == null || trackFlash == null || timerBar == null || timerLabel == null)
            {
                Debug.LogWarning($"{nameof(WellSpinRingView)}: needs a game, target, indicator pivot, labels, a track flash and a timer.", this);
                enabled = false;
                return;
            }

            game.Changed += Refresh;
            game.Pressed += ShowFeedback;
        }

        private void OnDisable()
        {
            if (game != null)
            {
                game.Changed -= Refresh;
                game.Pressed -= ShowFeedback;
            }
        }

        // After the game's Update, so the indicator shows where it is this frame.
        private void LateUpdate()
        {
            if (game.Rules != null)
            {
                indicatorPivot.localRotation = ClockwiseRotation(game.Rules.IndicatorAngle);
                ShowTimer(game.Rules);
            }
        }

        /// <summary>Moves in whole seconds, so the bar and the number always agree.</summary>
        private void ShowTimer(WellSpinRules rules)
        {
            int secondsLeft = Mathf.CeilToInt(rules.TimeLeft);
            if (secondsLeft == shownSecondsLeft)
            {
                return;
            }

            shownSecondsLeft = secondsLeft;
            timerLabel.text = $"{secondsLeft}s";
            timerBar.SetFill(secondsLeft / rules.TimeLimit);
        }

        private void ShowFeedback(WellSpinPress result)
        {
            trackFlash.Play(result == WellSpinPress.Hit ? hitColor : missColor, flashSeconds);
        }

        private void Refresh()
        {
            WellSpinRules rules = game.Rules;

            // The fill runs clockwise from its start, so turn its start to the target's first edge.
            target.fillAmount = rules.TargetWidth / FullCircle;
            target.rectTransform.localRotation = ClockwiseRotation(rules.TargetCenter - rules.TargetWidth / 2f);

            string pressKey = $"<b>{game.PressKeyName}</b>";
            string quitKey = $"<b>{game.QuitKeyName}</b>";
            switch (game.Phase)
            {
                case WellSpinPhase.Intro:
                    scoreLabel.text = "";
                    statusLabel.text = "Spin the wheel\nto draw water";
                    hintLabel.text = $"Press {pressKey} to start   ·   {quitKey} to leave";
                    break;

                case WellSpinPhase.Playing:
                    scoreLabel.text = rules.Hits.ToString();
                    statusLabel.text = $"Try {rules.Tries + 1} of {rules.MaxTries}\nMisses {rules.Misses} of {rules.MaxMisses}";
                    hintLabel.text = $"Press {pressKey} when the white marker is in the green area";
                    break;

                case WellSpinPhase.Results:
                    scoreLabel.text = rules.Hits.ToString();
                    statusLabel.text = $"{GetResultTitle(rules)}\n{rules.Hits} hits in {rules.Tries} tries";
                    hintLabel.text = $"Press {pressKey} to continue";
                    break;
            }
        }

        private static string GetResultTitle(WellSpinRules rules)
        {
            if (rules.Hits == rules.MaxTries)
            {
                return "Perfect!";
            }

            if (rules.Misses >= rules.MaxMisses)
            {
                return "Too many misses";
            }

            return rules.IsOutOfTime ? "Time's up!" : "Well done!";
        }

        // Game angles increase clockwise, while a positive UI rotation turns counterclockwise.
        private static Quaternion ClockwiseRotation(float angle)
        {
            return Quaternion.Euler(0f, 0f, -angle);
        }
    }
}
