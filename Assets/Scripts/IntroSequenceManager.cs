using System.Collections;
using UnityEngine;
using UnityEngine.XR.Templates.MR;
using UnityEngine.Video;

public class IntroSequenceManager : MonoBehaviour
{
    [SerializeField] private float delayInicial = 3f;

    [SerializeField] private float duracionParpadeo = 4f;

    [SerializeField, Range(0f, 1f)] private float alphaOscuridad = 0.88f;

    [SerializeField] public float duracionEnVR = 10f;
    [SerializeField] private float duracionFadeVuelta = 1.5f;

    [SerializeField] private float intervaloInicialParpadeo = 0.55f;

    [SerializeField] private float intervaloFinalParpadeo = 0.04f;

    [Header("Parpadeo Rojo VR")]
    [SerializeField] private float intervaloParpadeoRojo = 0.2f;
    [SerializeField, Range(0f, 1f)] private float alphaRojoMaximo = 0.4f;

    [SerializeField] private Renderer overlayRenderer;

    [SerializeField] private ArduinoLuz arduinoLuz;

    [Header("Focos DMX")]
    [SerializeField, Tooltip("Controlador DMX de los focos físicos de la sala")]
    private DMXController dmxController;

    [SerializeField] private CameraCullingMaskController cameraCullingMask;

    [SerializeField] private AudioSource audioMusica;
    [SerializeField, Tooltip("Efecto de sonido que se reproduce durante el parpadeo")]
    private AudioSource audioDuranteParpadeo;

    [Header("Tutorial e Inspectora")]
    [SerializeField, Tooltip("Objeto base del video intro")]
    private GameObject rootVideoTutorial;
    [SerializeField, Tooltip("Componente VideoPlayer dentro del tutorial")]
    private VideoPlayer videoTutorial;
    [SerializeField, Tooltip("Segundo audio donde menciona la electricidad")]
    private AudioSource audioInspector2;
    [SerializeField, Tooltip("Objeto que se activa cuando se va la luz por completo")]
    private GameObject objetoApareceOscuridad;
    [SerializeField] private GameObject portalAscensor;
    [SerializeField] private DoorPuzzleManager doorPuzzleManager;

    [Header("Ascensor")]
    [SerializeField, Tooltip("GameObjects")]
    private GameObject[] objetosJumpscare;
    [SerializeField, Tooltip("Controller de la polaroid")]
    private PolaroidJumpscareController polaroidJumpscare;
    [SerializeField, Tooltip("Fastidio")]
    private AudioSource audioFastidio;

    [Header("Vitrina")]
    [SerializeField, Tooltip("Gestor de la vitrina")]
    private VitrineManager vitrineManager;

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

        if (objetoApareceOscuridad != null)
            objetoApareceOscuridad.SetActive(false);

        if (rootVideoTutorial != null)
            rootVideoTutorial.SetActive(false);
        else if (videoTutorial != null)
            videoTutorial.gameObject.SetActive(false);

        if (arduinoLuz != null)
            arduinoLuz.habilitado = false;

        if (dmxController != null)
            dmxController.EncenderBlanco();

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
    }

    private IEnumerator SecuenciaIntro()
    {
        if (videoTutorial != null)
        {
            if (rootVideoTutorial != null) rootVideoTutorial.SetActive(true);
            else videoTutorial.gameObject.SetActive(true);

            videoTutorial.Play();
            yield return new WaitForSeconds(0.5f);
            yield return new WaitWhile(() => videoTutorial.isPlaying);

            if (rootVideoTutorial != null) rootVideoTutorial.SetActive(false);
            else videoTutorial.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(delayInicial); 

        if (audioDuranteParpadeo != null)
            audioDuranteParpadeo.Play();

        yield return StartCoroutine(CoroutineParpadeo()); 

        if (objetoApareceOscuridad != null)
            objetoApareceOscuridad.SetActive(true);

        if (audioInspector2 != null)
        {
            audioInspector2.Play();
        }

        if (arduinoLuz != null)
            arduinoLuz.habilitado = true; 

        yield return new WaitUntil(() => arduinoLuz == null || arduinoLuz.puzzleCompletado); 

        if (objetoApareceOscuridad != null)
            objetoApareceOscuridad.SetActive(false);

        foreach (var obj in objetosJumpscare)
            if (obj != null) obj.SetActive(false);

        if (environmentFade != null)
            environmentFade.FadeSkybox(false);

        yield return StartCoroutine(CoroutineFadeOverlay(0f, 0.4f)); 

        if (audioMusica != null)
            audioMusica.Play(); 

        yield return StartCoroutine(CoroutineParpadeoRojo(duracionEnVR));

        foreach (var obj in objetosJumpscare)
            if (obj != null) obj.SetActive(true);

        yield return StartCoroutine(CoroutineFadeOverlay(0.6f, 0.4f));

        if (environmentFade != null)
            environmentFade.FadeSkybox(true);

        cameraCullingMask?.SetMode(true);

        if (portalAscensor != null)
            portalAscensor.SetActive(true);

        StartCoroutine(CoroutineFadeOverlay(0f, duracionFadeVuelta));

        if (polaroidJumpscare != null)
            yield return StartCoroutine(polaroidJumpscare.QuemadoConEspera());

        if (audioFastidio != null && audioFastidio.clip != null)
        {
            audioFastidio.Play();
            yield return new WaitForSeconds(audioFastidio.clip.length);
        }


        if (doorPuzzleManager != null)
            doorPuzzleManager.IniciarPuzzle();

        if (vitrineManager != null)
            vitrineManager.Activar();

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
            float alphaActual = flashActivo ? alphaMax : alphaMin;
            SetOverlayAlpha(alphaActual);

            if (dmxController != null)
            {
                byte brilloFoco = (byte)(Mathf.Clamp01(1f - alphaActual) * 255f);
                dmxController.SetBrilloBlanco(brilloFoco);
            }

            float tiempoEspera = intervalo * (flashActivo ? 0.35f : 0.65f);
            yield return new WaitForSeconds(tiempoEspera);
            tiempoAcumulado += intervalo;
        }

        if (dmxController != null)
            dmxController.Apagar();

        yield return StartCoroutine(CoroutineFadeOverlay(alphaOscuridad, 0.5f));
    }

    private IEnumerator CoroutineParpadeoRojo(float duracion)
    {
        float tiempoAcumulado = 0f;
        bool flashActivo = false;
        
        Color colorOriginal = _overlayMat.color;
        _overlayMat.color = new Color(1f, 0f, 0f, colorOriginal.a);

        while (tiempoAcumulado < duracion)
        {
            flashActivo = !flashActivo;
            SetOverlayAlpha(flashActivo ? alphaRojoMaximo : 0f);

            if (dmxController != null)
            {
                byte brilloRojo = (byte)(flashActivo ? 255 : 0);
                dmxController.SetBrilloRojo(brilloRojo);
            }

            float tiempoEspera = flashActivo ? (intervaloParpadeoRojo * 0.4f) : (intervaloParpadeoRojo * 0.6f);
            
            if (tiempoAcumulado + tiempoEspera > duracion)
            {
                tiempoEspera = duracion - tiempoAcumulado;
            }

            yield return new WaitForSeconds(tiempoEspera);
            tiempoAcumulado += tiempoEspera;
        }

        if (dmxController != null)
            dmxController.Apagar();

        _overlayMat.color = new Color(0f, 0f, 0f, 0f);
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
