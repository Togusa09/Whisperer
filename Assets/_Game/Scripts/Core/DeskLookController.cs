using UnityEngine;
using UnityEngine.InputSystem;

namespace Whisperer
{
    public class DeskLookController : MonoBehaviour
    {
        [Header("Look")]
        [SerializeField] float lookSensitivity = 0.12f;
        [SerializeField] float minYaw = -18f;
        [SerializeField] float maxYaw = 18f;
        [SerializeField] float minPitch = -12f;
        [SerializeField] float maxPitch = 10f;
        [SerializeField] bool lockCursor = true;

        Quaternion initialLocalRotation;
        float yaw;
        float pitch;

        void Awake()
        {
            CacheCurrentRotation();
        }

        void OnEnable()
        {
            CacheCurrentRotation();

            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        void OnDisable()
        {
            transform.localRotation = initialLocalRotation;
            yaw = 0f;
            pitch = 0f;
        }

        void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue();
            yaw = Mathf.Clamp(yaw + delta.x * lookSensitivity, minYaw, maxYaw);
            pitch = Mathf.Clamp(pitch - delta.y * lookSensitivity, minPitch, maxPitch);

            transform.localRotation = initialLocalRotation * Quaternion.Euler(pitch, yaw, 0f);
        }

        void CacheCurrentRotation()
        {
            initialLocalRotation = transform.localRotation;
            yaw = 0f;
            pitch = 0f;
        }
    }
}