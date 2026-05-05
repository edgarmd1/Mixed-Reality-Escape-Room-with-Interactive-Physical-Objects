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

    // ── Tilt Switch (teléfono) ───────────────────────────────────────────
    // El Arduino envía "PHONE\n" cuando el Tilt Switch se cierra (auricular inclinado).
    private bool _telefonoHabilitado = false;
    private volatile bool _senalTelefono = false;
    private bool _telefonoDescolgado = false;

    /// <summary>True cuando el Tilt Switch ha enviado "PHONE" y el puzzle está habilitado.</summary>
    public bool TelefonoDescolgado => _telefonoDescolgado;

    /// <summary>Llamado por TelefonoManager para empezar a escuchar el Tilt Switch.</summary>
    public void HabilitarTelefono() => _telefonoHabilitado = true;

    // ── Acelerómetro (golpes en mesa) ────────────────────────────────────
    // El Arduino envía "KNOCK\n" cuando detecta un golpe en la mesa.
    private volatile bool _senalKnock = false;

    /// <summary>Evento disparado en el hilo principal cada vez que se detecta un golpe.</summary>
    public System.Action OnKnockDetected;

    // ── Serial ───────────────────────────────────────────────────────────
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
        // El bucle ya NO termina al completarse puzzleCompletado:
        // el puerto sigue abierto para leer la señal del Tilt Switch.
        while (puerto.IsOpen)
        {
            try
            {
                string valor = puerto.ReadLine().Trim();

                if (valor == "PHONE")
                {
                    // Señal del Tilt Switch: auricular inclinado (descolgado)
                    _senalTelefono = true;
                }
                else if (valor == "KNOCK")
                {
                    // Señal del acelerómetro: golpe en la mesa
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
        // ── Puzzle de luz ────────────────────────────────────────────────
        if (habilitado && luzDetectada && !puzzleCompletado)
            ActivarTransicion();

        // ── Tilt Switch (teléfono) ───────────────────────────────────────
        if (_telefonoHabilitado && _senalTelefono && !_telefonoDescolgado)
        {
            _telefonoDescolgado = true;
            Debug.Log("[ArduinoLuz] Señal PHONE recibida – teléfono descolgado.");
        }

        // ── Acelerómetro (golpes) ────────────────────────────────────────
        if (_senalKnock)
        {
            _senalKnock = false;
            OnKnockDetected?.Invoke();
            Debug.Log("[ArduinoLuz] Señal KNOCK recibida – golpe detectado.");
        }
    }

    void ActivarTransicion()
    {
        puzzleCompletado = true;
        luzDetectada = false;

        cameraCullingMaskController?.SetMode(false);

        // NOTA: NO cerramos el puerto aquí.
        // El hilo sigue leyendo para detectar la señal del Tilt Switch del teléfono.
    }

    void OnDestroy()
    {
        if (puerto.IsOpen) puerto.Close();
        hiloSerie?.Join(200);
    }
}
