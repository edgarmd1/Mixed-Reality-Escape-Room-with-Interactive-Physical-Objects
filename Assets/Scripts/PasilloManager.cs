using UnityEngine;

public class PasilloManager : MonoBehaviour
{
    public static PasilloManager Instance { get; private set; }

    [Header("Spawn")]
    [SerializeField, Tooltip("Punto donde aparece el jugador al entrar al pasillo")]
    private Transform spawnInicio;

    [Header("Puerta 217")]
    [SerializeField, Tooltip("Transform de la puerta cerrada (se desactiva al abrir)")]
    private Transform puerta217;

    [SerializeField, Tooltip("GameObject de la puerta abierta (debe estar desactivado por defecto).")]
    private GameObject puertaAbierta;

    [SerializeField, Tooltip("Collider/Interactable de la puerta para detectar interacción")]
    private Collider puertaInteractable;

    [Header("Inscripción")]
    [SerializeField, Tooltip("GameObject con la inscripción en la pared (desactivado por defecto)")]
    private GameObject inscripcion;

    [Header("Audio")]
    [SerializeField, Tooltip("AudioSource para reproducir golpes en la puerta")]
    private AudioSource audioGolpe;

    [SerializeField, Tooltip("AudioSource para el sonido de puerta abriéndose")]
    private AudioSource audioPuertaAbriendo;


    public Transform SpawnInicio => spawnInicio;
    public Transform Puerta217 => puerta217;
    public GameObject PuertaAbierta => puertaAbierta;
    public Collider PuertaInteractable => puertaInteractable;
    public GameObject Inscripcion => inscripcion;
    public AudioSource AudioGolpe => audioGolpe;
    public AudioSource AudioPuertaAbriendo => audioPuertaAbriendo;

    void Awake()
    {
        Instance = this;

        if (inscripcion != null) inscripcion.SetActive(false);
        if (puertaAbierta != null) puertaAbierta.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
