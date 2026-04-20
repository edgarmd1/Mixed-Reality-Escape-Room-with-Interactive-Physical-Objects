using UnityEngine;

public class TableroDestructible : MonoBehaviour
{
    
    [SerializeField, Tooltip("Gestor central del puzzle")]
    private DoorPuzzleManager doorPuzzleManager;


    [SerializeField, Tooltip("AudioSource con el sonido de madera rompiéndose (one-shot)")]
    private AudioSource sonidoRotura;

    [SerializeField, Tooltip("Partículas/debris de madera al romper (puede ser null)")]
    private GameObject efectoRotura;

    public bool Roto { get; private set; } = false;

    public void RecibirImpacto()
    {
        if (Roto) return;

        Roto = true;

        if (efectoRotura != null)
        {
            Instantiate(efectoRotura, transform.position, transform.rotation);
        }
        if (sonidoRotura != null)
        {
            sonidoRotura.transform.SetParent(null);
            sonidoRotura.Play();
            Destroy(sonidoRotura.gameObject, sonidoRotura.clip != null ? sonidoRotura.clip.length + 0.1f : 2f);
        }

        Debug.Log($"[Tablero] Roto: {gameObject.name}");

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
