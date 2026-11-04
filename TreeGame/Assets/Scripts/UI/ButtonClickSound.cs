using TreeGame.Audio;
using UnityEngine.UI;

namespace TreeGame.UI
{
    /// <summary>
    /// Plays a click whenever any button under this object is pressed, by mouse or keyboard, so a menu's buttons don't
    /// each need their own. Buttons are found once, so it suits menus whose buttons are built ahead of time.
    /// </summary>
    public class ButtonClickSound : OneShotSound
    {
        private Button[] buttons;

        protected override void Awake()
        {
            base.Awake();

            // Including hidden ones, such as the buttons of screens that aren't open yet.
            buttons = GetComponentsInChildren<Button>(includeInactive: true);
        }

        protected override bool TryConnect()
        {
            foreach (Button button in buttons)
            {
                button.onClick.AddListener(Play);
            }

            return true;
        }

        protected override void Disconnect()
        {
            foreach (Button button in buttons)
            {
                button.onClick.RemoveListener(Play);
            }
        }
    }
}
