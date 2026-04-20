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
    }

    public void IniciarPuzzle()
    {
        _tablonesRotos = 0;
        _puzzleCompletado = false;

        if (puertaRoot != null) puertaRoot.SetActive(true);
        if (hachaRoot  != null) hachaRoot.SetActive(true);

        Debug.Log("[DoorPuzzle] Puzzle iniciado – puerta y hacha activadas.");
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

        yield return new WaitForSeconds(0.8f);

        sonidoPortalAbierto?.Play();

        Debug.Log("[DoorPuzzle] ¡Portal abierto! Activando teléfono...");

        if (telefonoManager != null)
            telefonoManager.IniciarTelefono();
    }

    public bool PuzzleCompletado => _puzzleCompletado;
    public int TablonesRotos    => _tablonesRotos;
}
