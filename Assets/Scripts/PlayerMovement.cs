// --- PL: Movement (CharacterController + Input System) ---
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Transform cameraYaw; // zwykle Player albo Camera_Rig (kierunek przodu)

    [Header("Speed")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8.5f;
    [SerializeField] private float acceleration = 18f;
    [SerializeField] private float airAcceleration = 8f;

    [Header("Jump/Gravity")]
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float coyoteTime = 0.08f;
    [SerializeField] private float jumpBuffer = 0.10f;
    [SerializeField] private float maxFallSpeed = -35f;

    private CharacterController cc;

    private Vector2 moveInput;
    private bool sprintHeld;
    private float verticalVel;
    private Vector3 planarVel;

    private float lastGroundedTime;
    private float lastJumpPressedTime;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (cameraYaw == null) cameraYaw = transform;

        // Startowe "czyszczenie" timerów (bez edge-case na 1. klatkach)
        lastGroundedTime = -999f;
        lastJumpPressedTime = -999f;
    }

    // Input System (Send Messages)
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnSprint(InputValue value) => sprintHeld = value.isPressed;
    public void OnJump(InputValue value)
    {
        if (value.isPressed) lastJumpPressedTime = Time.time;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        bool grounded = cc.isGrounded;
        if (grounded)
        {
            lastGroundedTime = Time.time;
            if (verticalVel < 0f) verticalVel = -2f; // trzyma przy ziemi
        }

        // Kierunek ruchu względem kamery (tylko yaw)
        Vector3 forward = cameraYaw.forward; forward.y = 0f; forward.Normalize();
        Vector3 right   = cameraYaw.right;   right.y = 0f;   right.Normalize();

        Vector3 desiredDir = forward * moveInput.y + right * moveInput.x;
        if (desiredDir.sqrMagnitude > 1f) desiredDir.Normalize();

        float targetSpeed = sprintHeld ? sprintSpeed : walkSpeed;
        Vector3 targetPlanarVel = desiredDir * targetSpeed;

        float accel = grounded ? acceleration : airAcceleration;
        planarVel = Vector3.MoveTowards(planarVel, targetPlanarVel, accel * dt);

        // Jump (buffer + coyote)
        bool canCoyote = (Time.time - lastGroundedTime) <= coyoteTime;
        bool hasBufferedJump = (Time.time - lastJumpPressedTime) <= jumpBuffer;

        if (hasBufferedJump && canCoyote)
        {
            lastJumpPressedTime = -999f;
            lastGroundedTime = -999f;

            verticalVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Gravity
        verticalVel += gravity * dt;
        if (verticalVel < maxFallSpeed) verticalVel = maxFallSpeed;

        Vector3 velocity = planarVel + Vector3.up * verticalVel;
        cc.Move(velocity * dt);
    }
}
