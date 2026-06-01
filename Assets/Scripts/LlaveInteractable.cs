using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class LlaveInteractable : MonoBehaviour
{
    [Header("Al coger la llave")]
    [SerializeField, Tooltip("Padre que contiene la pared y la puerta trasera (desactivado por defecto)")]
    private GameObject puertaTraseraRoot;

    [SerializeField, Tooltip("AudioSource con el clip de portazo")]
    private AudioSource audioPortazo;

    [SerializeField, Tooltip("Referencia al KeypadPuzzleManager (si es null se busca automáticamente)")]
    private KeypadPuzzleManager keypadManager;

    // ── Estado ────────────────────────────────────────────────────────────────
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;
    private bool _llaveCogida = false;

    // ── Ciclo de vida ─────────────────────────────────────────────────────────
    void Awake()
    {
        _grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    void OnEnable()
    {
        _grab.selectEntered.AddListener(OnGrabbed);
    }

    void OnDisable()
    {
        _grab.selectEntered.RemoveListener(OnGrabbed);
    }

    // ── Evento de agarre ──────────────────────────────────────────────────────
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (_llaveCogida) return;
        _llaveCogida = true;

        Debug.Log("[LlaveInteractable] Llave cogida por el jugador.");

        if (puertaTraseraRoot != null)
        {
            puertaTraseraRoot.SetActive(true);
            Debug.Log("[LlaveInteractable] Puerta trasera activada.");
        }
        else
        {
            Debug.LogWarning("[LlaveInteractable] 'puertaTraseraRoot' no asignado – la pared trasera no aparecerá.");
        }

        if (audioPortazo != null)
            audioPortazo.Play();
        else
            Debug.LogWarning("[LlaveInteractable] 'audioPortazo' no asignado – sin sonido de portazo.");

        if (keypadManager == null)
            keypadManager = FindObjectOfType<KeypadPuzzleManager>();

        if (keypadManager != null)
            keypadManager.OnLlaveCogida();
        else
            Debug.LogWarning("[LlaveInteractable] No se encontró KeypadPuzzleManager.");
    }
}
