using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class BandejaHacha : MonoBehaviour
{
    // ── Referencia directa al hacha ────────────────────────────────────────
    [Header("Hacha")]
    [SerializeField, Tooltip("El AxeGrabController del hacha (arrástralo desde la jerarquía)")]
    private AxeGrabController hachaController;

    [SerializeField, Tooltip(
        "Distancia máxima (metros) entre el centro de la bandeja y el hacha " +
        "para considerar que ha sido depositada.")]
    private float radioDeposito = 0.35f;

    // ── Punto central de detección ─────────────────────────────────────────
    [Header("Zona")]
    [SerializeField, Tooltip(
        "Transform que marca el centro de la zona de depósito. " +
        "Si se deja vacío se usa el transform de este GameObject.")]
    private Transform centroZona;

    // ── Audio ──────────────────────────────────────────────────────────────
    [Header("Audio")]
    [SerializeField, Tooltip("Sonido de confirmación al depositar el hacha en la bandeja")]
    private AudioSource audioDeposito;

    // ── Visual (opcional) ──────────────────────────────────────────────────
    [Header("Visual (opcional)")]
    [SerializeField, Tooltip("GameObject de highlight que se muestra mientras espera la hacha")]
    private GameObject efectoHighlight;

    // ── Estado ────────────────────────────────────────────────────────────
    /// <summary>True en cuanto el hacha ha sido depositada (y suelta) correctamente.</summary>
    public bool HachaDepositada { get; private set; } = false;

    private bool _activa = false;

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (efectoHighlight != null) efectoHighlight.SetActive(false);

        // Diagnóstico
        if (hachaController == null)
            Debug.LogWarning("[BandejaHacha] ¡CAMPO NULO! 'Hacha Controller' no asignado en Inspector.");
        else
            Debug.Log($"[BandejaHacha] hachaController OK → {hachaController.name}");

        if (centroZona == null)
        {
            centroZona = transform;
            Debug.Log("[BandejaHacha] centroZona no asignado – usando transform de este GameObject.");
        }
    }

    void Update()
    {
        if (_activa && !HachaDepositada)
            ComprobarDepositoPorDistancia();

#if UNITY_EDITOR
        SimularDepositoEditor();
#endif
    }

    public void Activar()
    {
        if (_activa) return;
        _activa = true;

        if (efectoHighlight != null) efectoHighlight.SetActive(true);

        Debug.Log("[BandejaHacha] Bandeja activada – esperando que el jugador deposite el hacha.");
    }

    // ── Detección por distancia ───────────────────────────────────────────

    private void ComprobarDepositoPorDistancia()
    {
        if (hachaController == null) return;

        float dist = Vector3.Distance(centroZona.position, hachaController.transform.position);

        if (dist <= radioDeposito)
        {
            if (hachaController.EstaEnMano)
            {
                // El jugador todavía lleva el hacha – esperar a que la suelte
                return;
            }

            Debug.Log($"[BandejaHacha] Hacha a {dist:F3}m del centro → ¡Depositada!");
            ConfirmarDeposito();
        }
    }

    private void ConfirmarDeposito()
    {
        HachaDepositada = true;
        _activa = false;

        if (efectoHighlight != null) efectoHighlight.SetActive(false);

        audioDeposito?.Play();

        Debug.Log("[BandejaHacha] ¡Hacha depositada correctamente en la bandeja!");
    }

    // ── Simulación en Editor ──────────────────────────────────────────────

#if UNITY_EDITOR
    private void SimularDepositoEditor()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.bKey.wasPressedThisFrame && _activa && !HachaDepositada)
        {
            Debug.Log("[BandejaHacha] (Editor) B → Simulando depósito del hacha.");
            ConfirmarDeposito();
        }
    }
#endif

    // ── Gizmos ────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Vector3 centro = (centroZona != null) ? centroZona.position : transform.position;

        Gizmos.color = HachaDepositada
            ? new Color(0f, 1f, 0f, 0.5f)
            : (_activa ? new Color(1f, 0.8f, 0f, 0.5f) : new Color(0.8f, 0.8f, 0.8f, 0.3f));

        Gizmos.DrawWireSphere(centro, radioDeposito);
        Gizmos.DrawSphere(centro, 0.03f);
    }
}
