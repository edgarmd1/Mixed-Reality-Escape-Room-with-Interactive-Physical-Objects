using System.Collections;
using UnityEngine;

public class PolaroidJumpscareController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform de la foto polaroid virtual")]
    public Transform polaroidTransform;

    [Tooltip("Renderer de la polaroid")]
    public Renderer polaroidRenderer;

    [Tooltip("ArduinoLuz")]
    public ArduinoLuz arduinoLuz;

    [Tooltip("Activa el jumpscare VR")]
    public CameraCullingMaskController cameraCullingMaskController;

    [Header("Fase 1 – Vuelo")]
    [Tooltip("Duración de la fase de vuelo hacia la cámara")]
    public float duracionVuelo = 2.5f;

    [Tooltip("Escala máxima que alcanza la polaroid")]
    public float multiplicadorEscalaFinal = 6f;

    [Tooltip("Distancia detrás de la cámara hasta la que viaja la polaroid")]
    public float distanciaDetras = 0.4f;

    [Tooltip("Curva de easing para el vuelo")]
    public AnimationCurve curvaVuelo = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Fase 2 – Quemado")]
    [Tooltip("Duración del quemado")]
    public float duracionQuemado = 2f;

    [Tooltip("Shader quemado")]
    public Material materialQuemado;

    [Tooltip("Audio quemado")]
    public AudioSource audioQuemado;

    [Tooltip("Nombre del parámetro de quemado en el shader")]
    public string shaderParamBurn = "_BurnAmount";

    private Vector3    _posicionOriginal;
    private Quaternion _rotacionOriginal;
    private Vector3    _escalaOriginal;
    private Material   _materialOriginal;
    private bool       _secuenciaActiva = false;

    void Start()
    {
        // Guardar estado original de la polaroid
        if (polaroidTransform != null)
        {
            _posicionOriginal = polaroidTransform.position;
            _rotacionOriginal = polaroidTransform.rotation;
            _escalaOriginal   = polaroidTransform.localScale;
        }

        if (polaroidRenderer != null)
            _materialOriginal = polaroidRenderer.material;

        // Suscribirse al evento del sensor de luz
        if (arduinoLuz != null)
            arduinoLuz.OnLuzDetectada += IniciarSecuencia;
        else
            Debug.LogWarning("[PolaroidJumpscareController] ArduinoLuz no asignado.");
    }

    void OnDestroy()
    {
        if (arduinoLuz != null)
            arduinoLuz.OnLuzDetectada -= IniciarSecuencia;
    }

    public void IniciarSecuencia()
    {
        if (_secuenciaActiva) return;
        _secuenciaActiva = true;
        StartCoroutine(CoroutineSecuencia());
    }

    private IEnumerator CoroutineSecuencia()
    {
        Debug.Log("[PolaroidJumpscare] Iniciando secuencia polaroid.");

        yield return StartCoroutine(CoroutineVuelo());

        yield return StartCoroutine(CoroutineQuemado());

        Debug.Log("[PolaroidJumpscare] Activando jumpscare VR.");
        cameraCullingMaskController?.SetMode(false);
    }

    private IEnumerator CoroutineVuelo()
    {
        if (polaroidTransform == null)
        {
            Debug.LogWarning("[PolaroidJumpscare] polaroidTransform no asignado – saltando vuelo.");
            yield break;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[PolaroidJumpscare] No se encontró Camera.main – saltando vuelo.");
            yield break;
        }

        Vector3 posInicio  = _posicionOriginal;
        Vector3 escInicio  = _escalaOriginal;
        Vector3 escFinal   = _escalaOriginal * multiplicadorEscalaFinal;

        float tiempo = 0f;

        while (tiempo < duracionVuelo)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionVuelo);
            float tEased = curvaVuelo.Evaluate(t);

            Vector3 posFinal = cam.transform.position - cam.transform.forward * distanciaDetras;

            polaroidTransform.position   = Vector3.Lerp(posInicio, posFinal, tEased);
            polaroidTransform.localScale = Vector3.Lerp(escInicio, escFinal, tEased);

            yield return null;
        }

        polaroidTransform.gameObject.SetActive(false);
        Debug.Log("[PolaroidJumpscare] Vuelo completado – polaroid oculta.");
    }

    private IEnumerator CoroutineQuemado()
    {
        if (polaroidTransform == null || polaroidRenderer == null)
        {
            Debug.LogWarning("[PolaroidJumpscare] polaroidTransform/Renderer no asignado – saltando quemado.");
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

        Debug.Log("[PolaroidJumpscare] Quemado completado – polaroid desactivada.");
    }
}
