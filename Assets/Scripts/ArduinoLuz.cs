using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class ArduinoLuz : MonoBehaviour
{
    SerialPort puerto = new SerialPort("COM4", 9600);

    public int umbralActivacion = 700;
    public bool puzzleCompletado = false;

    public bool habilitado = false;

    public CameraCullingMaskController cameraCullingMaskController;

    private bool _telefonoHabilitado = false;
    private volatile bool _senalTelefono = false;
    private bool _telefonoDescolgado = false;

    public bool TelefonoDescolgado => _telefonoDescolgado;

    public void HabilitarTelefono() => _telefonoHabilitado = true;

    private volatile bool _senalKnock = false;

    [Tooltip("Tiempo mínimo entre golpes (segundos). Evita rebotes del acelerómetro.")]
    public float debounceKnock = 0.2f;
    private float _ultimoKnockTime = -999f;

    public System.Action OnKnockDetected;

    public System.Action<string> OnComboRecibido;

    public System.Action OnLuzDetectada;

    private volatile string _comboRecibido = null;

    private volatile bool luzDetectada = false;
    private bool _luzEnProceso = false; // true mientras la secuencia de la polaroid está en marcha
    private Thread hiloSerie;

    void Start()
    {
        if (cameraCullingMaskController == null)
            cameraCullingMaskController = FindObjectOfType<CameraCullingMaskController>();

        if (!puerto.IsOpen)
        {
            puerto.ReadTimeout = 500;
            puerto.Open();
        }

        hiloSerie = new Thread(LeerSerie) { IsBackground = true };
        hiloSerie.Start();
    }

    void LeerSerie()
    {
        while (puerto.IsOpen)
        {
            try
            {
                string valor = puerto.ReadLine().Trim();

                if (valor == "PHONE")
                {
                    _senalTelefono = true;
                }
                else if (valor == "KNOCK")
                {
                    _senalKnock = true;
                }
                else if (valor.StartsWith("COMBO:"))
                {
                    _comboRecibido = valor.Substring(6).Trim();
                }
                else if (int.TryParse(valor, out int luz))
                {
                    if (luz >= umbralActivacion)
                        luzDetectada = true;
                }
            }
            catch (System.TimeoutException) { }
            catch (System.Exception) { break; }
        }
    }

    void Update()
    {
        if (habilitado && luzDetectada && !puzzleCompletado && !_luzEnProceso)
            ActivarTransicion();

        if (_telefonoHabilitado && _senalTelefono && !_telefonoDescolgado)
        {
            _telefonoDescolgado = true;
            Debug.Log("[ArduinoLuz] Señal PHONE recibida – teléfono descolgado.");
        }

        if (_senalKnock)
        {
            _senalKnock = false;
            if (Time.time - _ultimoKnockTime >= debounceKnock)
            {
                _ultimoKnockTime = Time.time;
                Debug.Log("[ArduinoLuz] Señal KNOCK recibida – golpe detectado.");
                OnKnockDetected?.Invoke();
            }
            else
            {
                Debug.Log($"[ArduinoLuz] KNOCK ignorado (debounce: {Time.time - _ultimoKnockTime:F3}s < {debounceKnock}s).");
            }
        }

        if (_comboRecibido != null)
        {
            string combo = _comboRecibido;
            _comboRecibido = null;
            Debug.Log($"[ArduinoLuz] Combo recibido del keypad: '{combo}'");
            OnComboRecibido?.Invoke(combo);
        }
    }

    void ActivarTransicion()
    {
        luzDetectada = false;
        _luzEnProceso = true;

        if (OnLuzDetectada != null)
        {
            // PolaroidJumpscareController hará el vuelo y, al terminar,
            // llamará a CompletarPuzzle() para despertar al IntroSequenceManager.
            OnLuzDetectada.Invoke();
        }
        else
        {
            // Fallback sin animación: comportamiento original
            Debug.LogWarning("[ArduinoLuz] OnLuzDetectada sin suscriptores – activando jumpscare directamente.");
            CompletarPuzzle();
            cameraCullingMaskController?.SetMode(false);
        }
    }

    /// <summary>
    /// Llamado por PolaroidJumpscareController cuando el vuelo termina.
    /// Pone puzzleCompletado = true, lo que despierta al IntroSequenceManager.
    /// </summary>
    public void CompletarPuzzle()
    {
        puzzleCompletado = true;
        _luzEnProceso = false;
    }

    void OnDestroy()
    {
        if (puerto.IsOpen) puerto.Close();
        hiloSerie?.Join(200);
    }
}
