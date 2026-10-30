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
        [Tooltip("How far the player can walk along X, in world units (left limit, right limit).")]
        [SerializeField] private Vector2 walkableRangeX = new Vector2(-13f, 13f);

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

            Vector3 position = transform.position;
            position.x = Mathf.Clamp(
                position.x + direction * config.PlayerMoveSpeed * Time.deltaTime,
                walkableRangeX.x,
                walkableRangeX.y);
            transform.position = position;
        }
    }
}
