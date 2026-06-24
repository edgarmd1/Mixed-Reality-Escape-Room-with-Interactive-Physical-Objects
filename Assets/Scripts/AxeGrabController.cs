using UnityEngine;
using UnityEngine.XR;

public class AxeGrabController : MonoBehaviour
{
    [Header("Agarrar")]

    [SerializeField, Tooltip("rotacion del mando respecto al hacha")]
    private Vector3 rotationOffset = new Vector3(90f, 180f, 0f);

    [SerializeField, Tooltip("Posición del punto de agarre en el espacio local del hacha")]
    private Vector3 gripLocalPosition = Vector3.zero;


    [Header("Detección de impacto")]
    [SerializeField] private Transform puntoImpacto;
    [SerializeField] private Transform ejeHoja;
    [SerializeField] private float umbralVelocidad = 1.2f;
    [SerializeField] private float umbralAnguloHoja = 65f;
    [SerializeField] private float radioImpacto = 0.18f;
    [SerializeField] private float cooldownEntreGolpes = 0.55f;
    [SerializeField] private LayerMask layerTableros;

    [Header("Haptic feedback")]
    [SerializeField, Range(0f, 1f)] private float hapticIntensidad = 0.6f;
    [SerializeField, Range(0.05f, 1f)] private float hapticDuracion = 0.25f;

    [Header("Vitrina")]
    [SerializeField, Tooltip("Audio vitrina")]
    private AudioSource audioVitrineBloqueo;
    [SerializeField, Tooltip("Collider vitrina")]
    private Collider colliderVitrina;
    [SerializeField, Tooltip("Cooldown vitrina")]
    private float cooldownAudioVitrina = 4f;
    [SerializeField, Tooltip("Capas")]
    private LayerMask layerVitrina = ~0;

    private const string PREFS_KEY = "Hacha_PosicionReposo";

    private Vector3 _posReposo;
    private Quaternion _rotReposo;
    private bool _tienePosReposo = false;

    private InputDevice _mando;
    private bool _mandoEnMano;
    private float _tiempoUltimoGolpe = -999f;
    private Vector3 _posAnterior;
    private float _velocidadActual;

    public bool EstaEnMano => _mandoEnMano;

    private Unity.XR.CoreUtils.XROrigin _xrOrigin;
    private Rigidbody _rb;
    private Collider _propioCollider;
    private float _tiempoUltimoAudioVitrina = -999f;

    void Awake()
    {
        _xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        _rb = GetComponent<Rigidbody>();
        _propioCollider = GetComponent<Collider>();
        CargarPosicionReposo();
        _posAnterior = transform.position;

        MostrarEjesHacha();
    }

    void OnEnable()
    {
        InputDevices.deviceConnected += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;
        BuscarMandoDerecho();
    }

    void OnDisable()
    {
        InputDevices.deviceConnected -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
    }

    void Update()
    {
        _velocidadActual = (transform.position - _posAnterior).magnitude / Time.deltaTime;
        _posAnterior = transform.position;

        if (!_mandoEnMano) return;
        if (Time.time - _tiempoUltimoGolpe < cooldownEntreGolpes) return;
        if (_velocidadActual < umbralVelocidad) return;
        if (!InclinacionCorrecta()) return;

        ComprobarImpacto();
    }

    void LateUpdate()
    {
        ActualizarEstadoMando();

        if (_mandoEnMano)
            ColocarEnMando();
        else
            AplicarPosicionReposo();
    }

    private void GuardarPosicionReposo()
    {
        _posReposo = transform.position;
        _rotReposo = transform.rotation;
        _tienePosReposo = true;

        if (_mando.isValid && _mando.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 ctrlLocal))
        {
            _controllerRestLocalPos = ctrlLocal;
            _tieneControllerRestPos = true;
            PlayerPrefs.SetFloat(PREFS_KEY + "_CtrlX", ctrlLocal.x);
            PlayerPrefs.SetFloat(PREFS_KEY + "_CtrlY", ctrlLocal.y);
            PlayerPrefs.SetFloat(PREFS_KEY + "_CtrlZ", ctrlLocal.z);
        }

        PlayerPrefs.SetFloat(PREFS_KEY + "_PosX", _posReposo.x);
        PlayerPrefs.SetFloat(PREFS_KEY + "_PosY", _posReposo.y);
        PlayerPrefs.SetFloat(PREFS_KEY + "_PosZ", _posReposo.z);
        PlayerPrefs.SetFloat(PREFS_KEY + "_RotX", _rotReposo.x);
        PlayerPrefs.SetFloat(PREFS_KEY + "_RotY", _rotReposo.y);
        PlayerPrefs.SetFloat(PREFS_KEY + "_RotZ", _rotReposo.z);
        PlayerPrefs.SetFloat(PREFS_KEY + "_RotW", _rotReposo.w);
        PlayerPrefs.SetInt  (PREFS_KEY + "_OK",   1);
        PlayerPrefs.Save();
    }

    private void CargarPosicionReposo()
    {
        if (PlayerPrefs.GetInt(PREFS_KEY + "_OK", 0) == 0)
        {
            return;
        }

        _posReposo = new Vector3(
            PlayerPrefs.GetFloat(PREFS_KEY + "_PosX"),
            PlayerPrefs.GetFloat(PREFS_KEY + "_PosY"),
            PlayerPrefs.GetFloat(PREFS_KEY + "_PosZ")
        );
        _rotReposo = new Quaternion(
            PlayerPrefs.GetFloat(PREFS_KEY + "_RotX"),
            PlayerPrefs.GetFloat(PREFS_KEY + "_RotY"),
            PlayerPrefs.GetFloat(PREFS_KEY + "_RotZ"),
            PlayerPrefs.GetFloat(PREFS_KEY + "_RotW")
        );
        _tienePosReposo = true;

        if (PlayerPrefs.HasKey(PREFS_KEY + "_CtrlX"))
        {
            _controllerRestLocalPos = new Vector3(
                PlayerPrefs.GetFloat(PREFS_KEY + "_CtrlX"),
                PlayerPrefs.GetFloat(PREFS_KEY + "_CtrlY"),
                PlayerPrefs.GetFloat(PREFS_KEY + "_CtrlZ")
            );
            _tieneControllerRestPos = true;
        }

        transform.position = _posReposo;
        transform.rotation = _rotReposo;
    }

    private void AplicarPosicionReposo()
    {
        if (!_tienePosReposo) return;
        transform.position = _posReposo;
        transform.rotation = _rotReposo;
    }

    [ContextMenu("Mostrar ejes del hacha")]
    private void MostrarEjesHacha()
    {
        Debug.Log($"[Hacha] Ejes en mundo → " +
                  $"Derecha: {transform.right:F2} | " +
                  $"Arriba: {transform.up:F2} | " +
                  $"Adelante: {transform.forward:F2}");
    }

    [ContextMenu("Borrar posición de reposo guardada")]
    public void BorrarPosicionReposo()
    {
        PlayerPrefs.DeleteKey(PREFS_KEY + "_PosX");
        PlayerPrefs.DeleteKey(PREFS_KEY + "_PosY");
        PlayerPrefs.DeleteKey(PREFS_KEY + "_PosZ");
        PlayerPrefs.DeleteKey(PREFS_KEY + "_RotX");
        PlayerPrefs.DeleteKey(PREFS_KEY + "_RotY");
        PlayerPrefs.DeleteKey(PREFS_KEY + "_RotZ");
        PlayerPrefs.DeleteKey(PREFS_KEY + "_RotW");
        PlayerPrefs.DeleteKey(PREFS_KEY + "_OK");
        PlayerPrefs.Save();
        _tienePosReposo = false;
        Debug.Log("[Hacha] Posición de reposo borrada.");
    }

    private void OnDeviceConnected(InputDevice device)
    {
        if (EsControllerDerecho(device))
        {
            _mando = device;
        }
    }

    private void OnDeviceDisconnected(InputDevice device)
    {
        if (EsControllerDerecho(device))
        {
            _mando = default;
            _mandoEnMano = false;
        }
    }

    private void BuscarMandoDerecho()
    {
        var lista = new System.Collections.Generic.List<InputDevice>();
        InputDevices.GetDevices(lista);
        foreach (var d in lista)
        {
            if (EsControllerDerecho(d))
            {
                _mando = d;
                return;
            }
        }
    }

    private bool EsControllerDerecho(InputDevice device)
    {
        return (device.characteristics & InputDeviceCharacteristics.Right)      != 0
            && (device.characteristics & InputDeviceCharacteristics.Controller) != 0;
    }

    private void ActualizarEstadoMando()
    {
        if (!_mando.isValid)
        {
            BuscarMandoDerecho();
            if (_mandoEnMano) OnMandoSoltado();
            return;
        }

        bool tracked = _mando.TryGetFeatureValue(CommonUsages.isTracked, out bool t) && t;

        _mando.TryGetFeatureValue(CommonUsages.grip, out float grip);
        bool griping = grip > 0.3f;
        if (_estabaGripando && !griping && _mandoEnMano)
        {
            GuardarPosicionReposo();
        }
        _estabaGripando = griping;

        if (!tracked)
        {
            if (_mandoEnMano) OnMandoSoltado();
            return;
        }

        if (!_mandoEnMano)
        {
            if (ControllerHaMovidoDeReposo())
            {
                _mandoEnMano = true;
            }
        }
    }

    private const float UMBRAL_DISTANCIA_AGARRE = 0.12f;
    private Vector3 _controllerRestLocalPos;
    private bool _tieneControllerRestPos = false;
    private bool _estabaGripando = false;

    private bool ControllerHaMovidoDeReposo()
    {
        if (!_tieneControllerRestPos) return true;

        if (!_mando.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPos))
            return false;

        return Vector3.Distance(localPos, _controllerRestLocalPos) > UMBRAL_DISTANCIA_AGARRE;
    }

    private void OnMandoSoltado()
    {
        if (!_mandoEnMano) return;
        _mandoEnMano = false;
        GuardarPosicionReposo();
    }

    private void ColocarEnMando()
    {
        if (!_mando.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPos)) return;
        if (!_mando.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion localRot)) return;

        Vector3 mandoPos;
        Quaternion mandoRot;

        if (_xrOrigin != null)
        {
            mandoPos = _xrOrigin.transform.TransformPoint(localPos);
            mandoRot = _xrOrigin.transform.rotation * localRot;
        }
        else
        {
            mandoPos = localPos;
            mandoRot = localRot;
        }

        Quaternion targetRot = mandoRot * Quaternion.Euler(rotationOffset);
        Vector3 targetPos = mandoPos - targetRot * gripLocalPosition;

        targetPos = ResolverPenetracion(targetPos, targetRot);

        transform.position = targetPos;
        transform.rotation = targetRot;

        if (_rb != null)
        {
            _rb.position = targetPos;
            _rb.rotation = targetRot;
        }
    }

    private Vector3 ResolverPenetracion(Vector3 pos, Quaternion rot)
    {
        if (_propioCollider == null) return pos;
        Collider[] vecinos = Physics.OverlapSphere(pos, radioImpacto * 1.5f, layerVitrina,
                                                   QueryTriggerInteraction.Ignore);
        bool tocaVitrina = false;

        foreach (Collider otro in vecinos)
        {
            if (otro == _propioCollider) continue;
            if (((1 << otro.gameObject.layer) & layerTableros) != 0) continue;
            
            if (otro is MeshCollider meshCollider && !meshCollider.convex) continue;

            if (Physics.ComputePenetration(
                    _propioCollider, pos, rot,
                    otro, otro.transform.position, otro.transform.rotation,
                    out Vector3 dir, out float dist))
            {
                pos += dir * dist;
                
                if (colliderVitrina != null && otro == colliderVitrina)
                {
                    tocaVitrina = true;
                }
            }
        }

        if (tocaVitrina && audioVitrineBloqueo != null)
        {
            if (Time.time - _tiempoUltimoAudioVitrina >= cooldownAudioVitrina)
            {
                _tiempoUltimoAudioVitrina = Time.time;
                audioVitrineBloqueo.Play();
            }
        }

        return pos;
    }

    private bool InclinacionCorrecta()
    {
        if (ejeHoja == null)
            return Vector3.Angle(transform.up, Vector3.up) > 25f;

        return Vector3.Angle(ejeHoja.forward, Vector3.down) < umbralAnguloHoja;
    }

    private void ComprobarImpacto()
    {
        if (puntoImpacto == null)
        {
            return;
        }

        Collider[] cols = Physics.OverlapSphere(puntoImpacto.position, radioImpacto, layerTableros);
        foreach (Collider col in cols)
        {
            if (col.TryGetComponent<TableroDestructible>(out var tablero) && !tablero.Roto)
            {
                tablero.RecibirImpacto();
                _tiempoUltimoGolpe = Time.time;
                EnviarHapticFeedback();
                break;
            }
        }
    }

    private void EnviarHapticFeedback()
    {
        if (!_mando.isValid || hapticIntensidad <= 0f) return;
        if (_mando.TryGetHapticCapabilities(out HapticCapabilities caps) && caps.supportsImpulse)
            _mando.SendHapticImpulse(0, hapticIntensidad, hapticDuracion);
    }

    void OnDrawGizmosSelected()
    {
        Quaternion gizmoRot = transform.rotation;
        Vector3 gripWorldPos = transform.position + gizmoRot * gripLocalPosition;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(gripWorldPos, 0.04f);
        Gizmos.DrawLine(transform.position, gripWorldPos);

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
