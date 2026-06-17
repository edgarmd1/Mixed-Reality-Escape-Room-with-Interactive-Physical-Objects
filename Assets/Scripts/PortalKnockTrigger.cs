using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PortalKnockTrigger : MonoBehaviour
{
    [SerializeField, Tooltip("Referencia al KnockPuzzleManager de la escena")]
    private KnockPuzzleManager knockPuzzleManager;

    [SerializeField, Tooltip("Tag del jugador/cámara que activa el trigger")]
    private string tagJugador = "MainCamera";

    void OnTriggerEnter(Collider other)
    {
        if (knockPuzzleManager == null) return;

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
