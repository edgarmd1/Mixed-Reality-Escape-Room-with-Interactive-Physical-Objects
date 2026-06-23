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

    [Header("Final")]
    [SerializeField, Tooltip("FotoJumpscareManager")]
    private FotoJumpscareManager fotoJumpscareManager;

    [SerializeField, Tooltip("Transform vacío")]
    private Transform posicionObjetivoBanera;

    [SerializeField, Tooltip("Velocidad del lerp SmoothStep hacia la bañera")]
    private float velocidadSnap = 5f;

    private bool _camaraCogida = false;
    private bool _siendoSostenida = false;
    private bool _secuenciaIniciada = false;
    private bool _snapping = false;

    private IXRSelectInteractor _interactorActual;

    private XRGrabInteractable _grab;
    private Rigidbody _rb;
    private CamaraAutoGrabHelper _autoGrab;

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _rb   = GetComponent<Rigidbody>();

        ConfigurarGrab();

        _grab.enabled = false;
        _autoGrab = GetComponent<CamaraAutoGrabHelper>();
        if (_autoGrab != null) _autoGrab.enabled = false;
        Debug.Log("[CamaraInteractable] XRGrabInteractable desactivado");
    }

    void OnEnable()
    {
        _grab.selectEntered.AddListener(OnCogida);
        _grab.selectExited.AddListener(OnSoltada);
    }

    void OnDisable()
    {
        _grab.selectEntered.RemoveListener(OnCogida);
        _grab.selectExited.RemoveListener(OnSoltada);
    }

    private void ConfigurarGrab()
    {
        _grab.movementType = XRBaseInteractable.MovementType.Kinematic;
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

    private void OnCogida(SelectEnterEventArgs args)
    {
        _interactorActual = args.interactorObject;
        LogicaCogida();
    }

    private void OnSoltada(SelectExitEventArgs args)
    {
        _interactorActual = null;
        LogicaSoltada();
    }
    public void NotificarCogidaPorMano()
    {
        _interactorActual = null;
        LogicaCogida();
    }

    public void NotificarSoltadaPorMano()
    {
        _interactorActual = null;
        LogicaSoltada();
    }

    private void LogicaCogida()
    {
        _siendoSostenida = true;

        if (audioCogida != null)
            audioCogida.Play();

        if (_rb != null)
            _rb.useGravity = false;

        if (_camaraCogida) return;
        _camaraCogida = true;

        Debug.Log("[CamaraInteractable] Cámara cogida por primera vez.");

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

    private void LogicaSoltada()
    {
        _siendoSostenida  = false;
        _interactorActual = null;

        if (_secuenciaIniciada) return;

        Debug.Log("[CamaraInteractable] Soltada.");

        if (fotoJumpscareManager == null)
        {
            Debug.LogWarning("[CamaraInteractable] fotoJumpscareManager es NULL.");
            return;
        }

        float dist  = Vector3.Distance(transform.position, fotoJumpscareManager.transform.position);
        float radio = fotoJumpscareManager.RadioDeteccion;
        Debug.Log($"[CamaraInteractable] Dist cámara→bañera: {dist:F2} m | ¿Dentro? {dist <= radio}");

        if (dist <= radio)
        {
            Debug.Log("[CamaraInteractable] Dentro del radio – iniciando snap.");
            IniciarSnapYSecuencia();
        }
        else
        {
            Debug.Log("[CamaraInteractable] Fuera del radio – suelta la cámara más cerca de la bañera.");
        }
    }

    void Update()
    {
        if (!_siendoSostenida || _snapping || _secuenciaIniciada) return;
        if (fotoJumpscareManager == null) return;

        float dist  = Vector3.Distance(transform.position, fotoJumpscareManager.transform.position);
        float radio = fotoJumpscareManager.RadioDeteccion;

        if (dist <= radio)
        {
            Debug.Log($"[CamaraInteractable] Zona bañera detectada mientras se sostiene ({dist:F2} m) → auto-suelta.");

            _autoGrab?.ForzarSuelta();

            if (_interactorActual != null && _grab.interactionManager != null)
                _grab.interactionManager.SelectExit(_interactorActual, _grab);

            _siendoSostenida = false;
            IniciarSnapYSecuencia();
        }
    }

    public void HabilitarGrab()
    {
        if (_autoGrab != null)
        {
            if (!_autoGrab.enabled) _autoGrab.enabled = true;
            Debug.Log("[CamaraInteractable] CamaraAutoGrabHelper activado ");
        }
        else
        {
            if (!_grab.enabled)
            {
                _grab.enabled = true;
                Debug.Log("[CamaraInteractable] XRGrabInteractable ACTIVADO.");
            }
        }
    }

    private void IniciarSnapYSecuencia()
    {
        if (_secuenciaIniciada) return;
        _secuenciaIniciada = true;
        _grab.enabled = false;
        if (_autoGrab != null) _autoGrab.enabled = false;

        if (posicionObjetivoBanera != null)
            StartCoroutine(SnapHaciaBanera());
        else
            fotoJumpscareManager.IniciarSecuenciaFoto(gameObject);
    }

    private IEnumerator SnapHaciaBanera()
    {
        _snapping = true;

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

        Debug.Log("[CamaraInteractable] Snap completado → IniciarSecuenciaFoto.");
        fotoJumpscareManager.IniciarSecuenciaFoto(gameObject);
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