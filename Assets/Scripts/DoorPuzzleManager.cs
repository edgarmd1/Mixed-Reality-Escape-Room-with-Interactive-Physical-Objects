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

    [SerializeField, Tooltip("Audio que le indica al jugador que debe devolver el hacha a su sitio")]
    private AudioSource audioDevuelveHacha;

    [SerializeField, Tooltip("CamaraInteractable que se desbloquea al romper todos los tablones")]
    private CamaraInteractable camaraInteractable;

    private int _tablonesRotos = 0;
    private bool _puzzleCompletado = false;

    void Awake()
    {
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
            camaraInteractable?.HabilitarGrab();
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
            }
        }

        if (_tablonesRotos >= tablones.Length)
        {
            StartCoroutine(CompletarPuzzle());
            return;
        }
    }

    public void NotificarTablaRota()
    {
        if (_puzzleCompletado) return;

        _tablonesRotos++;

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
            audioDevuelveHacha.Play();
        }

        camaraInteractable?.HabilitarGrab();
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
            return;
        }

        foreach (int idx in DoorPuzzleState.TablonesRotosIndices)
        {
            if (idx >= 0 && idx < tablones.Length && tablones[idx] != null)
            {
                tablones[idx].gameObject.SetActive(false);
            }
        }
    }
}
