using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;

/// <summary>
/// Controlador DMX para Enttec Open DMX USB (chip FTDI).
/// Envía DMX solo cuando cambia el estado. El receptor mantiene el último
/// valor recibido sin necesitar refresh continuo, evitando el flickering
/// causado por los breaks frecuentes del protocolo DMX en USB.
/// </summary>
public class DMXController : MonoBehaviour
{
    [SerializeField] private string puertoCOM = "COM6";
    [SerializeField] private int    baudRate  = 250000;

    private SerialPort _serialPort;
    private byte[]     _dmxData = new byte[513];

    // ── Ciclo de colores de prueba (solo para tests) ──────────────────────────
    [Header("Prueba (desactivar en producción)")]
    [SerializeField] private bool  cicloColoresPrueba = false;
    [SerializeField] private float colorChangInterval  = 1.5f;
    private Color[] _colors = { Color.red, Color.green, Color.blue,
                                 Color.yellow, Color.cyan,
                                 new Color(1f, 0.5f, 0f), Color.white };
    private int   _currentColor = 0;
    private float _colorTimer   = 0f;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        try
        {
            _serialPort = new SerialPort(puertoCOM, baudRate, Parity.None, 8, StopBits.Two);
            _serialPort.Open();
            Debug.Log($"[DMX] Puerto {puertoCOM} abierto.");

            // Encender focos en blanco desde el primer momento
            SendColor(255, 255, 255);
            Debug.Log("[DMX] Focos encendidos en blanco.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[DMX] Error abriendo {puertoCOM}: {e.Message}");
        }
    }

    // ── Update (ciclo de prueba opcional) ─────────────────────────────────────
    void Update()
    {
        if (!cicloColoresPrueba) return;
        _colorTimer += Time.deltaTime;
        if (_colorTimer >= colorChangInterval)
        {
            _colorTimer = 0f;
            Color c = _colors[_currentColor];
            SendColor((byte)(c.r * 255), (byte)(c.g * 255), (byte)(c.b * 255));
            _currentColor = (_currentColor + 1) % _colors.Length;
        }
    }

    // ── API pública ───────────────────────────────────────────────────────────

    /// <summary>Enciende ambos focos en blanco a máxima intensidad.</summary>
    public void EncenderBlanco()
    {
        cicloColoresPrueba = false;
        SendColor(255, 255, 255);
    }

    /// <summary>Apaga ambos focos.</summary>
    public void Apagar()
    {
        cicloColoresPrueba = false;
        SendColor(0, 0, 0);
    }

    /// <summary>Establece el brillo blanco de ambos focos (0-255).
    /// Llamado por IntroSequenceManager para sincronizar el parpadeo.</summary>
    public void SetBrilloBlanco(byte brillo)
    {
        cicloColoresPrueba = false;
        SendColor(brillo, brillo, brillo);
    }

    // ── Envío DMX ─────────────────────────────────────────────────────────────

    private void SendColor(byte r, byte g, byte b)
    {
        // Foco 1 (dirección 1)
        _dmxData[1] = r; _dmxData[2] = g; _dmxData[3] = b; _dmxData[4] = 255;
        // Foco 2 (dirección 5)
        _dmxData[5] = r; _dmxData[6] = g; _dmxData[7] = b; _dmxData[8] = 255;
        SendDMX();
    }

    private void SendDMX()
    {
        if (_serialPort == null || !_serialPort.IsOpen) return;
        try
        {
            _serialPort.BreakState = true;
            Thread.Sleep(1);
            _serialPort.BreakState = false;
            Thread.Sleep(1);

            _dmxData[0] = 0;
            _serialPort.Write(_dmxData, 0, 513);
        }
        catch (Exception e)
        {
            Debug.LogError($"[DMX] Error enviando: {e.Message}");
        }
    }

    void OnDestroy()
    {
        if (_serialPort != null && _serialPort.IsOpen)
        {
            SendColor(0, 0, 0);
            _serialPort.Close();
            Debug.Log($"[DMX] Puerto {puertoCOM} cerrado.");
        }
    }
}