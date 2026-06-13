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


    [SerializeField, Tooltip("Sonido al abrir el portal (todos los tablones rotos)")]
    private AudioSource sonidoPortalAbierto;

    [SerializeField, Tooltip("Audio que le indica al jugador que debe devolver el hacha a la bandeja")]
    private AudioSource audioDevuelveHacha;

    [SerializeField, Tooltip("Bandeja virtual donde el jugador debe depositar el hacha antes de que suene el teléfono")]
    private BandejaHacha bandejaHacha;

    private int _tablonesRotos = 0;
    private bool _puzzleCompletado = false;

    void Awake()
    {
        if (puertaRoot != null) puertaRoot.SetActive(false);
        if (hachaRoot != null)  hachaRoot.SetActive(false);

        for (int i = 0; i < tablones.Length; i++)
        {
            if (tablones[i] != null)
                tablones[i].indice = i;
        }
    }

    void Start()
    {
        
    }

    public void IniciarPuzzle()
    {
        if (DoorPuzzleState.PuzzleCompletado)
        {
            _puzzleCompletado = true;
            _tablonesRotos = tablones.Length;
            foreach (var t in tablones)
                if (t != null) t.gameObject.SetActive(false);
            Debug.Log("[DoorPuzzle] Puzzle ya completado.");
            return;
        }

        _tablonesRotos = 0;
        _puzzleCompletado = false;

        if (puertaRoot != null) puertaRoot.SetActive(true);
        if (hachaRoot  != null) hachaRoot.SetActive(true);

        for (int i = 0; i < tablones.Length; i++)
        {
            if (tablones[i] == null) continue;

            if (DoorPuzzleState.TablonesRotosIndices.Contains(i))
            {
                tablones[i].gameObject.SetActive(false);
                _tablonesRotos++;
                Debug.Log($"[DoorPuzzle] Tablero {i} restaurado como ya roto.");
            }
        }

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
        DoorPuzzleState.PuzzleCompletado = true;  

        yield return new WaitForSeconds(0.8f);

        sonidoPortalAbierto?.Play();

        if (sonidoPortalAbierto != null)
        {
            yield return new WaitUntil(() => !sonidoPortalAbierto.isPlaying);
            yield return new WaitForSeconds(0.3f);  
        }

        if (audioDevuelveHacha != null)
        {
            Debug.Log($"[DoorPuzzle] Reproduciendo audio 'devuelve hacha': {audioDevuelveHacha.clip?.name}");
            audioDevuelveHacha.Play();
        }
        else
        {
            Debug.LogWarning("[DoorPuzzle] audioDevuelveHacha no asignado.");
        }

        Debug.Log("[DoorPuzzle] ¡Tablones rotos! El teléfono se activará desde la vitrina, no desde aquí.");

        if (bandejaHacha != null)
        {
            bandejaHacha.Activar();
            Debug.Log("[DoorPuzzle] Bandeja activada.");
        }
    }

    public bool PuzzleCompletado => _puzzleCompletado;

    public int TablonesRotos    => _tablonesRotos;

    public void RestaurarEstadoTablonesRotos()
    {
        if (!_puzzleCompletado && DoorPuzzleState.TablonesRotosIndices.Count == 0) return;

        if (_puzzleCompletado || DoorPuzzleState.PuzzleCompletado)
        {
            foreach (var t in tablones)
                if (t != null) t.gameObject.SetActive(false);
            Debug.Log("[DoorPuzzle] RestaurarEstado: puzzle completado, todos los tablones ocultos.");
            return;
        }

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
