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
    [SerializeField, Tooltip("Puerta trasera que se cierra")]
    private GameObject puertaTraseraRoot;

    [SerializeField, Tooltip("GameObject de la habitación 217")]
    private GameObject habitacion217Root;

    [SerializeField, Tooltip("GameObjects que se desactivan al coger la cámara")]
    private GameObject[] objetosADesactivar;

    [SerializeField, Tooltip("Sonido de portazo al cerrarse la puerta")]
    private AudioSource audioPortazo;

    [SerializeField, Tooltip("Camara en Mano")]
    private GameObject camaraMano;

    [SerializeField, Tooltip("Transform de la mano derecha (al que se emparentará la cámara al cogerla).")] //todo: implementar mano izquierda tb
    private Transform transformManoDerecha;

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
    private bool _siguiendoMano = false;

    private IXRSelectInteractor _interactorActual;

    private XRGrabInteractable _grab;
    private Rigidbody _rb;
    private CamaraAutoGrabHelper _autoGrab;
    private bool _grabHabilitado = false;
    public bool GrabHabilitado => _grabHabilitado;
    
    private System.Collections.Generic.List<Renderer> _renderersOcultados = new System.Collections.Generic.List<Renderer>();

    void Awake()
    {
        InicializarComponentes();

        if (!_grabHabilitado)
        {
            _grab.enabled = false;
            if (_autoGrab != null) _autoGrab.enabled = false;
        }
    }

    private void InicializarComponentes()
    {
        if (_grab == null)
        {
            _grab = GetComponent<XRGrabInteractable>();
            _rb   = GetComponent<Rigidbody>();
            _autoGrab = GetComponent<CamaraAutoGrabHelper>();
            ConfigurarGrab();
        }
    }

    void OnEnable()
    {
        _grab.selectEntered.AddListener(OnCogida);
        _grab.selectExited.AddListener(OnSoltada);
        _grab.hoverEntered.AddListener(OnTocada);
    }

    void OnDisable()
    {
        _grab.selectEntered.RemoveListener(OnCogida);
        _grab.selectExited.RemoveListener(OnSoltada);
        _grab.hoverEntered.RemoveListener(OnTocada);
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

    private void OnTocada(HoverEnterEventArgs args)
    {
        if (!_grabHabilitado) return;
        if (!_camaraCogida)
        {
            LogicaCogida();
        }
    }

    private void OnCogida(SelectEnterEventArgs args)
    {
        if (!_camaraCogida)
        {
            _interactorActual = args.interactorObject;
            LogicaCogida();
        }
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

        if (!_camaraCogida)
        {
            _camaraCogida = true;

            if (puertaTraseraRoot != null)
            {
                puertaTraseraRoot.SetActive(true);
            }

            if (camaraMano != null)
            {
                camaraMano.transform.position = transform.position;
                camaraMano.transform.rotation = transform.rotation;

                if (transformManoDerecha != null)
                {
                    camaraMano.transform.SetParent(transformManoDerecha, false);
                    camaraMano.transform.localPosition = Vector3.zero;
                    camaraMano.transform.localRotation = Quaternion.Euler(0f, -28.34f, 54.30f);
                }

                camaraMano.SetActive(true);
            }

            if (habitacion217Root != null)
            {
                habitacion217Root.SetActive(true);
            }

            if (objetosADesactivar != null)
            {
                foreach (GameObject obj in objetosADesactivar)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                    }
                }
            }

            if (audioPortazo != null)
                audioPortazo.Play();
        }

        StartCoroutine(ActivarCamaraManoYDesvincular());
    }

    private IEnumerator ActivarCamaraManoYDesvincular()
    {
        yield return new WaitForEndOfFrame();

        if (_interactorActual != null && _grab != null && _grab.interactionManager != null)
        {
            _grab.interactionManager.SelectExit(_interactorActual, _grab);
        }
        if (_grab != null) _grab.enabled = false;
        if (_autoGrab != null) _autoGrab.enabled = false;

        OcultarVisuales();

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var c in colliders) c.enabled = false;

        _siendoSostenida = false;
        _siguiendoMano = true;
    }

    private void OcultarVisuales()
    {
        _renderersOcultados.Clear();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r.enabled)
            {
                _renderersOcultados.Add(r);
                r.enabled = false;
            }
        }
    }

    private void MostrarVisuales()
    {
        foreach (var r in _renderersOcultados)
        {
            if (r != null) r.enabled = true;
        }
        _renderersOcultados.Clear();
    }

    private void LogicaSoltada()
    {
        _siendoSostenida  = false;
        _interactorActual = null;
    }

    void Update()
    {
        if (_secuenciaIniciada || _snapping) return;

        if (_siguiendoMano && camaraMano != null)
        {
            if (fotoJumpscareManager != null)
            {
                float dist  = Vector3.Distance(camaraMano.transform.position, fotoJumpscareManager.transform.position);
                float radio = fotoJumpscareManager.RadioDeteccion;

                if (dist <= radio)
                {
                    _siguiendoMano = false;
                    IniciarSnapYSecuencia();
                }
            }
        }
    }

    public void PrehabilitarGrab()
    {
        _grabHabilitado = true;
    }

    public void HabilitarGrab()
    {
        _grabHabilitado = true;
        InicializarComponentes();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (_autoGrab != null)
        {
            if (!_autoGrab.enabled) _autoGrab.enabled = true;
        }
        else if (_grab != null)
        {
            if (!_grab.enabled)
            {
                _grab.enabled = true;
            }
        }
    }

    private void IniciarSnapYSecuencia()
    {
        if (_secuenciaIniciada) return;
        _secuenciaIniciada = true;
        
        if (_grab != null) _grab.enabled = false;
        if (_autoGrab != null) _autoGrab.enabled = false;

        if (camaraMano != null)
        {
            transform.position = camaraMano.transform.position;
            transform.rotation = camaraMano.transform.rotation;

            camaraMano.SetActive(false);
        }

        MostrarVisuales();

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