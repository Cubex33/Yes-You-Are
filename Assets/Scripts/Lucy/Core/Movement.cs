using UnityEngine;
using System;

namespace Lucy.Core
{
    [RequireComponent(typeof(CharacterController))]
    public class Movement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float sprintMultiplier = 1.5f;
        public float crouchMultiplier = 0.5f;
        public float jumpHeight = 1.5f;
        public float rotationSpeed = 720f;
        public float doubleJumpMultiplyer = 2f;
        public bool doubleJumpEnabled = true;

        [Header("Speed Growth")]
        public float speedGrowPerSecond = 0.5f;
        public float minSpeedMultiplier = 1f;
        public float stopResetMultiplier = 1f;

        [Tooltip("Максимальный множитель скорости при разгоне в воздухе")]
        public float maxSpeedMultiplier = 8f;

        [Header("Gravity Settings")]
        public float groundedGravity = -2f;
        public float gravity = -9.81f;

        [Header("Runtime")]
        public Vector3 moveDirection;
        public Vector3 velocity;
        public Vector3 currentHorizontalVelocity;
        public bool isSprinting;
        public bool isCrouching;
        public bool canDoubleJump;

        public CharacterController Controller { get; set; }
        public bool IsMoving => moveDirection.sqrMagnitude > 0f;

        public event Action OnJumpPerformed;

        private float speedMultiplier;

        private void Awake()
        {
            Controller = GetComponent<CharacterController>();
            speedMultiplier = minSpeedMultiplier;
        }

        private void LateUpdate() => HandleMovement();

        private void HandleMovement()
        {
            if (Controller.isGrounded && velocity.y < 0) velocity.y = groundedGravity;
            else velocity.y += gravity * Time.deltaTime;
            if (IsMoving)
            {
                if (!Controller.isGrounded)
                {
                    speedMultiplier = Mathf.MoveTowards(
                        speedMultiplier,
                        maxSpeedMultiplier,
                        speedGrowPerSecond * Time.deltaTime
                    );
                }
            }
            else
            {
                speedMultiplier = stopResetMultiplier;
                currentHorizontalVelocity = Vector3.zero;
            }

            float speed = moveSpeed * speedMultiplier;

            if (isSprinting) speed *= sprintMultiplier;
            if (isCrouching) speed *= crouchMultiplier;

            Vector3 targetHorizontalVelocity = moveDirection.normalized * speed;

            currentHorizontalVelocity = targetHorizontalVelocity;

            Controller.Move((currentHorizontalVelocity + Vector3.up * velocity.y) * Time.deltaTime);
        }

        public void Jump()
        {
            if (Controller.isGrounded)
            {
                canDoubleJump = true;
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            else
            {
                if (!doubleJumpEnabled || !canDoubleJump) return;
                canDoubleJump = false;
                velocity.y = Mathf.Sqrt((jumpHeight * -2f * gravity) + (currentHorizontalVelocity.magnitude * doubleJumpMultiplyer));
            }

            OnJumpPerformed?.Invoke();
        }
    }
}