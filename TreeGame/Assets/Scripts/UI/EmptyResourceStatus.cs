using TreeGame.Care;
using UnityEngine;

namespace TreeGame.UI
{
    /// <summary>
    /// Shows a warning badge while one of the tree's resources (water or nutrients) is empty, which is when the tree loses
    /// health. Hides it again as soon as the tree is watered or fed. Only reacts to the resource changing, so it does no
    /// work while nothing happens.
    /// </summary>
    public class EmptyResourceStatus : MonoBehaviour
    {
        [SerializeField] private TreeResource resource;
        [Tooltip("Shown only while the resource is empty.")]
        [SerializeField] private GameObject badge;

        private void OnEnable()
        {
            if (resource == null || badge == null)
            {
                Debug.LogWarning($"{nameof(EmptyResourceStatus)}: needs a resource and a badge.", this);
                enabled = false;
                return;
            }

            resource.Changed += Refresh;
            Refresh(resource);
        }

        private void OnDisable()
        {
            if (resource != null)
            {
                resource.Changed -= Refresh;
            }
        }

        private void Refresh(TreeResource changed)
        {
            // The same test the damage uses: only an empty resource costs health.
            badge.SetActive(changed.Normalized <= 0f);
        }
    }
}
