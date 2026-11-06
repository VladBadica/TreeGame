using UnityEngine;

namespace TreeGame.Cameras
{
    /// <summary>
    /// Keeps the camera at a fixed offset from a target, easing to catch up.
    /// The offset is taken from where the camera and target are placed in the scene,
    /// so framing is adjusted by moving the camera in the Editor rather than in code.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("Approximate seconds for the camera to catch up with the target. 0 = locked to it.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.25f;

        private Vector3 offset;
        private Vector3 velocity;
        private float rise;

        /// <summary>
        /// How far above its usual offset the camera sits, e.g. to fit a tall tree in view. Followed with the same easing.
        /// </summary>
        public void SetRise(float height)
        {
            rise = float.IsFinite(height) ? height : 0f;
        }

        private void Start()
        {
            if (target == null)
            {
                Debug.LogWarning($"{nameof(CameraFollow)}: no target assigned.", this);
                enabled = false;
                return;
            }

            offset = transform.position - target.position;
        }

        /// <summary>Jumps to the target instead of easing there, e.g. after loading. Call it after Start.</summary>
        public void SnapToTarget()
        {
            if (target != null)
            {
                transform.position = Desired();
                velocity = Vector3.zero;
            }
        }

        private Vector3 Desired()
        {
            return target.position + offset + Vector3.up * rise;
        }

        // LateUpdate so the target has finished moving this frame; avoids jitter.
        private void LateUpdate()
        {
            Vector3 desired = Desired();
            if (transform.position == desired)
            {
                return;
            }

            transform.position = smoothTime > 0f
                ? Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime)
                : desired;
        }
    }
}
