using System.Collections.Generic;
using TMPro;
using TreeGame.UI;
using UnityEngine;
using UnityEngine.UI;
using FieldPoint = System.Numerics.Vector2;

namespace TreeGame.Minigames.InsectSwarm
{
    /// <summary>
    /// Draws the state of an <see cref="InsectSwarmGame"/> on a close-up of the bark: it only reads the game and changes
    /// nothing, so this can later be replaced with real art without touching the gameplay. Everything on the field is
    /// placed by anchors, as a share of the field's size, so it fits any screen.
    /// </summary>
    public class InsectSwarmView : MonoBehaviour
    {
        [SerializeField] private InsectSwarmGame game;
        [Tooltip("The healthy patch insects crawl toward.")]
        [SerializeField] private RectTransform patch;
        [Tooltip("An inactive object, copied once per crack.")]
        [SerializeField] private RectTransform crackTemplate;
        [Tooltip("An inactive object, copied once per insect.")]
        [SerializeField] private Image insectTemplate;
        [Tooltip("Holds the message shown before and after play, and is hidden during it so it doesn't cover the insects.")]
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private TMP_Text countLabel;
        [SerializeField] private TMP_Text hintLabel;
        [SerializeField] private CountdownBar timer;

        [Header("Sizes (in field units)")]
        [SerializeField, Min(0.01f)] private float crackSize = 1.4f;
        [SerializeField, Min(0.01f)] private float insectSize = 0.9f;

        [Header("Insect Movement")]
        [Tooltip("Degrees insects wiggle from side to side as they crawl.")]
        [SerializeField, Min(0f)] private float wiggleDegrees = 12f;
        [SerializeField, Min(0f)] private float wiggleSpeed = 18f;
        [Tooltip("How long a stopped insect takes to fly off and fade out.")]
        [SerializeField, Min(0.01f)] private float flyOffSeconds = 0.35f;
        [Tooltip("How far a stopped insect flies back the way it came, in field units.")]
        [SerializeField, Min(0f)] private float flyOffDistance = 1.5f;

        [Header("Feedback")]
        [Tooltip("Flashes the patch when an insect gets through.")]
        [SerializeField] private ImageFlash patchFlash;
        [SerializeField] private Color gotThroughColor = new Color(0.95f, 0.3f, 0.25f);
        [SerializeField, Min(0.01f)] private float flashSeconds = 0.4f;

        private sealed class InsectSprite
        {
            public Image Image;
            public RectTransform Rect;
            public float SecondsSinceStopped;
        }

        private readonly List<InsectSprite> insectSprites = new List<InsectSprite>();
        private InsectSwarmRules shownRules;
        private bool cracksCreated;
        private int shownStopped;
        private int shownGotThrough;

        private void OnEnable()
        {
            if (game == null || patch == null || crackTemplate == null || insectTemplate == null || messagePanel == null ||
                messageLabel == null || countLabel == null || hintLabel == null || timer == null || patchFlash == null)
            {
                Debug.LogWarning($"{nameof(InsectSwarmView)}: needs a game, the field's parts, its labels, a timer and a patch flash.", this);
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

        // After the game's Update, so insects show where they are this frame.
        private void LateUpdate()
        {
            InsectSwarmRules rules = game.Rules;
            if (rules == null)
            {
                return;
            }

            timer.Show(rules.TimeLeft, rules.TimeLimit);

            for (int i = 0; i < rules.SwarmSize; i++)
            {
                ShowInsect(insectSprites[i], rules.Insects[i], i);
            }

            ShowCounts(rules);
        }

        private void Refresh()
        {
            InsectSwarmRules rules = game.Rules;
            if (rules != shownRules)
            {
                ShowNewGame(rules);
            }

            messagePanel.SetActive(game.Phase != MinigamePhase.Playing);
            countLabel.gameObject.SetActive(game.Phase == MinigamePhase.Playing);
            string quitKey = $"<b>{game.QuitKeyName}</b>";

            switch (game.Phase)
            {
                case MinigamePhase.Intro:
                    messageLabel.text = "Insects in the bark!\nClick them before they reach the green patch";
                    hintLabel.text = $"<b>Click</b> to start   ·   {quitKey} to leave";
                    break;

                case MinigamePhase.Playing:
                    hintLabel.text = "<b>Click</b> the insects before they reach the green patch";
                    break;

                case MinigamePhase.Results:
                    messageLabel.text = $"{GetResultTitle(rules)}\nStopped {rules.Stopped} of {rules.SwarmSize}";
                    hintLabel.text = "<b>Click</b> to continue";
                    break;
            }
        }

        private static string GetResultTitle(InsectSwarmRules rules)
        {
            if (rules.Stopped == rules.SwarmSize)
            {
                return "All clear!";
            }

            return rules.GotThrough > 0 ? "Some got through" : "Time's up!";
        }

        private void ShowNewGame(InsectSwarmRules rules)
        {
            shownRules = rules;
            shownStopped = 0;
            shownGotThrough = 0;
            countLabel.text = FormatCount(rules);

            PlaceOnField(patch, rules.PatchCenter, rules.PatchRadius * 2f, rules);
            CreateCracksIfNeeded(rules);

            // Only grown here, at the start of a game, and reused by later games.
            while (insectSprites.Count < rules.SwarmSize)
            {
                Image image = Instantiate(insectTemplate, insectTemplate.transform.parent);
                image.name = $"Insect {insectSprites.Count}";
                insectSprites.Add(new InsectSprite { Image = image, Rect = image.rectTransform });
            }

            foreach (InsectSprite sprite in insectSprites)
            {
                sprite.Image.gameObject.SetActive(false);
                sprite.Image.color = insectTemplate.color; // A previous game may have left it faded out.
                sprite.SecondsSinceStopped = 0f;
            }
        }

        private void CreateCracksIfNeeded(InsectSwarmRules rules)
        {
            if (cracksCreated)
            {
                return;
            }

            cracksCreated = true;
            foreach (FieldPoint crackPosition in rules.Cracks)
            {
                RectTransform crack = Instantiate(crackTemplate, crackTemplate.parent);
                crack.gameObject.SetActive(true);
                PlaceOnField(crack, crackPosition, crackSize, rules);
            }
        }

        private void ShowInsect(InsectSprite sprite, SwarmInsect insect, int index)
        {
            switch (insect.State)
            {
                case SwarmInsectState.Waiting:
                    sprite.Image.gameObject.SetActive(false);
                    break;

                case SwarmInsectState.Crawling:
                    sprite.Image.gameObject.SetActive(true);
                    PlaceOnField(sprite.Rect, insect.Position, insectSize, game.Rules);

                    // Each insect is offset in time, so they don't all wiggle in step.
                    float wiggle = Mathf.Sin((Time.time + index) * wiggleSpeed) * wiggleDegrees;
                    sprite.Rect.localRotation = Quaternion.Euler(0f, 0f, HeadingAngle(insect) + wiggle);
                    break;

                case SwarmInsectState.Stopped:
                    ShowFlyingOff(sprite, insect);
                    break;

                case SwarmInsectState.GotThrough:
                    // Stays on the edge of the patch, as a reminder that it got there.
                    PlaceOnField(sprite.Rect, insect.Position, insectSize, game.Rules);
                    break;
            }
        }

        private void ShowFlyingOff(InsectSprite sprite, SwarmInsect insect)
        {
            sprite.SecondsSinceStopped += Time.deltaTime;
            float progress = sprite.SecondsSinceStopped / flyOffSeconds;
            if (progress >= 1f)
            {
                sprite.Image.gameObject.SetActive(false);
                return;
            }

            PlaceOnField(sprite.Rect, insect.Position - insect.Heading * (flyOffDistance * progress), insectSize, game.Rules);

            Color color = sprite.Image.color;
            color.a = 1f - progress;
            sprite.Image.color = color;
        }

        private void ShowCounts(InsectSwarmRules rules)
        {
            if (rules.GotThrough > shownGotThrough)
            {
                patchFlash.Play(gotThroughColor, flashSeconds);
            }

            if (rules.Stopped != shownStopped || rules.GotThrough != shownGotThrough)
            {
                shownStopped = rules.Stopped;
                shownGotThrough = rules.GotThrough;
                countLabel.text = FormatCount(rules);
            }
        }

        private static string FormatCount(InsectSwarmRules rules)
        {
            return $"Stopped {rules.Stopped} of {rules.SwarmSize}";
        }

        /// <summary>The sprite's head points up, while a positive UI rotation turns counterclockwise.</summary>
        private static float HeadingAngle(SwarmInsect insect)
        {
            return -Mathf.Atan2(insect.Heading.X, insect.Heading.Y) * Mathf.Rad2Deg;
        }

        private static void PlaceOnField(RectTransform rect, FieldPoint center, float size, InsectSwarmRules rules)
        {
            var share = new Vector2(center.X / rules.FieldWidth, center.Y / rules.FieldHeight);
            var halfSize = new Vector2(size / 2f / rules.FieldWidth, size / 2f / rules.FieldHeight);

            rect.anchorMin = share - halfSize;
            rect.anchorMax = share + halfSize;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
