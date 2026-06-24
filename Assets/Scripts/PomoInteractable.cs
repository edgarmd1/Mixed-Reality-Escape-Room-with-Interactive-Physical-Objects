using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class PomoInteractable : MonoBehaviour
{
    [Header("Puerta")]
    [SerializeField, Tooltip("GameObject de la puerta cerrada")]
    private GameObject puertaCerrada;

    [SerializeField, Tooltip("GameObject de la puerta abierta")]
    private GameObject puertaAbierta;

    [Header("Audio")]
    [SerializeField, Tooltip("Sonido de puerta abriéndose")]
    private AudioSource audioApertura;

    private XRSimpleInteractable _interactable;
    private bool _abierta = false;

    void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();
    }

    void OnEnable()
    {
        _interactable.hoverEntered.AddListener(OnHover);
    }

    void OnDisable()
    {
        _interactable.hoverEntered.RemoveListener(OnHover);
    }

    private void OnHover(HoverEnterEventArgs args)
    {
        AbrirPuerta();
    }

    void OnTriggerEnter(Collider other)
    {
        AbrirPuerta();
    }

    private void AbrirPuerta()
    {
        if (_abierta) return;
        _abierta = true;

        if (puertaCerrada != null)
            puertaCerrada.SetActive(false);

        if (puertaAbierta != null)
            puertaAbierta.SetActive(true);

        if (audioApertura != null)
            audioApertura.Play();
    }

#if UNITY_EDITOR
    [ContextMenu("Simular toque pomo (Editor)")]
    private void SimularToque() => AbrirPuerta();
#endif
}
