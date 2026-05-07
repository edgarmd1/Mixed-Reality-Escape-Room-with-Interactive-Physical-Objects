using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Gestiona el puzzle de la habitación 217: secuencia de golpes en la puerta,
/// transiciones MR↔VR mediante carga aditiva de escena, validación del patrón
/// con acelerómetro y apertura de puerta.
///
/// Vive en la escena PRINCIPAL (SampleScene). La escena "Pasillo" se carga
/// y descarga aditivamente, preservando todo el estado MR.
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
        TransicionAVR,      // Fade a negro → cargando escena Pasillo
        EnPasillo,          // Jugador en el pasillo VR
        SecuenciaSonando,   // Reproduciendo la secuencia de golpes de la puerta
        EsperandoLectura,   // Inscripción visible, jugador leyendo instrucciones
        TransicionAMR,      // Fade a negro → descargando escena Pasillo
        EscuchandoGolpes,   // En MR, esperando golpes del jugador en la mesa
        ValidandoExito,     // Patrón correcto → cargando escena Pasillo de nuevo
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
    //  AUDIO (en escena principal, para que suene en MR también)
    // ═══════════════════════════════════════════════════════════════════════

    [Header("Audio (escena principal)")]
    [SerializeField, Tooltip("Clip del golpe en la puerta")]
    private AudioClip clipGolpePuerta;

    [SerializeField, Tooltip("AudioSource para sonido de error (patrón incorrecto)")]
    private AudioSource audioError;

    [SerializeField, Tooltip("AudioSource genérico para feedback de golpes en MR")]
    private AudioSource audioFeedbackGolpe;

    // ═══════════════════════════════════════════════════════════════════════
    //  REFERENCIAS (escena principal)
    // ═══════════════════════════════════════════════════════════════════════

    [Header("Referencias")]
    [SerializeField] private ArduinoLuz arduinoLuz;
    [SerializeField] private CameraCullingMaskController cameraCulling;
    [SerializeField] private Renderer overlayRenderer;

    [Header("Escena del Pasillo")]
    [SerializeField, Tooltip("Nombre de la escena del pasillo (debe estar en Build Settings)")]
    private string nombreEscenaPasillo = "Pasillo";

    // ═══════════════════════════════════════════════════════════════════════
    //  TRANSICIÓN (FADE)
    // ═══════════════════════════════════════════════════════════════════════

    [Header("Transición")]
    [SerializeField] private float duracionFade = 1.5f;

    private Material _overlayMat;

    // ═══════════════════════════════════════════════════════════════════════
    //  ESTADO INTERNO
    // ═══════════════════════════════════════════════════════════════════════

    private readonly List<float> _timestampsGolpes = new List<float>();
    private float _tiempoInicioEscucha;
    private int _totalGolpesEsperados;

    // Guardar posición MR del XR Origin para restaurar al volver
    private Vector3 _posicionMRGuardada;
    private Quaternion _rotacionMRGuardada;

    // Control de intentos
    private int _intentos = 0;

    // Referencia al PasilloManager (tras cargar la escena)
    private PasilloManager _pasilloManager;

    // ═══════════════════════════════════════════════════════════════════════
    //  INICIALIZACIÓN
    // ═══════════════════════════════════════════════════════════════════════

    void Awake()
    {
        _totalGolpesEsperados = patronIntervalos.Length + 1;

        if (overlayRenderer != null)
            _overlayMat = overlayRenderer.material;
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
    /// Llamado cuando el jugador interactúa con la puerta 217 en la escena Pasillo.
    /// Conectar en el Inspector de la escena Pasillo (Interactable → OnActivate).
    /// O llamar desde PasilloManager.
    /// </summary>
    public void OnIntentarAbrirPuerta()
    {
        if (estadoActual != EstadoPuzzle.EnPasillo) return;
        StartCoroutine(ReproducirSecuenciaGolpes());
    }

    /// <summary>
    /// Llamado por ReturnToMRTrigger cuando el jugador quiere volver al MR.
    /// Funciona en cualquier estado mientras el jugador esté en el pasillo.
    /// </summary>
    public void OnVolverAMR()
    {
        if (estadoActual != EstadoPuzzle.EnPasillo &&
            estadoActual != EstadoPuzzle.SecuenciaSonando &&
            estadoActual != EstadoPuzzle.EsperandoLectura)
        {
            Debug.Log($"[KnockPuzzle] OnVolverAMR ignorado – estado actual: {estadoActual}");
            return;
        }

        // Parar coroutines de secuencia si estaban en marcha
        StopAllCoroutines();
        StartCoroutine(TransicionVRaMR());
    }

    public EstadoPuzzle Estado => estadoActual;
    public bool PuzzleCompletado => estadoActual == EstadoPuzzle.PuertaAbierta;

    // ═══════════════════════════════════════════════════════════════════════
    //  CARGA / DESCARGA DE ESCENA
    // ═══════════════════════════════════════════════════════════════════════

    private IEnumerator CargarEscenaPasillo()
    {
        // Comprobar si ya está cargada
        Scene escena = SceneManager.GetSceneByName(nombreEscenaPasillo);
        if (!escena.isLoaded)
        {
            Debug.Log($"[KnockPuzzle] Cargando escena '{nombreEscenaPasillo}' aditivamente...");
            AsyncOperation op = SceneManager.LoadSceneAsync(nombreEscenaPasillo, LoadSceneMode.Additive);
            yield return op;
            Debug.Log($"[KnockPuzzle] Escena '{nombreEscenaPasillo}' cargada.");
        }

        // Esperar un frame para que Awake/Start se ejecuten
        yield return null;

        // ── Limpiar componentes XR duplicados de la escena cargada ────────
        // Si la escena se creó desde un template, puede traer su propio
        // OVRManager, Camera Rig, XR Origin, etc. que conflictuarían.
        Scene escenaCargada = SceneManager.GetSceneByName(nombreEscenaPasillo);
        if (escenaCargada.isLoaded)
        {
            LimpiarXRDuplicados(escenaCargada);
        }

        yield return null; // Otro frame tras la limpieza

        _pasilloManager = PasilloManager.Instance;
        if (_pasilloManager == null)
        {
            Debug.LogError("[KnockPuzzle] No se encontró PasilloManager en la escena cargada. " +
                           "Asegúrate de que la escena tiene un GameObject con PasilloManager.");
        }
    }

    /// <summary>
    /// Busca y destruye componentes XR duplicados en la escena cargada
    /// (OVRManager, OVRCameraRig, XROrigin, cámaras extra, EventSystem, etc.)
    /// para evitar conflictos con los de la escena principal.
    /// </summary>
    private void LimpiarXRDuplicados(Scene escena)
    {
        int destruidos = 0;

        foreach (GameObject root in escena.GetRootGameObjects())
        {
            // Destruir GameObjects con componentes XR/OVR problemáticos
            bool destruir = false;
            string razon = "";

            // OVRManager / OVRCameraRig
            foreach (var comp in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                string tipo = comp.GetType().Name;
                if (tipo.Contains("OVRManager") || tipo.Contains("OVRCameraRig") ||
                    tipo.Contains("OVRHeadsetEmulator"))
                {
                    destruir = true;
                    razon = tipo;
                    break;
                }
            }

            // XR Origin
            if (!destruir && root.GetComponentInChildren<Unity.XR.CoreUtils.XROrigin>(true) != null)
            {
                destruir = true;
                razon = "XROrigin";
            }

            // EventSystem duplicado
            if (!destruir && root.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true) != null)
            {
                destruir = true;
                razon = "EventSystem";
            }

            // Cámara suelta (no parte de PasilloManager)
            if (!destruir && root.GetComponentInChildren<Camera>(true) != null &&
                root.GetComponent<PasilloManager>() == null)
            {
                destruir = true;
                razon = "Camera";
            }

            if (destruir)
            {
                Debug.Log($"[KnockPuzzle] Destruyendo '{root.name}' de escena Pasillo (contiene {razon}).");
                Destroy(root);
                destruidos++;
            }
        }

        if (destruidos > 0)
            Debug.Log($"[KnockPuzzle] Limpieza: {destruidos} objeto(s) XR duplicados eliminados de la escena Pasillo.");
        else
            Debug.Log("[KnockPuzzle] Limpieza: escena Pasillo limpia, sin duplicados XR.");
    }

    private IEnumerator DescargarEscenaPasillo()
    {
        Scene escena = SceneManager.GetSceneByName(nombreEscenaPasillo);
        if (escena.isLoaded)
        {
            Debug.Log($"[KnockPuzzle] Descargando escena '{nombreEscenaPasillo}'...");
            AsyncOperation op = SceneManager.UnloadSceneAsync(escena);
            yield return op;
            Debug.Log($"[KnockPuzzle] Escena '{nombreEscenaPasillo}' descargada.");
        }

        _pasilloManager = null;
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  TRANSICIONES
    // ═══════════════════════════════════════════════════════════════════════

    private IEnumerator TransicionMRaVR()
    {
        estadoActual = EstadoPuzzle.TransicionAVR;
        Debug.Log("[KnockPuzzle] Transición MR → VR (cargando escena Pasillo).");

        // Guardar posición del XR Origin en MR
        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            _posicionMRGuardada = xrOrigin.transform.position;
            _rotacionMRGuardada = xrOrigin.transform.rotation;
        }

        // Fade a negro
        yield return StartCoroutine(FadeOverlay(1f, duracionFade * 0.5f));

        // Cargar la escena del pasillo
        yield return StartCoroutine(CargarEscenaPasillo());

        // Cambiar solo el culling mask (no activa objetos de Mundo_Virtual)
        cameraCulling?.SetModeSoloVisual(false);

        // Teleportar al spawn del pasillo
        yield return null;
        TeleportarASpawn(xrOrigin, _pasilloManager?.SpawnInicio);

        // Pausa en negro
        yield return new WaitForSeconds(0.2f);

        // Fade in
        yield return StartCoroutine(FadeOverlay(0f, duracionFade * 0.5f));

        estadoActual = EstadoPuzzle.EnPasillo;
        Debug.Log("[KnockPuzzle] Jugador en el pasillo VR.");
    }

    private IEnumerator TransicionVRaMR()
    {
        estadoActual = EstadoPuzzle.TransicionAMR;
        Debug.Log("[KnockPuzzle] Transición VR → MR (descargando escena Pasillo).");

        // Fade a negro
        yield return StartCoroutine(FadeOverlay(1f, duracionFade * 0.5f));

        // Descargar la escena del pasillo
        yield return StartCoroutine(DescargarEscenaPasillo());

        // Restaurar posición MR del XR Origin
        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            xrOrigin.transform.position = _posicionMRGuardada;
            xrOrigin.transform.rotation = _rotacionMRGuardada;
            Debug.Log($"[KnockPuzzle] Posición MR restaurada: {_posicionMRGuardada}");
        }

        // Volver a MR completo (reactiva objetos MR)
        cameraCulling?.SetMode(true);

        yield return new WaitForSeconds(0.2f);

        // Fade in
        yield return StartCoroutine(FadeOverlay(0f, duracionFade * 0.5f));

        // Empezar a escuchar golpes
        EmpezarEscucha();
        Debug.Log("[KnockPuzzle] De vuelta en MR – escuchando golpes del jugador.");
    }

    private IEnumerator TransicionExitoAVR()
    {
        estadoActual = EstadoPuzzle.ValidandoExito;
        Debug.Log("[KnockPuzzle] ¡Patrón correcto! Volviendo al pasillo para abrir la puerta.");

        // Fade a negro
        yield return StartCoroutine(FadeOverlay(1f, duracionFade * 0.5f));

        // Cargar la escena del pasillo de nuevo
        yield return StartCoroutine(CargarEscenaPasillo());

        // Modo visual VR
        cameraCulling?.SetModeSoloVisual(false);

        // Teleportar al spawn
        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        yield return null;
        TeleportarASpawn(xrOrigin, _pasilloManager?.SpawnInicio);

        yield return new WaitForSeconds(0.2f);

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

    private void TeleportarASpawn(Unity.XR.CoreUtils.XROrigin xrOrigin, Transform spawn)
    {
        if (xrOrigin == null || spawn == null) return;

        Camera cam = xrOrigin.Camera;
        if (cam != null)
        {
            Vector3 camOffset = cam.transform.localPosition;
            xrOrigin.transform.position = new Vector3(
                spawn.position.x - camOffset.x,
                spawn.position.y - camOffset.y,
                spawn.position.z - camOffset.z
            );
            xrOrigin.transform.rotation = Quaternion.Euler(0f, spawn.eulerAngles.y, 0f);
            Debug.Log($"[KnockPuzzle] Teleportado a spawn: {xrOrigin.transform.position}");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  SECUENCIA DE GOLPES (REPRODUCCIÓN)
    // ═══════════════════════════════════════════════════════════════════════

    private IEnumerator ReproducirSecuenciaGolpes()
    {
        estadoActual = EstadoPuzzle.SecuenciaSonando;
        Debug.Log("[KnockPuzzle] Reproduciendo secuencia de golpes en la puerta...");

        // Usar el AudioSource del pasillo
        AudioSource audioGolpe = _pasilloManager?.AudioGolpe;

        ReproducirGolpeEn(audioGolpe);
        for (int i = 0; i < patronIntervalos.Length; i++)
        {
            yield return new WaitForSeconds(patronIntervalos[i]);
            ReproducirGolpeEn(audioGolpe);
        }

        yield return new WaitForSeconds(1f);

        // Mostrar inscripción
        if (_pasilloManager?.Inscripcion != null)
            _pasilloManager.Inscripcion.SetActive(true);

        estadoActual = EstadoPuzzle.EsperandoLectura;
        Debug.Log("[KnockPuzzle] Inscripción visible. Jugador puede volver al MR.");
    }

    private void ReproducirGolpeEn(AudioSource source)
    {
        if (source != null && clipGolpePuerta != null)
            source.PlayOneShot(clipGolpePuerta);
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

        // Feedback sonoro en MR
        if (audioFeedbackGolpe != null && clipGolpePuerta != null)
            audioFeedbackGolpe.PlayOneShot(clipGolpePuerta);

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

    [Header("Puerta 217")]
    [SerializeField] private float anguloApertura = -90f;
    [SerializeField] private float duracionApertura = 1.5f;

    private IEnumerator AnimarAperturaPuerta()
    {
        Transform puerta = _pasilloManager?.Puerta217;
        if (puerta == null) yield break;

        _pasilloManager.AudioPuertaAbriendo?.Play();

        Quaternion rotInicial = puerta.localRotation;
        Quaternion rotFinal = rotInicial * Quaternion.Euler(0f, anguloApertura, 0f);

        float t = 0f;
        while (t < duracionApertura)
        {
            t += Time.deltaTime;
            float progreso = Mathf.SmoothStep(0f, 1f, t / duracionApertura);
            puerta.localRotation = Quaternion.Slerp(rotInicial, rotFinal, progreso);
            yield return null;
        }

        puerta.localRotation = rotFinal;

        Collider col = _pasilloManager.PuertaInteractable;
        if (col != null) col.enabled = false;

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
}
