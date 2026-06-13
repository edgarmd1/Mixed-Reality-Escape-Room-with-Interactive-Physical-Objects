using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRGrabInteractable))]
public class CamaraInteractable : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField, Tooltip("Sonido al coger la cámara")]
    private AudioSource audioCogida;

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
        Debug.Log("[CamaraInteractable] Cámara cogida.");

        if (audioCogida != null)
            audioCogida.Play();

        if (_rb != null)
            _rb.useGravity = false;
    }

    private void OnSoltada(SelectExitEventArgs args)
    {
        Debug.Log("[CamaraInteractable] Cámara soltada. Queda en la posición actual.");
    }
}
