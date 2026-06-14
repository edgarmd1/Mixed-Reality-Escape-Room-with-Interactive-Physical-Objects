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
    [SerializeField, Tooltip("Sonido del primer jumpscare (habitación 217)")]
    private AudioSource audioJumpscare;

    [Header("Jumpscare 1 (habitación 217)")]
    [SerializeField, Tooltip("GameObject con la imagen del primer jumpscare")]
    private GameObject imagenJumpscare;
    [SerializeField, Tooltip("Duración del vuelo del jumpscare hacia el jugador")]
    private float duracionJumpscareMov = 1.2f;
    [SerializeField, Tooltip("Distancia detrás de la cámara hasta donde llega el jumpscare")]
    private float distanciaDetrasJumpscare = 0.3f;

    [Header("Final – Volver al MR")]
    [SerializeField, Tooltip("GameObject de la habitación 217")]
    private GameObject habitacion217Root;
    [SerializeField, Tooltip("CameraCullingMaskController para volver al modo MR")]
    private CameraCullingMaskController cameraCulling;
    [SerializeField, Tooltip("Segundos de pausa tras el primer jumpscare antes de volver al MR")]
    private float pausaAntesMR = 1.5f;

    [Header("Parpadeo de luces final (en MR)")]
    [SerializeField, Tooltip("Duración total del parpadeo de luces en segundos")]
    private float duracionParpadeoFinal = 4f;
    [SerializeField, Tooltip("Intervalo entre flashes al inicio (lento)")]
    private float intervaloInicialParpadeo = 0.55f;
    [SerializeField, Tooltip("Intervalo entre flashes al final (rápido)")]
    private float intervaloFinalParpadeo = 0.04f;
    [SerializeField, Tooltip("Alpha máximo del overlay durante el parpadeo")]
    [Range(0f, 1f)] private float alphaParpadeo = 0.88f;
    [SerializeField, Tooltip("Audio que suena durante el parpadeo de luces final")]
    private AudioSource audioParpadeo;

    [Header("Jumpscare 2 (mundo real – final)")]
    [SerializeField, Tooltip("Imagen del segundo jumpscare (puede ser diferente al primero)")]
    private GameObject imagenJumpscare2;
    [SerializeField, Tooltip("Sonido del segundo jumpscare")]
    private AudioSource audioJumpscare2;
    [SerializeField, Tooltip("Duración del vuelo del segundo jumpscare")]
    private float duracionJumpscare2Mov = 1.0f;

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

        if (imagenJumpscare  != null) imagenJumpscare.SetActive(false);
        if (imagenJumpscare2 != null) imagenJumpscare2.SetActive(false);
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

        // ── 1. Mostrar visor / botón ───────────────────────────────
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

        // ── 2. Esperar input del jugador (timeout 20 s) ────────────
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

        // ── 3. Flash blanco + shutter ──────────────────────────────
        yield return StartCoroutine(FlashBlancoCoroutine());
        if (audioShutter != null) audioShutter.Play();
        else Debug.LogWarning("[FotoJumpscare] audioShutter no asignado.");

        yield return new WaitForSeconds(0.2f);

        // ── 4. Desactivar cámara prop ──────────────────────────────
        if (camaraGO != null)
        {
            camaraGO.SetActive(false);
            Debug.Log("[FotoJumpscare] Cámara prop desactivada.");
        }

        yield return new WaitForSeconds(0.4f);

        // ── 5. Primer jumpscare (dentro de la hab. 217) ────────────
        yield return StartCoroutine(JumpscareCoroutine(imagenJumpscare, audioJumpscare, duracionJumpscareMov));

        // ── 6. Pausa → desactivar hab. 217 → volver al MR ─────────
        yield return new WaitForSeconds(pausaAntesMR);

        if (habitacion217Root != null)
        {
            habitacion217Root.SetActive(false);
            Debug.Log("[FotoJumpscare] Habitación 217 desactivada.");
        }

        cameraCulling?.SetMode(true);
        Debug.Log("[FotoJumpscare] De vuelta al mundo real.");

        // ── 7. Pequeña pausa para que el jugador se oriente ────────
        yield return new WaitForSeconds(1.5f);

        // ── 8. Parpadeo de luces en el mundo real ──────────────────
        if (audioParpadeo != null) audioParpadeo.Play();
        yield return StartCoroutine(CoroutineParpadeoFinal());
        if (audioParpadeo != null) audioParpadeo.Stop();

        // ── 9. Segundo jumpscare (en el mundo real) ────────────────
        yield return StartCoroutine(JumpscareCoroutine(imagenJumpscare2, audioJumpscare2, duracionJumpscare2Mov));

        // ── 10. Fin ────────────────────────────────────────────────
        if (_overlayMat != null)
            _overlayMat.color = new Color(0f, 0f, 0f, 0f);

        Debug.Log("[FotoJumpscare] Secuencia final completada.");
    }

    private void OnVisorHovered(HoverEnterEventArgs _)   => _fotoDisparada = true;
    private void OnVisorSelected(SelectEnterEventArgs _)  => _fotoDisparada = true;

    // ── Flash blanco de cámara ─────────────────────────────────────
    private IEnumerator FlashBlancoCoroutine()
    {
        if (_overlayMat == null) yield break;
        Color colorOriginal = _overlayMat.color;
        _overlayMat.color = new Color(1f, 1f, 1f, 1f);

        float t = 0f;
        while (t < duracionFlash)
        {
            t += Time.deltaTime;
            _overlayMat.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, t / duracionFlash));
            yield return null;
        }

        _overlayMat.color = colorOriginal;
    }

    // ── Parpadeo de luces (igual que IntroSequenceManager) ─────────
    private IEnumerator CoroutineParpadeoFinal()
    {
        if (_overlayMat == null) yield break;

        // Poner color negro (parpadeo de oscuridad, como el intro)
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

            float espera = intervalo * (flashActivo ? 0.35f : 0.65f);
            yield return new WaitForSeconds(espera);
            tiempoAcumulado += intervalo;
        }

        // Apagar overlay al terminar
        _overlayMat.color = new Color(0f, 0f, 0f, 0f);
    }

    // ── Jumpscare genérico (reutilizable para ambos) ───────────────
    private IEnumerator JumpscareCoroutine(GameObject imagen, AudioSource audio, float duracion)
    {
        if (imagen == null) yield break;

        Camera cam = Camera.main;
        if (cam == null) yield break;

        if (audio != null) audio.Play();

        Vector3 posInicio = imagen.transform.position;
        Vector3 escInicio = imagen.transform.localScale;
        Vector3 escFinal  = escInicio * 8f;

        imagen.SetActive(true);

        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duracion);
            float eased    = progress * progress;

            Vector3 posDestino = cam.transform.position - cam.transform.forward * distanciaDetrasJumpscare;
            imagen.transform.position   = Vector3.Lerp(posInicio, posDestino, eased);
            imagen.transform.localScale = Vector3.Lerp(escInicio, escFinal, eased);

            yield return null;
        }

        imagen.SetActive(false);
        Debug.Log($"[FotoJumpscare] Jumpscare '{imagen.name}' completado.");
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
