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
    public float duracionVuelo = 2.5f;

    [Tooltip("Multiplicador de escala máxima durante el vuelo")]
    public float multiplicadorEscalaFinal = 6f;

    [Tooltip("Distancia detrás de la cámara hasta la que viaja la polaroid")]
    public float distanciaDetras = 0.4f;

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

    private Vector3    _posicionOriginal;
    private Quaternion _rotacionOriginal;
    private Vector3    _escalaOriginal;
    private Material   _materialOriginal;
    private bool       _vueloActivo = false;

    void Awake()
    {
        CapturarEstadoOriginal();
    }

    void Start()
    {
        if (polaroidRenderer != null)
            _materialOriginal = polaroidRenderer.material;

        if (arduinoLuz != null)
            arduinoLuz.OnLuzDetectada += IniciarVuelo;
        else
            Debug.LogWarning("[PolaroidJumpscare] ArduinoLuz no asignado.");
    }

    void OnDestroy()
    {
        if (arduinoLuz != null)
            arduinoLuz.OnLuzDetectada -= IniciarVuelo;
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
            Debug.LogWarning("[PolaroidJumpscare] polaroidTransform no asignado – saltando vuelo.");
            FallbackActivarVR();
            yield break;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[PolaroidJumpscare] No se encontró Camera.main – saltando vuelo.");
            FallbackActivarVR();
            yield break;
        }

        polaroidTransform.gameObject.SetActive(true);
        int capaPolaroid = polaroidTransform.gameObject.layer;
        cam.cullingMask |= (1 << capaPolaroid);
        _posicionOriginal = polaroidTransform.position;
        _rotacionOriginal = polaroidTransform.rotation;
        _escalaOriginal   = polaroidTransform.localScale;

        Vector3 posInicio = _posicionOriginal;
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
}
