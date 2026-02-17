using UnityEngine;
using System.IO.Ports; 

public class ArduinoController : MonoBehaviour
{
    SerialPort stream = new SerialPort("COM4", 9600); 
    public Transform controllerTransform; 
    public Transform arduinoTarget;     
    public float activationDistance = 0.2f; 

    bool isOn = false;

    void Start() {
        stream.Open(); 
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
        stream.Close(); 
    }
}