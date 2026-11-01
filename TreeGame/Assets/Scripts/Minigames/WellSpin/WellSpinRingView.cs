using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TreeGame.Minigames.WellSpin
{
    /// <summary>
    /// Draws the state of a <see cref="WellSpinGame"/> as a flat ring: it only reads the rules and changes nothing,
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

        private void OnEnable()
        {
            if (game == null || target == null || indicatorPivot == null || scoreLabel == null || statusLabel == null)
            {
                Debug.LogWarning($"{nameof(WellSpinRingView)}: needs a game, target, indicator pivot and labels.", this);
                enabled = false;
                return;
            }

            game.Changed += Refresh;
        }

        private void OnDisable()
        {
            if (game != null)
            {
                game.Changed -= Refresh;
            }
        }

        // After the game's Update, so the indicator shows where it is this frame.
        private void LateUpdate()
        {
            if (game.Rules != null)
            {
                indicatorPivot.localRotation = ClockwiseRotation(game.Rules.IndicatorAngle);
            }
        }

        private void Refresh()
        {
            WellSpinRules rules = game.Rules;

            // The fill runs clockwise from its start, so turn its start to the target's first edge.
            target.fillAmount = rules.TargetWidth / FullCircle;
            target.rectTransform.localRotation = ClockwiseRotation(rules.TargetCenter - rules.TargetWidth / 2f);

            scoreLabel.text = rules.Hits.ToString();
            statusLabel.text = rules.IsFinished
                ? "Game over\nPress <b>Space</b> to play again"
                : $"Try {rules.Tries + 1} of {rules.MaxTries}\nMisses {rules.Misses} of {rules.MaxMisses}";
        }

        // Game angles increase clockwise, while a positive UI rotation turns counterclockwise.
        private static Quaternion ClockwiseRotation(float angle)
        {
            return Quaternion.Euler(0f, 0f, -angle);
        }
    }
}
