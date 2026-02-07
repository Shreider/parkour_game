using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(InputReader))]
public class PlayerLocomotion : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private InputReader inputReader;
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float baseRunSpeed = 7.5f;
    [SerializeField] private float rotationSpeed = 18f;
    [SerializeField] private float groundAcceleration = 45f;
    [SerializeField] private float groundDeceleration = 55f;
    [SerializeField] private float airAcceleration = 20f;
    [SerializeField] private float maxAirSpeed = 9.5f;

    [Header("Jump / Gravity")]
    [SerializeField] private float jumpHeight = 1.6f;
    [SerializeField] private float gravity = -30f;
    [SerializeField] private float fallGravityMultiplier = 1.35f;
    [SerializeField] private float maxFallSpeed = -35f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float groundedVerticalVelocity = -2f;

    private Vector3 planarVelocity;
    private float verticalVelocity;
    private float coyoteTimer;
    private float jumpBufferTimer;

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (inputReader == null)
        {
            inputReader = GetComponent<InputReader>();
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (characterController == null || inputReader == null)
        {
            return;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        bool isGrounded = characterController.isGrounded;
        Vector2 moveInput = inputReader.Move;
        Vector3 moveDirection = GetCameraRelativeDirection(moveInput);

        UpdateJumpTimers(isGrounded);
        HandleHorizontalMovement(moveDirection, isGrounded);
        HandleVerticalMotion(isGrounded);
        ApplyRotation(moveDirection);

        Vector3 fullVelocity = planarVelocity + Vector3.up * verticalVelocity;
        characterController.Move(fullVelocity * Time.deltaTime);
    }

    private void UpdateJumpTimers(bool isGrounded)
    {
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (inputReader.JumpPressedThisFrame)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void HandleHorizontalMovement(Vector3 moveDirection, bool isGrounded)
    {
        Vector3 targetPlanarVelocity = moveDirection * baseRunSpeed;
        bool hasInput = moveDirection.sqrMagnitude > 0.001f;

        float acceleration;
        if (isGrounded)
        {
            acceleration = hasInput ? groundAcceleration : groundDeceleration;
        }
        else
        {
            acceleration = hasInput ? airAcceleration : airAcceleration * 0.5f;
        }

        planarVelocity = Vector3.MoveTowards(planarVelocity, targetPlanarVelocity, acceleration * Time.deltaTime);

        if (!isGrounded && planarVelocity.magnitude > maxAirSpeed)
        {
            planarVelocity = planarVelocity.normalized * maxAirSpeed;
        }
    }

    private void HandleVerticalMotion(bool isGrounded)
    {
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedVerticalVelocity;
        }

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        float gravityScale = verticalVelocity < 0f ? fallGravityMultiplier : 1f;
        verticalVelocity += gravity * gravityScale * Time.deltaTime;
        verticalVelocity = Mathf.Max(verticalVelocity, maxFallSpeed);
    }

    private void ApplyRotation(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private Vector3 GetCameraRelativeDirection(Vector2 moveInput)
    {
        if (moveInput.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        if (cameraTransform == null)
        {
            return new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 direction = cameraForward * moveInput.y + cameraRight * moveInput.x;
        return direction.normalized;
    }
}
