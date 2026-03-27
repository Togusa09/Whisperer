using UnityEngine;
using UnityEngine.InputSystem;

namespace Whisperer
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonMover : MonoBehaviour
    {
        [Header("Look")]
        [SerializeField] Transform pitchTransform;
        [SerializeField] float lookSensitivity = 140f;
        [SerializeField] float minPitch = -80f;
        [SerializeField] float maxPitch = 80f;
        [SerializeField] bool lockCursor = true;

        [Header("Move")]
        [SerializeField] float walkSpeed = 3.5f;
        [SerializeField] float sprintSpeed = 5.5f;
        [SerializeField] float jumpHeight = 1.2f;
        [SerializeField] float gravity = -19.62f;

        CharacterController characterController;
        float pitch;
        float verticalVelocity;
        bool inputEnabled = true;

        void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (pitchTransform == null)
            {
                pitchTransform = transform;
            }
        }

        void OnEnable()
        {
            GameCursorController.StateChanged += HandleCursorStateChanged;
            ApplyCursorState();
        }

        void OnDisable()
        {
            GameCursorController.StateChanged -= HandleCursorStateChanged;
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
        }

        void Update()
        {
            if (!inputEnabled)
            {
                return;
            }

            UpdateLook(Time.deltaTime);
            UpdateMovement(Time.deltaTime);
        }

        void UpdateLook(float deltaTime)
        {
            if (GameCursorController.IsModalUiActive)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 look = mouse.delta.ReadValue();
            float yawDelta = look.x * lookSensitivity * deltaTime;
            float pitchDelta = look.y * lookSensitivity * deltaTime;

            transform.Rotate(0f, yawDelta, 0f, Space.Self);

            pitch = Mathf.Clamp(pitch - pitchDelta, minPitch, maxPitch);
            pitchTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        void HandleCursorStateChanged()
        {
            ApplyCursorState();
        }

        void ApplyCursorState()
        {
            GameCursorController.ApplyGameplayCursor(lockCursor);
        }

        void UpdateMovement(float deltaTime)
        {
            Keyboard keyboard = Keyboard.current;
            Vector2 moveInput = Vector2.zero;
            bool isSprinting = false;

            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed) moveInput.x -= 1f;
                if (keyboard.dKey.isPressed) moveInput.x += 1f;
                if (keyboard.sKey.isPressed) moveInput.y -= 1f;
                if (keyboard.wKey.isPressed) moveInput.y += 1f;

                moveInput = Vector2.ClampMagnitude(moveInput, 1f);
                isSprinting = keyboard.leftShiftKey.isPressed;
            }

            float speed = isSprinting ? sprintSpeed : walkSpeed;

            Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y) * speed;

            bool grounded = characterController.isGrounded;
            if (grounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (grounded && keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * deltaTime;
            move.y = verticalVelocity;

            characterController.Move(move * deltaTime);
        }
    }
}
