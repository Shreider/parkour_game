using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class ParkourWorldRecognition : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController characterController;

    [Header("Forward Ray")]
    [SerializeField] private bool showDebugRay = true;
    [SerializeField] private float rayOriginHeight = 0.001f;
    [SerializeField] private float minRayDistance = 3f;
    [SerializeField] private float maxRayDistance = 30f;
    [SerializeField] private float maxSpeedForRayDistance = 4.5f;
    [SerializeField] private float rayResponseExponent = 0.35f;
    [SerializeField] private float rayDistanceMultiplier = 2f;
    [SerializeField] private Color rayColor = Color.cyan;

    [Header("Climbable Detection")]
    [SerializeField] private LayerMask climbableLayerMask = ~0;
    [SerializeField] private string climbableTag = "Climbable";
    [SerializeField] private bool requireLayerAndTag = false;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
    
    [Header("Height Check")]
    [SerializeField] private bool validateDetectedHeight = false;
    [SerializeField] private float minDetectedHeight = 0.5f;
    [SerializeField] private float maxDetectedHeight = 2.2f;

    [Header("Object Measure Rays")]
    [SerializeField] private float topMeasureRayExtraHeight = 2.5f;
    [SerializeField] private float bottomMeasureRayExtraDepth = 2.5f;
    [SerializeField] private float sideMeasureRayDistance = 6f;
    [SerializeField] private float measureSurfaceInset = 0.02f;
    [SerializeField] private bool showMeasureRays = false;
    [SerializeField] private Color measureRayColor = Color.yellow;

    public bool HasClimbableAhead { get; private set; }
    public RaycastHit ClimbableHit { get; private set; }
    public float CurrentRayDistance { get; private set; }
    public float CurrentPlanarSpeed { get; private set; }
    public float DetectedObjectWorldHeight { get; private set; }
    public float DetectedObjectWorldWidth { get; private set; }
    public float DetectedObjectTopWorldY { get; private set; }
    public float DetectedObjectTopAboveFeet { get; private set; }
    public bool IsDetectedHeightValid { get; private set; }

    private Vector3 previousPosition;

    private void Awake()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        previousPosition = transform.position;
    }

    private void OnEnable()
    {
        previousPosition = transform.position;
        HasClimbableAhead = false;
        ClimbableHit = default;
        CurrentRayDistance = minRayDistance * Mathf.Max(rayDistanceMultiplier, 0f);
        CurrentPlanarSpeed = 0f;
        DetectedObjectWorldHeight = 0f;
        DetectedObjectWorldWidth = 0f;
        DetectedObjectTopWorldY = 0f;
        DetectedObjectTopAboveFeet = 0f;
        IsDetectedHeightValid = false;
    }

    private void LateUpdate()
    {
        if (characterController == null)
        {
            return;
        }

        Vector3 controllerPlanarVelocity = characterController.velocity;
        controllerPlanarVelocity.y = 0f;

        Vector3 delta = transform.position - previousPosition;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float transformPlanarSpeed = new Vector2(delta.x, delta.z).magnitude / dt;
        previousPosition = transform.position;

        CurrentPlanarSpeed = Mathf.Max(controllerPlanarVelocity.magnitude, transformPlanarSpeed);

        float speedRatio = Mathf.Clamp01(CurrentPlanarSpeed / Mathf.Max(maxSpeedForRayDistance, 0.01f));
        speedRatio = Mathf.Pow(speedRatio, rayResponseExponent);
        CurrentRayDistance = Mathf.Lerp(minRayDistance, maxRayDistance, speedRatio) * Mathf.Max(rayDistanceMultiplier, 0f);

        Bounds controllerBounds = characterController.bounds;
        float nearGroundOffset = Mathf.Clamp(rayOriginHeight, 0f, 0.02f);
        Vector3 rayOrigin = new Vector3(transform.position.x, controllerBounds.min.y + nearGroundOffset, transform.position.z);
        Vector3 rayDirection = transform.forward;

        if (showDebugRay)
        {
            Debug.DrawRay(rayOrigin, rayDirection * CurrentRayDistance, rayColor);
        }

        bool hasHit = Physics.Raycast(
            rayOrigin,
            rayDirection,
            out RaycastHit hit,
            CurrentRayDistance,
            climbableLayerMask,
            triggerInteraction);

        if (!hasHit)
        {
            HasClimbableAhead = false;
            ClimbableHit = default;
            DetectedObjectWorldHeight = 0f;
            DetectedObjectWorldWidth = 0f;
            DetectedObjectTopWorldY = 0f;
            DetectedObjectTopAboveFeet = 0f;
            IsDetectedHeightValid = false;
            return;
        }

        bool layerMatch = IsLayerInMask(hit.collider.gameObject.layer, climbableLayerMask);
        bool tagMatch = !string.IsNullOrEmpty(climbableTag) &&
            string.Equals(hit.collider.tag, climbableTag, System.StringComparison.Ordinal);

        MeasureHitObject(hit, rayDirection, out float measuredHeight, out float measuredWidth, out float measuredTopY);

        float feetY = controllerBounds.min.y;
        DetectedObjectWorldHeight = measuredHeight;
        DetectedObjectWorldWidth = measuredWidth;
        DetectedObjectTopWorldY = measuredTopY;
        DetectedObjectTopAboveFeet = measuredTopY - feetY;

        bool passesHeightCheck = !validateDetectedHeight ||
            (DetectedObjectTopAboveFeet >= minDetectedHeight && DetectedObjectTopAboveFeet <= maxDetectedHeight);
        IsDetectedHeightValid = passesHeightCheck;

        bool isClimbable = requireLayerAndTag ? (layerMatch && tagMatch) : (layerMatch || tagMatch);
        bool finalMatch = isClimbable && passesHeightCheck;
        HasClimbableAhead = finalMatch;
        ClimbableHit = finalMatch ? hit : default;
    }

    private static bool IsLayerInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    private void MeasureHitObject(RaycastHit baseHit, Vector3 forwardDirection, out float height, out float width, out float topY)
    {
        Collider target = baseHit.collider;
        Bounds bounds = target.bounds;

        Vector3 inSurfacePoint = baseHit.point - baseHit.normal * Mathf.Max(measureSurfaceInset, 0f);

        float topRayStartY = bounds.max.y + Mathf.Max(topMeasureRayExtraHeight, 0.05f);
        float bottomRayStartY = bounds.min.y - Mathf.Max(bottomMeasureRayExtraDepth, 0.05f);
        float verticalRayDistance = (topRayStartY - bottomRayStartY) + 0.1f;

        Vector3 topRayOrigin = new Vector3(inSurfacePoint.x, topRayStartY, inSurfacePoint.z);
        Vector3 bottomRayOrigin = new Vector3(inSurfacePoint.x, bottomRayStartY, inSurfacePoint.z);

        bool hasTop = target.Raycast(new Ray(topRayOrigin, Vector3.down), out RaycastHit topHit, verticalRayDistance);
        bool hasBottom = target.Raycast(new Ray(bottomRayOrigin, Vector3.up), out RaycastHit bottomHit, verticalRayDistance);

        float measuredTopY = hasTop ? topHit.point.y : bounds.max.y;
        float measuredBottomY = hasBottom ? bottomHit.point.y : bounds.min.y;
        height = Mathf.Max(measuredTopY - measuredBottomY, 0f);
        topY = measuredTopY;

        Vector3 rightAxis = Vector3.Cross(Vector3.up, forwardDirection).normalized;
        if (rightAxis.sqrMagnitude < 0.0001f)
        {
            rightAxis = transform.right;
        }

        float sideDistance = Mathf.Max(sideMeasureRayDistance, 0.1f);
        float centerY = (measuredTopY + measuredBottomY) * 0.5f;
        Vector3 widthCenter = new Vector3(inSurfacePoint.x, centerY, inSurfacePoint.z);

        Vector3 rightRayOrigin = widthCenter + rightAxis * sideDistance;
        Vector3 leftRayOrigin = widthCenter - rightAxis * sideDistance;

        bool hasRight = target.Raycast(new Ray(rightRayOrigin, -rightAxis), out RaycastHit rightHit, sideDistance * 2f);
        bool hasLeft = target.Raycast(new Ray(leftRayOrigin, rightAxis), out RaycastHit leftHit, sideDistance * 2f);

        if (hasLeft && hasRight)
        {
            width = Vector3.Distance(leftHit.point, rightHit.point);
        }
        else
        {
            Vector3 absAxis = new Vector3(Mathf.Abs(rightAxis.x), Mathf.Abs(rightAxis.y), Mathf.Abs(rightAxis.z));
            width = Vector3.Dot(bounds.size, absAxis);
        }

        if (showMeasureRays)
        {
            Debug.DrawRay(topRayOrigin, Vector3.down * verticalRayDistance, measureRayColor);
            Debug.DrawRay(bottomRayOrigin, Vector3.up * verticalRayDistance, measureRayColor);
            Debug.DrawRay(rightRayOrigin, -rightAxis * sideDistance * 2f, measureRayColor);
            Debug.DrawRay(leftRayOrigin, rightAxis * sideDistance * 2f, measureRayColor);
        }
    }
}
