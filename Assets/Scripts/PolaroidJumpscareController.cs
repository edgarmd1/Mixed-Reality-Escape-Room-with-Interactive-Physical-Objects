using System.Collections;
using UnityEngine;

public class PolaroidJumpscareController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform de la foto polaroid virtual")]
    public Transform polaroidTransform;

    [Tooltip("Renderer de la polaroid")]
    public Renderer polaroidRenderer;

    [Tooltip("ArduinoLuz que detecta la linterna")]
    public ArduinoLuz arduinoLuz;

    [Tooltip("Controlador de modos")]
    public CameraCullingMaskController cameraCullingMaskController;

    [Header("Fase 1 – Vuelo")]
    [Tooltip("Duración de la fase de vuelo")]
    public float duracionVuelo = 4f;

    [Tooltip("Multiplicador de escala máxima durante el vuelo")]
    public float multiplicadorEscalaFinal = 6f;

    [Tooltip("Distancia detrás de la cámara hasta la que viaja la polaroid")]
    public float distanciaDetras = 0.4f;

    [Tooltip("Desplazamiento extra hacia atrás respecto a su propia orientación")]
    public float offsetInicioHaciaAtras = 1.5f;

    [Tooltip("Curva de easing para el vuelo")]
    public AnimationCurve curvaVuelo = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Fase 2 – Quemado")]
    [Tooltip("Duración del efecto de quemado")]
    public float duracionQuemado = 2f;

    [Tooltip("Material con shader Custom/PolaroidBurn")]
    public Material materialQuemado;

    [Tooltip("AudioSource que se reproduce al inicio del quemado")]
    public AudioSource audioQuemado;

    [Tooltip("Nombre del parámetro de quemado en el shader")]
    public string shaderParamBurn = "_BurnAmount";

    [Tooltip("Si está activo, la polaroid virtual permanece oculta")]
    public bool mostrarSoloConLuz = true;

    private Vector3 _posicionOriginal;
    private Quaternion _rotacionOriginal;
    private Vector3 _escalaOriginal;
    private Material _materialOriginal;
    private bool _vueloActivo = false;
    private bool _polaroidVisible = false;
    private ObjectCalibrationManager _calibrationManager;


    void Awake()
    {
        CapturarEstadoOriginal();
        _calibrationManager = FindObjectOfType<ObjectCalibrationManager>();
    }

    void LateUpdate()
    {
        if (_calibrationManager != null && _calibrationManager.IsCalibrating) return;

        if (mostrarSoloConLuz && !_polaroidVisible && !_vueloActivo && polaroidTransform != null)
        {
            if (polaroidTransform.gameObject.activeSelf)
                polaroidTransform.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        if (polaroidRenderer != null)
            _materialOriginal = polaroidRenderer.material;

        if (mostrarSoloConLuz && polaroidTransform != null)
        {
            polaroidTransform.gameObject.SetActive(false);
            _polaroidVisible = false;
        }

        if (arduinoLuz != null)
        {
            arduinoLuz.OnUmbralSuperado += MostrarPolaroid;
            arduinoLuz.OnLuzDetectada   += IniciarVuelo;
        }
        else
            Debug.LogWarning("[PolaroidJumpscare] ArduinoLuz no asignado.");
    }

    void OnDestroy()
    {
        if (arduinoLuz != null)
        {
            arduinoLuz.OnUmbralSuperado -= MostrarPolaroid;
            arduinoLuz.OnLuzDetectada   -= IniciarVuelo;
        }
    }

    public void MostrarPolaroid()
    {
        if (_polaroidVisible || _vueloActivo || polaroidTransform == null) return;
        _polaroidVisible = true;
        polaroidTransform.gameObject.SetActive(true);
        Debug.Log("[PolaroidJumpscare] Polaroid virtual visible.");
    }

    public void OcultarPolaroid()
    {
        if (!_polaroidVisible || _vueloActivo || polaroidTransform == null) return;
        RefrescarPosicionOriginal();
        _polaroidVisible = false;
        polaroidTransform.gameObject.SetActive(false);
        Debug.Log("[PolaroidJumpscare] Polaroid virtual oculta. Posición original actualizada.");
    }

    public void RefrescarPosicionOriginal()
    {
        CapturarEstadoOriginal();
        Debug.Log($"[PolaroidJumpscare] Posición original actualizada: {_posicionOriginal}");
    }

    private void CapturarEstadoOriginal()
    {
        if (polaroidTransform != null)
        {
            _posicionOriginal = polaroidTransform.position;
            _rotacionOriginal = polaroidTransform.rotation;
            _escalaOriginal   = polaroidTransform.localScale;
        }
    }

    public void IniciarVuelo()
    {
        if (_vueloActivo) return;
        _vueloActivo = true;
        StartCoroutine(CoroutineVuelo());
    }

    public void IniciarQuemado()
    {
        StartCoroutine(CoroutineQuemado());
    }
    public IEnumerator QuemadoConEspera()
    {
        yield return StartCoroutine(CoroutineQuemado());
    }

    private IEnumerator CoroutineVuelo()
    {
        if (polaroidTransform == null)
        {
            Debug.LogWarning("[PolaroidJumpscare] polaroidTransform no asignado");
            FallbackActivarVR();
            yield break;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[PolaroidJumpscare] No se encontró Camera.main");
            FallbackActivarVR();
            yield break;
        }

        polaroidTransform.gameObject.SetActive(true);
        int capaPolaroid = polaroidTransform.gameObject.layer;
        cam.cullingMask |= (1 << capaPolaroid);

        _posicionOriginal = polaroidTransform.position;
        _rotacionOriginal = polaroidTransform.rotation;
        _escalaOriginal   = polaroidTransform.localScale;

        Vector3 dirAtras = -(_rotacionOriginal * Vector3.forward);
        Vector3 posInicio = _posicionOriginal + dirAtras * offsetInicioHaciaAtras;
        Vector3 escInicio = _escalaOriginal;
        Vector3 escFinal  = _escalaOriginal * multiplicadorEscalaFinal;

        float tiempo = 0f;

        Debug.Log("[PolaroidJumpscare] Iniciando vuelo de la polaroid.");

        while (tiempo < duracionVuelo)
        {
            tiempo += Time.deltaTime;
            float t      = Mathf.Clamp01(tiempo / duracionVuelo);
            float tEased = curvaVuelo.Evaluate(t);

            Vector3 posFinal = cam.transform.position - cam.transform.forward * distanciaDetras;

            polaroidTransform.position   = Vector3.Lerp(posInicio, posFinal, tEased);
            polaroidTransform.localScale = Vector3.Lerp(escInicio, escFinal, tEased);

            yield return null;
        }

        polaroidTransform.gameObject.SetActive(false);
        _polaroidVisible = false;
        Debug.Log("[PolaroidJumpscare] Vuelo completado. Activando VR.");

        arduinoLuz?.CompletarPuzzle();
        cameraCullingMaskController?.SetMode(false);
    }

    private IEnumerator CoroutineQuemado()
    {
        if (polaroidTransform == null || polaroidRenderer == null)
        {
            Debug.LogWarning("[PolaroidJumpscare] polaroidTransform/Renderer no asignado.");
            yield break;
        }

        polaroidTransform.position   = _posicionOriginal;
        polaroidTransform.rotation   = _rotacionOriginal;
        polaroidTransform.localScale = _escalaOriginal;
        if (materialQuemado != null)
        {
            polaroidRenderer.material = materialQuemado;
            materialQuemado.SetFloat(shaderParamBurn, 0f);
        }

        polaroidTransform.gameObject.SetActive(true);

        if (audioQuemado != null)
            audioQuemado.Play();

        Debug.Log("[PolaroidJumpscare] Iniciando quemado.");

        float tiempo = 0f;
        while (tiempo < duracionQuemado)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionQuemado);

            if (materialQuemado != null)
                materialQuemado.SetFloat(shaderParamBurn, t);

            yield return null;
        }

        polaroidTransform.gameObject.SetActive(false);

        if (_materialOriginal != null)
            polaroidRenderer.material = _materialOriginal;

        Debug.Log("[PolaroidJumpscare] Quemado completado.");
    }

    private void FallbackActivarVR()
    {
        arduinoLuz?.CompletarPuzzle();
        cameraCullingMaskController?.SetMode(false);
    }

#if UNITY_EDITOR
    [ContextMenu("Simular: Mostrar Polaroid (umbral luz)")]
    private void SimularMostrarPolaroid()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Solo en Play Mode."); return; }
        _polaroidVisible = false;
        MostrarPolaroid();
    }

    [ContextMenu("Simular: Iniciar Vuelo")]
    private void SimularVuelo()
    {
        if (!Application.isPlaying) { Debug.LogWarning("Solo en Play Mode."); return; }
        IniciarVuelo();
    }
#endif
}
