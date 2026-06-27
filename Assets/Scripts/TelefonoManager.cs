using System.Collections;
using UnityEngine;

public class TelefonoManager : MonoBehaviour
{
    [SerializeField] private ArduinoLuz arduinoLuz;

    [Header("Audio")]
    [SerializeField] private AudioSource telefonoSonando;
    [SerializeField] private AudioSource vozAudio;
    [SerializeField, Tooltip("Audio reflexion despues de la llamada")]
    private AudioSource audioReflexion;
    [SerializeField, Tooltip("Audio recordatorio: se reproduce cuando el jugador vuelve a descolgar")]
    private AudioSource vozRecordatorio;

    [Header("Timings")]
    [SerializeField] private float retrasoPrimeroSonido = 1.2f;
    [SerializeField] private float fadeSalidaTelefono = 0.4f;

    [Header("Puzzle siguiente")]
    [SerializeField, Tooltip("Gestor del nuevo puzzle de teclado matricial que se activa al terminar la voz")]
    private KeypadPuzzleManager keypadPuzzleManager;

    private bool _telefonoActivo = false;
    private bool _vozActivada = false; 
    private bool _puzzleActivado = false;  
    private bool _recordandoVoz = false;   

    private bool _descolgadoAnterior = false;

    void Update()
    {
        if (_telefonoActivo && !_vozActivada)
        {
            bool descolgado = LeerDescolgado();
            if (descolgado)
                StartCoroutine(ActivarVoz());
            return;
        }

        if (_puzzleActivado && !_recordandoVoz)
        {
            bool puzzleResuelto = keypadPuzzleManager != null &&
                                  keypadPuzzleManager.Estado != KeypadPuzzleManager.EstadoKeypad.Inactivo &&
                                  keypadPuzzleManager.Estado != KeypadPuzzleManager.EstadoKeypad.EsperandoCombo;

            if (!puzzleResuelto && LeerDescolgado())
                StartCoroutine(ActivarRecordatorio());
        }
    }

    private bool LeerDescolgado()
    {
        bool resultado = false;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null && kb.tKey.wasPressedThisFrame) resultado = true;
        if (kb != null && kb.rKey.wasPressedThisFrame) resultado = true;

        if (arduinoLuz != null && arduinoLuz.TelefonoDescolgado)
            resultado = true;

        return resultado;
    }

    public void IniciarTelefono()
    {
        if (_telefonoActivo) return;

        _telefonoActivo = true;

        if (arduinoLuz != null)
            arduinoLuz.HabilitarTelefono();

        StartCoroutine(EmpezarSonidoTelefono());
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

        yield return FadeYPararTimbre();

        if (vozAudio != null)
            vozAudio.Play();

        yield return new WaitUntil(() => vozAudio == null || !vozAudio.isPlaying);

        if (audioReflexion != null)
        {
            audioReflexion.Play();
            yield return new WaitUntil(() => !audioReflexion.isPlaying);
        }

        if (keypadPuzzleManager != null)
            keypadPuzzleManager.IniciarPuzzle();

        _puzzleActivado = true;
        _descolgadoAnterior = LeerDescolgado();
    }

    private IEnumerator ActivarRecordatorio()
    {
        _recordandoVoz = true;

        yield return FadeYPararTimbre();

        if (vozRecordatorio != null)
            vozRecordatorio.Play();
        else if (vozAudio != null)
            vozAudio.Play();

        AudioSource audioActivo = vozRecordatorio != null ? vozRecordatorio : vozAudio;
        yield return new WaitUntil(() => audioActivo == null || !audioActivo.isPlaying);

        _descolgadoAnterior = LeerDescolgado();
        _recordandoVoz = false;
    }

    private IEnumerator FadeYPararTimbre()
    {
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
    }
}

