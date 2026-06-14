using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class CamaraInteractable : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField, Tooltip("Sonido al coger la cámara")]
    private AudioSource audioCogida;

    [Header("Al coger la cámara")]
    [SerializeField, Tooltip("Puerta trasera que se cierra al coger la cámara")]
    private GameObject puertaTraseraRoot;

    [SerializeField, Tooltip("GameObject de la habitación 217")]
    private GameObject habitacion217Root;

    [SerializeField, Tooltip("Sonido de portazo al cerrarse la puerta")]
    private AudioSource audioPortazo;

    private bool _camaraCogida = false;

    private XRGrabInteractable _grab;
    private Rigidbody _rb;

    void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _rb   = GetComponent<Rigidbody>();

        ConfigurarGrab();
    }

    void OnEnable()
    {
        _grab.selectEntered.AddListener(OnCogida);
        _grab.selectExited.AddListener(OnSoltada);
    }

    void OnDisable()
    {
        _grab.selectEntered.RemoveListener(OnCogida);
        _grab.selectExited.RemoveListener(OnSoltada);
    }

    private void ConfigurarGrab()
    {
        _grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        _grab.trackPosition = true;
        _grab.trackRotation = true;
        _grab.throwOnDetach = false;
        _grab.attachTransform = null;

        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity  = false;
        }
    }

    private void OnCogida(SelectEnterEventArgs args)
    {
        if (audioCogida != null)
            audioCogida.Play();

        if (_rb != null)
            _rb.useGravity = false;

        if (_camaraCogida) return;
        _camaraCogida = true;

        Debug.Log("[CamaraInteractable] Cámara cogida por primera vez.");

        if (puertaTraseraRoot != null)
        {
            puertaTraseraRoot.SetActive(true);
            Debug.Log("[CamaraInteractable] Puerta trasera cerrada activada.");
        }
        else
        {
            Debug.LogWarning("[CamaraInteractable] puertaTraseraRoot no asignado.");
        }

        if (habitacion217Root != null)
        {
            habitacion217Root.SetActive(true);
            Debug.Log("[CamaraInteractable] Habitación 217 activada.");
        }
        else
        {
            Debug.LogWarning("[CamaraInteractable] habitacion217Root no asignado.");
        }

        if (audioPortazo != null)
            audioPortazo.Play();
        else
            Debug.LogWarning("[CamaraInteractable] audioPortazo no asignado.");
    }

    private void OnSoltada(SelectExitEventArgs args)
    {
        Debug.Log("[CamaraInteractable] Cámara soltada. Queda en la posición actual.");
    }
}
