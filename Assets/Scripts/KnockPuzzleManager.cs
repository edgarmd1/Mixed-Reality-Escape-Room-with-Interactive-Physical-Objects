using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class KnockPuzzleManager : MonoBehaviour
{
    public enum EstadoPuzzle
    {
        Inactivo,
        EsperandoPortal,
        TransicionAVR,
        EnPasillo,
        SecuenciaSonando,
        EsperandoLectura,
        TransicionAMR,
        EscuchandoGolpes,
        ValidandoExito,
        PuertaAbierta
    }

    [Header("Estado actual")]
    [SerializeField] private EstadoPuzzle estadoActual = EstadoPuzzle.Inactivo;

    [Header("Secuencia de golpes")]
    [SerializeField, Tooltip("Intervalos entre golpes en segundos.\n" +
        "Golpes totales = intervalos + 1\n" +
        "Patrón actual: 2 golpes (pum pum)")]
    private float[] patronIntervalos = { 0.5f };   
    [SerializeField, Range(0.1f, 0.9f), Tooltip("Tolerancia de timing (±%). 0.5 = ±50%")]
    private float tolerancia = 0.5f;

    [SerializeField, Tooltip("Tiempo máximo (s) para completar toda la secuencia antes de resetear")]
    private float timeoutSecuencia = 3f;

    [SerializeField, Tooltip("Segundos de espera al iniciar la escucha antes de aceptar cualquier golpe. " +
        "Evita falsos positivos por vibraciones de la transición.")]
    private float retardoInicialEscucha = 2f;

    [Header("Audio (escena principal)")]
    [SerializeField, Tooltip("Clip del golpe en la puerta")]
    private AudioClip clipGolpePuerta;

    [SerializeField, Tooltip("AudioSource para sonido de error (patrón incorrecto)")]
    private AudioSource audioError;

    [SerializeField, Tooltip("AudioSource genérico para feedback de golpes en MR")]
    private AudioSource audioFeedbackGolpe;

    [Header("Referencias")]
    [SerializeField] private ArduinoLuz arduinoLuz;
    [SerializeField] private CameraCullingMaskController cameraCulling;
    [SerializeField] private Renderer overlayRenderer;
    [SerializeField, Tooltip("Gestor del puzzle de tablones (en SampleScene). Necesario para re-ocultar tablones rotos al volver del pasillo.")]
    private DoorPuzzleManager doorPuzzleManager;

    [Header("Escena del Pasillo")]
    [SerializeField, Tooltip("Nombre de la escena del pasillo (debe estar en Build Settings)")]
    private string nombreEscenaPasillo = "Pasillo";

    [Header("Transición")]
    [SerializeField] private float duracionFade = 1.5f;

    private Material _overlayMat;
    private readonly List<float> _timestampsGolpes = new List<float>();
    private float _tiempoInicioEscucha;
    private int _totalGolpesEsperados;

    private Vector3 _posicionMRGuardada;
    private Quaternion _rotacionMRGuardada;

    private int _intentos = 0;

    private PasilloManager _pasilloManager;

    void Awake()
    {
        _totalGolpesEsperados = patronIntervalos.Length + 1;

        Debug.Log($"[KnockPuzzle] Patrón cargado: {patronIntervalos.Length} intervalo(s), " +
                  $"{_totalGolpesEsperados} golpe(s) esperados. " +
                  $"Intervalos: [{string.Join(", ", patronIntervalos)}]");

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

    public void IniciarPuzzleGolpes()
    {
        estadoActual = EstadoPuzzle.EsperandoPortal;
        Debug.Log("[KnockPuzzle] Puzzle activado – esperando que el jugador entre al portal.");
    }

    public void OnJugadorEntraPortal()
    {
        if (estadoActual != EstadoPuzzle.EsperandoPortal &&
            estadoActual != EstadoPuzzle.EscuchandoGolpes) return;
        StopAllCoroutines();
        StartCoroutine(TransicionMRaVR());
    }

    public void OnIntentarAbrirPuerta()
    {
        if (estadoActual != EstadoPuzzle.EnPasillo) return;
        StartCoroutine(ReproducirSecuenciaGolpes());
    }

    public void OnVolverAMR()
    {
        if (estadoActual != EstadoPuzzle.EnPasillo &&
            estadoActual != EstadoPuzzle.SecuenciaSonando &&
            estadoActual != EstadoPuzzle.EsperandoLectura)
        {
            Debug.Log($"[KnockPuzzle] OnVolverAMR ignorado – estado actual: {estadoActual}");
            return;
        }

        PararAudioPasillo();

        StopAllCoroutines();
        StartCoroutine(TransicionVRaMR());
    }

    public EstadoPuzzle Estado => estadoActual;
    public bool PuzzleCompletado => estadoActual == EstadoPuzzle.PuertaAbierta;

    private IEnumerator CargarEscenaPasillo()
    {
        Scene escena = SceneManager.GetSceneByName(nombreEscenaPasillo);
        if (!escena.isLoaded)
        {
            Debug.Log($"[KnockPuzzle] Cargando escena '{nombreEscenaPasillo}' aditivamente...");
            AsyncOperation op = SceneManager.LoadSceneAsync(nombreEscenaPasillo, LoadSceneMode.Additive);
            yield return op;
            Debug.Log($"[KnockPuzzle] Escena '{nombreEscenaPasillo}' cargada.");
        }

        yield return null;
        Scene escenaCargada = SceneManager.GetSceneByName(nombreEscenaPasillo);
        if (escenaCargada.isLoaded)
        {
            LimpiarXRDuplicados(escenaCargada);
        }

        yield return null;

        _pasilloManager = PasilloManager.Instance;
        if (_pasilloManager == null)
        {
            Debug.LogError("[KnockPuzzle] No se encontró PasilloManager en la escena cargada. " +
                           "Asegúrate de que la escena tiene un GameObject con PasilloManager.");
        }
    }

    private void LimpiarXRDuplicados(Scene escena)
    {
        int destruidos = 0;

        foreach (GameObject root in escena.GetRootGameObjects())
        {
            bool destruir = false;
            string razon = "";

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

            if (!destruir && root.GetComponentInChildren<Unity.XR.CoreUtils.XROrigin>(true) != null)
            {
                destruir = true;
                razon = "XROrigin";
            }

            if (!destruir && root.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true) != null)
            {
                destruir = true;
                razon = "EventSystem";
            }

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

    private IEnumerator TransicionMRaVR()
    {
        estadoActual = EstadoPuzzle.TransicionAVR;
        Debug.Log("[KnockPuzzle] Transición MR → VR (cargando escena Pasillo).");
        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            _posicionMRGuardada = xrOrigin.transform.position;
            _rotacionMRGuardada = xrOrigin.transform.rotation;
        }

        yield return StartCoroutine(FadeOverlay(1f, duracionFade * 0.5f));
        yield return StartCoroutine(CargarEscenaPasillo());

        cameraCulling?.SetModeSoloVisual(false);

        yield return null;
        TeleportarASpawn(xrOrigin, _pasilloManager?.SpawnInicio);

        yield return new WaitForSeconds(0.2f);
        yield return StartCoroutine(FadeOverlay(0f, duracionFade * 0.5f));

        estadoActual = EstadoPuzzle.EnPasillo;
        Debug.Log("[KnockPuzzle] Jugador en el pasillo VR.");

        StartCoroutine(ReproducirSecuenciaGolpes());
    }

    private IEnumerator TransicionVRaMR()
    {
        estadoActual = EstadoPuzzle.TransicionAMR;
        Debug.Log("[KnockPuzzle] Transición VR → MR (descargando escena Pasillo).");

        PararAudioPasillo();

        audioError?.Stop();
        audioFeedbackGolpe?.Stop();

        yield return StartCoroutine(FadeOverlay(1f, duracionFade * 0.5f));

        yield return StartCoroutine(DescargarEscenaPasillo());

        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null)
        {
            xrOrigin.transform.position = _posicionMRGuardada;
            xrOrigin.transform.rotation = _rotacionMRGuardada;
            Debug.Log($"[KnockPuzzle] Posición MR restaurada: {_posicionMRGuardada}");
        }

        cameraCulling?.SetMode(true);

        doorPuzzleManager?.RestaurarEstadoTablonesRotos();

        yield return new WaitForSeconds(0.2f);

        audioError?.Stop();
        audioFeedbackGolpe?.Stop();

        yield return StartCoroutine(FadeOverlay(0f, duracionFade * 0.5f));

        EmpezarEscucha();
        Debug.Log("[KnockPuzzle] De vuelta en MR – escuchando golpes del jugador.");
    }

    private void PararAudioPasillo()
    {
        if (_pasilloManager == null) return;

        _pasilloManager.AudioGolpe?.Stop();
        _pasilloManager.AudioPuertaAbriendo?.Stop();
        Debug.Log("[KnockPuzzle] Audio del pasillo detenido.");
    }

    private IEnumerator TransicionExitoAVR()
    {
        estadoActual = EstadoPuzzle.ValidandoExito;
        Debug.Log("[KnockPuzzle] ¡Patrón correcto! Volviendo al pasillo para abrir la puerta.");

        yield return StartCoroutine(FadeOverlay(1f, duracionFade * 0.5f));

        yield return StartCoroutine(CargarEscenaPasillo());

        cameraCulling?.SetModeSoloVisual(false);

        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        yield return null;
        TeleportarASpawn(xrOrigin, _pasilloManager?.SpawnInicio);

        yield return new WaitForSeconds(0.2f);

        yield return StartCoroutine(FadeOverlay(0f, duracionFade * 0.5f));

        yield return StartCoroutine(AnimarAperturaPuerta());

        estadoActual = EstadoPuzzle.PuertaAbierta;
        Debug.Log("[KnockPuzzle] ¡Puerta 217 abierta! Puzzle completado.");
    }

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

    private IEnumerator ReproducirSecuenciaGolpes()
    {
        if (estadoActual != EstadoPuzzle.EnPasillo)
        {
            Debug.Log($"[KnockPuzzle] ReproducirSecuenciaGolpes ignorado – estado: {estadoActual}");
            yield break;
        }

        estadoActual = EstadoPuzzle.SecuenciaSonando;
        Debug.Log("[KnockPuzzle] Reproduciendo secuencia de golpes en la puerta...");

        AudioSource audioGolpe = _pasilloManager?.AudioGolpe;

        if (audioGolpe != null)
        {
            audioGolpe.loop = false;
            audioGolpe.Play();
            yield return new WaitUntil(() => audioGolpe == null || !audioGolpe.isPlaying);
        }

        yield return new WaitForSeconds(1f);

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

        float tiempoEscuchando = Time.time - _tiempoInicioEscucha;
        if (tiempoEscuchando < retardoInicialEscucha)
        {
            Debug.Log($"[KnockPuzzle] Golpe ignorado (retardo inicial: {tiempoEscuchando:F2}s < {retardoInicialEscucha}s).");
            return;
        }

        _timestampsGolpes.Add(Time.time);
        Debug.Log($"[KnockPuzzle] Golpe #{_timestampsGolpes.Count}/{_totalGolpesEsperados} detectado.");

        if (audioFeedbackGolpe != null && audioFeedbackGolpe.clip != null)
            audioFeedbackGolpe.PlayOneShot(audioFeedbackGolpe.clip);

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
        estadoActual = EstadoPuzzle.Inactivo;
        _timestampsGolpes.Clear();
        _intentos++;
        Debug.Log($"[KnockPuzzle] Escucha reiniciada (intento #{_intentos}).");
        estadoActual = EstadoPuzzle.EscuchandoGolpes;
    }

    [Header("Puerta 217")]
    [SerializeField] private float duracionApertura = 1.5f;

    private IEnumerator AnimarAperturaPuerta()
    {
        _pasilloManager.AudioPuertaAbriendo?.Play();

        // Esperar la duración del sonido/animación antes del swap
        yield return new WaitForSeconds(duracionApertura);

        // Desactivar puerta cerrada y activar puerta abierta
        if (_pasilloManager.Puerta217 != null)
            _pasilloManager.Puerta217.gameObject.SetActive(false);

        if (_pasilloManager.PuertaAbierta != null)
            _pasilloManager.PuertaAbierta.SetActive(true);

        // Desactivar el collider interactable
        Collider col = _pasilloManager.PuertaInteractable;
        if (col != null) col.enabled = false;

        // Activar el trigger de entrada a la habitación 217
        if (_pasilloManager.Trigger217Entrada != null)
        {
            _pasilloManager.Trigger217Entrada.enabled = true;
            Debug.Log("[KnockPuzzle] Trigger de entrada a habitación 217 activado.");
        }

        Debug.Log("[KnockPuzzle] Puerta abierta: cerrada desactivada, abierta activada.");
    }

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
