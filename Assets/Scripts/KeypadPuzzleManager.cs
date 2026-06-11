using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class KeypadPuzzleManager : MonoBehaviour
{
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

    [Header("Combinación")]
    [SerializeField, Tooltip("Combinación numérica correcta")]
    private string combinacionCorrecta = "2026";

    [Header("Arduino")]
    [SerializeField] private ArduinoLuz arduinoLuz;

    [Header("Cofre y Llave")]
    [SerializeField, Tooltip("GameObject de la llave dentro del cofre")]
    private GameObject llaveEnCofre;

    [SerializeField, Tooltip("GameObject de la llave en el punto destino")]
    private GameObject llaveEnDestino;

    [SerializeField, Tooltip("GameObject de la llave que las gemelas llevan consigo mientras se mueven")]
    private GameObject llavePortada;

    [Header("Detección de proximidad")]
    [SerializeField, Tooltip("Cámara principal del XR Origin ")]
    private Camera camaraPrincipal;

    [SerializeField, Tooltip("Distancia entre la cámara del jugador y la llave")]
    private float distanciaActivacion = 1.5f;

    [Header("Pasillo")]
    [SerializeField, Tooltip("GameObject raíz del pasillo")]
    private GameObject pasilloRoot;

    [Header("Gemelas")]
    [SerializeField, Tooltip("GameObject de las gemelas")]
    private GameObject gemelasRoot;

    [SerializeField, Tooltip("Velocidad de movimiento de las gemelas")]
    private float velocidadGemelas = 1.2f;

    [SerializeField, Tooltip("Waypoints que siguen las gemelas desde el cofre hasta el punto destino")]
    private List<Transform> waypointsGemelas = new List<Transform>();

    [SerializeField, Tooltip("Tiempo que las gemelas esperan junto al cofre antes de empezar a moverse")]
    private float pausaGemelasEnCofre = 1.5f;

    [Header("Puzzle siguiente")]
    [SerializeField, Tooltip("KnockPuzzleManager que se activa al coger la llave")]
    private KnockPuzzleManager knockPuzzleManager;

    [Header("Audio (opcional)")]
    [SerializeField, Tooltip("Sonido de error al introducir la combinación incorrecta")]
    private AudioSource audioError;

    [SerializeField, Tooltip("Sonido de éxito al introducir la combinación correcta")]
    private AudioSource audioExito;

    [SerializeField, Tooltip("Sonido cuando las gemelas cogen la llave")]
    private AudioSource audioGemelasCogenLlave;

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

    void Update()
    {
#if UNITY_EDITOR
        SimularEntradaEditor();
#endif

        if (estadoActual == EstadoKeypad.EsperandoGiro)
            ComprobarProximidadLlave();
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

        if (knockPuzzleManager != null)
            knockPuzzleManager.IniciarPuzzleGolpes();
    }


    public EstadoKeypad Estado => estadoActual;

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

    private IEnumerator SecuenciaExito()
    {
        estadoActual = EstadoKeypad.ComboExito;

        if (llaveEnCofre != null)
        {
            llaveEnCofre.SetActive(true);
            Debug.Log("[KeypadPuzzle] Llave visible en el cofre.");
        }

        yield return new WaitForSeconds(1.0f);

        estadoActual = EstadoKeypad.EsperandoGiro;
        Debug.Log($"[KeypadPuzzle] Esperando proximidad del jugador a la llave (≤ {distanciaActivacion:F1} m).");
    }

    private void ComprobarProximidadLlave()
    {
        if (camaraPrincipal == null) return;
        if (llaveEnCofre == null || !llaveEnCofre.activeInHierarchy) return;

        float dist = Vector3.Distance(camaraPrincipal.transform.position, llaveEnCofre.transform.position);

        if (dist <= distanciaActivacion)
        {
            Debug.Log($"[KeypadPuzzle] Jugador a {dist:F2} m de la llave. Activando pasillo y gemelas.");
            estadoActual = EstadoKeypad.GemelasMoviendose;

            if (pasilloRoot != null)
            {
                pasilloRoot.SetActive(true);
                Debug.Log("[KeypadPuzzle] Pasillo activado.");
            }

            StartCoroutine(SecuenciaGemelas());
        }
    }

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

        if (audioGemelasCogenLlave != null)
        {
            audioGemelasCogenLlave.Play();
            Debug.Log("[KeypadPuzzle] Reproduciendo audio de gemelas cogiendo la llave.");
        }

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
            Debug.Log("[KeypadPuzzle] (Editor) F8 → Simulando proximidad a la llave.");
            estadoActual = EstadoKeypad.GemelasMoviendose;
            if (pasilloRoot != null) pasilloRoot.SetActive(true);
            StartCoroutine(SecuenciaGemelas());
        }
    }
#endif
}
