using UnityEngine;
using System.IO.Ports; 

public class ControlArduino : MonoBehaviour
{
    SerialPort port = new SerialPort("/dev/cu.usbmodem1101", 9600);

    void Start() {
        port.Open();
        port.ReadTimeout = 100;
    }

    public void EncederLED() {
        port.Write("1");
    }

    public void ApagarLED() {
        port.Write("0");
    }

    void OnApplicationQuit() {
        port.Close();
    }
}