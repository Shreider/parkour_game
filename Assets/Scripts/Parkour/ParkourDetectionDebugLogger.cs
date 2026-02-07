using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ParkourWorldRecognition))]
public class ParkourDetectionDebugLogger : MonoBehaviour
{
    [SerializeField] private ParkourWorldRecognition worldRecognition;
    [SerializeField] private bool includeDistanceInLog = true;
    [SerializeField] private bool logContinuously = true;
    [SerializeField] private float logInterval = 0.15f;

    private Collider lastLoggedCollider;
    private float lastLogTime;

    private void Awake()
    {
        if (worldRecognition == null)
        {
            worldRecognition = GetComponent<ParkourWorldRecognition>();
        }
    }

    private void OnEnable()
    {
        lastLoggedCollider = null;
        lastLogTime = -999f;
    }

    private void Update()
    {
        if (worldRecognition == null || !worldRecognition.HasClimbableAhead)
        {
            lastLoggedCollider = null;
            return;
        }

        Collider detectedCollider = worldRecognition.ClimbableHit.collider;
        if (detectedCollider == null)
        {
            return;
        }

        if (!logContinuously && detectedCollider == lastLoggedCollider)
        {
            return;
        }

        if (logContinuously && detectedCollider == lastLoggedCollider && Time.time - lastLogTime < Mathf.Max(logInterval, 0.01f))
        {
            return;
        }

        lastLoggedCollider = detectedCollider;
        lastLogTime = Time.time;

        if (includeDistanceInLog)
        {
            Debug.Log(
                $"[ParkourDebug] Wykryto climbable: {detectedCollider.name}, " +
                $"distance={worldRecognition.ClimbableHit.distance:F2}, " +
                $"objHeight={worldRecognition.DetectedObjectWorldHeight:F2}, " +
                $"objWidth={worldRecognition.DetectedObjectWorldWidth:F2}, " +
                $"topY={worldRecognition.DetectedObjectTopWorldY:F2}, " +
                $"topAboveFeet={worldRecognition.DetectedObjectTopAboveFeet:F2}, " +
                $"heightValid={worldRecognition.IsDetectedHeightValid}",
                detectedCollider);
            return;
        }

        Debug.Log(
            $"[ParkourDebug] Wykryto climbable: {detectedCollider.name}, " +
            $"objHeight={worldRecognition.DetectedObjectWorldHeight:F2}, " +
            $"objWidth={worldRecognition.DetectedObjectWorldWidth:F2}, " +
            $"topY={worldRecognition.DetectedObjectTopWorldY:F2}, " +
            $"topAboveFeet={worldRecognition.DetectedObjectTopAboveFeet:F2}, " +
            $"heightValid={worldRecognition.IsDetectedHeightValid}",
            detectedCollider);
    }
}
