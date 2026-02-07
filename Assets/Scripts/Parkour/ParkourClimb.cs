using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(ParkourWorldRecognition))]
[RequireComponent(typeof(InputReader))]
public class ParkourClimb : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private ParkourWorldRecognition worldRecognition;
    [SerializeField] private PlayerLocomotion playerLocomotion;
    [SerializeField] private InputReader inputReader;

    [Header("Trigger Conditions")]
    [SerializeField] private float maxClimbDetectDistance = 1.2f;
    [SerializeField] private bool requireGrounded = true;
    [SerializeField] [Range(0.1f, 1f)] private float maxClimbHeightPlayerRatio = 0.6667f;

    [Header("Climb Move (No Animation)")]
    [SerializeField] private float lowClimbMoveSpeed = 5.2f;
    [SerializeField] private float highClimbVerticalSpeed = 4.8f;
    [SerializeField] private float highClimbForwardSpeed = 4.5f;
    [SerializeField] private float climbForwardOffset = 0.15f;
    [SerializeField] private float climbForwardOnTopOffset = 0.05f;
    [SerializeField] private float widthToForwardFactor = 0.05f;
    [SerializeField] private float maxWidthForwardContribution = 0.15f;
    [SerializeField] private float maxTotalForwardOffset = 0.35f;
    [SerializeField] private float runClimbForwardBoostFactor = 0.08f;
    [SerializeField] private float maxRunClimbForwardBoost = 0.45f;
    [SerializeField] private float climbTopOffset = 0.03f;
    [SerializeField] private float climbCooldown = 0.2f;

    [Header("Vault (Hold Space In Run)")]
    [SerializeField] private float minRunSpeedForVault = 2f;
    [SerializeField] [Range(0.1f, 1f)] private float maxVaultHeightPlayerRatio = 0.55f;
    [SerializeField] private float maxVaultObjectWidth = 2.2f;
    [SerializeField] private float vaultForwardBase = 1.1f;
    [SerializeField] private float vaultForwardWidthFactor = 0.55f;
    [SerializeField] private float vaultForwardSpeedFactor = 0.14f;
    [SerializeField] private float maxVaultSpeedForwardBoost = 1.1f;
    [SerializeField] private float maxVaultForwardDistance = 2.8f;
    [SerializeField] private float vaultHeightOffset = 0.45f;
    [SerializeField] private float vaultUpSpeed = 5f;
    [SerializeField] private float vaultForwardSpeed = 6f;
    [SerializeField] private float vaultDownSpeed = 5.5f;
    [SerializeField] private float vaultCooldown = 0.15f;
    [SerializeField] private float landingProbeHeight = 2.5f;
    [SerializeField] private float landingProbeDistance = 5f;
    [SerializeField] private float landingSurfaceOffset = 0.02f;
    [SerializeField] private LayerMask landingLayerMask = ~0;

    private bool isClimbing;
    private float nextAllowedClimbTime;

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (worldRecognition == null)
        {
            worldRecognition = GetComponent<ParkourWorldRecognition>();
        }

        if (playerLocomotion == null)
        {
            playerLocomotion = GetComponent<PlayerLocomotion>();
        }

        if (inputReader == null)
        {
            inputReader = GetComponent<InputReader>();
        }
    }

    private void Update()
    {
        if (isClimbing || Time.time < nextAllowedClimbTime)
        {
            return;
        }

        if (inputReader == null || worldRecognition == null || !worldRecognition.HasClimbableAhead)
        {
            return;
        }

        RaycastHit hit = worldRecognition.ClimbableHit;
        if (hit.collider == null)
        {
            return;
        }

        if (requireGrounded && characterController != null && !characterController.isGrounded)
        {
            return;
        }

        if (hit.distance > maxClimbDetectDistance)
        {
            return;
        }

        float playerWorldHeight = characterController.bounds.size.y;
        float currentSpeed = worldRecognition.CurrentPlanarSpeed;

        if (inputReader.JumpAltPressedThisFrame && CanVault(playerWorldHeight, currentSpeed))
        {
            StartCoroutine(PerformVault(hit, currentSpeed));
            return;
        }

        if (!inputReader.JumpPressedThisFrame)
        {
            return;
        }

        float maxAllowedClimbHeight = playerWorldHeight * maxClimbHeightPlayerRatio;
        bool requiresHighClimb = worldRecognition.DetectedObjectTopAboveFeet > maxAllowedClimbHeight;

        StartCoroutine(PerformClimb(hit, requiresHighClimb, currentSpeed));
    }

    private bool CanVault(float playerWorldHeight, float currentSpeed)
    {
        if (currentSpeed < minRunSpeedForVault)
        {
            return false;
        }

        float maxVaultHeight = playerWorldHeight * maxVaultHeightPlayerRatio;
        if (worldRecognition.DetectedObjectTopAboveFeet > maxVaultHeight)
        {
            return false;
        }

        if (worldRecognition.DetectedObjectWorldWidth > maxVaultObjectWidth)
        {
            return false;
        }

        return true;
    }

    private IEnumerator PerformClimb(RaycastHit hit, bool requiresHighClimb, float approachSpeed)
    {
        isClimbing = true;
        nextAllowedClimbTime = Time.time + Mathf.Max(climbCooldown, 0f);

        Vector3 startPosition = transform.position;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }
        forward.Normalize();

        Bounds playerBounds = characterController.bounds;
        float feetOffsetFromPivot = startPosition.y - playerBounds.min.y;

        float widthContribution = Mathf.Min(
            Mathf.Max(worldRecognition.DetectedObjectWorldWidth, 0f) * Mathf.Max(widthToForwardFactor, 0f),
            Mathf.Max(maxWidthForwardContribution, 0f));

        float momentumForward = Mathf.Min(
            Mathf.Max(approachSpeed, 0f) * Mathf.Max(runClimbForwardBoostFactor, 0f),
            Mathf.Max(maxRunClimbForwardBoost, 0f));

        float surfaceY = worldRecognition.DetectedObjectTopWorldY;
        float unclampedForward = climbForwardOffset + climbForwardOnTopOffset + widthContribution + momentumForward;
        float finalForward = Mathf.Clamp(unclampedForward, 0f, Mathf.Max(maxTotalForwardOffset, 0f));

        Vector3 topForwardPoint = hit.point + forward * finalForward;
        Vector3 finalTargetPosition = new Vector3(
            topForwardPoint.x,
            surfaceY + feetOffsetFromPivot + climbTopOffset,
            topForwardPoint.z);

        bool wasLocomotionEnabled = playerLocomotion != null && playerLocomotion.enabled;
        if (wasLocomotionEnabled)
        {
            playerLocomotion.enabled = false;
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        float speedScale = 1f + Mathf.Clamp01(approachSpeed / 8f) * 0.35f;

        if (requiresHighClimb)
        {
            Vector3 verticalTarget = new Vector3(startPosition.x, finalTargetPosition.y, startPosition.z);
            yield return MoveToPosition(verticalTarget, highClimbVerticalSpeed * speedScale);
            yield return MoveToPosition(finalTargetPosition, highClimbForwardSpeed * speedScale);
        }
        else
        {
            yield return MoveToPosition(finalTargetPosition, lowClimbMoveSpeed * speedScale);
        }

        transform.position = finalTargetPosition;

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        if (wasLocomotionEnabled)
        {
            playerLocomotion.enabled = true;
        }

        isClimbing = false;
    }

    private IEnumerator PerformVault(RaycastHit hit, float approachSpeed)
    {
        isClimbing = true;
        nextAllowedClimbTime = Time.time + Mathf.Max(vaultCooldown, 0f);

        Vector3 startPosition = transform.position;
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }
        forward.Normalize();

        Bounds playerBounds = characterController.bounds;
        float feetOffsetFromPivot = startPosition.y - playerBounds.min.y;

        float widthForward = Mathf.Max(worldRecognition.DetectedObjectWorldWidth, 0f) * Mathf.Max(vaultForwardWidthFactor, 0f);
        float speedForward = Mathf.Min(
            Mathf.Max(approachSpeed, 0f) * Mathf.Max(vaultForwardSpeedFactor, 0f),
            Mathf.Max(maxVaultSpeedForwardBoost, 0f));

        float forwardDistance = Mathf.Clamp(
            vaultForwardBase + widthForward + speedForward,
            0.1f,
            Mathf.Max(maxVaultForwardDistance, 0.1f));

        Vector3 landingXZ = hit.point + forward * forwardDistance;
        float landingPivotY = ResolveLandingPivotY(landingXZ, feetOffsetFromPivot, startPosition.y);
        Vector3 landingPosition = new Vector3(landingXZ.x, landingPivotY, landingXZ.z);

        float obstacleTopPivotY = worldRecognition.DetectedObjectTopWorldY + feetOffsetFromPivot + climbTopOffset;
        float apexY = Mathf.Max(obstacleTopPivotY + vaultHeightOffset, startPosition.y + 0.2f);

        Vector3 apexPosition = new Vector3(
            Mathf.Lerp(startPosition.x, landingPosition.x, 0.4f),
            apexY,
            Mathf.Lerp(startPosition.z, landingPosition.z, 0.4f));

        Vector3 crossPosition = new Vector3(
            Mathf.Lerp(startPosition.x, landingPosition.x, 0.82f),
            apexY,
            Mathf.Lerp(startPosition.z, landingPosition.z, 0.82f));

        bool wasLocomotionEnabled = playerLocomotion != null && playerLocomotion.enabled;
        if (wasLocomotionEnabled)
        {
            playerLocomotion.enabled = false;
        }

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        yield return MoveToPosition(apexPosition, vaultUpSpeed);
        yield return MoveToPosition(crossPosition, vaultForwardSpeed);
        yield return MoveToPosition(landingPosition, vaultDownSpeed);

        transform.position = landingPosition;

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        if (wasLocomotionEnabled)
        {
            playerLocomotion.enabled = true;
        }

        isClimbing = false;
    }

    private float ResolveLandingPivotY(Vector3 landingXZ, float feetOffsetFromPivot, float fallbackPivotY)
    {
        Vector3 origin = new Vector3(landingXZ.x, landingXZ.y + Mathf.Max(landingProbeHeight, 0.2f), landingXZ.z);
        float distance = Mathf.Max(landingProbeDistance, 0.2f);

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, distance, landingLayerMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y + feetOffsetFromPivot + landingSurfaceOffset;
        }

        return fallbackPivotY;
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition, float moveSpeed)
    {
        float safeSpeed = Mathf.Max(moveSpeed, 0.01f);
        while ((transform.position - targetPosition).sqrMagnitude > 0.0001f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, safeSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;
    }
}
