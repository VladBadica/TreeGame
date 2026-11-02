using UnityEngine;
using UnityEngine.InputSystem;
using FieldPoint = System.Numerics.Vector2;

namespace TreeGame.Minigames.InsectSwarm
{
    /// <summary>
    /// Runs Insect Swarm in the scene: feeds time and clicks into <see cref="InsectSwarmRules"/>, turning the pointer's
    /// position on screen into a position on the field. It holds no rules and draws nothing itself.
    /// </summary>
    public class InsectSwarmGame : PhasedMinigame
    {
        [SerializeField] private InsectSwarmSettings settings;
        [Tooltip("A screen-position action, e.g. the mouse position.")]
        [SerializeField] private InputActionReference pointAction;
        [Tooltip("The area of the screen showing the field.")]
        [SerializeField] private RectTransform field;

        /// <summary>The current game. Available from Start on.</summary>
        public InsectSwarmRules Rules { get; private set; }

        protected override bool HasWhatItNeeds =>
            settings != null && field != null && pointAction != null && pointAction.action != null;

        protected override bool IsGameOver => Rules.IsFinished;

        protected override float SecondsBeforeResultsCanClose => settings.SecondsBeforeResultsCanClose;

        protected override MinigameResult Result => new MinigameResult(Rules.IsFinished, Rules.Score);

        protected override void OnEnable()
        {
            base.OnEnable();

            if (enabled)
            {
                pointAction.action.Enable();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (pointAction != null && pointAction.action != null)
            {
                pointAction.action.Disable();
            }
        }

        protected override void PrepareNewGame()
        {
            // Not seeded on purpose: each game should use its cracks differently.
            Rules = new InsectSwarmRules(settings.ToTuning(), settings.SwarmSize, new System.Random());
        }

        protected override void PlayFrame(bool pressed)
        {
            // The click is judged before moving the insects, so it counts where the player saw them.
            if (pressed)
            {
                Rules.Click(PointerOnField());
            }

            Rules.Tick(Time.deltaTime);
        }

        private FieldPoint PointerOnField()
        {
            Vector2 screenPoint = pointAction.action.ReadValue<Vector2>();

            // No camera, because the canvas is drawn directly on the screen.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(field, screenPoint, null, out Vector2 localPoint);

            Rect area = field.rect;
            return new FieldPoint(
                (localPoint.x - area.xMin) / area.width * Rules.FieldWidth,
                (localPoint.y - area.yMin) / area.height * Rules.FieldHeight);
        }
    }
}
