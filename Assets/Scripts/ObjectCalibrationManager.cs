using UnityEngine;
using System.Collections.Generic;

public class ObjectCalibrationManager : MonoBehaviour
{
    [System.Serializable]
    public class CalibratableObject
    {
        public string name;                    
        public Transform objectTransform;      
        [HideInInspector] public string prefsPrefix; 
    }

    [Header("Configuración")]
    [SerializeField, Tooltip("Lista de objetos que se pueden calibrar")]
    private List<CalibratableObject> calibratableObjects = new List<CalibratableObject>();

    [SerializeField, Tooltip("Controlador para posicionar objetos durante calibración")]
    private Transform calibrationController;

    [Header("Estado")]
    [SerializeField, Tooltip("Índice del objeto actualmente seleccionado")]
    private int selectedObjectIndex = 0;

    [SerializeField, Tooltip("Debug: Nombre del objeto seleccionado")]
    private string selectedObjectName = "";

    private bool isCalibrating = false;
    private CalibratableObject currentObject;

    void Start()
    {
        for (int i = 0; i < calibratableObjects.Count; i++)
        {
            calibratableObjects[i].prefsPrefix = $"CalObj_{i}_";
        }
        if (calibratableObjects.Count > 0)
        {
            selectedObjectName = calibratableObjects[0].name;
        }

        LoadAllPositions();
    }

    void Update()
    {
        if (isCalibrating && currentObject != null && calibrationController != null)
        {
            currentObject.objectTransform.position = calibrationController.position;
            currentObject.objectTransform.rotation = calibrationController.rotation;
        }
    }

    public void SelectObject(int index)
    {
        Debug.Log($"ObjectCalibrationManager: SelectObject llamado con índice: {index}");
        
        if (index < 0 || index >= calibratableObjects.Count)
        {
            Debug.LogError($"ObjectCalibrationManager: Índice {index} fuera de rango (0-{calibratableObjects.Count - 1}).");
            return;
        }

        selectedObjectIndex = index;
        selectedObjectName = calibratableObjects[index].name;
        Debug.Log($"ObjectCalibrationManager: ✅ Objeto seleccionado: '{selectedObjectName}' (índice {index})");
    }

    public void StartCalibration()
    {
        Debug.Log($"ObjectCalibrationManager: StartCalibration llamado. Índice seleccionado: {selectedObjectIndex}");
        
        if (calibrationController == null)
        {
            Debug.LogError("ObjectCalibrationManager: calibrationController no asignado.");
            return;
        }

        if (calibratableObjects.Count == 0)
        {
            Debug.LogError("ObjectCalibrationManager: No hay objetos para calibrar.");
            return;
        }

        currentObject = calibratableObjects[selectedObjectIndex];
        Debug.Log($"ObjectCalibrationManager: ✅ Calibrando '{currentObject.name}' (Transform: {currentObject.objectTransform.name})");
        
        isCalibrating = true;
    }

    public void StopCalibration()
    {
        if (!isCalibrating || currentObject == null)
        {
            Debug.LogWarning("ObjectCalibrationManager: No hay calibración activa.");
            return;
        }

        SavePosition(currentObject);
        isCalibrating = false;
        Debug.Log($"ObjectCalibrationManager: '{currentObject.name}' calibrado y guardado.");
        currentObject = null;
    }

    private void SavePosition(CalibratableObject obj)
    {
        string prefix = obj.prefsPrefix;
        Vector3 pos = obj.objectTransform.position;
        Quaternion rot = obj.objectTransform.rotation;

        PlayerPrefs.SetFloat(prefix + "PosX", pos.x);
        PlayerPrefs.SetFloat(prefix + "PosY", pos.y);
        PlayerPrefs.SetFloat(prefix + "PosZ", pos.z);
        PlayerPrefs.SetFloat(prefix + "RotX", rot.x);
        PlayerPrefs.SetFloat(prefix + "RotY", rot.y);
        PlayerPrefs.SetFloat(prefix + "RotZ", rot.z);
        PlayerPrefs.SetFloat(prefix + "RotW", rot.w);
        PlayerPrefs.SetInt(prefix + "HasPos", 1);
        PlayerPrefs.Save();

        Debug.Log($"ObjectCalibrationManager: Guardado '{obj.name}' - Pos: {pos}");
    }

    private void LoadPosition(CalibratableObject obj)
    {
        string prefix = obj.prefsPrefix;

        if (PlayerPrefs.GetInt(prefix + "HasPos", 0) == 0)
        {
            Debug.Log($"ObjectCalibrationManager: '{obj.name}' no tiene posición guardada.");
            return;
        }

        Vector3 pos = new Vector3(
            PlayerPrefs.GetFloat(prefix + "PosX", 0f),
            PlayerPrefs.GetFloat(prefix + "PosY", 0f),
            PlayerPrefs.GetFloat(prefix + "PosZ", 0f)
        );

        Quaternion rot = new Quaternion(
            PlayerPrefs.GetFloat(prefix + "RotX", 0f),
            PlayerPrefs.GetFloat(prefix + "RotY", 0f),
            PlayerPrefs.GetFloat(prefix + "RotZ", 0f),
            PlayerPrefs.GetFloat(prefix + "RotW", 1f)
        );

        obj.objectTransform.position = pos;
        obj.objectTransform.rotation = rot;

        Debug.Log($"ObjectCalibrationManager: '{obj.name}' cargado - Pos: {pos}");
    }

    private void LoadAllPositions()
    {
        foreach (var obj in calibratableObjects)
        {
            LoadPosition(obj);
        }
    }

    public void ClearAllPositions()
    {
        foreach (var obj in calibratableObjects)
        {
            string prefix = obj.prefsPrefix;
            PlayerPrefs.DeleteKey(prefix + "PosX");
            PlayerPrefs.DeleteKey(prefix + "PosY");
            PlayerPrefs.DeleteKey(prefix + "PosZ");
            PlayerPrefs.DeleteKey(prefix + "RotX");
            PlayerPrefs.DeleteKey(prefix + "RotY");
            PlayerPrefs.DeleteKey(prefix + "RotZ");
            PlayerPrefs.DeleteKey(prefix + "RotW");
            PlayerPrefs.DeleteKey(prefix + "HasPos");
        }
        PlayerPrefs.Save();
        Debug.Log("ObjectCalibrationManager: Todas las posiciones borradas.");
    }

    public List<string> GetObjectNames()
    {
        List<string> names = new List<string>();
        foreach (var obj in calibratableObjects)
        {
            names.Add(obj.name);
        }
        return names;
    }

    public bool IsCalibrating => isCalibrating;
}
