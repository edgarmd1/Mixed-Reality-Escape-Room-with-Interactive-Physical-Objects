using UnityEngine;
using UnityEngine.XR;


public class FollowController : MonoBehaviour
{
    [Header("Controlador a seguir")]
    [Tooltip("LeftHand = mando izquierdo, RightHand = mando derecho")]
    public XRNode controllerNode = XRNode.LeftHand;

    [Header("Offset respecto al controlador (metros)")]
    public Vector3 positionOffset = new Vector3(0f, 0.05f, 0.05f);
    public Vector3 rotationOffset = new Vector3(0f, 0f, 0f);

    [Header("Suavizado (0 = instantáneo, sin vibrado)")]
    [Range(0f, 20f)]
    public float smoothSpeed = 0f;

    [Header("Visibilidad automática - Gesto de palma")]
    [Tooltip("Activa el mostrar/ocultar automático según el gesto")]
    public bool autoHide = true;

    [Tooltip("GameObject raíz del menú a mostrar/ocultar (normalmente 'Spatial Panel Scroll')")]
    public GameObject menuRoot;

    [Tooltip("Eje local del controlador que define la dirección de la palma.\n" +
             "Meta Quest izquierdo: (-1,0,0) para gesto de muñeca lado-a-lado.\n" +
             "Si el gesto está invertido, prueba (1,0,0).")]
    public Vector3 palmLocalAxis = new Vector3(-1f, 0f, 0f);

    [Tooltip("Ángulo máximo (grados) entre la palma y la dirección a la cámara para mostrar el menú")]
    [Range(10f, 170f)]
    public float palmAngleThreshold = 90f;

    [Tooltip("Segundos de retardo antes de ocultar (evita parpadeo)")]
    [Range(0f, 1f)]
    public float hideDelay = 0.1f;

    [Header("Desactivar HandMenu conflictivo")]
    [Tooltip("Desactiva el componente HandMenu/LazyFollow del padre para evitar vibrado")]
    public bool disableParentHandMenu = true;

    
    private Transform _xrOriginTransform;
    private float     _hideTimer = 0f;
    private bool      _menuVisible = false;

    void Start()
    {
        OVRCameraRig ovrCameraRig = FindObjectOfType<OVRCameraRig>();
        if (ovrCameraRig != null)
            _xrOriginTransform = ovrCameraRig.transform;
        else
            Debug.LogWarning("[FollowController] No se encontró OVRCameraRig en la escena.");

        if (disableParentHandMenu && transform.parent != null)
        {
            foreach (var mb in transform.parent.GetComponents<MonoBehaviour>())
            {
                string t = mb.GetType().Name;
                if (t.Contains("HandMenu") || t.Contains("LazyFollow"))
                {
                    mb.enabled = false;
                    Debug.Log($"[FollowController] Desactivado: {t}");
                }
            }
        }

        if (autoHide && menuRoot != null)
        {
            menuRoot.SetActive(false);
            _menuVisible = false;
        }
    }

    void LateUpdate()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(controllerNode);
        if (!device.isValid) return;

        if (!device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPos)) return;
        if (!device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion localRot)) return;

        Vector3    worldPos;
        Quaternion worldRot;

        if (_xrOriginTransform != null)
        {
            worldPos = _xrOriginTransform.TransformPoint(localPos);
            worldRot  = _xrOriginTransform.rotation * localRot;
        }
        else
        {
            worldPos = localPos;
            worldRot  = localRot;
        }

        Vector3    targetPos = worldPos + worldRot * positionOffset;
        Quaternion targetRot = worldRot * Quaternion.Euler(rotationOffset);

        if (smoothSpeed <= 0f)
        {
            transform.position = targetPos;
            transform.rotation = targetRot;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smoothSpeed);
        }

        if (autoHide && menuRoot != null && Camera.main != null)
        {
            Vector3 palmDir  = worldRot * palmLocalAxis.normalized;
            Vector3 toCamera = (Camera.main.transform.position - worldPos).normalized;

            float angle = Vector3.Angle(palmDir, toCamera);
            bool  palmFacingUser = angle < palmAngleThreshold;

            if (palmFacingUser)
            {
                _hideTimer = 0f;
                if (!_menuVisible)
                {
                    _menuVisible = true;
                    menuRoot.SetActive(true);
                }
            }
            else
            {
                _hideTimer += Time.deltaTime;
                if (_menuVisible && _hideTimer >= hideDelay)
                {
                    _menuVisible = false;
                    menuRoot.SetActive(false);
                }
            }
        }
    }
}
