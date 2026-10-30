using TreeGame.Items;
using UnityEngine;

namespace TreeGame.UI
{
    /// <summary>
    /// Shows "Press [key] to gather" above the nearest pickup the <see cref="ItemCollector"/> can reach.
    /// The key comes from the collect action's current binding, so rebinding updates the prompt automatically.
    /// </summary>
    public class GatherPrompt : MonoBehaviour
    {
        [SerializeField] private ItemCollector collector;
        [SerializeField] private WorldPopup popup;
        [SerializeField] private string actionDescription = "gather";
        [Tooltip("How far above the pickup the popup sits, in world units; pickups lie on the ground.")]
        [SerializeField, Min(0f)] private float heightAbovePickup = 0.6f;
        [Tooltip("Which control scheme's binding to show, e.g. \"Keyboard&Mouse\".")]
        [SerializeField] private string bindingGroup = "Keyboard&Mouse";

        private void OnEnable()
        {
            if (collector == null || popup == null)
            {
                Debug.LogWarning($"{nameof(GatherPrompt)}: needs a collector and a popup.", this);
                enabled = false;
                return;
            }

            collector.NearestChanged += Show;
            Show(collector.Nearest);
        }

        private void OnDisable()
        {
            if (collector != null)
            {
                collector.NearestChanged -= Show;
            }

            if (popup != null)
            {
                popup.Hide();
            }
        }

        private void Show(ItemPickup pickup)
        {
            if (pickup == null)
            {
                popup.Hide();
                return;
            }

            string key = BindingText.KeyName(collector.CollectAction, bindingGroup, fallback: "Collect");
            popup.Show(pickup.transform, BindingText.PressToPrompt(key, actionDescription), Vector3.up * heightAbovePickup);
        }
    }
}
