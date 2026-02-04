using UnityEngine;

public class TimeSafety : MonoBehaviour
{
    [Tooltip("Maks. deltaTime używane do symulacji (np. 1/20 = 0.05s).")]
    [SerializeField] private float maxDeltaTime = 1f / 20f;

    void Awake()
    {
        // Ogranicza największy krok czasu po lagspike/freeze
        Time.maximumDeltaTime = maxDeltaTime;
    }
}
