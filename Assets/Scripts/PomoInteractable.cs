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
        _interactable.selectEntered.AddListener(OnTocado);
    }

    void OnDisable()
    {
        _interactable.selectEntered.RemoveListener(OnTocado);
    }

    private void OnTocado(SelectEnterEventArgs args)
    {
        if (_abierta) return;
        _abierta = true;

        Debug.Log("[PomoInteractable] Pomo tocado.");

        if (puertaCerrada != null)
            puertaCerrada.SetActive(false);

        if (puertaAbierta != null)
            puertaAbierta.SetActive(true);

        if (audioApertura != null)
            audioApertura.Play();
        else
            Debug.LogWarning("[PomoInteractable] audioApertura no asignado.");
    }

#if UNITY_EDITOR
    [UnityEngine.ContextMenu("Simular toque pomo (Editor)")]
    private void SimularToque()
    {
        if (_abierta)
        {
            Debug.Log("[PomoInteractable] Ya abierta.");
            return;
        }
        OnTocado(null);
    }
#endif
}
