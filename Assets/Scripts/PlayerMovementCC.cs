/*
PL: PlayerMovementCC — CharacterController + PlayerInput (Send Messages) + camera-relative movement
PL: Shift = HOLD sprint, sprint ma "zryw" w ruchu, ale animacje blendują płynnie (osobny animSpeed01).
PL: Parametry Animatora: Speed(float 0..1), IsGrounded(bool), YVelocity(float), IsSprinting(bool)
*/
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementCC : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;   // jeśli null -> Camera.main
    [SerializeField] private Animator animator;           // Animator na childzie modelu

    [Header("Speeds (m/s)")]
    [SerializeField] private float runSpeed = 4.8f;
    [SerializeField] private float sprintSpeed = 7.2f;

    [Header("Acceleration")]
    [SerializeField] private float runAccel = 12f;
    [SerializeField] private float runDecel = 18f;
    [SerializeField] private float sprintAccel = 55f;     // zryw
    [SerializeField] private float sprintDecel = 30f;

    [Header("Sprint Burst")]
    [SerializeField] private float sprintBurstMultiplier = 1.10f;
    [SerializeField] private float sprintBurstDuration = 0.18f;

    [Header("Rotation")]
    [SerializeField] private float turnSpeed = 14f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedStick = -2f;

    [Header("Animator Blend (smooth)")]
    [SerializeField] private float animBlendTimeRun = 0.12f;     // płynny start biegu (walk jako rozruch)
    [SerializeField] private float animBlendTimeSprint = 0.08f;  // sprint szybciej, ale nie instant

    // Animator params (muszą istnieć w Controllerze)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int YVelHash = Animator.StringToHash("YVelocity");
    private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");

    private CharacterController cc;

    // Input z PlayerInput (Send Messages)
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool sprintHeld;

    public Vector2 LookInput => lookInput; // dla kamery (jeśli czytasz look z playera)

    // Movement state
    private Vector3 planarVelocity;
    private float verticalVelocity;

    // Sprint burst
    private bool wasSprinting;
    private float sprintStartTime;

    // Animator smoothing
    private float animSpeed01;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
    }

    // === PlayerInput (Behavior: Send Messages) ===
    public void OnMove(InputValue v) => moveInput = v.Get<Vector2>();
    public void OnLook(InputValue v) => lookInput = v.Get<Vector2>();
    public void OnSprint(InputValue v) => sprintHeld = v.isPressed; // HOLD

    void Update()
    {
        Vector2 input = Vector2.ClampMagnitude(moveInput, 1f);

        // sprint start -> burst
        if (sprintHeld && !wasSprinting) sprintStartTime = Time.time;
        wasSprinting = sprintHeld;

        // Direction relative to camera
        Vector3 moveDir;
        if (cameraTransform)
        {
            Vector3 fwd = cameraTransform.forward; fwd.y = 0; fwd.Normalize();
            Vector3 right = cameraTransform.right; right.y = 0; right.Normalize();
            moveDir = right * input.x + fwd * input.y;
        }
        else
        {
            moveDir = new Vector3(input.x, 0, input.y);
        }

        float inputMag = input.magnitude;
        if (moveDir.sqrMagnitude > 0.0001f) moveDir.Normalize();

        // Target speed + burst
        float baseMax = sprintHeld ? sprintSpeed : runSpeed;
        float burst = (sprintHeld && (Time.time - sprintStartTime) < sprintBurstDuration) ? sprintBurstMultiplier : 1f;
        float targetSpeed = baseMax * burst * inputMag;

        // Accel / decel
        float currentSpeed = new Vector3(planarVelocity.x, 0, planarVelocity.z).magnitude;
        float accel = sprintHeld ? sprintAccel : runAccel;
        float decel = sprintHeld ? sprintDecel : runDecel;
        float rate = (targetSpeed > currentSpeed) ? accel : decel;

        float newSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);
        planarVelocity = moveDir * newSpeed;

        // Rotate to movement direction
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        // Gravity
        bool grounded = cc.isGrounded;
        if (grounded && verticalVelocity < 0f) verticalVelocity = groundedStick;
        verticalVelocity += gravity * Time.deltaTime;

        // Move
        cc.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

        // Animator (smooth blend)
        if (animator)
        {
            float targetSpeed01 = (sprintSpeed > 0.001f) ? (newSpeed / sprintSpeed) : 0f;
            targetSpeed01 = Mathf.Clamp01(targetSpeed01);

            float blendTime = sprintHeld ? animBlendTimeSprint : animBlendTimeRun;
            blendTime = Mathf.Max(0.001f, blendTime);

            float maxDelta = Time.deltaTime / blendTime;
            animSpeed01 = Mathf.MoveTowards(animSpeed01, targetSpeed01, maxDelta);

            animator.SetFloat(SpeedHash, animSpeed01);
            animator.SetBool(IsGroundedHash, grounded);
            animator.SetFloat(YVelHash, verticalVelocity);
            animator.SetBool(IsSprintingHash, sprintHeld);
        }
    }
}
