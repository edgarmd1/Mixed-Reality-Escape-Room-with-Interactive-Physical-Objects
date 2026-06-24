using UnityEngine;

//old, to remove

public class MicKnockDetector : MonoBehaviour
{
    [Header("Dispositivo de audio")]
    [Tooltip("Nombre exacto del micrófono a usar. Dejar vacío para usar el primero disponible.")]
    [SerializeField] private string deviceName = "";

    [Header("Detección de golpes")]
    [Tooltip("Amplitud RMS mínima para considerar un golpe (rango 0–1). " +
             "Sube si hay muchos falsos positivos; baja si no detecta.")]
    [SerializeField, Range(0.001f, 1f)] private float umbralRMS = 0.05f;

    [Tooltip("Tiempo mínimo entre golpes (segundos). Evita dobles detecciones del mismo golpe.")]
    [SerializeField, Range(0.05f, 2f)] private float debounceSegundos = 0.3f;

    [Tooltip("Tamaño de la ventana de análisis en segundos (cuántos samples se leen por frame).")]
    [SerializeField, Range(0.01f, 0.2f)] private float windowSegundos = 0.05f;

    [Header("Debug")]
    [Tooltip("Muestra el RMS actual en cada frame en la consola (solo para calibración).")]
    [SerializeField] private bool logRMSContinuo = false;

    public System.Action OnKnockDetected;

    private AudioClip _clipGrabacion;
    private string    _dispositivoActivo;
    private int       _frecuenciaMuestreo = 44100;
    private int       _ultimaPosicion     = 0;
    private float     _ultimoKnockTime    = -999f;
    private bool      _micInicializado    = false;

    void Start()
    {
        IniciarMicrofono();
    }

    void Update()
    {
        if (!_micInicializado) return;

        if (!Microphone.IsRecording(_dispositivoActivo))
        {
            Debug.LogWarning("[MicKnock] El micrófono dejó de grabar. Intentando reiniciar...");
            IniciarMicrofono();
            return;
        }

        AnalizarAudio();
    }

    void OnDestroy()
    {
        if (_micInicializado && Microphone.IsRecording(_dispositivoActivo))
        {
            Microphone.End(_dispositivoActivo);
            Debug.Log("[MicKnock] Micrófono detenido.");
        }
    }

    private void IniciarMicrofono()
    {
        string[] dispositivos = Microphone.devices;

        if (dispositivos == null || dispositivos.Length == 0)
        {
            Debug.LogError("[MicKnock] No se encontró ningún micrófono. " +
                           "El KnockPuzzle no detectará golpes físicos.");
            return;
        }

        _dispositivoActivo = string.IsNullOrEmpty(deviceName)
            ? dispositivos[0]
            : deviceName;

        bool encontrado = false;
        foreach (string d in dispositivos)
            if (d == _dispositivoActivo) { encontrado = true; break; }

        if (!encontrado)
        {
            Debug.LogWarning($"[MicKnock] Dispositivo '{_dispositivoActivo}' no encontrado. " +
                             $"Usando '{dispositivos[0]}' por defecto.");
            _dispositivoActivo = dispositivos[0];
        }

        Microphone.GetDeviceCaps(_dispositivoActivo, out int minFreq, out int maxFreq);
        _frecuenciaMuestreo = (maxFreq > 0) ? Mathf.Clamp(44100, minFreq, maxFreq) : 44100;

        _clipGrabacion = Microphone.Start(_dispositivoActivo, true, 1, _frecuenciaMuestreo);
        _ultimaPosicion = 0;
        _micInicializado = true;

        Debug.Log($"[MicKnock] Micrófono iniciado: '{_dispositivoActivo}' " +
                  $"@ {_frecuenciaMuestreo} Hz | Umbral RMS: {umbralRMS:F3} | " +
                  $"Debounce: {debounceSegundos:F2}s");
    }

    private void AnalizarAudio()
    {
        int posActual = Microphone.GetPosition(_dispositivoActivo);
        int totalSamples = _clipGrabacion.samples;

        int samplesNuevos = (posActual - _ultimaPosicion + totalSamples) % totalSamples;

        int windowSamples = Mathf.Min(
            Mathf.RoundToInt(windowSegundos * _frecuenciaMuestreo),
            samplesNuevos
        );

        if (windowSamples <= 0) return;

        int startSample = (posActual - windowSamples + totalSamples) % totalSamples;

        float[] datos = new float[windowSamples];
        _clipGrabacion.GetData(datos, startSample);

        _ultimaPosicion = posActual;

        float sumaCuadrados = 0f;
        foreach (float s in datos)
            sumaCuadrados += s * s;
        float rms = Mathf.Sqrt(sumaCuadrados / datos.Length);

        if (logRMSContinuo)
            Debug.Log($"[MicKnock] RMS: {rms:F4}");

        if (rms >= umbralRMS)
        {
            float ahora = Time.time;
            if (ahora - _ultimoKnockTime >= debounceSegundos)
            {
                _ultimoKnockTime = ahora;
                Debug.Log($"[MicKnock] ¡Golpe detectado! (RMS: {rms:F4} ≥ umbral: {umbralRMS:F4})");
                OnKnockDetected?.Invoke();
            }
            else
            {
                Debug.Log($"[MicKnock] Pico ignorado por debounce " +
                          $"({ahora - _ultimoKnockTime:F3}s < {debounceSegundos:F2}s)");
            }
        }
    }

    [ContextMenu("Listar micrófonos disponibles")]
    private void ListarMicrofonos()
    {
        string[] dispositivos = Microphone.devices;
        if (dispositivos == null || dispositivos.Length == 0)
        {
            Debug.Log("[MicKnock] No hay micrófonos disponibles.");
            return;
        }
        Debug.Log($"[MicKnock] {dispositivos.Length} micrófono(s) disponible(s):");
        for (int i = 0; i < dispositivos.Length; i++)
            Debug.Log($"  [{i}] {dispositivos[i]}");
    }

    [ContextMenu("Simular golpe")]
    private void SimularGolpe()
    {
        Debug.Log("[MicKnock] Golpe simulado desde el Inspector.");
        OnKnockDetected?.Invoke();
    }
}
