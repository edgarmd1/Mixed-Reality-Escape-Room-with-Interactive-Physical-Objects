using UnityEngine;
using UnityEngine.XR.Templates.MR;

public class CameraCullingMaskController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Cámara principal a controlar")]
    private Camera mainCamera;

    [SerializeField, Tooltip("ARFeatureController")]
    private ARFeatureController arFeatureController;

    [SerializeField, Tooltip("Controlador de spawn inicial en el mundo virtual")]
    private VRSpawnController vrSpawnController;

    [Header("Configuración de Capas")]
    [SerializeField, Tooltip("Nombre de la capa para objetos del mundo real")]
    private string mundoRealLayerName = "Mundo_Real";

    [SerializeField, Tooltip("Nombre de la capa para objetos del mundo virtual")]
    private string mundoVirtualLayerName = "Mundo_Virtual";

    private int mundoRealLayer;
    private int mundoVirtualLayer;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }
        }

        if (arFeatureController == null)
        {
            arFeatureController = FindObjectOfType<ARFeatureController>();
            if (arFeatureController == null)
            {
                return;
            }
        }

        mundoRealLayer = LayerMask.NameToLayer(mundoRealLayerName);
        mundoVirtualLayer = LayerMask.NameToLayer(mundoVirtualLayerName);

        if (arFeatureController != null && arFeatureController.onARPassthroughFeatureChanged != null)
        {
            arFeatureController.onARPassthroughFeatureChanged.AddListener(OnPassthroughChanged);
        }
    }

    void OnDestroy()
    {
        if (arFeatureController != null && arFeatureController.onARPassthroughFeatureChanged != null)
        {
            arFeatureController.onARPassthroughFeatureChanged.RemoveListener(OnPassthroughChanged);
        }
    }

    private void OnPassthroughChanged(bool passthroughEnabled)
    {
        if (mainCamera == null)
            return;

        GameModeManager.SetMode(passthroughEnabled);

        if (passthroughEnabled)
        {
            SetLayerObjectsActive(mundoRealLayer, true);
            SetLayerVisibility(mundoRealLayer, true);
            SetLayerVisibility(mundoVirtualLayer, false);
            SetLayerObjectsActive<VRSiempreActivo>(mundoVirtualLayer, false);
        }
        else
        {
            SetLayerVisibility(mundoRealLayer, false);
            SetLayerObjectsActive<MRSiempreActivo>(mundoRealLayer, false);
            SetLayerObjectsActive(mundoVirtualLayer, true);
            SetLayerVisibility(mundoVirtualLayer, true);

            vrSpawnController?.TeletransportarAlSpawnVR();

        }
    }

    private void SetLayerVisibility(int layer, bool visible)
    {
        if (layer == -1)
            return;

        if (visible)
            mainCamera.cullingMask |= (1 << layer);
        else
            mainCamera.cullingMask &= ~(1 << layer);
    }

    private void SetLayerObjectsActive(int layer, bool active)
    {
        if (layer == -1)
            return;

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject go in allObjects)
        {
            if (go.layer == layer)
                go.SetActive(active);
        }
    }

    private void SetLayerObjectsActive<T>(int layer, bool active) where T : Component
    {
        if (layer == -1)
            return;

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject go in allObjects)
        {
            if (go.layer != layer)
                continue;

            if (!active && go.GetComponent<T>() != null)
            {
                continue;
            }

            go.SetActive(active);
        }
    }

    public void SetMode(bool enableMR)
    {
        OnPassthroughChanged(enableMR);
    }

    public void SetModeSoloVisual(bool enableMR)
    {
        if (mainCamera == null) return;

        GameModeManager.SetMode(enableMR);

        if (enableMR)
        {
            SetLayerVisibility(mundoRealLayer, true);
            SetLayerVisibility(mundoVirtualLayer, false);
        }
        else
        {
            SetLayerVisibility(mundoRealLayer, false);
            SetLayerVisibility(mundoVirtualLayer, true);
        }
    }
}
