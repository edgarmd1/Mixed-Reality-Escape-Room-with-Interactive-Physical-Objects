using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class BandejaHacha : MonoBehaviour
{
    // ── Detección ─────────────────────────────────────────────────────────
    [Header("Detección")]
    [SerializeField, Tooltip(
        "Collider trigger (BoxCollider con Is Trigger = true) que cubre la superficie " +
        "de la bandeja. Añádelo como componente adicional en este mismo GameObject o en " +
        "un hijo vacío, ya que el MeshCollider del FBX no puede ser trigger si es cóncavo.")]
    private Collider triggerZona;

    // ── Audio ─────────────────────────────────────────────────────────────
    [Header("Audio")]
    [SerializeField, Tooltip("Sonido de confirmación al depositar el hacha en la bandeja")]
    private AudioSource audioDeposito;

    // ── Estado ────────────────────────────────────────────────────────────
    public bool HachaDepositada { get; private set; } = false;

    private bool _activa = false;

    // ─────────────────────────────────────────────────────────────────────

    void Awake()
    {
        // La bandeja empieza inactiva
        if (triggerZona    != null) triggerZona.enabled = false;
    }

    void Update()
    {
#if UNITY_EDITOR
        SimularDepositoEditor();
#endif
    }

    public void Activar()
    {
        if (_activa) return;
        _activa = true;

        if (triggerZona    != null) triggerZona.enabled = true;

        Debug.Log("[BandejaHacha] Bandeja activada – esperando que el jugador deposite el hacha.");
    }

    // ── Detección de depósito ─────────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        ComprobarDeposito(other);
    }

    void OnTriggerStay(Collider other)
    {
        ComprobarDeposito(other);
    }

    private void ComprobarDeposito(Collider other)
    {
        if (!_activa || HachaDepositada) return;

        var hacha = other.GetComponent<AxeGrabController>()
                 ?? other.GetComponentInParent<AxeGrabController>();

        if (hacha == null) return;
        if (hacha.EstaEnMano)
        {
            Debug.Log("[BandejaHacha] Hacha detectada en la bandeja, pero el jugador aún la sostiene.");
            return;
        }

        ConfirmarDeposito();
    }

    private void ConfirmarDeposito()
    {
        HachaDepositada = true;

        if (triggerZona    != null) triggerZona.enabled = false;

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

    void OnDrawGizmos()
    {
        if (triggerZona == null) return;

        Gizmos.color = HachaDepositada
            ? new Color(0f, 1f, 0f, 0.3f)
            : (_activa ? new Color(1f, 0.8f, 0f, 0.4f) : new Color(1f, 1f, 1f, 0.15f));

        if (triggerZona is BoxCollider bc)
        {
            Gizmos.matrix = triggerZona.transform.localToWorldMatrix;
            Gizmos.DrawCube(bc.center, bc.size);
        }
    }
}
