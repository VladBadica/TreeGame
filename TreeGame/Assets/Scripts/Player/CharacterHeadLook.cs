using UnityEngine;
using UnityEngine.InputSystem;

namespace TreeGame.Player
{
    /// <summary>
    /// While resting, turns the character's head toward the pointer (mouse, pen or touch), within a natural limit.
    /// While walking, the head looks straight ahead, i.e. where the body is heading.
    /// Purely presentation: it only rotates the head pivot, so walking and facing are unaffected.
    /// </summary>
    // After CharacterWalkAnimation has turned the body this frame, so the head aims from the body's final pose.
    [DefaultExecutionOrder(50)]
    public class CharacterHeadLook : MonoBehaviour
    {
        [Tooltip("A screen-position action, e.g. UI/Point.")]
        [SerializeField] private InputActionReference pointAction;
        [Tooltip("Head pivot; its local -Z is the face direction. Eyes and hat should be its children.")]
        [SerializeField] private Transform head;
        [SerializeField] private Camera viewCamera;
        [Tooltip("Optional. When assigned, the head faces forward while walking and only follows the pointer at rest.")]
        [SerializeField] private CharacterWalkAnimation walkAnimation;

        [Tooltip("Furthest the head turns away from straight ahead, in degrees.")]
        [SerializeField, Range(0f, 90f)] private float maxLookAngle = 60f;
        [Tooltip("Turning speed in degrees per second.")]
        [SerializeField, Min(0f)] private float turnSpeed = 360f;
        [Tooltip("Aims at a point this far in front of the pointer (toward the camera), so the face stays visible.")]
        [SerializeField, Min(0f)] private float lookTowardCamera = 1f;

        private Vector2 lastPointerPosition;
        private Quaternion lastParentRotation;

        private void OnEnable()
        {
            if (pointAction == null || pointAction.action == null || head == null || viewCamera == null)
            {
                Debug.LogWarning($"{nameof(CharacterHeadLook)}: needs a point action, a head and a camera.", this);
                enabled = false;
                return;
            }

            // Not disabled in OnDisable: the UI input module shares this action.
            pointAction.action.Enable();
        }

        private void LateUpdate()
        {
            Vector2 pointerPosition = pointAction.action.ReadValue<Vector2>();
            bool isWalking = walkAnimation != null && walkAnimation.IsWalking;
            // Straight ahead (identity) follows the body, which already faces the walking direction.
            Quaternion targetRotation = isWalking ? Quaternion.identity : GetTargetLocalRotation(pointerPosition);

            // Pointer still, body still and head already there: nothing to do.
            if (pointerPosition == lastPointerPosition && head.parent.rotation == lastParentRotation &&
                head.localRotation == targetRotation)
            {
                return;
            }

            lastPointerPosition = pointerPosition;
            lastParentRotation = head.parent.rotation;
            head.localRotation = Quaternion.RotateTowards(head.localRotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        private Quaternion GetTargetLocalRotation(Vector2 pointerPosition)
        {
            // Where the pointer ray meets the vertical plane the character stands in.
            Ray ray = viewCamera.ScreenPointToRay(pointerPosition);
            var characterPlane = new Plane(Vector3.forward, head.position);
            if (!characterPlane.Raycast(ray, out float distance))
            {
                return head.localRotation;
            }

            Vector3 target = ray.GetPoint(distance) + Vector3.back * lookTowardCamera;
            Vector3 toTarget = target - head.position;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return head.localRotation;
            }

            // The face is local -Z, so point +Z away from the target.
            Quaternion worldLook = Quaternion.LookRotation(-toTarget, Vector3.up);
            Quaternion localLook = Quaternion.Inverse(head.parent.rotation) * worldLook;
            return Quaternion.RotateTowards(Quaternion.identity, localLook, maxLookAngle);
        }
    }
}
