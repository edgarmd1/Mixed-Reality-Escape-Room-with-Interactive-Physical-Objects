using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class DMXController : MonoBehaviour
{
    private SerialPort serialPort;
    private byte[] dmxData = new byte[513];
    
    // Colores de prueba
    private Color[] colors = new Color[]
    {
        Color.red,
        Color.green,
        Color.blue,
        Color.yellow,
        Color.cyan,
        new Color(1f, 0.5f, 0f), // naranja
        Color.white
    };
    
    private int currentColor = 0;
    private float timer = 0f;
    public float colorChangInterval = 1.5f; // segundos entre cambios

    void Start()
    {
        try
        {
            serialPort = new SerialPort("COM6", 250000, Parity.None, 8, StopBits.Two);
            serialPort.Open();
            Debug.Log("Puerto COM6 abierto correctamente");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error abriendo COM6: " + e.Message);
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= colorChangInterval)
        {
            timer = 0f;
            SetBothLights(colors[currentColor]);
            currentColor = (currentColor + 1) % colors.Length;
        }
    }

    void SetBothLights(Color color)
    {
        byte r = (byte)(color.r * 255);
        byte g = (byte)(color.g * 255);
        byte b = (byte)(color.b * 255);

        // Foco 1 (dirección 1)
        dmxData[1] = r;
        dmxData[2] = g;
        dmxData[3] = b;
        dmxData[4] = 255;

        // Foco 2 (dirección 5)
        dmxData[5] = r;
        dmxData[6] = g;
        dmxData[7] = b;
        dmxData[8] = 255;

        SendDMX();
        
        Debug.Log($"Color: R={r} G={g} B={b}");
    }

    void SendDMX()
    {
        if (serialPort == null || !serialPort.IsOpen) return;

        try
        {
            serialPort.BreakState = true;
            Thread.Sleep(1);
            serialPort.BreakState = false;
            Thread.Sleep(1);

            dmxData[0] = 0; // Start code
            serialPort.Write(dmxData, 0, 513);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error enviando DMX: " + e.Message);
        }
    }

    void OnDestroy()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            // Apagar focos al salir
            SetBothLights(Color.black);
            serialPort.Close();
            Debug.Log("Puerto COM6 cerrado");
        }
    }
}