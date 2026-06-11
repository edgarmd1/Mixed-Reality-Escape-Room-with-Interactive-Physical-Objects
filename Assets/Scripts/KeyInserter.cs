using System.Collections;
using UnityEngine;

public class KeyInserter : MonoBehaviour
{
    [Header("Llave")]
    [SerializeField, Tooltip("XRGrabInteractable de la llave")]
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable llaveInteractable;

    [SerializeField, Tooltip("Radio para detectar la llave cerca de la cerradura")]
    private float radioDeteccion = 0.12f;

    [Header("Puerta trasera")]
    [SerializeField, Tooltip("Objeto de la puerta cerrada")]
    private GameObject puertaCerrada;

    [SerializeField, Tooltip("Objeto de la puerta abierta")]
    private GameObject puertaAbierta;

    [Header("Audio")]
    [SerializeField, Tooltip("Sonido de la llave en la cerradura")]
    private AudioSource audioCerradura;

    [SerializeField, Tooltip("Sonido de la puerta abriéndose")]
    private AudioSource audioApertura;

    [Header("Timing")]
    [SerializeField, Tooltip("Segundos entre el clic de cerradura y que la puerta se abra")]
    private float retardoApertura = 1.2f;

    private bool _insertado = false;

    void Update()
    {
        if (_insertado) return;
        if (llaveInteractable == null) return;

        if (!llaveInteractable.isSelected) return;

        float dist = Vector3.Distance(llaveInteractable.transform.position, transform.position);
        if (dist <= radioDeteccion)
        {
            _insertado = true;
            Debug.Log($"[KeyInserter] Llave insertada (dist={dist:F3}m). Abriendo puerta...");
            StartCoroutine(SecuenciaApertura());
        }
    }

    private IEnumerator SecuenciaApertura()
    {
        llaveInteractable.enabled = false;
        llaveInteractable.transform.SetParent(transform);
        llaveInteractable.transform.localPosition = Vector3.zero;
        llaveInteractable.transform.localRotation = Quaternion.identity;
        audioCerradura?.Play();

        yield return new WaitForSeconds(retardoApertura);
        audioApertura?.Play();

        if (puertaCerrada != null) puertaCerrada.SetActive(false);
        if (puertaAbierta != null) puertaAbierta.SetActive(true);

        Debug.Log("[KeyInserter] Puerta trasera abierta.");
    }

    public void SimularInsercion()
    {
        if (_insertado) return;
        _insertado = true;
        Debug.Log("[KeyInserter] Inserción simulada desde editor.");
        StartCoroutine(SecuenciaApertura());
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
        Gizmos.DrawIcon(transform.position, "d_Prefab Icon", true);
    }
}
