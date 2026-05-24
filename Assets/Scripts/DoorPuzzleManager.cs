using System.Collections;
using UnityEngine;

public class DoorPuzzleManager : MonoBehaviour
{
    [SerializeField, Tooltip("Root GameObject de la puerta (Frame Door) con todos los tablones como hijos")]
    private GameObject puertaRoot;

    [SerializeField, Tooltip("Hacha que debe aparecer en escena junto con la puerta")]
    private GameObject hachaRoot;

    [SerializeField, Tooltip("Los 5 tablones destructibles")]
    private TableroDestructible[] tablones;

    [SerializeField, Tooltip("Gestor del teléfono que se activa al completar el puzzle")]
    private TelefonoManager telefonoManager;

    [SerializeField, Tooltip("Sonido al abrir el portal (todos los tablones rotos)")]
    private AudioSource sonidoPortalAbierto;

    private int _tablonesRotos = 0;
    private bool _puzzleCompletado = false;

    void Awake()
    {
        if (puertaRoot != null) puertaRoot.SetActive(false);
        if (hachaRoot != null)  hachaRoot.SetActive(false);

        // Asignar índices a cada tablero para que puedan persistir su estado
        for (int i = 0; i < tablones.Length; i++)
        {
            if (tablones[i] != null)
                tablones[i].indice = i;
        }
    }

    public void IniciarPuzzle()
    {
        // Si el puzzle ya estaba completado en una carga anterior, no reiniciar
        if (DoorPuzzleState.PuzzleCompletado)
        {
            _puzzleCompletado = true;
            _tablonesRotos = tablones.Length;
            // Desactivar todos los tablones (ya destruidos)
            foreach (var t in tablones)
                if (t != null) t.gameObject.SetActive(false);
            Debug.Log("[DoorPuzzle] Puzzle ya completado – restaurando estado destruido.");
            return;
        }

        // Restaurar tablones ya rotos en sesiones anteriores de esta carga
        _tablonesRotos = 0;
        _puzzleCompletado = false;

        if (puertaRoot != null) puertaRoot.SetActive(true);
        if (hachaRoot  != null) hachaRoot.SetActive(true);

        // Aplicar estado persistido: desactivar tablones que ya fueron rotos
        for (int i = 0; i < tablones.Length; i++)
        {
            if (tablones[i] == null) continue;

            if (DoorPuzzleState.TablonesRotosIndices.Contains(i))
            {
                // Este tablero ya fue roto antes – restaurar como destruido
                tablones[i].gameObject.SetActive(false);
                _tablonesRotos++;
                Debug.Log($"[DoorPuzzle] Tablero {i} restaurado como ya roto.");
            }
        }

        // Comprobar si ya estaban todos rotos antes de que el jugador hiciera nada
        if (_tablonesRotos >= tablones.Length)
        {
            StartCoroutine(CompletarPuzzle());
            return;
        }

        Debug.Log($"[DoorPuzzle] Puzzle iniciado – {_tablonesRotos}/{tablones.Length} tablones ya rotos.");
    }

    public void NotificarTablaRota()
    {
        if (_puzzleCompletado) return;

        _tablonesRotos++;
        Debug.Log($"[DoorPuzzle] Tablones rotos: {_tablonesRotos}/{tablones.Length}");

        if (_tablonesRotos >= tablones.Length)
            StartCoroutine(CompletarPuzzle());
    }

    private IEnumerator CompletarPuzzle()
    {
        _puzzleCompletado = true;
        DoorPuzzleState.PuzzleCompletado = true;  // Persistir entre recargas

        yield return new WaitForSeconds(0.8f);

        sonidoPortalAbierto?.Play();

        Debug.Log("[DoorPuzzle] ¡Portal abierto! Activando teléfono...");

        if (telefonoManager != null)
            telefonoManager.IniciarTelefono();
    }

    public bool PuzzleCompletado => _puzzleCompletado;
    public int TablonesRotos    => _tablonesRotos;

    /// <summary>
    /// Re-oculta los tablones que ya fueron rotos.
    /// Llamar después de que CameraCullingMask haya reactivado todos los objetos Mundo_Real,
    /// porque ese proceso pone todos los objetos a SetActive(true) indiscriminadamente.
    /// </summary>
    public void RestaurarEstadoTablonesRotos()
    {
        if (!_puzzleCompletado && DoorPuzzleState.TablonesRotosIndices.Count == 0) return;

        if (_puzzleCompletado || DoorPuzzleState.PuzzleCompletado)
        {
            // Puzzle completado: ocultar todos los tablones
            foreach (var t in tablones)
                if (t != null) t.gameObject.SetActive(false);
            Debug.Log("[DoorPuzzle] RestaurarEstado: puzzle completado, todos los tablones ocultos.");
            return;
        }

        // Ocultar solo los tablones que fueron rotos
        foreach (int idx in DoorPuzzleState.TablonesRotosIndices)
        {
            if (idx >= 0 && idx < tablones.Length && tablones[idx] != null)
            {
                tablones[idx].gameObject.SetActive(false);
                Debug.Log($"[DoorPuzzle] RestaurarEstado: tablero {idx} re-ocultado.");
            }
        }
    }
}
