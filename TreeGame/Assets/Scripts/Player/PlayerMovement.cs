using TreeGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TreeGame.Player
{
    /// <summary>
    /// Walks the player left and right along the X axis only. Depth (Z) and height never change,
    /// which keeps the side-on 2.5D feel. Disable the component to take control away (e.g. during a minigame).
    /// </summary>
    public class PlayerMovement : MonoBehaviour
    {
        [Tooltip("A Vector2 action; only its horizontal part is used.")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private GameConfig config;
        [Tooltip("What the walkable area is centered on (e.g. the tree). Empty = the world origin.")]
        [SerializeField] private Transform walkAreaCenter;
        [Tooltip("How far the player can walk to either side of the center, in world units.")]
        [SerializeField, Min(0f)] private float maxDistanceFromCenter = 8f;

        private void OnEnable()
        {
            if (moveAction == null || moveAction.action == null || config == null)
            {
                Debug.LogWarning($"{nameof(PlayerMovement)}: needs a move action and a {nameof(GameConfig)}.", this);
                enabled = false;
                return;
            }

            moveAction.action.Enable();
        }

        private void OnDisable()
        {
            if (moveAction != null && moveAction.action != null)
            {
                moveAction.action.Disable();
            }
        }

        private void Update()
        {
            float direction = moveAction.action.ReadValue<Vector2>().x;
            if (direction == 0f)
            {
                return;
            }

            float centerX = walkAreaCenter != null ? walkAreaCenter.position.x : 0f;
            Vector3 position = transform.position;
            position.x = Mathf.Clamp(
                position.x + direction * config.PlayerMoveSpeed * Time.deltaTime,
                centerX - maxDistanceFromCenter,
                centerX + maxDistanceFromCenter);
            transform.position = position;
        }
    }
}
