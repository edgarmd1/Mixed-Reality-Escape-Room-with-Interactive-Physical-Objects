using UnityEngine;
using System.IO.Ports; // Necesario para el puerto serie

public class ArduinoController : MonoBehaviour
{
    SerialPort stream = new SerialPort("COM4", 9600); // AJUSTA TU PUERTO COM
    public Transform controllerTransform; // Arrastra aquí el mando de Meta
    public Transform arduinoTarget;     // Punto donde "está" la placa
    public float activationDistance = 0.2f; // 20 cm

    bool isOn = false;

    void Start() {
        stream.Open(); // Abrir conexión
        stream.ReadTimeout = 50;
    }

    void Update() {
        if (stream.IsOpen) {
            float distance = Vector3.Distance(controllerTransform.position, arduinoTarget.position);

            if (distance < activationDistance && !isOn) {
                stream.Write("1");
                isOn = true;
                Debug.Log("Cerca: Encendido");
            } 
            else if (distance >= activationDistance && isOn) {
                stream.Write("0");
                isOn = false;
                Debug.Log("Lejos: Apagado");
            }
        }
    }

    void OnApplicationQuit() {
        stream.Close(); // ¡Importante cerrar el puerto al salir!
    }
}