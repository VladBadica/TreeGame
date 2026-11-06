using TreeGame.Trees;
using UnityEngine;

namespace TreeGame.Cameras
{
    /// <summary>
    /// Pulls the camera back as the tree grows taller than it shows, so the whole tree stays in view, and eases back in
    /// if it gets shorter. The bottom of the view stays where it is, so the ground doesn't move: the view gets taller
    /// (and wider with it) and the camera rises by as much as it grows. Never closer than the camera was set up, which
    /// is the framing for walking around. Only reads how tall the tree is; the camera's following stays with
    /// <see cref="CameraFollow"/>, which it tells how far to rise.
    /// </summary>
    // Before CameraFollow, so the rise it sets is followed the same frame.
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(Camera))]
    public class CameraFitsTree : MonoBehaviour
    {
        [SerializeField] private TreeVisuals tree;
        [SerializeField] private CameraFollow follow;
        [Tooltip("Space kept above the top of the tree, in world units.")]
        [SerializeField, Min(0f)] private float headroom = 0.4f;
        [Tooltip("Approximate seconds for the view to catch up with the tree's height. 0 = at once.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.8f;

        private Camera view;
        private float closestSize;
        private float viewBottom;
        private float size;
        private float sizeVelocity;
        private bool placed;

        private void Start()
        {
            view = GetComponent<Camera>();
            if (tree == null || follow == null || !view.orthographic)
            {
                Debug.LogWarning($"{nameof(CameraFitsTree)}: needs the tree, the camera's follow and an orthographic camera.", this);
                enabled = false;
                return;
            }

            closestSize = view.orthographicSize;
            size = closestSize;
            viewBottom = ViewMiddleHeightAtTree() - closestSize;
        }

        private void LateUpdate()
        {
            float wanted = Mathf.Max(closestSize, (tree.TopHeight + headroom - viewBottom) * 0.5f);

            // The first frame jumps straight there, e.g. to a big tree just loaded, rather than gliding out.
            if (!placed)
            {
                placed = true;
                Apply(wanted);
                follow.SnapToTarget();
                return;
            }

            if (size == wanted)
            {
                return;
            }

            float next = smoothTime > 0f ? Mathf.SmoothDamp(size, wanted, ref sizeVelocity, smoothTime) : wanted;
            Apply(Mathf.Abs(next - wanted) < 0.001f ? wanted : next);
        }

        private void Apply(float newSize)
        {
            size = newSize;
            view.orthographicSize = size;

            // Rising by as much as the view grew keeps its bottom where it was.
            follow.SetRise(size - closestSize);
        }

        /// <summary>How high the middle of the view is where the tree stands, along the camera's slightly tilted gaze.</summary>
        private float ViewMiddleHeightAtTree()
        {
            Vector3 gaze = transform.forward;
            if (Mathf.Abs(gaze.z) < 0.0001f)
            {
                return transform.position.y;
            }

            float distance = (tree.transform.position.z - transform.position.z) / gaze.z;
            return transform.position.y + gaze.y * distance;
        }
    }
}
