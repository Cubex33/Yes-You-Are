// Game layer
// Controller
// V1.1
using UnityEngine;
using System;
using UnityEngine.InputSystem;
using Lucy.Core;

namespace Lucy.Game 
{
    [RequireComponent(typeof(Movement))]
    public class FPPController : MonoBehaviour
    {
        [Tooltip("Camera")]
        public Camera mainCamera;
        public Transform headSocket;
        [HideInInspector] public Movement movement;
        [HideInInspector] public Vector2 moveInput;


        protected virtual void Awake()
        {
            movement = GetComponent<Movement>();

            InputProvider.Global.Controls.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
            InputProvider.Global.Controls.Player.Move.canceled += _ => moveInput = Vector2.zero;

            InputProvider.Global.Controls.Player.Sprint.performed += _ => movement.isSprinting = true;
            InputProvider.Global.Controls.Player.Sprint.canceled += _ => movement.isSprinting = false;

            InputProvider.Global.Controls.Player.Crouch.performed += _ => movement.isCrouching = true;
            InputProvider.Global.Controls.Player.Crouch.canceled += _ => movement.isCrouching = false;

            InputProvider.Global.Controls.Player.Jump.performed += _ => movement.Jump();

        }

        protected virtual void Update()
        {
            UpdateRotation();
            UpdateMovement();
        }


        void UpdateRotation()
        {
            if (mainCamera == null) return;
            Vector3 cameraEuler = mainCamera.transform.eulerAngles;
            movement.transform.rotation = Quaternion.Euler(0f, cameraEuler.y, 0f);
            headSocket.rotation = Quaternion.Euler(cameraEuler.x, cameraEuler.y, 0f);
        }

        void UpdateMovement()
        {
            Vector3 moveDir = transform.forward * moveInput.y + transform.right * moveInput.x;
            movement.moveDirection = new Vector3(moveDir.x, 0f, moveDir.z).normalized;
        }

        void OnEnable() => InputProvider.Global.Controls.Player.Enable();
        void OnDisable()
        {
            if (InputProvider.instance != null) InputProvider.Global.Controls.Player.Disable();
        }
    }
}