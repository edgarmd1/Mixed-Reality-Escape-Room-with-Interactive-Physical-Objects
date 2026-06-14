using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class FotoJumpscareManager : MonoBehaviour
{
    [Header("Detección de proximidad (bañera)")]
    [SerializeField, Tooltip("Radio alrededor de la bañera para detectar la cámara suelta")]
    private float radioDeteccion = 1.5f;

    [Header("Visor de foto")]
    [SerializeField, Tooltip("GameObject del visor (world-space). Necesita XRSimpleInteractable. Se posiciona automáticamente frente al jugador.")]
    private GameObject visorFoto;
    [SerializeField, Tooltip("Distancia delante de la cámara del jugador donde aparece el visor")]
    private float distanciaVisor = 0.55f;

    [Header("Flash de cámara")]
    [SerializeField, Tooltip("Renderer del overlay existente en la escena")]
    private Renderer overlayRenderer;
    [SerializeField, Tooltip("Duración del flash blanco en segundos")]
    private float duracionFlash = 0.3f;

    [Header("Audio")]
    [SerializeField, Tooltip("Sonido de disparo de cámara (shutter)")]
    private AudioSource audioShutter;
    [SerializeField, Tooltip("Sonido del jumpscare")]
    private AudioSource audioJumpscare;

    [Header("Jumpscare")]
    [SerializeField, Tooltip("GameObject con la imagen del jumpscare")]
    private GameObject imagenJumpscare;
    [SerializeField, Tooltip("Duración del vuelo del jumpscare hacia el jugador")]
    private float duracionJumpscareMov = 1.2f;
    [SerializeField, Tooltip("Distancia detrás de la cámara hasta donde llega el jumpscare")]
    private float distanciaDetrasJumpscare = 0.3f;

    [Header("Final")]
    [SerializeField, Tooltip("GameObject de la habitación 217")]
    private GameObject habitacion217Root;
    [SerializeField, Tooltip("CameraCullingMaskController para volver al modo MR")]
    private CameraCullingMaskController cameraCulling;
    [SerializeField, Tooltip("Segundos de pausa tras el jumpscare antes de volver al MR")]
    private float pausaFinalTrasJumpscare = 1.5f;

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

        if (imagenJumpscare != null)
            imagenJumpscare.SetActive(false);
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

        if (visorFoto != null)
            visorFoto.SetActive(false);

        yield return StartCoroutine(FlashBlancoCoroutine());
        if (audioShutter != null)
            audioShutter.Play();
        else
            Debug.LogWarning("[FotoJumpscare] audioShutter no asignado.");

        yield return new WaitForSeconds(0.2f);

        if (camaraGO != null)
        {
            camaraGO.SetActive(false);
            Debug.Log("[FotoJumpscare] Cámara prop desactivada.");
        }

        yield return new WaitForSeconds(0.4f);

        yield return StartCoroutine(JumpscareCoroutine());

        yield return new WaitForSeconds(pausaFinalTrasJumpscare);

        if (habitacion217Root != null)
        {
            habitacion217Root.SetActive(false);
            Debug.Log("[FotoJumpscare] Habitación 217 desactivada.");
        }

        cameraCulling?.SetMode(true);
        Debug.Log("[FotoJumpscare] Secuencia completada. Volviendo al mundo real.");
    }

    private void OnVisorHovered(HoverEnterEventArgs _)  => _fotoDisparada = true;
    private void OnVisorSelected(SelectEnterEventArgs _) => _fotoDisparada = true;

    private IEnumerator FlashBlancoCoroutine()
    {
        if (_overlayMat == null) yield break;
        Color colorOriginal = _overlayMat.color;
        _overlayMat.color = new Color(1f, 1f, 1f, 1f);

        float t = 0f;
        while (t < duracionFlash)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / duracionFlash);
            _overlayMat.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        _overlayMat.color = colorOriginal;
    }

    private IEnumerator JumpscareCoroutine()
    {
        if (imagenJumpscare == null) yield break;

        Camera cam = Camera.main;
        if (cam == null) yield break;

        if (audioJumpscare != null)
            audioJumpscare.Play();

        Vector3 posInicio  = imagenJumpscare.transform.position;
        Vector3 escInicio  = imagenJumpscare.transform.localScale;
        Vector3 escFinal   = escInicio * 8f;

        imagenJumpscare.SetActive(true);

        float t = 0f;
        while (t < duracionJumpscareMov)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duracionJumpscareMov);
            float eased    = progress * progress;

            Vector3 posDestino = cam.transform.position - cam.transform.forward * distanciaDetrasJumpscare;
            imagenJumpscare.transform.position   = Vector3.Lerp(posInicio, posDestino, eased);
            imagenJumpscare.transform.localScale = Vector3.Lerp(escInicio, escFinal, eased);

            yield return null;
        }

        imagenJumpscare.SetActive(false);
        Debug.Log("[FotoJumpscare] Jumpscare completado.");
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawSphere(transform.position, radioDeteccion);
    }

#if UNITY_EDITOR
    [ContextMenu("Simular foto (Editor)")]
    private void SimularFoto()
    {
        if (!Application.isPlaying) return;
        IniciarSecuenciaFoto(null);
    }
#endif
}
