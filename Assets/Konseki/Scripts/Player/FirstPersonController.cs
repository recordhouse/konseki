using UnityEngine;

namespace Konseki
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        [Header("Slow walking")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 2.15f;
        [SerializeField, Min(0.1f)] private float acceleration = 9f;

        [Header("View")]
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 2.8f;
        [SerializeField] private float minPitch = -75f;
        [SerializeField] private float maxPitch = 78f;

        [Header("Physics")]
        [SerializeField] private float gravity = -24f;

        private CharacterController characterController;
        private Camera viewCamera;
        private Vector3 planarVelocity;
        private float verticalVelocity;
        private float pitch;

        public Camera ViewCamera => viewCamera;
        public bool InputEnabled { get; set; } = true;

        public void Configure(Camera cameraToUse)
        {
            viewCamera = cameraToUse;
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (viewCamera == null)
            {
                viewCamera = GetComponentInChildren<Camera>();
            }
        }

        private void Start()
        {
            LockCursor(true);
        }

        private void OnDisable()
        {
            LockCursor(false);
        }

        private void Update()
        {
            HandleCursor();

            if (!InputEnabled)
            {
                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, acceleration * Time.deltaTime);
                ApplyMovement(Vector3.zero);
                return;
            }

            HandleLook();
            HandleWalk();
        }

        private void HandleCursor()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                LockCursor(false);
            }
            else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor(true);
            }
        }

        private void HandleLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked || viewCamera == null)
            {
                return;
            }

            float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

            transform.Rotate(Vector3.up, mouseX, Space.World);
            pitch = Mathf.Clamp(pitch - mouseY, minPitch, maxPitch);
            viewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleWalk()
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D)) horizontal += 1f;
            if (Input.GetKey(KeyCode.S)) vertical -= 1f;
            if (Input.GetKey(KeyCode.W)) vertical += 1f;

            Vector3 input = Vector3.ClampMagnitude(new Vector3(horizontal, 0f, vertical), 1f);
            Vector3 desired = (transform.right * input.x + transform.forward * input.z) * walkSpeed;
            planarVelocity = Vector3.MoveTowards(planarVelocity, desired, acceleration * Time.deltaTime);

            ApplyMovement(planarVelocity);
        }

        private void ApplyMovement(Vector3 horizontalVelocity)
        {
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            Vector3 velocity = horizontalVelocity + Vector3.up * verticalVelocity;
            characterController.Move(velocity * Time.deltaTime);
        }

        private static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
