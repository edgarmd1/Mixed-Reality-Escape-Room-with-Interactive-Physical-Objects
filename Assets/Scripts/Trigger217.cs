using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class Trigger217 : MonoBehaviour
{
    [SerializeField, Tooltip("escena")]
    private string nombreEscena217 = "217";

    [SerializeField, Tooltip("KeypadPuzzleManager que controla el estado de la secuencia del fantasma")]
    private KeypadPuzzleManager keypadPuzzleManager;

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

        if (keypadPuzzleManager != null)
        {
            var estado = keypadPuzzleManager.Estado;
            bool estadoValido = estado == KeypadPuzzleManager.EstadoKeypad.CamaraDisponible
                             || estado == KeypadPuzzleManager.EstadoKeypad.PuzzleCompletado;
            if (!estadoValido) return;
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        if (PuntoEnBox(cam.transform.position))
        {
            _activado = true;
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
            yield break;
        }

        var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
        if (xrOrigin != null && manager.SpawnInicio != null)
        {
            Camera camXR = xrOrigin.Camera;
            if (camXR != null)
            {
                Vector3 camOffset = camXR.transform.localPosition;
                xrOrigin.transform.position = new Vector3(
                    manager.SpawnInicio.position.x - camOffset.x,
                    manager.SpawnInicio.position.y - camOffset.y,
                    manager.SpawnInicio.position.z - camOffset.z
                );
                xrOrigin.transform.rotation = Quaternion.Euler(0f, manager.SpawnInicio.eulerAngles.y, 0f);
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
