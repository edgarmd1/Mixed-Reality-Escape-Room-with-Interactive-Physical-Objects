using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class KeypadPuzzleManager : MonoBehaviour
{
    // ─── Estados ──────────────────────────────────────────────────────────────

    public enum EstadoKeypad
    {
        Inactivo,
        EsperandoCombo,
        ComboExito,
        EsperandoGiro,
        GemelasMoviendose,
        LlaveDisponible,
        PuzzleCompletado
    }

    [Header("Estado")]
    [SerializeField] private EstadoKeypad estadoActual = EstadoKeypad.Inactivo;

    // ─── Combinación ──────────────────────────────────────────────────────────

    [Header("Combinación")]
    [SerializeField, Tooltip("Combinación numérica correcta. Por defecto: 2026")]
    private string combinacionCorrecta = "2026";

    // ─── Arduino ──────────────────────────────────────────────────────────────

    [Header("Arduino")]
    [SerializeField] private ArduinoLuz arduinoLuz;

    // ─── Cofre y Llave ────────────────────────────────────────────────────────

    [Header("Cofre y Llave")]
    [SerializeField, Tooltip("GameObject de la llave dentro del cofre (se activa al acertar la combo; se desactiva cuando las gemelas la cogen)")]
    private GameObject llaveEnCofre;

    [SerializeField, Tooltip("GameObject de la llave en el punto destino (interactable; empieza desactivado)")]
    private GameObject llaveEnDestino;

    [SerializeField, Tooltip("GameObject de la llave que las gemelas llevan consigo mientras se mueven " +
        "(hijo de gemelasRoot o Transform independiente que se mueve junto a ellas). " +
        "Empieza desactivado.")]
    private GameObject llavePortada;

    // ─── Giro de cabeza ───────────────────────────────────────────────────────

    [Header("Detección de giro")]
    [SerializeField, Tooltip("Cámara principal del XR Origin (o MainCamera si queda null)")]
    private Camera camaraPrincipal;

    [SerializeField, Tooltip("Tolerancia en grados sobre el giro de 180\u00b0 para considerar que el usuario ha girado (ej: 40 = acepta desde 140\u00b0 hasta 180\u00b0+)")]
    private float toleranciaGiro = 40f;

    [SerializeField, Tooltip("Segundos que el usuario debe mantener la mirada hacia la sala para activar la secuencia")]
    private float tiempoMantenerMirada = 0.5f;

    private float _yawInicialGiro = 0f;   // yaw de la cámara cuando se acertó la combo
    private float _tiempoMirandoHaciaAtras = 0f;

    // ─── Pasillo ──────────────────────────────────────────────────────────────

    [Header("Pasillo")]
    [SerializeField, Tooltip("GameObject raíz del pasillo. Se activa cuando el jugador gira 180\u00b0 tras acertar la combo.")]
    private GameObject pasilloRoot;

    // ─── Gemelas ──────────────────────────────────────────────────────────────

    [Header("Gemelas")]
    [SerializeField, Tooltip("GameObject de las gemelas (Quad billboard con twins.png; empieza desactivado)")]
    private GameObject gemelasRoot;

    [SerializeField, Tooltip("Velocidad de movimiento de las gemelas en m/s")]
    private float velocidadGemelas = 1.2f;

    [SerializeField, Tooltip("Waypoints que siguen las gemelas desde el cofre hasta el punto destino")]
    private List<Transform> waypointsGemelas = new List<Transform>();

    [SerializeField, Tooltip("Tiempo que las gemelas 'esperan' junto al cofre antes de empezar a moverse (segundos)")]
    private float pausaGemelasEnCofre = 1.5f;

    // ─── Puzzle siguiente ────────────────────────────────────────────────────

    [Header("Puzzle siguiente")]
    [SerializeField, Tooltip("KnockPuzzleManager que se activa al coger la llave")]
    private KnockPuzzleManager knockPuzzleManager;

    // ─── Audio ───────────────────────────────────────────────────────────────

    [Header("Audio (opcional)")]
    [SerializeField, Tooltip("Sonido de error al introducir la combinación incorrecta")]
    private AudioSource audioError;

    [SerializeField, Tooltip("Sonido de éxito al introducir la combinación correcta")]
    private AudioSource audioExito;

    // ─────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (llaveEnCofre  != null) llaveEnCofre.SetActive(false);
        if (llaveEnDestino != null) llaveEnDestino.SetActive(false);
        if (llavePortada   != null) llavePortada.SetActive(false);
        if (gemelasRoot    != null) gemelasRoot.SetActive(false);
    }

    void Start()
    {
        if (camaraPrincipal == null)
            camaraPrincipal = Camera.main;
    }

    void OnEnable()
    {
        if (arduinoLuz != null)
            arduinoLuz.OnComboRecibido += OnComboArduino;
    }

    void OnDisable()
    {
        if (arduinoLuz != null)
            arduinoLuz.OnComboRecibido -= OnComboArduino;
    }

    // ─── Update ───────────────────────────────────────────────────────────────

    void Update()
    {
#if UNITY_EDITOR
        SimularEntradaEditor();
#endif

        if (estadoActual == EstadoKeypad.EsperandoGiro)
            ComprobarGiroCabeza();
    }

    public void IniciarPuzzle()
    {
        if (estadoActual != EstadoKeypad.Inactivo) return;

        estadoActual = EstadoKeypad.EsperandoCombo;
        Debug.Log($"[KeypadPuzzle] Puzzle iniciado – esperando combinación '{combinacionCorrecta}' en el keypad.");
    }

    public void OnLlaveCogida()
    {
        if (estadoActual != EstadoKeypad.LlaveDisponible) return;

        estadoActual = EstadoKeypad.PuzzleCompletado;
        Debug.Log("[KeypadPuzzle] ¡Llave cogida! Puzzle de llave iniciado.");

        // El KnockPuzzle se activará más adelante; de momento es opcional
        if (knockPuzzleManager != null)
            knockPuzzleManager.IniciarPuzzleGolpes();
    }


    public EstadoKeypad Estado => estadoActual;

    // ─── Recepción de combo ───────────────────────────────────────────────────

    private void OnComboArduino(string combo)
    {
        if (estadoActual != EstadoKeypad.EsperandoCombo) return;

        Debug.Log($"[KeypadPuzzle] Combo recibido del Arduino: '{combo}'");

        if (combo == combinacionCorrecta)
        {
            Debug.Log("[KeypadPuzzle] ¡Combinación correcta!");
            audioExito?.Play();
            StartCoroutine(SecuenciaExito());
        }
        else
        {
            Debug.Log($"[KeypadPuzzle] Combinación incorrecta ('{combo}'). Inténtalo de nuevo.");
            audioError?.Play();
        }
    }

    // ─── Secuencias ───────────────────────────────────────────────────────────

    private IEnumerator SecuenciaExito()
    {
        estadoActual = EstadoKeypad.ComboExito;

        if (llaveEnCofre != null)
        {
            llaveEnCofre.SetActive(true);
            Debug.Log("[KeypadPuzzle] Llave visible en el cofre.");
        }

        yield return new WaitForSeconds(1.0f);

        if (camaraPrincipal != null)
            _yawInicialGiro = camaraPrincipal.transform.eulerAngles.y;

        _tiempoMirandoHaciaAtras = 0f;
        estadoActual = EstadoKeypad.EsperandoGiro;
        Debug.Log($"[KeypadPuzzle] Esperando giro de 180° (yaw inicial: {_yawInicialGiro:F1}°).");
    }

    // ─── Detección de giro de cabeza ──────────────────────────────────────────

    private void ComprobarGiroCabeza()
    {
        if (camaraPrincipal == null) return;

        float yawActual = camaraPrincipal.transform.eulerAngles.y;
        
        float girado = Mathf.Abs(Mathf.DeltaAngle(yawActual, _yawInicialGiro));

        bool haGirado = girado >= (180f - toleranciaGiro);

        if (haGirado)
        {
            _tiempoMirandoHaciaAtras += Time.deltaTime;

            if (_tiempoMirandoHaciaAtras >= tiempoMantenerMirada)
            {
                Debug.Log($"[KeypadPuzzle] Giro detectado ({girado:F1}°). Activando pasillo y gemelas.");
                estadoActual = EstadoKeypad.GemelasMoviendose;

                if (pasilloRoot != null)
                {
                    pasilloRoot.SetActive(true);
                    Debug.Log("[KeypadPuzzle] Pasillo activado.");
                }

                StartCoroutine(SecuenciaGemelas());
            }
        }
        else
        {
            _tiempoMirandoHaciaAtras = 0f;
        }
    }

    // ─── Secuencia de gemelas ─────────────────────────────────────────────────

    private IEnumerator SecuenciaGemelas()
    {
        if (waypointsGemelas == null || waypointsGemelas.Count == 0)
        {
            Debug.LogWarning("[KeypadPuzzle] No hay waypoints asignados para las gemelas. Saltando animación.");
            FinalizarSecuenciaGemelas();
            yield break;
        }

        gemelasRoot.transform.position = waypointsGemelas[0].position;
        gemelasRoot.SetActive(true);
        Debug.Log("[KeypadPuzzle] Gemelas aparecidas junto al cofre.");

        yield return new WaitForSeconds(pausaGemelasEnCofre);

        if (llaveEnCofre != null)
        {
            llaveEnCofre.SetActive(false);
            Debug.Log("[KeypadPuzzle] Llave recogida por las gemelas.");
        }
        if (llavePortada != null)
            llavePortada.SetActive(true);

        for (int i = 1; i < waypointsGemelas.Count; i++)
        {
            Transform destino = waypointsGemelas[i];
            if (destino == null) continue;

            while (Vector3.Distance(gemelasRoot.transform.position, destino.position) > 0.05f)
            {
                gemelasRoot.transform.position = Vector3.MoveTowards(
                    gemelasRoot.transform.position,
                    destino.position,
                    velocidadGemelas * Time.deltaTime
                );

                if (llavePortada != null && llavePortada.transform.parent != gemelasRoot.transform)
                    llavePortada.transform.position = gemelasRoot.transform.position;

                if (camaraPrincipal != null)
                {
                    Vector3 dir = camaraPrincipal.transform.position - gemelasRoot.transform.position;
                    dir.y = 0f;
                    if (dir != Vector3.zero)
                        gemelasRoot.transform.rotation = Quaternion.LookRotation(-dir);
                }

                yield return null;
            }

            gemelasRoot.transform.position = destino.position;
        }

        Debug.Log("[KeypadPuzzle] Gemelas llegaron al destino.");
        FinalizarSecuenciaGemelas();
    }

    private void FinalizarSecuenciaGemelas()
    {
        if (gemelasRoot != null)
            gemelasRoot.SetActive(false);
        if (llavePortada != null)
            llavePortada.SetActive(false);

        if (llaveEnDestino != null)
            llaveEnDestino.SetActive(true);

        estadoActual = EstadoKeypad.LlaveDisponible;
        Debug.Log("[KeypadPuzzle] Llave disponible en el punto destino – el usuario puede cogerla.");
    }

    // ─── Simulación en Editor ─────────────────────────────────────────────────

#if UNITY_EDITOR
    private void SimularEntradaEditor()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.f4Key.wasPressedThisFrame)
        {
            Debug.Log("[KeypadPuzzle] (Editor) F4 → IniciarPuzzle().");
            IniciarPuzzle();
        }

        if (kb.f5Key.wasPressedThisFrame)
        {
            if (estadoActual == EstadoKeypad.Inactivo)
            {
                Debug.Log("[KeypadPuzzle] (Editor) F5 → puzzle estaba Inactivo, llamando IniciarPuzzle() primero.");
                IniciarPuzzle();
            }

            if (estadoActual == EstadoKeypad.EsperandoCombo)
            {
                Debug.Log("[KeypadPuzzle] (Editor) F5 → Simulando combo correcto.");
                OnComboArduino(combinacionCorrecta);
            }
            else
            {
                Debug.Log($"[KeypadPuzzle] (Editor) F5 ignorado – estado actual: {estadoActual}");
            }
        }

        if (kb.f6Key.wasPressedThisFrame && estadoActual == EstadoKeypad.EsperandoCombo)
        {
            Debug.Log("[KeypadPuzzle] (Editor) F6 → Simulando combo incorrecto.");
            OnComboArduino("0000");
        }

        if (kb.f7Key.wasPressedThisFrame && estadoActual == EstadoKeypad.LlaveDisponible)
        {
            Debug.Log("[KeypadPuzzle] (Editor) F7 → Simulando coger la llave.");
            OnLlaveCogida();
        }

        if (kb.f9Key.wasPressedThisFrame)
        {
            Debug.Log("[KeypadPuzzle] (Editor) F9 → Simulando inserción de llave (KeyInserter).");
            var inserter = FindObjectOfType<KeyInserter>();
            if (inserter != null)
                inserter.SimularInsercion();
            else
                Debug.LogWarning("[KeypadPuzzle] F9: No se encontró KeyInserter en la escena.");
        }

        if (kb.f8Key.wasPressedThisFrame && estadoActual == EstadoKeypad.EsperandoGiro)
        {
            Debug.Log("[KeypadPuzzle] (Editor) F8 → Simulando giro de 180°.");
            estadoActual = EstadoKeypad.GemelasMoviendose;
            if (pasilloRoot != null) pasilloRoot.SetActive(true);
            StartCoroutine(SecuenciaGemelas());
        }
    }
#endif
}
