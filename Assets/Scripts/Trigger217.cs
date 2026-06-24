using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class Trigger217 : MonoBehaviour
{
    [SerializeField, Tooltip("escena")]
    private string nombreEscena217 = "217";

    private BoxCollider _zona;
    private bool _activado = false;

    void Start()
    {
        _zona = GetComponent<BoxCollider>();
        _zona.isTrigger = true;
    }

    void Update()
    {
        if (_activado) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        if (PuntoEnBox(cam.transform.position))
        {
            _activado = true;
            Debug.Log($"[Trigger217] Jugador dentro de la zona – cargando escena '{nombreEscena217}'.");
            StartCoroutine(CargarHabitacion217());
        }
    }

    private IEnumerator CargarHabitacion217()
    {
        Scene escena = SceneManager.GetSceneByName(nombreEscena217);
        if (!escena.isLoaded)
        {
            AsyncOperation op = SceneManager.LoadSceneAsync(nombreEscena217, LoadSceneMode.Additive);
            yield return op;
        }

        yield return null;

        Habitacion217Manager manager = Habitacion217Manager.Instance;
        if (manager == null)
        {
            Debug.LogError("[Trigger217] No se encontró Habitacion217Manager en la escena 217.");
            yield break;
        }

        var ovrCameraRig = FindObjectOfType<OVRCameraRig>();
        if (ovrCameraRig != null && manager.SpawnInicio != null)
        {
            Camera camXR = ovrCameraRig.centerEyeAnchor != null ? ovrCameraRig.centerEyeAnchor.GetComponent<Camera>() : null;
            if (camXR != null)
            {
                Vector3 camOffset = camXR.transform.localPosition;
                ovrCameraRig.transform.position = new Vector3(
                    manager.SpawnInicio.position.x - camOffset.x,
                    manager.SpawnInicio.position.y - camOffset.y,
                    manager.SpawnInicio.position.z - camOffset.z
                );
                ovrCameraRig.transform.rotation = Quaternion.Euler(0f, manager.SpawnInicio.eulerAngles.y, 0f);
                Debug.Log($"[Trigger217] Jugador teleportado a {ovrCameraRig.transform.position}.");
            }
        }
    }

    private bool PuntoEnBox(Vector3 worldPoint)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        Vector3 center   = _zona.center;
        Vector3 halfSize = _zona.size * 0.5f;

        return localPoint.x >= center.x - halfSize.x && localPoint.x <= center.x + halfSize.x &&
               localPoint.y >= center.y - halfSize.y && localPoint.y <= center.y + halfSize.y &&
               localPoint.z >= center.z - halfSize.z && localPoint.z <= center.z + halfSize.z;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.35f);
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
        }
    }
}
