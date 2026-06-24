using System.Collections;
using UnityEngine;

public class VitrineManager : MonoBehaviour
{
    public enum EstadoVitrine
    {
        Inactivo,
        Listo,
        AudioContexto,
        EsperandoTelefono,
        TelefonoActivado
    }

    [Header("Estado")]
    [SerializeField] private EstadoVitrine estadoActual = EstadoVitrine.Inactivo;

    [Header("Detección de proximidad")]
    [SerializeField, Tooltip("Cámara principal del XR Origin")]
    private Camera camaraPrincipal;
    [SerializeField, Tooltip("Distancia a la que el jugador activa el audio de contexto")]
    private float distanciaActivacion = 2f;
    [SerializeField, Tooltip("Transform para medir la distancia")]
    private Transform puntoReferencia;

    [Header("Audio de contexto")]
    [SerializeField, Tooltip("Audio")]
    private AudioSource audioContexto;

    [Header("Teléfono")]
    [SerializeField, Tooltip("Gestor del teléfono")]
    private TelefonoManager telefonoManager;
    [SerializeField, Tooltip("Segundos de pausa tras el audio")]
    private float pausaAntesTelefono = 2f;

    [Header("Testing / Debug")]
    [SerializeField, Tooltip("Si está activo, la vitrina se activa automáticamente al Start.")]
    private bool activarDesdeStart = false;

    void Start()
    {
        if (camaraPrincipal == null)
            camaraPrincipal = Camera.main;

        if (activarDesdeStart)
            Activar();
    }

    private float _logTimer = 0f;

    void Update()
    {
#if UNITY_EDITOR
        SimularEntradaEditor();
#endif
        if (estadoActual != EstadoVitrine.Listo) return;

        if (camaraPrincipal == null) return;

        Vector3 origen = puntoReferencia != null ? puntoReferencia.position : transform.position;
        float dist = Vector3.Distance(camaraPrincipal.transform.position, origen);

#if UNITY_EDITOR
        _logTimer += Time.deltaTime;
        if (_logTimer >= 1f)
        {
            _logTimer = 0f;
        }
#endif

        if (dist <= distanciaActivacion)
        {
            StartCoroutine(SecuenciaAudioContexto());
        }
    }

    public void Activar()
    {
        if (estadoActual != EstadoVitrine.Inactivo) return;
        estadoActual = EstadoVitrine.Listo;
    }

    private IEnumerator SecuenciaAudioContexto()
    {
        estadoActual = EstadoVitrine.AudioContexto;

        if (audioContexto != null && audioContexto.clip != null)
        {
            audioContexto.Play();
            yield return new WaitUntil(() => audioContexto == null || !audioContexto.isPlaying);
        }

        estadoActual = EstadoVitrine.EsperandoTelefono;
        yield return new WaitForSeconds(pausaAntesTelefono);

        ActivarTelefono();
    }

    private void ActivarTelefono()
    {
        estadoActual = EstadoVitrine.TelefonoActivado;

        if (telefonoManager != null)
        {
            telefonoManager.IniciarTelefono();
        }
    }

#if UNITY_EDITOR
    private void SimularEntradaEditor()
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        if (kb.vKey.wasPressedThisFrame)
        {
            if (estadoActual == EstadoVitrine.Inactivo)
            {
                Activar();
            }
            else if (estadoActual == EstadoVitrine.Listo)
            {
                StartCoroutine(SecuenciaAudioContexto());
            }
        }
    }
#endif
}
