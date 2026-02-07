using UnityEngine;

[DisallowMultipleComponent]
public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private InputReader inputReader;

    [Header("Follow")]
    [SerializeField] private float distance = 4.5f;
    [SerializeField] private float height = 1.6f;
    [SerializeField] private float followSmoothTime = 0.08f;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 0.12f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnStart = true;

    private float yaw;
    private float pitch;
    private Vector3 currentVelocity;

    private void Awake()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        if (inputReader == null && target != null)
        {
            inputReader = target.GetComponent<InputReader>();
        }

        Vector3 currentEuler = transform.eulerAngles;
        yaw = currentEuler.y;
        pitch = NormalizePitch(currentEuler.x);
    }

    private void Start()
    {
        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void LateUpdate()
    {
        if (target == null || inputReader == null)
        {
            return;
        }

        Vector2 lookInput = inputReader.Look;
        yaw += lookInput.x * lookSensitivity;
        pitch -= lookInput.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 focusPoint = target.position + Vector3.up * height;
        Vector3 desiredPosition = focusPoint - rotation * Vector3.forward * distance;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            followSmoothTime);

        transform.rotation = rotation;
    }

    private static float NormalizePitch(float rawPitch)
    {
        if (rawPitch > 180f)
        {
            rawPitch -= 360f;
        }

        return rawPitch;
    }
}
