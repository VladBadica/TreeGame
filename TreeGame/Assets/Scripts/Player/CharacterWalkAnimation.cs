using UnityEngine;

namespace TreeGame.Player
{
    /// <summary>
    /// Simple procedural walk for the placeholder character: turns toward the walking direction,
    /// swings arms and legs, and bobs. It watches its own movement rather than reading PlayerMovement,
    /// so anything that moves the character (input, cutscenes, minigames) animates it the same way.
    /// Purely presentation; replaceable by an Animator once there is a real character model.
    /// </summary>
    public class CharacterWalkAnimation : MonoBehaviour
    {
        private const float MovingThreshold = 0.05f; // World units per second.
        private const float WalkBlendSpeed = 6f;     // How quickly limbs start and stop swinging.

        [Header("Parts")]
        [Tooltip("The model root that turns and bobs. Its local -Z is the character's front.")]
        [SerializeField] private Transform body;
        [Tooltip("Limb pivots at the hips and shoulders; they swing around their local X axis.")]
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;

        [Header("Feel")]
        [Tooltip("Degrees the character turns from facing the camera toward its walking direction. 90 = full side profile.")]
        [SerializeField, Range(0f, 90f)] private float facingAngle = 50f;
        [Tooltip("Turning speed in degrees per second.")]
        [SerializeField, Min(0f)] private float turnSpeed = 600f;
        [SerializeField, Range(0f, 80f)] private float limbSwingAngle = 30f;
        [SerializeField, Min(0f)] private float stepsPerSecond = 4f;
        [SerializeField, Min(0f)] private float bobHeight = 0.05f;

        private Vector3 bodyRestPosition;
        private float lastX;
        private float facingSign = 1f;
        private float walkBlend;
        private float phase;

        private void Start()
        {
            if (body == null)
            {
                Debug.LogWarning($"{nameof(CharacterWalkAnimation)}: no body assigned.", this);
                enabled = false;
                return;
            }

            bodyRestPosition = body.localPosition;
            lastX = transform.position.x;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            float velocityX = (transform.position.x - lastX) / deltaTime;
            lastX = transform.position.x;

            bool isMoving = Mathf.Abs(velocityX) > MovingThreshold;
            if (isMoving)
            {
                facingSign = Mathf.Sign(velocityX);
            }

            // Rotating the front (-Z) by a negative yaw turns it toward +X, so negate for the facing side.
            Quaternion targetRotation = Quaternion.Euler(0f, -facingSign * facingAngle, 0f);
            float targetBlend = isMoving ? 1f : 0f;

            // Standing still with limbs at rest and turn finished: nothing to update.
            if (!isMoving && walkBlend == 0f && body.localRotation == targetRotation)
            {
                return;
            }

            body.localRotation = Quaternion.RotateTowards(body.localRotation, targetRotation, turnSpeed * deltaTime);
            walkBlend = Mathf.MoveTowards(walkBlend, targetBlend, WalkBlendSpeed * deltaTime);
            if (isMoving)
            {
                // One step is half a sine cycle.
                phase += stepsPerSecond * Mathf.PI * deltaTime;
            }

            // Arms swing opposite to the leg on the same side, like a natural walk.
            float swing = Mathf.Sin(phase) * limbSwingAngle * walkBlend;
            SetSwing(leftLeg, swing);
            SetSwing(rightLeg, -swing);
            SetSwing(leftArm, -swing);
            SetSwing(rightArm, swing);

            body.localPosition = bodyRestPosition + Vector3.up * (Mathf.Abs(Mathf.Sin(phase)) * bobHeight * walkBlend);
        }

        private static void SetSwing(Transform limb, float angle)
        {
            if (limb != null)
            {
                limb.localRotation = Quaternion.Euler(angle, 0f, 0f);
            }
        }
    }
}
