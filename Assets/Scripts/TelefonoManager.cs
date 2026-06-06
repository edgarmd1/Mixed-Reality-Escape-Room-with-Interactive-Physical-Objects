using System.Collections;
using UnityEngine;

public class TelefonoManager : MonoBehaviour
{
    [SerializeField] private ArduinoLuz arduinoLuz;

    [Header("Audio")]
    [SerializeField] private AudioSource telefonoSonando;
    [SerializeField] private AudioSource vozAudio;
    [SerializeField, Tooltip("Audio relfexion despues llamads")]
    private AudioSource audioReflexion;

    [Header("Timings")]
    [SerializeField] private float retrasoPrimeroSonido = 1.2f;
    [SerializeField] private float fadeSalidaTelefono = 0.4f;

    [Header("Puzzle siguiente")]
    [SerializeField, Tooltip("Gestor del nuevo puzzle de teclado matricial que se activa al terminar la voz")]
    private KeypadPuzzleManager keypadPuzzleManager;

    private bool _telefonoActivo  = false;
    private bool _vozActivada     = false;

    void Update()
    {
        if (!_telefonoActivo || _vozActivada) return;

        bool descolgado = false;

#if UNITY_EDITOR
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.tKey.wasPressedThisFrame) descolgado = true;
#endif

        if (arduinoLuz != null && arduinoLuz.TelefonoDescolgado)
            descolgado = true;

        if (descolgado)
            StartCoroutine(ActivarVoz());
    }

    public void IniciarTelefono()
    {
        if (_telefonoActivo) return;

        _telefonoActivo = true;

        if (arduinoLuz != null)
            arduinoLuz.HabilitarTelefono();

        StartCoroutine(EmpezarSonidoTelefono());
        Debug.Log("[Telefono] Teléfono iniciado – esperando que el jugador descuelgue el auricular.");
    }

    private IEnumerator EmpezarSonidoTelefono()
    {
        yield return new WaitForSeconds(retrasoPrimeroSonido);

        if (telefonoSonando != null)
        {
            telefonoSonando.loop = true;
            telefonoSonando.Play();
        }
    }

    private IEnumerator ActivarVoz()
    {
        _vozActivada = true;
        Debug.Log("[Telefono] Auricular descolgado – reproduciendo voz del Hotel Overlook.");

        if (telefonoSonando != null && telefonoSonando.isPlaying)
        {
            float volInicial = telefonoSonando.volume;
            float t = 0f;
            while (t < fadeSalidaTelefono)
            {
                t += Time.deltaTime;
                telefonoSonando.volume = Mathf.Lerp(volInicial, 0f, t / fadeSalidaTelefono);
                yield return null;
            }
            telefonoSonando.Stop();
            telefonoSonando.volume = volInicial;
        }

        if (vozAudio != null)
            vozAudio.Play();

        // Esperar a que termine el audio de la voz
        yield return new WaitUntil(() => vozAudio == null || !vozAudio.isPlaying);

        if (audioReflexion != null)
        {
            audioReflexion.Play();
            yield return new WaitUntil(() => !audioReflexion.isPlaying);
        }

        Debug.Log("[Telefono] Activando puzzle de teclado matricial.");

        // Activar el puzzle de teclado matricial
        if (keypadPuzzleManager != null)
            keypadPuzzleManager.IniciarPuzzle();
    }
}

