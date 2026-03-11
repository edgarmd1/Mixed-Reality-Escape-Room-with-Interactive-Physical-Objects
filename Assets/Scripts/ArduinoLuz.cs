using UnityEngine;
using System.IO.Ports;

public class ArduinoLuz : MonoBehaviour
{
    SerialPort puerto = new SerialPort("COM4", 9600);

    [Header("Configuración de Luz")]
    public int umbralActivacion = 700;
    public bool puzzleCompletado = false;

    [Header("Referencias de Escena")]
    public CameraCullingMaskController cameraCullingMaskController;

    void Start()
    {
        if (cameraCullingMaskController == null)
            cameraCullingMaskController = FindObjectOfType<CameraCullingMaskController>();

        if (!puerto.IsOpen)
        {
            puerto.ReadTimeout = 100;
            puerto.Open();
        }
    }

    void Update()
    {
        if (puerto.IsOpen && !puzzleCompletado)
        {
            try
            {
                string valor = puerto.ReadLine();
                int luz = int.Parse(valor);
                Debug.Log("Luz actual: " + luz);

                if (luz >= umbralActivacion)
                {
                    ActivarTransicion();
                }
            }
            catch (System.Exception) { }
        }
    }

    void ActivarTransicion()
    {
        puzzleCompletado = true;
        Debug.Log("¡Luz detectada! Desactivando passthrough...");

        // Desactiva passthrough → pasa a modo VR
        cameraCullingMaskController?.SetMode(false);

        puerto.Close();
    }
}
