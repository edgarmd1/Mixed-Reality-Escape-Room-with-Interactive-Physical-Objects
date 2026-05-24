using UnityEngine;

public class TableroDestructible : MonoBehaviour
{
    
    [SerializeField, Tooltip("Gestor central del puzzle")]
    private DoorPuzzleManager doorPuzzleManager;

    [SerializeField, Tooltip("Clip de sonido de madera rompiéndose")]
    private AudioClip clipRotura;

    [SerializeField, Tooltip("Volumen del sonido de rotura"), Range(0f, 1f)]
    private float volumenRotura = 1f;

    [SerializeField, Tooltip("Partículas/debris de madera al romper (puede ser null)")]
    private GameObject efectoRotura;

    /// <summary>Índice de este tablero dentro del array del DoorPuzzleManager. Asignado por el propio manager al inicio.</summary>
    [HideInInspector] public int indice = -1;

    public bool Roto { get; private set; } = false;

    public void RecibirImpacto()
    {
        if (Roto) return;

        Roto = true;

        // Persistir el estado entre recargas de la escena
        if (indice >= 0)
            DoorPuzzleState.TablonesRotosIndices.Add(indice);

        if (efectoRotura != null)
        {
            Instantiate(efectoRotura, transform.position, transform.rotation);
        }

        if (clipRotura != null)
        {
            AudioSource.PlayClipAtPoint(clipRotura, transform.position, volumenRotura);
        }

        Debug.Log($"[Tablero] Roto: {gameObject.name} (índice {indice})");

        doorPuzzleManager?.NotificarTablaRota();

        gameObject.SetActive(false);
    }

    void OnDrawGizmos()
    {
        if (!Roto)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.4f);
            if (TryGetComponent<BoxCollider>(out var bc))
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(bc.center, bc.size);
            }
        }
    }
}
