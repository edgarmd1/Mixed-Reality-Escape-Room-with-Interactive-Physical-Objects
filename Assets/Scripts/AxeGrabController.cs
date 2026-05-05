using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class AxeGrabController : MonoBehaviour
{
    [Header("── Detección de impacto ──")]
    [SerializeField] private Transform puntoImpacto;

    [SerializeField] private Transform ejeHoja;

    [SerializeField] private float umbralVelocidad = 1.2f;
    [SerializeField] private float umbralAnguloHoja = 65f;

    [SerializeField] private float radioImpacto = 0.18f;

    [SerializeField] private float cooldownEntreGolpes = 0.55f;

    [SerializeField] private LayerMask layerTableros;

    [Header("── Configuración del agarre ──")]
    [SerializeField, Tooltip("Punto de agarre personalizado (crear un hijo vacío en el mango).\n" +
        "Si está asignado, se usará como attachTransform del XRGrabInteractable.")]
    private Transform puntoAgarre;

    [SerializeField, Tooltip("Offset de rotación del agarre (grados Euler).\n" +
        "Ajusta cómo se orienta el hacha respecto al mando.\n" +
        "(-90, 0, 0) = mango vertical con mando recto.")]
    private Vector3 rotacionAgarre = new Vector3(0f, 0f, -90f);

    [SerializeField, Tooltip("Usar Velocity tracking en vez de Instantaneous.\n" +
        "Velocity da un movimiento más suave y natural al balancear.")]
    private bool usarVelocityTracking = true;

    [SerializeField, Tooltip("Permitir que el hacha siga la rotación del mando.")]
    private bool seguirRotacionMando = true;

    [Header("── Haptic feedback (vibración) ──")]
    [SerializeField, Tooltip("Intensidad de vibración al golpear madera (0 = nada, 1 = máximo)")]
    [Range(0f, 1f)]
    private float hapticIntensidad = 0.6f;

    [SerializeField, Tooltip("Duración de la vibración en segundos")]
    [Range(0.05f, 1f)]
    private float hapticDuracion = 0.25f;

    private XRGrabInteractable _grab;
    private bool  _agarrada          = false;
    private float _tiempoUltimoGolpe = -999f;

    // Referencia al interactor activo (mando que agarra el hacha)
    private IXRSelectInteractor _interactorActivo;

    private Vector3 _posAnterior;
    private float   _velocidadActual;

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();

        // ── Configurar attach point para un agarre natural ──────────────
        // Si no hay punto de agarre asignado, crear uno automáticamente
        if (puntoAgarre == null)
        {
            GameObject attachGO = new GameObject("_AutoAttachPoint");
            attachGO.transform.SetParent(transform);
            attachGO.transform.localPosition = Vector3.zero;
            attachGO.transform.localRotation = Quaternion.Euler(rotacionAgarre);
            puntoAgarre = attachGO.transform;
            Debug.Log($"[Hacha] Creado attach point automático con rotación {rotacionAgarre}");
        }
        else
        {
            // Aplicar el offset de rotación al punto de agarre existente
            puntoAgarre.localRotation = Quaternion.Euler(rotacionAgarre);
            Debug.Log("[Hacha] Usando punto de agarre personalizado con rotación aplicada.");
        }
        _grab.attachTransform = puntoAgarre;

        // ── Configurar movement type ────────────────────────────────────
        if (usarVelocityTracking)
        {
            _grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        }

        // ── Rotación ────────────────────────────────────────────────────
        _grab.trackRotation = seguirRotacionMando;

        // ── Throw: ajustar para que no salga volando al soltar ──────────
        _grab.throwOnDetach = false;

        // ── Registrar listeners ─────────────────────────────────────────
        _grab.selectEntered.AddListener(OnAgarrada);
        _grab.selectExited.AddListener(OnSoltada);
    }

    void Start()
    {
        _posAnterior = transform.position;
    }

    void Update()
    {
        _velocidadActual = (transform.position - _posAnterior).magnitude / Time.deltaTime;
        _posAnterior = transform.position;

        if (!_agarrada)                                           return;
        if (Time.time - _tiempoUltimoGolpe < cooldownEntreGolpes) return;
        if (_velocidadActual < umbralVelocidad)                   return;
        if (!InclinacionCorrecta())                               return;

        ComprobarImpacto();
    }

    void OnDestroy()
    {
        if (_grab != null)
        {
            _grab.selectEntered.RemoveListener(OnAgarrada);
            _grab.selectExited.RemoveListener(OnSoltada);
        }
    }

    private void OnAgarrada(SelectEnterEventArgs args)
    {
        _agarrada        = true;
        _posAnterior     = transform.position;
        _interactorActivo = args.interactorObject;
        Debug.Log("[Hacha] Agarrada por el jugador.");
    }

    private void OnSoltada(SelectExitEventArgs args)
    {
        _agarrada         = false;
        _interactorActivo = null;
        Debug.Log("[Hacha] Soltada.");
    }

   
    private bool InclinacionCorrecta()
    {
        if (ejeHoja == null)
        {
            float angHandleVertical = Vector3.Angle(transform.up, Vector3.up);
            return angHandleVertical > 25f;
        }
        float anguloBajoHoja = Vector3.Angle(ejeHoja.forward, Vector3.down);
        return anguloBajoHoja < umbralAnguloHoja;
    }

    private void ComprobarImpacto()
    {
        if (puntoImpacto == null)
        {
            Debug.LogWarning("[Hacha] 'PuntoImpacto' no asignado en el Inspector.");
            return;
        }

        Collider[] colisiones = Physics.OverlapSphere(puntoImpacto.position, radioImpacto, layerTableros);

        foreach (Collider col in colisiones)
        {
            if (col.TryGetComponent<TableroDestructible>(out var tablero) && !tablero.Roto)
            {
                Debug.Log($"[Hacha] ¡Impacto válido en {tablero.gameObject.name}! " +
                          $"Vel: {_velocidadActual:F2} m/s");
                tablero.RecibirImpacto();
                _tiempoUltimoGolpe = Time.time;

                // ── Vibrar el mando al impactar ─────────────────────────
                EnviarHapticFeedback();

                break; // Un impacto por swing
            }
        }
    }

    /// <summary>
    /// Envía una vibración (haptic impulse) al mando que tiene agarrada el hacha.
    /// </summary>
    private void EnviarHapticFeedback()
    {
        if (_interactorActivo == null) return;
        if (hapticIntensidad <= 0f) return;

        // En XRI 3.x el interactor implementa IXRHapticImpulseProvider
        // a través de su XRBaseController, pero la forma más directa
        // es buscar el XRBaseInputInteractor que expone SendHapticImpulse.
        if (_interactorActivo is XRBaseInputInteractor inputInteractor)
        {
            inputInteractor.SendHapticImpulse(hapticIntensidad, hapticDuracion);
            Debug.Log($"[Hacha] Haptic enviado – intensidad: {hapticIntensidad}, duración: {hapticDuracion}s");
        }
        else
        {
            Debug.LogWarning("[Hacha] El interactor no soporta haptics.");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (puntoImpacto != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(puntoImpacto.position, radioImpacto);
        }

        if (ejeHoja != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(ejeHoja.position, ejeHoja.forward * 0.3f);
        }
    }
}
