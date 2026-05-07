using System.Collections;
using UnityEngine;
using UnityEngine.XR.Templates.MR;

public class IntroSequenceManager : MonoBehaviour
{
    [SerializeField] private float delayInicial = 3f;

    [SerializeField] private float duracionParpadeo = 4f;

    [SerializeField, Range(0f, 1f)] private float alphaOscuridad = 0.88f;

    [SerializeField] public float duracionEnVR = 10f;
    [SerializeField] private float duracionFadeVuelta = 1.5f;

    [SerializeField] private float intervaloInicialParpadeo = 0.55f;

    [SerializeField] private float intervaloFinalParpadeo = 0.04f;

    [SerializeField] private Renderer overlayRenderer;

    [SerializeField] private ArduinoLuz arduinoLuz;

    [SerializeField] private CameraCullingMaskController cameraCullingMask;

    [SerializeField] private AudioSource audioMusica;
    [SerializeField, Tooltip("Efecto de sonido que se reproduce durante el parpadeo")]
    private AudioSource audioDuranteParpadeo;
    [SerializeField] private GameObject portalAscensor;
    [SerializeField] private DoorPuzzleManager doorPuzzleManager;

    [Header("Environment VR")]
    [SerializeField, Tooltip("FadeMaterial del Environment para hacerlo visible en la escena VR")]
    private FadeMaterial environmentFade;

    [Header("Modo Demo")]
    [SerializeField, Tooltip("Si está activo, salta toda la secuencia y va directo al puzzle de la puerta")]
    private bool modoDemo = false;

    private Material _overlayMat;
    private bool _secuenciaFinalizada = false;

    void Start()
    {
        if (overlayRenderer != null)
        {
            _overlayMat = overlayRenderer.material;
            SetOverlayAlpha(0f);
        }
        if (portalAscensor != null)
            portalAscensor.SetActive(false);

        if (arduinoLuz != null)
            arduinoLuz.habilitado = false;

        if (modoDemo)
            StartCoroutine(SecuenciaDemoDirecta());
        else
            StartCoroutine(SecuenciaIntro());
    }

    private IEnumerator SecuenciaDemoDirecta()
    {
        SetOverlayAlpha(0f);
        cameraCullingMask?.SetMode(true);  
        if (portalAscensor != null) portalAscensor.SetActive(false);
        yield return null;                 
        if (doorPuzzleManager != null)
            doorPuzzleManager.IniciarPuzzle();
        _secuenciaFinalizada = true;
        Debug.Log("[Intro] MODO DEMO: puzzle de puerta activado directamente.");
    }

    private IEnumerator SecuenciaIntro()
    {
        yield return new WaitForSeconds(delayInicial); 

        if (audioDuranteParpadeo != null)
            audioDuranteParpadeo.Play();

        yield return StartCoroutine(CoroutineParpadeo()); 

        if (arduinoLuz != null)
            arduinoLuz.habilitado = true; 

        yield return new WaitUntil(() => arduinoLuz == null || arduinoLuz.puzzleCompletado); 

        if (environmentFade != null)
            environmentFade.FadeSkybox(false);

        yield return StartCoroutine(CoroutineFadeOverlay(0f, 0.4f)); 

        if (audioMusica != null)
            audioMusica.Play(); 

        yield return new WaitForSeconds(duracionEnVR); 

        yield return StartCoroutine(CoroutineFadeOverlay(0.6f, 0.4f));

        if (environmentFade != null)
            environmentFade.FadeSkybox(true);

        cameraCullingMask?.SetMode(true);

        if (portalAscensor != null)
            portalAscensor.SetActive(true);

        yield return StartCoroutine(CoroutineFadeOverlay(0f, duracionFadeVuelta));

        if (doorPuzzleManager != null)
            doorPuzzleManager.IniciarPuzzle();

        _secuenciaFinalizada = true;
    }

    private IEnumerator CoroutineParpadeo()
    {
        float tiempoAcumulado = 0f;
        bool flashActivo = false;

        while (tiempoAcumulado < duracionParpadeo)
        {
            float progreso = Mathf.Clamp01(tiempoAcumulado / duracionParpadeo);

            float t = progreso * progreso;
            float intervalo = Mathf.Lerp(intervaloInicialParpadeo, intervaloFinalParpadeo, t);

            float alphaMax = Mathf.Lerp(0.35f, alphaOscuridad, progreso);

            float alphaMin = Mathf.Lerp(0f, alphaOscuridad * 0.25f, progreso);

            flashActivo = !flashActivo;
            SetOverlayAlpha(flashActivo ? alphaMax : alphaMin);
            float tiempoEspera = intervalo * (flashActivo ? 0.35f : 0.65f);
            yield return new WaitForSeconds(tiempoEspera);
            tiempoAcumulado += intervalo;
        }
        yield return StartCoroutine(CoroutineFadeOverlay(alphaOscuridad, 0.5f));
    }

    private IEnumerator CoroutineFadeOverlay(float alphaObjetivo, float duracion)
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

    public bool SecuenciaFinalizada => _secuenciaFinalizada;
}
