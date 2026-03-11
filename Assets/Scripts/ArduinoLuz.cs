using UnityEngine;
using System.IO.Ports;
using System.Threading;

public class ArduinoLuz : MonoBehaviour
{
    SerialPort puerto = new SerialPort("COM4", 9600);

    [Header("Configuración de Luz")]
    public int umbralActivacion = 700;
    public bool puzzleCompletado = false;

    [Header("Referencias de Escena")]
    public CameraCullingMaskController cameraCullingMaskController;

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
        while (puerto.IsOpen && !puzzleCompletado)
        {
            try
            {
                string valor = puerto.ReadLine();
                if (int.TryParse(valor.Trim(), out int luz))
                {
                    if (luz >= umbralActivacion)
                        luzDetectada = true;
                }
            }
            catch (System.TimeoutException) { /* normal, sin datos */ }
            catch (System.Exception) { break; }
        }
    }

    void Update()
    {
        if (luzDetectada && !puzzleCompletado)
        {
            ActivarTransicion();
        }
    }

    void ActivarTransicion()
    {
        puzzleCompletado = true;
        luzDetectada = false;

        cameraCullingMaskController?.SetMode(false);

        puerto.Close();
    }

    void OnDestroy()
    {
        puerto.Close();
        hiloSerie?.Join(200);
    }
}
