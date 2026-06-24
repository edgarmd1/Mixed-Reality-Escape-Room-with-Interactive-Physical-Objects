using System.Collections;
using UnityEngine;

public class VRSpawnController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Punto de spawn inicial en el mundo virtual.")]
    [SerializeField] private Transform puntoSpawnVR;

    [Tooltip("OVR Camera Rig de la escena. Si se deja vacío se busca automáticamente.")]
    [SerializeField] private OVRCameraRig ovrCameraRig;

    [Header("Tracking")]
    [Tooltip("Altura mínima")]
    [SerializeField] private float alturaTrackingMinima = 0.05f;

    [Tooltip("Tiempo máximo.")]
    [SerializeField] private float timeoutTracking = 5f;

    void Awake()
    {
        if (ovrCameraRig == null)
            ovrCameraRig = FindObjectOfType<OVRCameraRig>();
    }
    
    public void TeletransportarAlSpawnVR()
    {
        if (ovrCameraRig == null)
        {
            Debug.LogError("[VRSpawnController] No se encontró OVRCameraRig.");
            return;
        }

        if (puntoSpawnVR == null)
        {
            Debug.LogWarning("[VRSpawnController] No hay punto de spawn VR asignado. " +
                             "Asigna un Transform en el Inspector.");
            return;
        }

        StartCoroutine(EsperarYSpawnear());
    }

    private IEnumerator EsperarYSpawnear()
    {
        if (ovrCameraRig == null)
        {
            Debug.LogError("[VRSpawnController] OVRCameraRig es nulo en Coroutine.");
            yield break;
        }

        Camera cam = ovrCameraRig.centerEyeAnchor != null ? ovrCameraRig.centerEyeAnchor.GetComponent<Camera>() : null;
        if (cam == null)
        {
            Debug.LogError("[VRSpawnController] OVRCameraRig no tiene cámara asignada en centerEyeAnchor.");
            yield break;
        }

        yield return new WaitForEndOfFrame();
        
        float tiempoEspera = 0f;
        while (cam.transform.localPosition.y < alturaTrackingMinima && tiempoEspera < timeoutTracking)
        {
            tiempoEspera += Time.deltaTime;
            yield return null;
        }

        if (tiempoEspera >= timeoutTracking)
            Debug.LogWarning("[VRSpawnController] Timeout esperando tracking. " +
                             "Aplicando spawn con el offset actual.");
        else
            Debug.Log($"[VRSpawnController] Tracking listo. Offset cámara: {cam.transform.localPosition}");

        AplicarSpawn(cam);
    }

    private void AplicarSpawn(Camera cam)
    {
        Vector3 camOffsetLocal = cam.transform.localPosition;

        Vector3 nuevaPosicion = new Vector3(
            puntoSpawnVR.position.x - camOffsetLocal.x,
            puntoSpawnVR.position.y - camOffsetLocal.y,
            puntoSpawnVR.position.z - camOffsetLocal.z
        );

        ovrCameraRig.transform.position = nuevaPosicion;

        float rotacionY = puntoSpawnVR.eulerAngles.y;
        ovrCameraRig.transform.rotation = Quaternion.Euler(0f, rotacionY, 0f);

        Debug.Log($"[VRSpawnController] Spawn aplicado → OVRCameraRig: {nuevaPosicion} | " +
                  $"Spawn: {puntoSpawnVR.position} | CamOffset: {camOffsetLocal}");
    }
}
