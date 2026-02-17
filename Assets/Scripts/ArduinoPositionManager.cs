using UnityEngine;

public class ArduinoPositionManager : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField, Tooltip("Transform del CubeArduino a posicionar")]
    private Transform arduinoTransform;

    [SerializeField, Tooltip("Controlador izquierdo para calibración")]
    private Transform leftController;

    [Header("Estado")]
    [SerializeField, Tooltip("Muestra si el modo calibración está activo")]
    private bool isCalibrating = false;

    private const string KEY_POS_X = "Arduino_PosX";
    private const string KEY_POS_Y = "Arduino_PosY";
    private const string KEY_POS_Z = "Arduino_PosZ";
    private const string KEY_ROT_X = "Arduino_RotX";
    private const string KEY_ROT_Y = "Arduino_RotY";
    private const string KEY_ROT_Z = "Arduino_RotZ";
    private const string KEY_ROT_W = "Arduino_RotW";
    private const string KEY_HAS_POSITION = "Arduino_HasSavedPosition";

    void Start()
    {
        if (arduinoTransform == null)
        {
            arduinoTransform = transform;
            Debug.LogWarning("ArduinoPositionManager: arduinoTransform no asignado, usando el propio transform.");
        }

        if (leftController == null)
        {
            Debug.LogWarning("ArduinoPositionManager: leftController no asignado. La calibración no funcionará.");
        }

        LoadPosition();
    }

    void Update()
    {
        if (isCalibrating && leftController != null)
        {
            arduinoTransform.position = leftController.position;
            arduinoTransform.rotation = leftController.rotation;
        }
    }

    public void StartCalibration()
    {
        if (leftController == null)
        {
            Debug.LogError("ArduinoPositionManager: No se puede calibrar, leftController no está asignado.");
            return;
        }

        isCalibrating = true;
        Debug.Log("ArduinoPositionManager: Modo calibración activado. El cubo sigue al controlador izquierdo.");
    }

    public void StopCalibration()
    {
        if (!isCalibrating)
        {
            Debug.LogWarning("ArduinoPositionManager: No estás en modo calibración.");
            return;
        }

        isCalibrating = false;
        SavePosition();
        Debug.Log("ArduinoPositionManager: Calibración finalizada. Posición guardada.");
    }

    private void SavePosition()
    {
        Vector3 pos = arduinoTransform.position;
        Quaternion rot = arduinoTransform.rotation;

        PlayerPrefs.SetFloat(KEY_POS_X, pos.x);
        PlayerPrefs.SetFloat(KEY_POS_Y, pos.y);
        PlayerPrefs.SetFloat(KEY_POS_Z, pos.z);
        PlayerPrefs.SetFloat(KEY_ROT_X, rot.x);
        PlayerPrefs.SetFloat(KEY_ROT_Y, rot.y);
        PlayerPrefs.SetFloat(KEY_ROT_Z, rot.z);
        PlayerPrefs.SetFloat(KEY_ROT_W, rot.w);
        PlayerPrefs.SetInt(KEY_HAS_POSITION, 1);
        PlayerPrefs.Save();

        Debug.Log($"ArduinoPositionManager: Posición guardada - Pos: {pos}, Rot: {rot.eulerAngles}");
    }

    private void LoadPosition()
    {
        if (PlayerPrefs.GetInt(KEY_HAS_POSITION, 0) == 0)
        {
            Debug.Log("ArduinoPositionManager: No hay posición guardada. Usa el botón 'Calibrar Arduino' para configurar.");
            return;
        }

        Vector3 pos = new Vector3(
            PlayerPrefs.GetFloat(KEY_POS_X, 0f),
            PlayerPrefs.GetFloat(KEY_POS_Y, 0f),
            PlayerPrefs.GetFloat(KEY_POS_Z, 0f)
        );

        Quaternion rot = new Quaternion(
            PlayerPrefs.GetFloat(KEY_ROT_X, 0f),
            PlayerPrefs.GetFloat(KEY_ROT_Y, 0f),
            PlayerPrefs.GetFloat(KEY_ROT_Z, 0f),
            PlayerPrefs.GetFloat(KEY_ROT_W, 1f)
        );

        arduinoTransform.position = pos;
        arduinoTransform.rotation = rot;

        Debug.Log($"ArduinoPositionManager: Posición cargada - Pos: {pos}, Rot: {rot.eulerAngles}");
    }

    public void ClearSavedPosition()
    {
        PlayerPrefs.DeleteKey(KEY_POS_X);
        PlayerPrefs.DeleteKey(KEY_POS_Y);
        PlayerPrefs.DeleteKey(KEY_POS_Z);
        PlayerPrefs.DeleteKey(KEY_ROT_X);
        PlayerPrefs.DeleteKey(KEY_ROT_Y);
        PlayerPrefs.DeleteKey(KEY_ROT_Z);
        PlayerPrefs.DeleteKey(KEY_ROT_W);
        PlayerPrefs.DeleteKey(KEY_HAS_POSITION);
        PlayerPrefs.Save();

        Debug.Log("ArduinoPositionManager: Posición guardada borrada.");
    }

    public bool IsCalibrating => isCalibrating;
}
