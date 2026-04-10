using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// Repositions the XR Origin when entering VR mode so the camera starts
/// correctly aligned with the virtual world floor, avoiding the jump on
/// the first teleport caused by the MR origin offset.
/// </summary>
public class VRSpawnController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Punto de spawn inicial en el mundo virtual. " +
             "Crea un GameObject vacío en la posición donde quieres que aparezca el jugador.")]
    [SerializeField] private Transform puntoSpawnVR;

    [Tooltip("XR Origin de la escena. Si se deja vacío se busca automáticamente.")]
    [SerializeField] private XROrigin xrOrigin;

    void Awake()
    {
        if (xrOrigin == null)
            xrOrigin = FindObjectOfType<XROrigin>();
    }

    /// <summary>
    /// Llamar a este método al entrar en modo VR para reposicionar el XR Origin.
    /// La posición se calcula para que la cámara (cabeza del jugador) quede
    /// sobre el punto de spawn, compensando el offset de tracking.
    /// </summary>
    public void TeletransportarAlSpawnVR()
    {
        if (xrOrigin == null)
        {
            Debug.LogError("[VRSpawnController] No se encontró XROrigin.");
            return;
        }

        if (puntoSpawnVR == null)
        {
            Debug.LogWarning("[VRSpawnController] No hay punto de spawn VR asignado. " +
                             "Asigna un Transform en el Inspector.");
            return;
        }

        // La cámara del XR Origin tiene un offset respecto al propio XR Origin
        // (la altura del tracking del casco). Necesitamos compensarlo para que
        // la cámara quede EXACTAMENTE sobre el punto de spawn.
        Camera cam = xrOrigin.Camera;
        if (cam == null)
        {
            Debug.LogError("[VRSpawnController] XROrigin no tiene cámara asignada.");
            return;
        }

        // Offset horizontal de la cámara respecto al XR Origin (solo X y Z)
        Vector3 camOffsetLocal = cam.transform.localPosition;
        Vector3 camOffsetWorld = new Vector3(camOffsetLocal.x, 0f, camOffsetLocal.z);

        // Nueva posición del XR Origin: spawn - offset horizontal de cámara
        // La Y del XR Origin se pone a la Y del spawn (suelo del mundo virtual)
        Vector3 nuevaPosicion = puntoSpawnVR.position - camOffsetWorld;
        nuevaPosicion.y = puntoSpawnVR.position.y;

        xrOrigin.transform.position = nuevaPosicion;

        // Orientación: ajustamos la rotación Y del XR Origin para que mire
        // en la dirección del punto de spawn, manteniendo el up del mundo.
        float rotacionY = puntoSpawnVR.eulerAngles.y;
        xrOrigin.transform.rotation = Quaternion.Euler(0f, rotacionY, 0f);

        Debug.Log($"[VRSpawnController] XR Origin reposicionado a {nuevaPosicion} " +
                  $"para spawn VR en {puntoSpawnVR.position}");
    }
}
