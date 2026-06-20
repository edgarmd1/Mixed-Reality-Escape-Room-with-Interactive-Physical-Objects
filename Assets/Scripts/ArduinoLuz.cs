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
    private bool _telefonoDescolgadoPulso = false;
    public bool TelefonoDescolgado => _telefonoDescolgadoPulso;

    public void HabilitarTelefono() => _telefonoHabilitado = true;

    private volatile bool _senalKnock = false;

    [Tooltip("Tiempo mínimo entre golpes.")]
    public float debounceKnock = 0.2f;
    private float _ultimoKnockTime = -999f;

    public System.Action OnKnockDetected;

    public System.Action<string> OnComboRecibido;

    public System.Action OnLuzDetectada;

    public System.Action OnUmbralSuperado;

    private volatile string _comboRecibido = null;

    private volatile bool luzDetectada = false;
    private volatile bool _umbralSuperado = false;
    private bool _luzEnProceso = false;
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
                    {
                        luzDetectada = true;
                        _umbralSuperado = true;
                    }
                }
            }
            catch (System.TimeoutException) { }
            catch (System.Exception) { break; }
        }
    }

    void Update()
    {
        if (habilitado && _umbralSuperado && !puzzleCompletado)
        {
            _umbralSuperado = false;
            OnUmbralSuperado?.Invoke();
        }

        if (habilitado && luzDetectada && !puzzleCompletado && !_luzEnProceso)
            ActivarTransicion();

        _telefonoDescolgadoPulso = _telefonoHabilitado && _senalTelefono;
        if (_telefonoDescolgadoPulso)
        {
            _senalTelefono = false;
            Debug.Log("[ArduinoLuz] Señal PHONE recibida – teléfono descolgado (pulso).");
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
            OnLuzDetectada.Invoke();
        }
        else
        {
            Debug.LogWarning("[ArduinoLuz] OnLuzDetectada sin suscriptores");
            CompletarPuzzle();
            cameraCullingMaskController?.SetMode(false);
        }
    }

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
