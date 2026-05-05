using UnityEngine;

/// <summary>
/// Componente auxiliar que se coloca en el portal (portalAscensor).
/// Detecta automáticamente cuando el jugador entra en el trigger
/// y notifica al KnockPuzzleManager para iniciar la transición al pasillo VR.
///
/// Uso: Añadir este script al GameObject del portal que ya tiene un Collider (isTrigger=true).
///      Asignar el KnockPuzzleManager en el Inspector.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PortalKnockTrigger : MonoBehaviour
{
    [SerializeField, Tooltip("Referencia al KnockPuzzleManager de la escena")]
    private KnockPuzzleManager knockPuzzleManager;

    [SerializeField, Tooltip("Tag del jugador/cámara que activa el trigger (normalmente 'MainCamera' o 'Player')")]
    private string tagJugador = "MainCamera";

    void OnTriggerEnter(Collider other)
    {
        if (knockPuzzleManager == null) return;

        // Comprobar si es el jugador (por tag o si tiene Camera)
        bool esJugador = other.CompareTag(tagJugador) ||
                         other.GetComponent<Camera>() != null ||
                         other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null;

        if (esJugador)
        {
            Debug.Log($"[PortalKnockTrigger] Jugador entró en el portal ({other.gameObject.name}).");
            knockPuzzleManager.OnJugadorEntraPortal();
        }
    }
}
