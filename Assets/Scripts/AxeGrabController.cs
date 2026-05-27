using UnityEngine;
using UnityEngine.XR;

public class AxeGrabController : MonoBehaviour
{
    // ── Agarre ────────────────────────────────────────────────────────────
    [Header("── Agarre ──")]

    [SerializeField, Tooltip(
        "Rotación del hacha respecto al mando (grados Euler).\n" +
        "El mando tiene estos ejes cuando se sujeta naturalmente:\n" +
        "  +Y = arriba (hacia el techo)\n" +
        "  +Z = hacia delante (la dirección del rayo)\n" +
        "  +X = a la derecha\n\n" +
        "Para que el mango quede VERTICAL (correcto para hacha):\n" +
        "  Prueba (90, 0, 0) → mango queda a lo largo del brazo\n" +
        "  Prueba (-90, 0, 0) → mango queda al revés\n" +
        "  Añade 180 en Y si la hoja apunta hacia ti")]
    private Vector3 rotationOffset = new Vector3(90f, 180f, 0f);

    [SerializeField, Tooltip(
        "Posición del punto de agarre en el espacio LOCAL del hacha (después de aplicar la rotación).\n\n" +
        "Indica cuál es el punto del mango donde el jugador agarra,\n" +
        "respecto al origen del hacha.\n\n" +
        "Ejemplo: si el mango mide 0.5m y el origen del hacha está en la hoja,\n" +
        "el punto de agarre estaría en (0, -0.5, 0) o (0, 0.5, 0)\n" +
        "dependiendo de hacia dónde apunta el mango.\n\n" +
        "USA el ContextMenu 'Mostrar ejes del hacha' para ver cuáles son tus ejes.")]
    private Vector3 gripLocalPosition = Vector3.zero;


    // ── Detección de impacto ───────────────────────────────────────────────
    [Header("── Detección de impacto ──")]
    [SerializeField] private Transform puntoImpacto;
    [SerializeField] private Transform ejeHoja;
    [SerializeField] private float umbralVelocidad     = 1.2f;
    [SerializeField] private float umbralAnguloHoja    = 65f;
    [SerializeField] private float radioImpacto        = 0.18f;
    [SerializeField] private float cooldownEntreGolpes = 0.55f;
    [SerializeField] private LayerMask layerTableros;

    // ── Haptic ─────────────────────────────────────────────────────────────
    [Header("── Haptic feedback ──")]
    [SerializeField, Range(0f, 1f)] private float hapticIntensidad = 0.6f;
    [SerializeField, Range(0.05f, 1f)] private float hapticDuracion = 0.25f;

    // ── Auto-calibración ───────────────────────────────────────────────────
    // Clave para PlayerPrefs
    private const string PREFS_KEY = "Hacha_PosicionReposo";

    // Posición y rotación de reposo (guardada/cargada de PlayerPrefs)
    private Vector3    _posReposo;
    private Quaternion _rotReposo;
    private bool       _tienePosReposo = false;

    // ── Estado interno ─────────────────────────────────────────────────────
    private InputDevice _mando;
    private bool        _mandoEnMano;
    private float       _tiempoUltimoGolpe = -999f;
    private Vector3     _posAnterior;
    private float       _velocidadActual;

    private Unity.XR.CoreUtils.XROrigin _xrOrigin;

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        _xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        CargarPosicionReposo();
        _posAnterior = transform.position;

        MostrarEjesHacha();
    }

    void OnEnable()
    {
        InputDevices.deviceConnected    += OnDeviceConnected;
        InputDevices.deviceDisconnected += OnDeviceDisconnected;
        BuscarMandoDerecho();
    }

    void OnDisable()
    {
        InputDevices.deviceConnected    -= OnDeviceConnected;
        InputDevices.deviceDisconnected -= OnDeviceDisconnected;
    }

    void Update()
    {
        _velocidadActual = (transform.position - _posAnterior).magnitude / Time.deltaTime;
        _posAnterior = transform.position;

        if (!_mandoEnMano)                                         return;
        if (Time.time - _tiempoUltimoGolpe < cooldownEntreGolpes) return;
        if (_velocidadActual < umbralVelocidad)                    return;
        if (!InclinacionCorrecta())                                 return;

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

    // ── Auto-calibración ───────────────────────────────────────────────────
    private void GuardarPosicionReposo()
    {
        _posReposo      = transform.position;
        _rotReposo      = transform.rotation;
        _tienePosReposo = true;

        PlayerPrefs.SetFloat(PREFS_KEY + "_PosX", _posReposo.x);
        PlayerPrefs.SetFloat(PREFS_KEY + "_PosY", _posReposo.y);
        PlayerPrefs.SetFloat(PREFS_KEY + "_PosZ", _posReposo.z);
        PlayerPrefs.SetFloat(PREFS_KEY + "_RotX", _rotReposo.x);
        PlayerPrefs.SetFloat(PREFS_KEY + "_RotY", _rotReposo.y);
        PlayerPrefs.SetFloat(PREFS_KEY + "_RotZ", _rotReposo.z);
        PlayerPrefs.SetFloat(PREFS_KEY + "_RotW", _rotReposo.w);
        PlayerPrefs.SetInt  (PREFS_KEY + "_OK",   1);
        PlayerPrefs.Save();

        Debug.Log($"[Hacha] Posición de reposo auto-guardada: {_posReposo}");
    }

    private void CargarPosicionReposo()
    {
        if (PlayerPrefs.GetInt(PREFS_KEY + "_OK", 0) == 0)
        {
            Debug.Log("[Hacha] Sin posición de reposo guardada — usando posición de escena.");
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

        // Aplicar inmediatamente
        transform.position = _posReposo;
        transform.rotation = _rotReposo;

        Debug.Log($"[Hacha] Posición de reposo cargada: {_posReposo}");
    }

    private void AplicarPosicionReposo()
    {
        if (!_tienePosReposo) return;
        transform.position = _posReposo;
        transform.rotation = _rotReposo;
    }

    [ContextMenu("Mostrar ejes del hacha (para configurar gripLocalPosition)")]
    private void MostrarEjesHacha()
    {
        Debug.Log($"[Hacha] Ejes en mundo → " +
                  $"Derecha: {transform.right:F2} | " +
                  $"Arriba: {transform.up:F2} | " +
                  $"Adelante: {transform.forward:F2}");
        Debug.Log("[Hacha] Para encontrar el eje del mango, mira cuál de los tres apunta a lo largo del palo del hacha.");
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

    // ── Dispositivo ────────────────────────────────────────────────────────

    private void OnDeviceConnected(InputDevice device)
    {
        if (EsControllerDerecho(device))
        {
            _mando = device;
            Debug.Log($"[Hacha] Mando derecho conectado: {device.name}");
        }
    }

    private void OnDeviceDisconnected(InputDevice device)
    {
        if (EsControllerDerecho(device))
        {
            _mando = default;
            _mandoEnMano = false;
            Debug.Log("[Hacha] Mando derecho desconectado.");
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
                Debug.Log($"[Hacha] Mando derecho encontrado: {d.name}");
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
            if (_mandoEnMano) { _mandoEnMano = false; Debug.Log("[Hacha] Mando perdido."); }
            return;
        }

        bool tracked = _mando.TryGetFeatureValue(CommonUsages.isTracked, out bool t) && t;
        if (tracked == _mandoEnMano) return;

        bool estabaMandoEnMano = _mandoEnMano;
        _mandoEnMano = tracked;

        if (!tracked && estabaMandoEnMano)
        {
            GuardarPosicionReposo();
            Debug.Log("[Hacha] Mando dejado en mesa → posición de reposo auto-guardada.");
        }
        else if (tracked)
        {
            Debug.Log("[Hacha] Mando cogido → detección de impacto activa.");
        }
    }

    private void ColocarEnMando()
    {
        if (!_mando.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPos)) return;
        if (!_mando.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion localRot)) return;

        Vector3    mandoPos;
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

        // El gripLocalPosition indica dónde está el punto de agarre
        // en el espacio local del hacha (después de aplicar la rotación).
        // La hacha se posiciona para que ese punto coincida con el mando.
        // Fórmula: hachaPos = mandoPos - targetRot * gripLocalPosition
        Vector3 targetPos = mandoPos - targetRot * gripLocalPosition;

        transform.position = targetPos;
        transform.rotation = targetRot;
    }

    // ── Detección de impacto ───────────────────────────────────────────────

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
            Debug.LogWarning("[Hacha] 'PuntoImpacto' no asignado.");
            return;
        }

        Collider[] cols = Physics.OverlapSphere(puntoImpacto.position, radioImpacto, layerTableros);
        foreach (Collider col in cols)
        {
            if (col.TryGetComponent<TableroDestructible>(out var tablero) && !tablero.Roto)
            {
                Debug.Log($"[Hacha] Impacto en {tablero.gameObject.name} — vel: {_velocidadActual:F2} m/s");
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

    // ── Gizmos ─────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        // Punto de agarre en espacio mundo (después de aplicar rotation offset)
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
