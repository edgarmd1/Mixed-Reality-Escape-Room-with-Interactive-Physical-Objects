using UnityEngine;

/// <summary>
/// Script que vive en la escena "Pasillo".
/// Expone todas las referencias locales de la escena para que
/// KnockPuzzleManager (en la escena principal) pueda acceder a ellas
/// tras cargar la escena aditivamente.
///
/// Se registra automáticamente como singleton estático al activarse.
/// </summary>
public class PasilloManager : MonoBehaviour
{
    public static PasilloManager Instance { get; private set; }

    [Header("Spawn")]
    [SerializeField, Tooltip("Punto donde aparece el jugador al entrar al pasillo")]
    private Transform spawnInicio;

    [Header("Puerta 217")]
    [SerializeField, Tooltip("Transform de la puerta que rota al abrirse (Sketchfab_HN3_Door)")]
    private Transform puerta217;

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

    // ── Propiedades públicas ─────────────────────────────────────────────
    public Transform SpawnInicio => spawnInicio;
    public Transform Puerta217 => puerta217;
    public Collider PuertaInteractable => puertaInteractable;
    public GameObject Inscripcion => inscripcion;
    public AudioSource AudioGolpe => audioGolpe;
    public AudioSource AudioPuertaAbriendo => audioPuertaAbriendo;

    void Awake()
    {
        Instance = this;

        // Inscripción oculta al inicio
        if (inscripcion != null) inscripcion.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
