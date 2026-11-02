using TreeGame.Saving;
using UnityEngine;

namespace TreeGame.Menus
{
    /// <summary>
    /// Shows the menu buttons that fit whether there is a save: for example Continue, and a Play that warns about
    /// replacing it. A damaged save counts as none, as there would be nothing to continue. In Awake, so it is settled
    /// before the menu selects its first button.
    /// </summary>
    public class SaveDependentButtons : MonoBehaviour
    {
        [SerializeField] private GameObject[] shownWithSave = new GameObject[0];
        [SerializeField] private GameObject[] shownWithoutSave = new GameObject[0];

        private void Awake()
        {
            bool hasSave = SaveFile.ForThisGame().TryRead(out _);

            foreach (GameObject button in shownWithSave)
            {
                button.SetActive(hasSave);
            }

            foreach (GameObject button in shownWithoutSave)
            {
                button.SetActive(!hasSave);
            }
        }
    }
}
