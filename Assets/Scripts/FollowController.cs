using UnityEngine;
using Unity.XR.CoreUtils;

public class FollowController : MonoBehaviour
{
    [Header("Offset respecto al mando izquierdo (metros)")]
    public Vector3 positionOffset = new Vector3(0f, 0.05f, 0.05f);
    public Vector3 rotationOffset = new Vector3(0f, 0f, 0f);

    [Header("Suavizado (0 = instantáneo)")]
    [Range(0f, 20f)]
    public float smoothSpeed = 0f;

    [Header("Menú")]
    [Tooltip("GameObject a mostrar/ocultar al pulsar el grip izquierdo")]
    public GameObject menuRoot;

    [Header("Desactivar HandMenu conflictivo")]
    [Tooltip("Desactiva el componente HandMenu/LazyFollow del padre para evitar vibrado")]
    public bool disableParentHandMenu = true;

    private Transform _xrOriginTransform;
    private bool      _menuVisible     = false;
    private bool      _gripPressedPrev = false;

    void Start()
    {
        XROrigin xrOrigin = FindObjectOfType<XROrigin>();
        if (xrOrigin != null)
            _xrOriginTransform = xrOrigin.transform;

        if (disableParentHandMenu && transform.parent != null)
        {
            foreach (var mb in transform.parent.GetComponents<MonoBehaviour>())
            {
                string t = mb.GetType().Name;
                if (t.Contains("HandMenu") || t.Contains("LazyFollow"))
                {
                    mb.enabled = false;
                }
            }
        }

        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
            _menuVisible = false;
        }
    }

    void LateUpdate()
    {
        Vector3    localPos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
        Quaternion localRot = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);

        Vector3    worldPos;
        Quaternion worldRot;

        if (_xrOriginTransform != null)
        {
            worldPos = _xrOriginTransform.TransformPoint(localPos);
            worldRot = _xrOriginTransform.rotation * localRot;
        }
        else
        {
            worldPos = localPos;
            worldRot = localRot;
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

        if (menuRoot == null) return;

        bool gripPressed = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);

#if UNITY_EDITOR
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.gKey.wasPressedThisFrame)
            gripPressed = true;
#endif
        if (gripPressed && !_gripPressedPrev)
        {
            _menuVisible = !_menuVisible;
            menuRoot.SetActive(_menuVisible);
        }

        _gripPressedPrev = gripPressed;
    }
}
