using UnityEngine;
using UnityEngine.XR.Templates.MR;

public class CameraCullingMaskController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Cámara principal a controlar (Main Camera del XR Origin)")]
    private Camera mainCamera;

    [SerializeField, Tooltip("ARFeatureController que gestiona el Passthrough")]
    private ARFeatureController arFeatureController;

    [Header("Configuración de Capas")]
    [SerializeField, Tooltip("Nombre de la capa para objetos del mundo real (MR)")]
    private string mundoRealLayerName = "Mundo_Real";

    [SerializeField, Tooltip("Nombre de la capa para objetos del mundo virtual (VR)")]
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
                Debug.LogError("CameraCullingMaskController: No se encontró la cámara principal.");
                return;
            }
        }

        if (arFeatureController == null)
        {
            arFeatureController = FindObjectOfType<ARFeatureController>();
            if (arFeatureController == null)
            {
                Debug.LogError("CameraCullingMaskController: No se encontró el ARFeatureController.");
                return;
            }
        }

        mundoRealLayer = LayerMask.NameToLayer(mundoRealLayerName);
        mundoVirtualLayer = LayerMask.NameToLayer(mundoVirtualLayerName);

        if (mundoRealLayer == -1)
            Debug.LogWarning($"CameraCullingMaskController: La capa '{mundoRealLayerName}' no existe.");

        if (mundoVirtualLayer == -1)
            Debug.LogWarning($"CameraCullingMaskController: La capa '{mundoVirtualLayerName}' no existe.");

        if (arFeatureController != null && arFeatureController.onARPassthroughFeatureChanged != null)
        {
            arFeatureController.onARPassthroughFeatureChanged.AddListener(OnPassthroughChanged);
            Debug.Log("CameraCullingMaskController: Suscrito al evento de Passthrough.");
        }
    }

    void OnDestroy()
    {
        if (arFeatureController != null && arFeatureController.onARPassthroughFeatureChanged != null)
        {
            arFeatureController.onARPassthroughFeatureChanged.RemoveListener(OnPassthroughChanged);
        }
    }

    /// <param name="passthroughEnabled">True si Passthrough está activado (MR), False si está desactivado (VR)</param>
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
            Debug.Log("CameraCullingMaskController: Modo MR - Mundo_Real visible e interactuable.");
        }
        else
        {
            SetLayerVisibility(mundoRealLayer, false);
            SetLayerObjectsActive(mundoRealLayer, false);
            SetLayerVisibility(mundoVirtualLayer, true);
            Debug.Log("CameraCullingMaskController: Modo VR - Mundo_Real oculto y desactivado.");
        }
    }

    /// <param name="layer">Índice de la capa</param>
    /// <param name="visible">True para mostrar, False para ocultar</param>
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
            if (go.layer != layer)
                continue;

            if (!active && go.GetComponent<MRSiempreActivo>() != null)
            {
                Debug.Log($"[CameraCullingMask] '{go.name}' conserva MRSiempreActivo → permanece activo en VR.");
                continue;
            }

            go.SetActive(active);
        }
    }

    /// <param name="enableMR">True para MR, False para VR</param>
    public void SetMode(bool enableMR)
    {
        OnPassthroughChanged(enableMR);
    }
}
