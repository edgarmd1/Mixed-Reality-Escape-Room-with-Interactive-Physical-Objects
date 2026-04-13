using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;

public class VRSpawnController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Punto de spawn inicial en el mundo virtual. " +
             "Crea un GameObject vacío en la posición donde quieres que aparezca el jugador.")]
    [SerializeField] private Transform puntoSpawnVR;

    [Tooltip("XR Origin de la escena. Si se deja vacío se busca automáticamente.")]
    [SerializeField] private XROrigin xrOrigin;

    [Header("Tracking")]
    [Tooltip("Altura mínima (metros) que debe reportar la cámara para considerar que el " +
             "tracking está activo. Reduce a 0 si el tracking siempre está listo.")]
    [SerializeField] private float alturaTrackingMinima = 0.05f;

    [Tooltip("Tiempo máximo (segundos) esperando tracking antes de aplicar el spawn igualmente.")]
    [SerializeField] private float timeoutTracking = 5f;

    void Awake()
    {
        if (xrOrigin == null)
            xrOrigin = FindObjectOfType<XROrigin>();
    }
    
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

        StartCoroutine(EsperarYSpawnear());
    }

    private IEnumerator EsperarYSpawnear()
    {
        Camera cam = xrOrigin.Camera;
        if (cam == null)
        {
            Debug.LogError("[VRSpawnController] XROrigin no tiene cámara asignada.");
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

        xrOrigin.transform.position = nuevaPosicion;

        float rotacionY = puntoSpawnVR.eulerAngles.y;
        xrOrigin.transform.rotation = Quaternion.Euler(0f, rotacionY, 0f);

        Debug.Log($"[VRSpawnController] Spawn aplicado → XROrigin: {nuevaPosicion} | " +
                  $"Spawn: {puntoSpawnVR.position} | CamOffset: {camOffsetLocal}");
    }
}
