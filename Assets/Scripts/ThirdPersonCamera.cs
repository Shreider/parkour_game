/*
PL: ThirdPersonCamera — follow + obrót z Look (czytane z PlayerMovementCC)
PL: Kamera szuka targetu po tagu Player.
*/
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private float distance = 4.0f;

    [Header("Look")]
    [SerializeField] private float sensitivity = 0.10f;
    [SerializeField] private float pitchMin = -35f;
    [SerializeField] private float pitchMax = 70f;

    [Header("Smoothing")]
    [SerializeField] private float followSmooth = 12f;

    private PlayerMovementCC inputSource;
    private float yaw;
    private float pitch;

    void Start()
    {
        if (!target)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) target = go.transform;
        }

        if (target) inputSource = target.GetComponent<PlayerMovementCC>();

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (!target) return;

        Vector2 look = inputSource ? inputSource.LookInput : Vector2.zero;

        yaw += look.x * sensitivity;
        pitch -= look.y * sensitivity;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + pivotOffset;

        Vector3 desiredPos = pivot - (rot * Vector3.forward) * distance;
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSmooth * Time.deltaTime);
        transform.rotation = rot;
    }
}
