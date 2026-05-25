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

    private volatile bool luzDetectada = false;
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
                else if (int.TryParse(valor, out int luz))
                {
                    // Valor del sensor de luz
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
        if (habilitado && luzDetectada && !puzzleCompletado)
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
    }

    void ActivarTransicion()
    {
        puzzleCompletado = true;
        luzDetectada = false;

        cameraCullingMaskController?.SetMode(false);
    }

    void OnDestroy()
    {
        if (puerto.IsOpen) puerto.Close();
        hiloSerie?.Join(200);
    }
}
