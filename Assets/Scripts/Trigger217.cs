using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Coloca este script en un GameObject con un BoxCollider justo tras la puerta 217
/// (en la escena Pasillo). Se activa solo cuando KnockPuzzleManager lo habilita.
///
/// Usa comprobación por proximidad en Update (igual que ReturnToMRTrigger),
/// porque el XR Origin de SampleScene no tiene Rigidbody y OnTriggerEnter
/// no se dispara de forma fiable en XR.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class Trigger217 : MonoBehaviour
{
    [SerializeField, Tooltip("Nombre exacto de la escena a cargar (debe estar en Build Settings)")]
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
            SceneManager.LoadScene(nombreEscena217, LoadSceneMode.Single);
        }
    }

    /// <summary>
    /// Comprueba si un punto del mundo está dentro del BoxCollider,
    /// teniendo en cuenta posición, rotación y escala del GameObject.
    /// </summary>
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
