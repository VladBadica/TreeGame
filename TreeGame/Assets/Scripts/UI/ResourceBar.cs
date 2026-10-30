using TMPro;
using TreeGame.Care;
using UnityEngine;

namespace TreeGame.UI
{
    /// <summary>Shows one <see cref="TreeResource"/> as a horizontal fill bar with a label.</summary>
    public class ResourceBar : MonoBehaviour
    {
        [SerializeField] private TreeResource resource;
        [SerializeField] private FillBar bar;
        [SerializeField] private TMP_Text label;

        private void OnEnable()
        {
            if (resource == null)
            {
                Debug.LogWarning($"{nameof(ResourceBar)}: no {nameof(TreeResource)} assigned.", this);
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

        private void Refresh(TreeResource source)
        {
            float normalized = Mathf.Clamp01(source.Normalized);

            if (bar != null)
            {
                bar.SetFill(normalized);
            }

            if (label != null)
            {
                label.text = $"{source.Type} {normalized * 100f:0}%";
            }
        }
    }
}
