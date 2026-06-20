using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRGrabInteractable))]
public class CamaraInteractable : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField, Tooltip("Sonido al coger la cámara")]
    private AudioSource audioCogida;

    [Header("Al coger la cámara")]
    [SerializeField, Tooltip("Puerta trasera que se cierra al coger la cámara")]
    private GameObject puertaTraseraRoot;

    [SerializeField, Tooltip("GameObject de la habitación 217")]
    private GameObject habitacion217Root;

    [SerializeField, Tooltip("GameObjects que se desactivan al coger la cámara")]
    private GameObject[] objetosADesactivar;

    [SerializeField, Tooltip("Sonido de portazo al cerrarse la puerta")]
    private AudioSource audioPortazo;

    [Header("Auto-suelta en bañera")]
    [SerializeField, Tooltip("FotoJumpscareManager")]
    private FotoJumpscareManager fotoJumpscareManager;

    [SerializeField, Tooltip("Transform vacío")]
    private Transform posicionObjetivoBanera;

    [SerializeField, Tooltip("Velocidad del lerp")]
    private float velocidadSnap = 5f;

    private bool _camaraCogida = false;
    private bool _pegadaAMano = false;
    private bool _snapping = false;
    private bool _secuenciaIniciada = false;

    private XRGrabInteractable _grab;
    private Rigidbody _rb;
    private IXRSelectInteractor _interactorActual;

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _rb   = GetComponent<Rigidbody>();

        ConfigurarGrab();
    }

    void OnEnable()
    {
        _grab.hoverEntered.AddListener(OnHoverEntrada);
        _grab.selectEntered.AddListener(OnCogida);
        _grab.selectExited.AddListener(OnSoltada);
    }

    void OnDisable()
    {
        _grab.hoverEntered.RemoveListener(OnHoverEntrada);
        _grab.selectEntered.RemoveListener(OnCogida);
        _grab.selectExited.RemoveListener(OnSoltada);
    }

    private void ConfigurarGrab()
    {
        _grab.movementType  = XRBaseInteractable.MovementType.Kinematic;
        _grab.trackPosition = true;
        _grab.trackRotation = true;
        _grab.throwOnDetach = false;
        _grab.attachTransform = null;

        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity  = false;
        }
    }

    private void OnHoverEntrada(HoverEnterEventArgs args)
    {
        if (_pegadaAMano || _snapping || _secuenciaIniciada) return;

        if (args.interactorObject is IXRSelectInteractor selectInteractor)
        {
            var manager = _grab.interactionManager;
            if (manager != null)
                manager.SelectEnter(selectInteractor, _grab);
        }
    }

    private void OnCogida(SelectEnterEventArgs args)
    {
        _pegadaAMano     = true;
        _interactorActual = args.interactorObject;

        if (audioCogida != null)
            audioCogida.Play();

        if (_rb != null)
            _rb.useGravity = false;

        if (!_camaraCogida)
        {
            _camaraCogida = true;
            Debug.Log("[CamaraInteractable] Cámara cogida por primera vez.");
            ActivarHabitacion217();
        }
    }

    private void OnSoltada(SelectExitEventArgs args)
    {
        _pegadaAMano      = false;
        _interactorActual = null;
        Debug.Log("[CamaraInteractable] Cámara suelta (por el jugador).");
    }

    void Update()
    {
        if (!_pegadaAMano || _snapping || _secuenciaIniciada) return;
        if (fotoJumpscareManager == null) return;

        float dist  = Vector3.Distance(transform.position, fotoJumpscareManager.transform.position);
        float radio = fotoJumpscareManager.RadioDeteccion;

        if (dist <= radio)
        {
            Debug.Log($"[CamaraInteractable] Dentro del radio bañera ({dist:F2} m ≤ {radio:F2} m) → auto-suelta.");
            AutoSoltarEnBanera();
        }
    }

    private void AutoSoltarEnBanera()
    {
        _secuenciaIniciada = true;
        if (_interactorActual != null && _grab.interactionManager != null)
        {
            _grab.interactionManager.SelectExit(_interactorActual, _grab);
        }

        _grab.enabled = false;

        if (posicionObjetivoBanera != null)
            StartCoroutine(SnapHaciaBanera());
        else
        {
            Debug.LogWarning("[CamaraInteractable] posicionObjetivoBanera no asignado");
            fotoJumpscareManager.IniciarSecuenciaFoto(gameObject);
        }
    }

    private IEnumerator SnapHaciaBanera()
    {
        _snapping = true;
        Debug.Log("[CamaraInteractable] Iniciando snap lerp hacia la bañera.");

        Vector3 posInicial = transform.position;
        Quaternion rotInicial = transform.rotation;
        Vector3 posObjetivo = posicionObjetivoBanera.position;
        Quaternion rotObjetivo = posicionObjetivoBanera.rotation;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * velocidadSnap;
            transform.position = Vector3.Lerp(posInicial, posObjetivo, Mathf.SmoothStep(0f, 1f, t));
            transform.rotation = Quaternion.Slerp(rotInicial, rotObjetivo, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        transform.position = posObjetivo;
        transform.rotation = rotObjetivo;

        _snapping = false;
        Debug.Log("[CamaraInteractable] Snap completado. Iniciando secuencia de foto.");
        fotoJumpscareManager.IniciarSecuenciaFoto(gameObject);
    }

    private void ActivarHabitacion217()
    {
        if (puertaTraseraRoot != null)
        {
            puertaTraseraRoot.SetActive(true);
            Debug.Log("[CamaraInteractable] Puerta trasera cerrada activada.");
        }
        else
        {
            Debug.LogWarning("[CamaraInteractable] puertaTraseraRoot no asignado.");
        }

        if (habitacion217Root != null)
        {
            habitacion217Root.SetActive(true);
            Debug.Log("[CamaraInteractable] Habitación 217 activada.");
        }
        else
        {
            Debug.LogWarning("[CamaraInteractable] habitacion217Root no asignado.");
        }

        if (objetosADesactivar != null)
        {
            foreach (GameObject obj in objetosADesactivar)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    Debug.Log($"[CamaraInteractable] Desactivado: {obj.name}");
                }
            }
        }

        if (audioPortazo != null)
            audioPortazo.Play();
        else
            Debug.LogWarning("[CamaraInteractable] audioPortazo no asignado.");
    }

    void OnDrawGizmosSelected()
    {
        if (posicionObjetivoBanera != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(posicionObjetivoBanera.position, 0.08f);
            Gizmos.DrawLine(transform.position, posicionObjetivoBanera.position);
        }
    }
}
