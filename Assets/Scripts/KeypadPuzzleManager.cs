using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

public class KeypadPuzzleManager : MonoBehaviour
{
    public enum EstadoKeypad
    {
        Inactivo,
        EsperandoCombo,
        ComboExito,
        EsperandoGiro,
        GemelasMoviendose,
        CamaraDisponible,
        PuzzleCompletado
    }

    [Header("Estado")]
    [SerializeField] private EstadoKeypad estadoActual = EstadoKeypad.Inactivo;

    [Header("Combinación")]
    [SerializeField, Tooltip("Combinación numérica correcta")]
    private string combinacionCorrecta = "1331";

    [Header("Arduino")]
    [SerializeField] private ArduinoLuz arduinoLuz;

    [Header("Armario")]
    [SerializeField, Tooltip("GameObject del armario cerrado")]
    private GameObject armarioCerrado;

    [SerializeField, Tooltip("GameObject del armario abierto")]
    private GameObject armarioAbierto;

    [Header("Cámara en vitrina")]
    [SerializeField, Tooltip("GameObject de la cámara dentro del armario abierto")]
    private GameObject camaraEnVitrina;

    [SerializeField, Tooltip("GameObject de la cámara en el punto destino")]
    private GameObject camaraEnDestino;

    [SerializeField, Tooltip("GameObject de la cámara que lleva el fantasma")]
    private GameObject camaraPortada;

    [Header("Detección de proximidad")]
    [SerializeField, Tooltip("Cámara principal del XR Origin ")]
    private Camera camaraPrincipal;

    [SerializeField, Tooltip("Distancia entre la cámara del jugador y la cámara en el armario")]
    private float distanciaActivacion = 1.5f;

    [Header("Pasillo")]
    [SerializeField, Tooltip("GameObject del pasillo")]
    private GameObject pasilloRoot;

    [Header("Fantasma")]
    [SerializeField, Tooltip("GameObject del fantasma")]
    private GameObject fantasmaRoot;

    [SerializeField, Tooltip("Velocidad de movimiento del fantasma")]
    private float velocidadFantasma = 1.2f;

    [SerializeField, Tooltip("Waypoints que sigue el fantasma desde el armario hasta el pasillo")]
    private List<Transform> waypointsFantasma = new List<Transform>();

    [SerializeField, Tooltip("Tiempo que el fantasma espera junto al armario antes de empezar a moverse")]
    private float pausaFantasmaEnArmario = 1.5f;

    [Header("Referencia sin usar por falta de tiempo")]
    [SerializeField, Tooltip("KnockPuzzleManager que se activa al coger la llave (old)")]
    private KnockPuzzleManager knockPuzzleManager;

    [Header("Cámara interactuable")]
    [SerializeField, Tooltip("CamaraInteractable que se habilita al terminar la secuencia del fantasma")]
    private CamaraInteractable camaraInteractable;

    [Header("Audio")]
    [SerializeField, Tooltip("Sonido de error al introducir la combinación incorrecta")]
    private AudioSource audioError;

    [SerializeField, Tooltip("Sonido de éxito al introducir la combinación correcta")]
    private AudioSource audioExito;

    [SerializeField, Tooltip("Sonido que se reproduce en bucle mientras el fantasma lleva la cámara")]
    private AudioSource audioFantasma;

    [Header("Focos DMX")]
    [SerializeField, Tooltip("Controlador DMX")]
    private DMXController dmxController;

    [SerializeField, Tooltip("Intervalo de parpadeo rojo de los focos")]
    private float intervaloParpadeoDMX = 0.15f;

    private Coroutine _coroutineParpadeoRojo;

    void Awake()
    {
        if (camaraEnDestino != null) camaraEnDestino.SetActive(false);
        if (camaraPortada != null) camaraPortada.SetActive(false);
        if (fantasmaRoot != null) fantasmaRoot.SetActive(false);
        if (armarioAbierto != null) armarioAbierto.SetActive(false);
    }

    void Start()
    {
        if (camaraPrincipal == null)
            camaraPrincipal = Camera.main;

        if (camaraEnVitrina != null)
            camaraEnVitrina.SetActive(true);
    }

    void OnEnable()
    {
        if (arduinoLuz != null)
            arduinoLuz.OnComboRecibido += OnComboArduino;
    }

    void OnDisable()
    {
        if (arduinoLuz != null)
            arduinoLuz.OnComboRecibido -= OnComboArduino;
    }

    void Update()
    {
#if UNITY_EDITOR
        SimularEntradaEditor();
#endif

        if (estadoActual == EstadoKeypad.EsperandoGiro)
            ComprobarProximidadCamara();
    }

    public void IniciarPuzzle()
    {
        if (estadoActual != EstadoKeypad.Inactivo) return;

        estadoActual = EstadoKeypad.EsperandoCombo;
    }

    public void OnCamaraCogida()
    {
        if (estadoActual != EstadoKeypad.CamaraDisponible) return;

        estadoActual = EstadoKeypad.PuzzleCompletado;

        if (knockPuzzleManager != null)
            knockPuzzleManager.IniciarPuzzleGolpes();
    }


    public EstadoKeypad Estado => estadoActual;

    private void OnComboArduino(string combo)
    {
        if (estadoActual != EstadoKeypad.EsperandoCombo) return;

        if (combo == combinacionCorrecta)
        {
            audioExito?.Play();
            StartCoroutine(SecuenciaExito());
        }
        else
        {
            audioError?.Play();
        }
    }

    private IEnumerator SecuenciaExito()
    {
        estadoActual = EstadoKeypad.ComboExito;

        if (armarioCerrado != null)
        {
            armarioCerrado.SetActive(false);
        }
        if (armarioAbierto != null)
        {
            armarioAbierto.SetActive(true);
        }

        if (camaraEnVitrina != null)
        {
            camaraEnVitrina.SetActive(true);
        }

        yield return new WaitForSeconds(1.0f);

        estadoActual = EstadoKeypad.EsperandoGiro;
    }

    private float _logTimerCamara = 0f;

    private void ComprobarProximidadCamara()
    {
        if (camaraPrincipal == null) return;
        if (camaraEnVitrina == null || !camaraEnVitrina.activeInHierarchy) return;

        float dist = Vector3.Distance(camaraPrincipal.transform.position, camaraEnVitrina.transform.position);

#if UNITY_EDITOR
        _logTimerCamara += Time.deltaTime;
        if (_logTimerCamara >= 1f)
        {
            _logTimerCamara = 0f;
        }
#endif

        if (dist <= distanciaActivacion)
        {
            estadoActual = EstadoKeypad.GemelasMoviendose;

            if (pasilloRoot != null)
            {
                pasilloRoot.SetActive(true);
            }

            StartCoroutine(SecuenciaFantasma());
        }
    }


    private IEnumerator SecuenciaFantasma()
    {
        if (waypointsFantasma == null || waypointsFantasma.Count == 0)
        {
            FinalizarSecuenciaFantasma();
            yield break;
        }

        if (fantasmaRoot == null)
        {
            FinalizarSecuenciaFantasma();
            yield break;
        }

        fantasmaRoot.transform.position = waypointsFantasma[0].position;
        fantasmaRoot.SetActive(true);

        yield return new WaitForSeconds(pausaFantasmaEnArmario);

        if (camaraEnVitrina != null)
        {
            camaraEnVitrina.SetActive(false);
        }
        if (camaraPortada != null)
            camaraPortada.SetActive(true);

        if (audioFantasma != null)
        {
            audioFantasma.loop = true;
            audioFantasma.Play();
        }

        if (dmxController != null)
            _coroutineParpadeoRojo = StartCoroutine(CoroutineParpadeoRojoDMX());

        for (int i = 1; i < waypointsFantasma.Count; i++)
        {
            Transform destino = waypointsFantasma[i];
            if (destino == null) continue;

            while (Vector3.Distance(fantasmaRoot.transform.position, destino.position) > 0.05f)
            {
                fantasmaRoot.transform.position = Vector3.MoveTowards(
                    fantasmaRoot.transform.position,
                    destino.position,
                    velocidadFantasma * Time.deltaTime
                );

                if (camaraPortada != null && camaraPortada.transform.parent != fantasmaRoot.transform)
                    camaraPortada.transform.position = fantasmaRoot.transform.position;

                yield return null;
            }

            fantasmaRoot.transform.position = destino.position;
        }

        if (audioFantasma != null)
        {
            audioFantasma.Stop();
            audioFantasma.loop = false;
        }

        if (_coroutineParpadeoRojo != null)
        {
            StopCoroutine(_coroutineParpadeoRojo);
            _coroutineParpadeoRojo = null;
        }
        if (dmxController != null)
            dmxController.Apagar();

        FinalizarSecuenciaFantasma();
    }

    private void FinalizarSecuenciaFantasma()
    {
        if (fantasmaRoot != null)
            fantasmaRoot.SetActive(false);
        if (camaraPortada != null)
            camaraPortada.SetActive(false);

        if (camaraEnDestino != null)
            camaraEnDestino.SetActive(true);

        camaraInteractable?.HabilitarGrab();

        estadoActual = EstadoKeypad.CamaraDisponible;
    }

    private IEnumerator CoroutineParpadeoRojoDMX()
    {
        bool flashActivo = false;
        while (true)
        {
            flashActivo = !flashActivo;
            dmxController.SetBrilloRojo(flashActivo ? (byte)255 : (byte)0);
            float espera = flashActivo ? (intervaloParpadeoDMX * 0.4f) : (intervaloParpadeoDMX * 0.6f);
            yield return new WaitForSeconds(espera);
        }
    }

#if UNITY_EDITOR
    private void SimularEntradaEditor()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.f4Key.wasPressedThisFrame)
        {
            IniciarPuzzle();
        }

        if (kb.f5Key.wasPressedThisFrame)
        {
            if (estadoActual == EstadoKeypad.Inactivo)
            {
                IniciarPuzzle();
            }

            if (estadoActual == EstadoKeypad.EsperandoCombo)
            {
                OnComboArduino(combinacionCorrecta);
            }
            else
            {
                
            }
        }

        if (kb.f6Key.wasPressedThisFrame && estadoActual == EstadoKeypad.EsperandoCombo)
        {
            OnComboArduino("0000");
        }

        if (kb.f7Key.wasPressedThisFrame && estadoActual == EstadoKeypad.CamaraDisponible)
        {
            OnCamaraCogida();
        }

        if (kb.f9Key.wasPressedThisFrame)
        {
            var inserter = FindObjectOfType<KeyInserter>();
            if (inserter != null)
                inserter.SimularInsercion();
        }

        if (kb.f8Key.wasPressedThisFrame && estadoActual == EstadoKeypad.EsperandoGiro)
        {
            estadoActual = EstadoKeypad.GemelasMoviendose;
            if (pasilloRoot != null) pasilloRoot.SetActive(true);
            StartCoroutine(SecuenciaFantasma());
        }
    }
#endif
}
