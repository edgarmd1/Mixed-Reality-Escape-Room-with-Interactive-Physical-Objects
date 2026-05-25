using UnityEngine;

public class Habitacion217Manager : MonoBehaviour
{
    public static Habitacion217Manager Instance { get; private set; }

    [Header("Spawn")]
    [SerializeField, Tooltip("spawn habitación 217")]
    private Transform spawnInicio;

    public Transform SpawnInicio => spawnInicio;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
