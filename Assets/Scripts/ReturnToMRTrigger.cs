using UnityEngine;

/// <summary>
/// Trigger que se coloca al final del pasillo (o donde quieras en la escena Pasillo).
/// Cuando el jugador entra, notifica al KnockPuzzleManager para volver al mundo real.
///
/// Uso: Crear un GameObject con un Collider (isTrigger=true) y este script.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ReturnToMRTrigger : MonoBehaviour
{
    [SerializeField, Tooltip("Tag del jugador (normalmente 'MainCamera')")]
    private string tagJugador = "MainCamera";

    void OnTriggerEnter(Collider other)
    {
        bool esJugador = other.CompareTag(tagJugador) ||
                         other.GetComponent<Camera>() != null ||
                         other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null;

        if (esJugador)
        {
            // KnockPuzzleManager está en la escena principal (siempre cargada)
            var knockPuzzle = FindObjectOfType<KnockPuzzleManager>();
            if (knockPuzzle != null)
            {
                Debug.Log("[ReturnToMR] Jugador entró en el trigger de vuelta al MR.");
                knockPuzzle.OnVolverAMR();
            }
            else
            {
                Debug.LogWarning("[ReturnToMR] No se encontró KnockPuzzleManager.");
            }
        }
    }
}
