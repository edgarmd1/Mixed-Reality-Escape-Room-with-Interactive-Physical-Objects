using System.Collections;
using UnityEngine;
using Unity.XR.CoreUtils;

public class VRSpawnController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Punto de spawn inicial en el mundo virtual.")]
    [SerializeField] private Transform puntoSpawnVR;

    [Tooltip("XR Origin de la escena. Si se deja vacío se busca automáticamente.")]
    [SerializeField] private XROrigin xrOrigin;

    [Header("Tracking")]
    [Tooltip("Altura mínima")]
    [SerializeField] private float alturaTrackingMinima = 0.05f;

    [Tooltip("Tiempo máximo.")]
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
            return;
        }

        if (puntoSpawnVR == null)
        {
            return;
        }

        StartCoroutine(EsperarYSpawnear());
    }

    private IEnumerator EsperarYSpawnear()
    {
        Camera cam = xrOrigin.Camera;
        if (cam == null)
        {
            yield break;
        }

        yield return new WaitForEndOfFrame();
        
        float tiempoEspera = 0f;
        while (cam.transform.localPosition.y < alturaTrackingMinima && tiempoEspera < timeoutTracking)
        {
            tiempoEspera += Time.deltaTime;
            yield return null;
        }

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
    }
}
