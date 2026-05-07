using UnityEngine;

/// <summary>
/// Detecta cuando el jugador (cámara XR) está dentro del volumen del trigger
/// y notifica al KnockPuzzleManager para volver al mundo real.
///
/// Usa comprobación por proximidad en Update (no depende de OnTriggerEnter,
/// que falla en XR con teleportación porque la cámara no tiene Rigidbody).
///
/// Uso: Crear un GameObject con un BoxCollider (isTrigger=true) y este script.
///      El tamaño del BoxCollider define la zona de activación.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ReturnToMRTrigger : MonoBehaviour
{
    private BoxCollider _triggerZone;
    private KnockPuzzleManager _knockPuzzle;
    private bool _triggered = false;

    void Start()
    {
        _triggerZone = GetComponent<BoxCollider>();
        _triggerZone.isTrigger = true;

        // Buscar el KnockPuzzleManager en la escena principal
        _knockPuzzle = FindObjectOfType<KnockPuzzleManager>();
        if (_knockPuzzle == null)
            Debug.LogWarning("[ReturnToMR] No se encontró KnockPuzzleManager en la escena.");
    }

    void Update()
    {
        if (_triggered || _knockPuzzle == null) return;

        // Obtener la posición de la cámara XR (la "cabeza" del jugador)
        Camera cam = Camera.main;
        if (cam == null) return;

        // Comprobar si la cámara está dentro del BoxCollider
        if (PuntoEnBox(cam.transform.position))
        {
            _triggered = true;
            Debug.Log("[ReturnToMR] Cámara del jugador dentro de la zona de retorno.");
            _knockPuzzle.OnVolverAMR();
        }
    }

    /// <summary>
    /// Comprueba si un punto del mundo está dentro del BoxCollider (teniendo en cuenta
    /// posición, rotación y escala del GameObject).
    /// </summary>
    private bool PuntoEnBox(Vector3 worldPoint)
    {
        // Convertir el punto a espacio local del collider
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

        // Comprobar contra los bounds del BoxCollider en espacio local
        Vector3 center = _triggerZone.center;
        Vector3 halfSize = _triggerZone.size * 0.5f;

        return localPoint.x >= center.x - halfSize.x && localPoint.x <= center.x + halfSize.x &&
               localPoint.y >= center.y - halfSize.y && localPoint.y <= center.y + halfSize.y &&
               localPoint.z >= center.z - halfSize.z && localPoint.z <= center.z + halfSize.z;
    }
}
