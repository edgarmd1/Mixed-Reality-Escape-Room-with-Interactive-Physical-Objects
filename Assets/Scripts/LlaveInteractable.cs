using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable))]
public class LlaveInteractable : MonoBehaviour

//old
{
    [Header("Al coger la llave")]
    [SerializeField, Tooltip("Padre que contiene la pared y la puerta trasera (desactivado por defecto)")]
    private GameObject puertaTraseraRoot;

    [SerializeField, Tooltip("AudioSource con el clip de portazo")]
    private AudioSource audioPortazo;

    [SerializeField, Tooltip("Referencia al KeypadPuzzleManager (si es null se busca automáticamente)")]
    private KeypadPuzzleManager keypadManager;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;
    private bool _llaveCogida = false;

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

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (_llaveCogida) return;
        _llaveCogida = true;

        if (puertaTraseraRoot != null)
        {
            puertaTraseraRoot.SetActive(true);

            Transform par = puertaTraseraRoot.transform.parent;
            string parentInfo = par != null
                ? par.name + " (activeInHierarchy=" + par.gameObject.activeInHierarchy + ")"
                : "(sin padre)";
        }
        else
        {
          
        }

        if (audioPortazo != null)
            audioPortazo.Play();


        if (keypadManager == null)
            keypadManager = FindObjectOfType<KeypadPuzzleManager>();

        if (keypadManager != null)
            keypadManager.OnCamaraCogida();
    
    }
}
