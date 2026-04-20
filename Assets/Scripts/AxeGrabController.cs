using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class AxeGrabController : MonoBehaviour
{
    [SerializeField] private Transform puntoImpacto;

    [SerializeField] private Transform ejeHoja;

    [SerializeField] private float umbralVelocidad = 1.2f;
    [SerializeField] private float umbralAnguloHoja = 65f;

    [SerializeField] private float radioImpacto = 0.18f;

    [SerializeField] private float cooldownEntreGolpes = 0.55f;

    [SerializeField] private LayerMask layerTableros;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;
    private bool  _agarrada          = false;
    private float _tiempoUltimoGolpe = -999f;

    private Vector3 _posAnterior;
    private float   _velocidadActual;

    void Awake()
    {
        _grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
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
        _agarrada    = true;
        _posAnterior = transform.position;
        Debug.Log("[Hacha] Agarrada por el jugador.");
    }

    private void OnSoltada(SelectExitEventArgs args)
    {
        _agarrada = false;
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
                break; // Un impacto por swing
            }
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
