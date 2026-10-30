using UnityEngine;

namespace TreeGame.Cameras
{
    /// <summary>
    /// Moves a background layer by a fraction of the camera's horizontal movement, so distant layers
    /// drift slower than the foreground. An orthographic camera has no natural parallax, so this
    /// provides the depth cue that makes the scene read as layered 2.5D.
    /// </summary>
    // Runs after CameraFollow has moved the camera this frame, so layers don't lag a frame behind.
    [DefaultExecutionOrder(100)]
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [Tooltip("0 = moves like the foreground (no parallax), 1 = moves with the camera (infinitely far away).")]
        [SerializeField, Range(0f, 1f)] private float followFactor = 0.5f;

        private Vector3 startPosition;
        private float cameraStartX;
        private float lastCameraX;

        private void Start()
        {
            if (cameraTransform == null)
            {
                Debug.LogWarning($"{nameof(ParallaxLayer)}: no camera assigned.", this);
                enabled = false;
                return;
            }

            startPosition = transform.position;
            cameraStartX = lastCameraX = cameraTransform.position.x;
        }

        private void LateUpdate()
        {
            float cameraX = cameraTransform.position.x;
            if (cameraX == lastCameraX)
            {
                return;
            }

            lastCameraX = cameraX;
            transform.position = startPosition + Vector3.right * ((cameraX - cameraStartX) * followFactor);
        }
    }
}
