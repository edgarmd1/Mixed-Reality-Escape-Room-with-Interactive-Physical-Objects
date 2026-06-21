using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class FotoJumpscareManager : MonoBehaviour
{
    [Header("Detección de proximidad")]
    [SerializeField, Tooltip("Radio alrededor de la bañera para detectar la cámara suelta")]
    private float radioDeteccion = 1.5f;

    [Header("Visor de foto")]
    [SerializeField, Tooltip("GameObject del visor")]
    private GameObject visorFoto;
    [SerializeField, Tooltip("Distancia delante de la cámara del jugador donde aparece el visor")]
    private float distanciaVisor = 0.55f;

    [Header("Flash de cámara")]
    [SerializeField, Tooltip("Renderer del overlay existente en la escena")]
    private Renderer overlayRenderer;
    [SerializeField, Tooltip("Duración del flash blanco en segundos")]
    private float duracionFlash = 0.3f;

    [Header("Audio")]
    [SerializeField, Tooltip("Sonido de captura de cámara")]
    private AudioSource audioShutter;
    [SerializeField, Tooltip("Sonido que suena al activarse el objeto de susto")]
    private AudioSource audioSusto;

    [Header("Susto (en habitación 217)")]
    [SerializeField, Tooltip("GameObject zombie 2")]
    private GameObject objetoSusto;
    [SerializeField, Tooltip("GameObject zombie 1")]
    private GameObject zombie1;
    [SerializeField, Tooltip("GameObject zombie 3")]
    private GameObject zombie3;
    [SerializeField, Tooltip("Segundos que el objeto de susto permanece visible antes del parpadeo")]
    private float duracionSusto = 1.5f;

    [Header("Parpadeo de luces")]
    [SerializeField, Tooltip("Duración total del parpadeo en segundos")]
    private float duracionParpadeoFinal = 4f;
    [SerializeField, Tooltip("Intervalo entre flashes al inicio")]
    private float intervaloInicialParpadeo = 0.55f;
    [SerializeField, Tooltip("Intervalo entre flashes al final")]
    private float intervaloFinalParpadeo = 0.04f;
    [SerializeField, Tooltip("Alpha máximo del overlay durante el parpadeo")]
    [Range(0f, 1f)] private float alphaParpadeo = 0.88f;
    [SerializeField, Tooltip("Audio que suena durante el parpadeo de luces")]
    private AudioSource audioParpadeo;
    [SerializeField, Tooltip("(En Proceso) Controlador DMX para sincronizar focos físicos con el parpadeo")]
    private DMXController dmxController;

    [Header("Vuelta al MR")]
    [SerializeField, Tooltip("GameObject de la habitación 217")]
    private GameObject habitacion217Root;
    [SerializeField, Tooltip("CameraCullingMaskController para volver al modo MR")]
    private CameraCullingMaskController cameraCulling;
    [SerializeField, Tooltip("Video final al volver a MR")]
    private GameObject videoFinal;

    private bool _iniciado       = false;
    private bool _fotoDisparada  = false;
    private Material _overlayMat;
    private XRSimpleInteractable _visorInteractable;

    public float RadioDeteccion => radioDeteccion;

    void Awake()
    {
        if (overlayRenderer != null)
            _overlayMat = overlayRenderer.material;

        if (visorFoto != null)
        {
            visorFoto.SetActive(false);
            _visorInteractable = visorFoto.GetComponentInChildren<XRSimpleInteractable>();
        }

        if (objetoSusto != null) objetoSusto.SetActive(false);
        if (zombie3 != null) zombie3.SetActive(false);
        if (videoFinal != null) videoFinal.SetActive(false);
    }

    public void IniciarSecuenciaFoto(GameObject camaraGO)
    {
        if (_iniciado) return;
        _iniciado = true;
        StartCoroutine(SecuenciaCompleta(camaraGO));
    }

    private IEnumerator SecuenciaCompleta(GameObject camaraGO)
    {
        Debug.Log("[FotoJumpscare] Iniciando secuencia de foto.");

        if (visorFoto != null)
        {
            visorFoto.SetActive(true);

            if (_visorInteractable != null)
            {
                _fotoDisparada = false;
                _visorInteractable.hoverEntered.AddListener(OnVisorHovered);
                _visorInteractable.selectEntered.AddListener(OnVisorSelected);
            }
        }

        float elapsed = 0f;
        while (!_fotoDisparada && elapsed < 20f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (_visorInteractable != null)
        {
            _visorInteractable.hoverEntered.RemoveListener(OnVisorHovered);
            _visorInteractable.selectEntered.RemoveListener(OnVisorSelected);
        }

        if (visorFoto != null) visorFoto.SetActive(false);

        yield return StartCoroutine(FlashBlancoCoroutine());
        if (audioShutter != null) audioShutter.Play();
        else Debug.LogWarning("[FotoJumpscare] audioShutter no asignado.");

        yield return new WaitForSeconds(0.2f);

        if (camaraGO != null)
        {
            camaraGO.SetActive(false);
            Debug.Log("[FotoJumpscare] Cámara prop desactivada.");
        }

        yield return new WaitForSeconds(0.3f);

        if (objetoSusto != null)
        {
            objetoSusto.SetActive(true);
            Debug.Log("[FotoJumpscare] Objeto de susto activado.");
        }
        else
        {
            Debug.LogWarning("[FotoJumpscare] objetoSusto no asignado.");
        }

        if (zombie1 != null)
        {
            zombie1.SetActive(false);
        }

        if (audioSusto != null) audioSusto.Play();

        yield return new WaitForSeconds(duracionSusto);

        if (audioParpadeo != null) audioParpadeo.Play();
        yield return StartCoroutine(CoroutineParpadeoFinal());
        if (audioParpadeo != null) audioParpadeo.Stop();

        if (_overlayMat != null)
            _overlayMat.color = new Color(0f, 0f, 0f, 1f);

        if (objetoSusto != null) objetoSusto.SetActive(false);

        cameraCulling?.SetMode(true);
        Debug.Log("[FotoJumpscare] De vuelta al mundo real.");

        yield return StartCoroutine(FadeOverlayCoroutine(0f, 1.5f));

        if (habitacion217Root != null)
        {
            habitacion217Root.SetActive(false);
            Debug.Log("[FotoJumpscare] Habitación 217 desactivada.");
        }

        if (_overlayMat != null)
            _overlayMat.color = new Color(0f, 0f, 0f, 0f);

        if (videoFinal != null)
        {
            videoFinal.SetActive(true);
            Debug.Log("[FotoJumpscare] Vídeo final activado.");
        }

        Debug.Log("[FotoJumpscare] Secuencia final completada.");
    }

    private void OnVisorHovered(HoverEnterEventArgs _)   => _fotoDisparada = true;
    private void OnVisorSelected(SelectEnterEventArgs _)  => _fotoDisparada = true;

    private IEnumerator FlashBlancoCoroutine()
    {
        if (_overlayMat == null) yield break;
        Color colorOriginal = _overlayMat.color;
        _overlayMat.color = new Color(1f, 1f, 1f, 1f);

        if (dmxController != null)
            dmxController.FlashBlanco();

        float t = 0f;
        while (t < duracionFlash)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / duracionFlash);
            _overlayMat.color = new Color(1f, 1f, 1f, alpha);

            if (dmxController != null)
                dmxController.SetBrilloBlanco((byte)(alpha * 255f));

            yield return null;
        }

        _overlayMat.color = colorOriginal;

        if (dmxController != null)
            dmxController.Apagar();
    }

    private IEnumerator CoroutineParpadeoFinal()
    {
        if (_overlayMat == null) yield break;

        _overlayMat.color = new Color(0f, 0f, 0f, 0f);

        float tiempoAcumulado = 0f;
        bool flashActivo = false;

        while (tiempoAcumulado < duracionParpadeoFinal)
        {
            float progreso = Mathf.Clamp01(tiempoAcumulado / duracionParpadeoFinal);
            float t = progreso * progreso;
            float intervalo = Mathf.Lerp(intervaloInicialParpadeo, intervaloFinalParpadeo, t);

            float alphaMax = Mathf.Lerp(0.35f, alphaParpadeo, progreso);
            float alphaMin = Mathf.Lerp(0f, alphaParpadeo * 0.25f, progreso);

            flashActivo = !flashActivo;
            float alpha = flashActivo ? alphaMax : alphaMin;
            _overlayMat.color = new Color(0f, 0f, 0f, alpha);

            if (objetoSusto != null) objetoSusto.SetActive( flashActivo);
            if (zombie3 != null) zombie3.SetActive(!flashActivo);

            if (dmxController != null)
            {
                byte brillo = (byte)(Mathf.Clamp01(1f - alpha) * 255f);
                dmxController.SetBrilloBlanco(brillo);
            }

            float espera = intervalo * (flashActivo ? 0.35f : 0.65f);
            yield return new WaitForSeconds(espera);
            tiempoAcumulado += intervalo;
        }

        if (dmxController != null)
            dmxController.Apagar();

        if (zombie3 != null) zombie3.SetActive(false);
        if (objetoSusto != null) objetoSusto.SetActive(false);

        _overlayMat.color = new Color(0f, 0f, 0f, 0f);
    }

    private IEnumerator FadeOverlayCoroutine(float alphaObjetivo, float duracion)
    {
        if (_overlayMat == null) yield break;

        float alphaInicial = _overlayMat.color.a;
        Color c = _overlayMat.color;
        float tiempo = 0f;

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float alpha = Mathf.Lerp(alphaInicial, alphaObjetivo, tiempo / duracion);
            _overlayMat.color = new Color(c.r, c.g, c.b, alpha);

            if (dmxController != null && alphaObjetivo < alphaInicial)
            {
                byte brillo = (byte)(Mathf.Clamp01(1f - alpha) * 255f);
                dmxController.SetBrilloBlanco(brillo);
            }

            yield return null;
        }

        _overlayMat.color = new Color(c.r, c.g, c.b, alphaObjetivo);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawSphere(transform.position, radioDeteccion);
    }

#if UNITY_EDITOR
    [ContextMenu("Simular foto")]
    private void SimularFoto()
    {
        if (!Application.isPlaying) return;
        IniciarSecuenciaFoto(null);
    }
#endif
}
