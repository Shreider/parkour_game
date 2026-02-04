using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLookInputSystem : MonoBehaviour
{
    [SerializeField] private Transform playerRoot;
    [SerializeField] private Transform cameraPivot;

    private Vector2 look;
    private float yaw, pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnLook(InputValue value)
    {
        look = value.Get<Vector2>();
    }

    void Update()
    {
        yaw   += look.x * 0.08f;
        pitch -= look.y * 0.08f;
        pitch = Mathf.Clamp(pitch, -60f, 75f);

        playerRoot.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
