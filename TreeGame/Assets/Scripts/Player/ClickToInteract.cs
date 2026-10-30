using TreeGame.Interaction;
using TreeGame.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TreeGame.Player
{
    /// <summary>
    /// Lets the player click things in the world as an alternative to their keys: clicking an interactable
    /// (e.g. the well) interacts like the interact key, clicking a pickup collects that pickup. The same range
    /// rules apply as for the keys, and clicks on UI are ignored. Only connects input to the Interactor and
    /// ItemCollector; it decides nothing itself.
    /// </summary>
    public class ClickToInteract : MonoBehaviour
    {
        private const float MaxClickDistance = 100f;

        [Tooltip("A button action, e.g. UI/Click.")]
        [SerializeField] private InputActionReference clickAction;
        [Tooltip("A screen-position action, e.g. UI/Point.")]
        [SerializeField] private InputActionReference pointAction;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Interactor interactor;
        [SerializeField] private ItemCollector collector;

        private void OnEnable()
        {
            if (clickAction == null || clickAction.action == null || pointAction == null || pointAction.action == null ||
                viewCamera == null || interactor == null || collector == null)
            {
                Debug.LogWarning($"{nameof(ClickToInteract)}: needs click and point actions, a camera, an interactor and a collector.", this);
                enabled = false;
                return;
            }

            // Not disabled in OnDisable: the UI input module shares these actions.
            clickAction.action.Enable();
            pointAction.action.Enable();
        }

        // Polled rather than using a callback: asking whether the pointer is over UI only works outside input callbacks.
        private void Update()
        {
            if (!clickAction.action.WasPressedThisFrame() || IsPointerOverUI())
            {
                return;
            }

            Ray ray = viewCamera.ScreenPointToRay(pointAction.action.ReadValue<Vector2>());
            RaycastHit[] hits = Physics.RaycastAll(ray, MaxClickDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // The nearest clickable thing wins; anything else in the way (ground, the player itself) is ignored.
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.TryGetComponent(out ItemPickup pickup))
                {
                    collector.TryCollect(pickup);
                    return;
                }

                if (hit.collider.TryGetComponent(out Interactable interactable))
                {
                    interactor.TryInteract(interactable);
                    return;
                }
            }
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
