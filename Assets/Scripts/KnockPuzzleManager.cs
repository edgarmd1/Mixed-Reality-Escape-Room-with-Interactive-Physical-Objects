using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Gestiona el puzzle de la habitación 217: secuencia de golpes en la puerta,
/// transiciones MR↔VR, validación del patrón con acelerómetro y apertura de puerta.
/// </summary>
public class KnockPuzzleManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════
    //  ESTADOS DEL PUZZLE
    // ═══════════════════════════════════════════════════════════════════════

    public enum EstadoPuzzle
    {
        Inactivo,           // Esperando a ser activado por TelefonoManager
        EsperandoPortal,    // Portal activo, esperando que el jugador entre
        TransicionAVR,      // Fade a negro → cambiar a VR
        EnPasillo,          // Jugador en el pasillo VR, puede acercarse a la puerta
        SecuenciaSonando,   // Reproduciendo la secuencia de golpes de la puerta
        EsperandoLectura,   // Inscripción visible, jugador leyendo instrucciones
        TransicionAMR,      // Fade a negro → cambiar a MR
        EscuchandoGolpes,   // En MR, esperando golpes del jugador en la mesa
        ValidandoExito,     // Patrón correcto → transición de vuelta a VR
        PuertaAbierta       // Puerta abierta, puzzle completado
    }

    [Header("Estado actual")]
    [SerializeField] private EstadoPuzzle estadoActual = EstadoPuzzle.Inactivo;

    // ═══════════════════════════════════════════════════════════════════════
    //  SECUENCIA DE GOLPES (Patrón fútbol: TUN-TUN-TUN-TUN-TUN... TUN-TUN!)
    // ═══════════════════════════════════════════════════════════════════════

    [Header("Secuencia de golpes")]
    [SerializeField, Tooltip("Intervalos entre golpes en segundos.\n" +
        "Patrón fútbol: 5 rápidos, pausa, 2 rápidos.\n" +
        "Golpes totales = intervalos + 1")]
    private float[] patronIntervalos = { 0.3f, 0.3f, 0.3f, 0.3f, 0.7f, 0.3f };

    [SerializeField, Range(0.1f, 0.5f), Tooltip("Tolerancia de timing (±%). 0.3 = ±30%")]
    private float tolerancia = 0.3f;

    [SerializeField, Tooltip("Tiempo máximo (s) para completar toda la secuencia antes de resetear")]
    private float timeoutSecuencia = 8f;

    // ═══════════════════════════════════════════════════════════════════════
    //  AUDIO
    // ═══════════════════════════════════════════════════════════════════════

    [Header("Audio")]
    [SerializeField, Tooltip("AudioSource para reproducir golpes individuales")]
    private AudioSource audioGolpe;

    [SerializeField, Tooltip("Clip del golpe en la puerta")]
    private AudioClip clipGolpePuerta;

    [SerializeField, Tooltip("AudioSource para el sonido de puerta abriéndose")]
    private AudioSource audioPuertaAbriendo;

    [SerializeField, Tooltip("AudioSource para sonido de error (patrón incorrecto)")]
    private AudioSource audioError;

    // ═══════════════════════════════════════════════════════════════════════
    //  REFERENCIAS
    // ═══════════════════════════════════════════════════════════════════════

    [Header("Referencias")]
    [SerializeField] private ArduinoLuz arduinoLuz;
    [SerializeField] private CameraCullingMaskController cameraCulling;
    [SerializeField] private Renderer overlayRenderer;

    [Header("Posicionamiento – Frame Root")]
    [SerializeField, Tooltip("Transform del Frame Root (puerta MR calibrada).\n" +
        "El pasillo se reposicionará automáticamente detrás de este.\n" +
        "Coloca el pasillo donde quieras en el editor relativo al frame;\n" +
        "el script calculará el offset automáticamente.")]
    private Transform frameRoot;

    [Header("Pasillo VR (root)")]
    [SerializeField, Tooltip("GameObject raíz 'Pasillo' que contiene todo.\n" +
        "Dejarlo en capa Default para que KnockPuzzleManager tenga control exclusivo.")]
    private GameObject pasilloRoot;

    [SerializeField, Tooltip("Punto de spawn al inicio del pasillo VR (hijo de Pasillo)")]
    private Transform spawnPasillo;

    [Header("Puerta 217 (hijos de Pasillo)")]
    [SerializeField, Tooltip("Transform de la puerta que rota al abrirse (Sketchfab_HN3_Door)")]
    private Transform puerta217;

    [SerializeField, Tooltip("Ángulo de apertura de la puerta (grados)")]
    private float anguloApertura = -90f;

    [SerializeField, Tooltip("Duración de la animación de apertura (s)")]
    private float duracionApertura = 1.5f;

    [SerializeField, Tooltip("Collider de la puerta para detectar interacción")]
    private Collider puertaInteractable;

    [Header("Elementos UI dentro del Pasillo")]
    [SerializeField, Tooltip("GameObject con la inscripción en la pared (hijo de Pasillo)")]
    private GameObject inscripcion;

    [SerializeField, Tooltip("Botón/trigger para volver al mundo real (hijo de Pasillo)")]
    private GameObject botonVolverMR;

    // ═══════════════════════════════════════════════════════════════════════
    //  TRANSICIÓN (FADE)
    // ═══════════════════════════════════════════════════════════════════════

    [Header("Transición")]
    [SerializeField] private float duracionFade = 1.5f;

    private Material _overlayMat;

    // ═══════════════════════════════════════════════════════════════════════
    //  ESTADO INTERNO
    // ═══════════════════════════════════════════════════════════════════════

    // Golpes del jugador
    private readonly List<float> _timestampsGolpes = new List<float>();
    private float _tiempoInicioEscucha;
    private int _totalGolpesEsperados;

    // Guardar posición MR del XR Origin para restaurar al volver
    private Vector3 _posicionMRGuardada;
    private Quaternion _rotacionMRGuardada;

    // Offset relativo del pasillo respecto al Frame Root
    // (calculado automáticamente en Start según cómo estén puestos en el editor)
    private Vector3 _offsetPasilloLocal;
    private Quaternion _offsetPasilloRot;

    // Posición del frame guardada (para cuando el Frame Root se desactiva al ir a VR)
    private Vector3 _frameRootPosGuardada;
    private Quaternion _frameRootRotGuardada;

    // Control de intentos
    private int _intentos = 0;

    // ═══════════════════════════════════════════════════════════════════════
    //  INICIALIZACIÓN
    // ═══════════════════════════════════════════════════════════════════════

    void Awake()
    {
        _totalGolpesEsperados = patronIntervalos.Length + 1;

        if (overlayRenderer != null)
            _overlayMat = overlayRenderer.material;

        // Desactivar el pasillo entero al inicio
        if (pasilloRoot != null) pasilloRoot.SetActive(false);
    }

    void Start()
    {
        // Calcular el offset relativo entre el pasillo y el Frame Root
        // tal como el usuario los ha colocado en el editor.
        // Así si el frame se calibra/mueve, el pasillo lo seguirá.
        if (pasilloRoot != null && frameRoot != null)
        {
            Quaternion invFrameRot = Quaternion.Inverse(frameRoot.rotation);
            _offsetPasilloLocal = invFrameRot * (pasilloRoot.transform.position - frameRoot.position);
            _offsetPasilloRot = invFrameRot * pasilloRoot.transform.rotation;

            Debug.Log($"[KnockPuzzle] Offset pasillo→frame calculado: " +
                      $"pos={_offsetPasilloLocal}, rot={_offsetPasilloRot.eulerAngles}");
        }
    }

    void OnEnable()
    {
        if (arduinoLuz != null)
            arduinoLuz.OnKnockDetected += OnGolpeDetectado;
    }

    void OnDisable()
    {
        if (arduinoLuz != null)
            arduinoLuz.OnKnockDetected -= OnGolpeDetectado;
    }

    void Update()
    {
        // ── Debug en Editor: tecla K simula un golpe ─────────────────────
#if UNITY_EDITOR
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            if (estadoActual == EstadoPuzzle.EscuchandoGolpes)
            {
                Debug.Log("[KnockPuzzle] (Editor) Golpe simulado con tecla K");
                OnGolpeDetectado();
            }
        }
#endif

        // ── Timeout de la secuencia ──────────────────────────────────────
        if (estadoActual == EstadoPuzzle.EscuchandoGolpes &&
            _timestampsGolpes.Count > 0 &&
            Time.time - _timestampsGolpes[0] > timeoutSecuencia)
        {
            Debug.Log("[KnockPuzzle] Timeout: secuencia demasiado lenta. Reiniciando.");
            ResetearIntento();
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  API PÚBLICA
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Llamado por TelefonoManager cuando el audio de la voz termina.
    /// </summary>
    public void IniciarPuzzleGolpes()
    {
        estadoActual = EstadoPuzzle.EsperandoPortal;
        Debug.Log("[KnockPuzzle] Puzzle activado – esperando que el jugador entre al portal.");
    }

    /// <summary>
    /// Llamado por PortalKnockTrigger cuando el jugador entra en el portal.
    /// </summary>
    public void OnJugadorEntraPortal()
    {
        if (estadoActual != EstadoPuzzle.EsperandoPortal) return;
        StartCoroutine(TransicionMRaVR());
    }

    /// <summary>
    /// Llamado cuando el jugador interactúa con la puerta 217.
    /// </summary>
    public void OnIntentarAbrirPuerta()
    {
        if (estadoActual != EstadoPuzzle.EnPasillo) return;
        StartCoroutine(ReproducirSecuenciaGolpes());
    }

    /// <summary>
    /// Llamado cuando el jugador pulsa el botón "Volver al mundo real".
    /// </summary>
    public void OnVolverAMR()
    {
        if (estadoActual != EstadoPuzzle.EsperandoLectura) return;
        StartCoroutine(TransicionVRaMR());
    }

    public EstadoPuzzle Estado => estadoActual;
    public bool PuzzleCompletado => estadoActual == EstadoPuzzle.PuertaAbierta;

    // ═══════════════════════════════════════════════════════════════════════
    //  POSICIONAMIENTO DEL PASILLO
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Posiciona el pasillo detrás del Frame Root usando el offset calculado en Start.
    /// LLAMAR ANTES de cambiar a VR (el Frame Root se desactiva en VR).
    /// </summary>
    private void PosicionarPasilloDetrasDelFrame()
    {
        if (pasilloRoot == null || frameRoot == null) return;

        // Guardar posición actual del frame (antes de que MR se desactive)
        if (frameRoot.gameObject.activeInHierarchy)
        {
            _frameRootPosGuardada = frameRoot.position;
            _frameRootRotGuardada = frameRoot.rotation;
        }

        // Aplicar el offset calculado en Start a la posición actual del frame
        pasilloRoot.transform.position = _frameRootPosGuardada
            + _frameRootRotGuardada * _offsetPasilloLocal;
        pasilloRoot.transform.rotation = _frameRootRotGuardada * _offsetPasilloRot;

        Debug.Log($"[KnockPuzzle] Pasillo posicionado detrás del Frame Root. " +
                  $"Frame: {_frameRootPosGuardada}, Pasillo: {pasilloRoot.transform.position}");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TRANSICIONES
    // ═══════════════════════════════════════════════════════════════════════

    private IEnumerator TransicionMRaVR()
    {
        estadoActual = EstadoPuzzle.TransicionAVR;
        Debug.Log("[KnockPuzzle] Transición MR → VR iniciada.");

        // Guardar posición del XR Origin en MR para restaurar luego
        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            _posicionMRGuardada = xrOrigin.transform.position;
            _rotacionMRGuardada = xrOrigin.transform.rotation;
        }

        // Posicionar el pasillo ANTES de desactivar MR (frame aún visible)
        PosicionarPasilloDetrasDelFrame();

        // Fade a negro
        yield return StartCoroutine(FadeOverlay(1f, duracionFade * 0.5f));

        // Activar el pasillo (en capa Default, no afectado por CameraCulling)
        if (pasilloRoot != null) pasilloRoot.SetActive(true);
        if (inscripcion != null) inscripcion.SetActive(false);
        if (botonVolverMR != null) botonVolverMR.SetActive(false);

        // Cambiar SOLO el culling mask, SIN activar objetos de Mundo_Virtual
        // (así no se activa Room 2 / VirtualWorld)
        cameraCulling?.SetModeSoloVisual(false);

        // Teleportar al inicio del pasillo
        yield return null; // un frame para que se aplique
        TeleportarASpawnPasillo(xrOrigin);

        // Pausa en negro
        yield return new WaitForSeconds(0.3f);

        // Fade in
        yield return StartCoroutine(FadeOverlay(0f, duracionFade * 0.5f));

        estadoActual = EstadoPuzzle.EnPasillo;
        Debug.Log("[KnockPuzzle] Jugador en el pasillo VR.");
    }

    private IEnumerator TransicionVRaMR()
    {
        estadoActual = EstadoPuzzle.TransicionAMR;
        Debug.Log("[KnockPuzzle] Transición VR → MR iniciada.");

        if (inscripcion != null) inscripcion.SetActive(false);
        if (botonVolverMR != null) botonVolverMR.SetActive(false);

        // Fade a negro
        yield return StartCoroutine(FadeOverlay(1f, duracionFade * 0.5f));

        // Desactivar el pasillo
        if (pasilloRoot != null) pasilloRoot.SetActive(false);

        // Restaurar posición MR del XR Origin
        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            xrOrigin.transform.position = _posicionMRGuardada;
            xrOrigin.transform.rotation = _rotacionMRGuardada;
        }

        // Volver a MR (solo visual, sin tocar objetos)
        // Luego reactivar objetos MR con SetMode completo
        cameraCulling?.SetMode(true);

        // Pausa
        yield return new WaitForSeconds(0.3f);

        // Fade in
        yield return StartCoroutine(FadeOverlay(0f, duracionFade * 0.5f));

        // Empezar a escuchar golpes
        EmpezarEscucha();
        Debug.Log("[KnockPuzzle] De vuelta en MR – escuchando golpes del jugador.");
    }

    private IEnumerator TransicionExitoAVR()
    {
        estadoActual = EstadoPuzzle.ValidandoExito;
        Debug.Log("[KnockPuzzle] ¡Patrón correcto! Volviendo a VR para abrir la puerta.");

        // Reposicionar pasillo (por si el frame se recalibró)
        PosicionarPasilloDetrasDelFrame();

        // Fade a negro
        yield return StartCoroutine(FadeOverlay(1f, duracionFade * 0.5f));

        // Reactivar pasillo y modo visual VR
        if (pasilloRoot != null) pasilloRoot.SetActive(true);
        cameraCulling?.SetModeSoloVisual(false);

        // Teleportar al pasillo
        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        yield return null;
        TeleportarASpawnPasillo(xrOrigin);

        yield return new WaitForSeconds(0.3f);

        // Fade in
        yield return StartCoroutine(FadeOverlay(0f, duracionFade * 0.5f));

        // Abrir la puerta
        yield return StartCoroutine(AnimarAperturaPuerta());

        estadoActual = EstadoPuzzle.PuertaAbierta;
        Debug.Log("[KnockPuzzle] ¡Puerta 217 abierta! Puzzle completado.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TELEPORT HELPER
    // ═══════════════════════════════════════════════════════════════════════

    private void TeleportarASpawnPasillo(Unity.XR.CoreUtils.XROrigin xrOrigin)
    {
        if (xrOrigin == null || spawnPasillo == null) return;

        Camera cam = xrOrigin.Camera;
        if (cam != null)
        {
            Vector3 camOffset = cam.transform.localPosition;
            xrOrigin.transform.position = new Vector3(
                spawnPasillo.position.x - camOffset.x,
                spawnPasillo.position.y - camOffset.y,
                spawnPasillo.position.z - camOffset.z
            );
            xrOrigin.transform.rotation = Quaternion.Euler(0f, spawnPasillo.eulerAngles.y, 0f);
            Debug.Log($"[KnockPuzzle] Teleportado a spawn pasillo: {xrOrigin.transform.position}");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  SECUENCIA DE GOLPES (REPRODUCCIÓN)
    // ═══════════════════════════════════════════════════════════════════════

    private IEnumerator ReproducirSecuenciaGolpes()
    {
        estadoActual = EstadoPuzzle.SecuenciaSonando;
        Debug.Log("[KnockPuzzle] Reproduciendo secuencia de golpes en la puerta...");

        ReproducirGolpe();
        for (int i = 0; i < patronIntervalos.Length; i++)
        {
            yield return new WaitForSeconds(patronIntervalos[i]);
            ReproducirGolpe();
        }

        yield return new WaitForSeconds(1f);

        if (inscripcion != null) inscripcion.SetActive(true);
        if (botonVolverMR != null) botonVolverMR.SetActive(true);

        estadoActual = EstadoPuzzle.EsperandoLectura;
        Debug.Log("[KnockPuzzle] Inscripción visible. Esperando que el jugador decida volver al MR.");
    }

    private void ReproducirGolpe()
    {
        if (audioGolpe != null && clipGolpePuerta != null)
            audioGolpe.PlayOneShot(clipGolpePuerta);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DETECCIÓN Y VALIDACIÓN DE GOLPES
    // ═══════════════════════════════════════════════════════════════════════

    private void EmpezarEscucha()
    {
        _timestampsGolpes.Clear();
        _tiempoInicioEscucha = Time.time;
        _intentos++;
        estadoActual = EstadoPuzzle.EscuchandoGolpes;
        Debug.Log($"[KnockPuzzle] Escucha iniciada (intento #{_intentos}). " +
                  $"Esperando {_totalGolpesEsperados} golpes.");
    }

    private void OnGolpeDetectado()
    {
        if (estadoActual != EstadoPuzzle.EscuchandoGolpes) return;

        _timestampsGolpes.Add(Time.time);
        Debug.Log($"[KnockPuzzle] Golpe #{_timestampsGolpes.Count}/{_totalGolpesEsperados} detectado.");

        ReproducirGolpe();

        if (_timestampsGolpes.Count >= _totalGolpesEsperados)
            ValidarPatron();
    }

    private void ValidarPatron()
    {
        Debug.Log("[KnockPuzzle] Validando patrón de golpes...");

        float[] intervalosJugador = new float[_timestampsGolpes.Count - 1];
        for (int i = 0; i < intervalosJugador.Length; i++)
            intervalosJugador[i] = _timestampsGolpes[i + 1] - _timestampsGolpes[i];

        bool correcto = true;
        for (int i = 0; i < patronIntervalos.Length; i++)
        {
            float esperado = patronIntervalos[i];
            float real = intervalosJugador[i];
            float margen = esperado * tolerancia;
            float minimo = esperado - margen;
            float maximo = esperado + margen;

            Debug.Log($"[KnockPuzzle]   Intervalo {i + 1}: esperado={esperado:F2}s " +
                      $"(±{margen:F2}s), real={real:F2}s → " +
                      $"{(real >= minimo && real <= maximo ? "✓" : "✗")}");

            if (real < minimo || real > maximo)
                correcto = false;
        }

        if (correcto)
        {
            Debug.Log("[KnockPuzzle] ¡PATRÓN CORRECTO! 🎉");
            StartCoroutine(TransicionExitoAVR());
        }
        else
        {
            Debug.Log($"[KnockPuzzle] Patrón incorrecto (intento #{_intentos}). Reiniciando.");
            audioError?.Play();
            ResetearIntento();
        }
    }

    private void ResetearIntento()
    {
        _timestampsGolpes.Clear();
        _intentos++;
        Debug.Log($"[KnockPuzzle] Escucha reiniciada (intento #{_intentos}).");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  ANIMACIÓN PUERTA
    // ═══════════════════════════════════════════════════════════════════════

    private IEnumerator AnimarAperturaPuerta()
    {
        if (puerta217 == null) yield break;

        audioPuertaAbriendo?.Play();

        Quaternion rotInicial = puerta217.localRotation;
        Quaternion rotFinal = rotInicial * Quaternion.Euler(0f, anguloApertura, 0f);

        float t = 0f;
        while (t < duracionApertura)
        {
            t += Time.deltaTime;
            float progreso = Mathf.SmoothStep(0f, 1f, t / duracionApertura);
            puerta217.localRotation = Quaternion.Slerp(rotInicial, rotFinal, progreso);
            yield return null;
        }

        puerta217.localRotation = rotFinal;

        if (puertaInteractable != null)
            puertaInteractable.enabled = false;

        Debug.Log("[KnockPuzzle] Animación de apertura completada.");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  FADE OVERLAY
    // ═══════════════════════════════════════════════════════════════════════

    private IEnumerator FadeOverlay(float alphaObjetivo, float duracion)
    {
        if (_overlayMat == null) yield break;

        float alphaInicial = _overlayMat.color.a;
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            SetOverlayAlpha(Mathf.Lerp(alphaInicial, alphaObjetivo, tiempo / duracion));
            yield return null;
        }

        SetOverlayAlpha(alphaObjetivo);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (_overlayMat == null) return;
        Color c = _overlayMat.color;
        _overlayMat.color = new Color(c.r, c.g, c.b, alpha);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  GIZMOS
    // ═══════════════════════════════════════════════════════════════════════

    void OnDrawGizmosSelected()
    {
        if (spawnPasillo != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnPasillo.position, 0.3f);
            Gizmos.DrawRay(spawnPasillo.position, spawnPasillo.forward * 1f);
        }

        if (puerta217 != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            Gizmos.DrawWireCube(puerta217.position, new Vector3(1f, 2f, 0.1f));
        }

        // Visualizar dónde se colocará el pasillo respecto al Frame Root
        if (frameRoot != null && pasilloRoot != null)
        {
            Gizmos.color = Color.green;
            Quaternion invFrameRot = Quaternion.Inverse(frameRoot.rotation);
            Vector3 offset = invFrameRot * (pasilloRoot.transform.position - frameRoot.position);
            Vector3 posPasillo = frameRoot.position + frameRoot.rotation * offset;
            Gizmos.DrawWireCube(posPasillo, new Vector3(0.5f, 0.5f, 0.5f));
            Gizmos.DrawLine(frameRoot.position, posPasillo);
        }
    }
}
