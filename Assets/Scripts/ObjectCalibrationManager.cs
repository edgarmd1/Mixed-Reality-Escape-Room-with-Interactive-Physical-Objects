using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;


public class ObjectCalibrationManager : MonoBehaviour
{
    [System.Serializable]
    public class CalibratableObject
    {
        public string name;
        public Transform objectTransform;

        [Tooltip("Si está activo, la Y del objeto se calcula automáticamente")]
        public bool anclarAlSuelo = false;

        [Tooltip("Offset en metros sobre el suelo detectado")]
        public float offsetYSobreSuelo = 0f;

        [Tooltip("Se dispara al iniciar la calibración de este objeto.")]
        public UnityEvent onStartCalibration;

        [Tooltip("Se dispara al guardar la calibración de este objeto.")]
        public UnityEvent onStopCalibration;

        [HideInInspector] public string prefsPrefix;
    }

    [Header("Configuración")]
    [SerializeField, Tooltip("Lista de objetos que se pueden calibrar")]
    private List<CalibratableObject> calibratableObjects = new List<CalibratableObject>();

    [SerializeField, Tooltip("OVR Camera Rig de la escena. Necesario para anclar objetos al suelo. " +
                             "Si se deja vacío se busca automáticamente.")]
    private OVRCameraRig ovrCameraRig;

    [Header("Velocidades de calibración")]
    [SerializeField, Tooltip("Velocidad de traslación (m/s)")]
    private float velocidadMovimiento = 0.8f;

    [SerializeField, Tooltip("Velocidad de rotación (grados/s)")]
    private float velocidadRotacion = 60f;

    [Header("Estado")]
    [SerializeField, Tooltip("Índice del objeto actualmente seleccionado")]
    private int selectedObjectIndex = 0;

    [SerializeField, Tooltip("Debug: Nombre del objeto seleccionado")]
    private string selectedObjectName = "";

    private bool isCalibrating = false;
    private CalibratableObject currentObject;

    private float _eulerY = 0f;
    private float _eulerX = 0f;

    private readonly List<InputDevice> _leftDevices = new List<InputDevice>();
    private readonly List<InputDevice> _rightDevices = new List<InputDevice>();

    private float _floorWorldY = 0f;
    private bool _floorReady = false;

    void Awake()
    {
        if (ovrCameraRig == null)
            ovrCameraRig = FindObjectOfType<OVRCameraRig>();
    }

    void Start()
    {
        for (int i = 0; i < calibratableObjects.Count; i++)
            calibratableObjects[i].prefsPrefix = $"CalObj_{i}_";

        if (calibratableObjects.Count > 0)
            selectedObjectName = calibratableObjects[0].name;

        bool necesitaSuelo = false;
        foreach (var obj in calibratableObjects)
            if (obj.anclarAlSuelo) { necesitaSuelo = true; break; }

        if (necesitaSuelo)
            StartCoroutine(EsperarSueloYCargar());
        else
            LoadAllPositions();
    }

    private IEnumerator EsperarSueloYCargar()
    {
        Camera cam = ovrCameraRig != null && ovrCameraRig.centerEyeAnchor != null ? ovrCameraRig.centerEyeAnchor.GetComponent<Camera>() : null;
        if (ovrCameraRig == null || cam == null)
        {
            Debug.LogWarning("[ObjectCalibration] No se encontró OVRCameraRig/cámara. " +
                             "Usando Y=0 como suelo.");
            _floorWorldY = 0f;
            _floorReady = true;
            LoadAllPositions();
            yield break;
        }

        yield return null;

        float timeout = 5f;
        float elapsed = 0f;
        while (cam.transform.localPosition.y < 0.05f && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        float cameraWorldY = cam.transform.position.y;
        float cameraLocalY = cam.transform.localPosition.y;
        _floorWorldY = cameraWorldY - cameraLocalY;

        _floorReady = true;
        Debug.Log($"[ObjectCalibration] Suelo detectado en Y={_floorWorldY:F3} " +
                  $"(cámara mundo={cameraWorldY:F3}, local={cameraLocalY:F3})");

        LoadAllPositions();
    }

    void Update()
    {
        if (!isCalibrating || currentObject == null) return;

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
            _leftDevices);
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            _rightDevices);

        Vector2 leftStick  = Vector2.zero;
        Vector2 rightStick = Vector2.zero;
        bool    gripLeft   = false;

        if (_leftDevices.Count > 0)
        {
            _leftDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out leftStick);
            _leftDevices[0].TryGetFeatureValue(CommonUsages.gripButton,    out gripLeft);
        }
        if (_rightDevices.Count > 0)
            _rightDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out rightStick);

        float dt = Time.deltaTime;
        Transform obj = currentObject.objectTransform;

        if (!gripLeft)
        {
            Camera cam = Camera.main;
            if (cam != null && leftStick.sqrMagnitude > 0.01f)
            {
                Vector3 forward = new Vector3(cam.transform.forward.x, 0f, cam.transform.forward.z).normalized;
                Vector3 right   = new Vector3(cam.transform.right.x,   0f, cam.transform.right.z).normalized;
                obj.position += (right   * leftStick.x +
                                 forward * leftStick.y) * velocidadMovimiento * dt;
            }
        }
        else
        {
            obj.position += Vector3.up * leftStick.y * velocidadMovimiento * dt;
        }

        _eulerY += rightStick.x * velocidadRotacion * dt;
        _eulerX -= rightStick.y * velocidadRotacion * dt;
        _eulerX  = Mathf.Clamp(_eulerX, -85f, 85f);
        obj.rotation = Quaternion.Euler(_eulerX, _eulerY, 0f);
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

        if (calibratableObjects.Count == 0)
        {
            Debug.LogError("ObjectCalibrationManager: No hay objetos para calibrar.");
            return;
        }

        currentObject = calibratableObjects[selectedObjectIndex];

        currentObject.onStartCalibration?.Invoke();

        Vector3 currentAngles = currentObject.objectTransform.rotation.eulerAngles;
        _eulerX = currentAngles.x;
        _eulerY = currentAngles.y;
        
        if (_eulerX > 180f) _eulerX -= 360f;

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

        currentObject.onStopCalibration?.Invoke();
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
            if (obj.anclarAlSuelo && _floorReady)
            {
                Vector3 p = obj.objectTransform.position;
                p.y = _floorWorldY + obj.offsetYSobreSuelo;
                obj.objectTransform.position = p;
                Debug.Log($"ObjectCalibrationManager: '{obj.name}' sin posición guardada, " +
                          $"Y anclada al suelo: {p.y:F3}");
            }
            else
            {
                Debug.Log($"ObjectCalibrationManager: '{obj.name}' no tiene posición guardada.");
            }
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

        if (obj.anclarAlSuelo && _floorReady)
        {
            float yOriginal = pos.y;
            pos.y = _floorWorldY + obj.offsetYSobreSuelo;
            Debug.Log($"ObjectCalibrationManager: '{obj.name}' Y anclada al suelo: " +
                      $"{yOriginal:F3} → {pos.y:F3} (suelo={_floorWorldY:F3}, offset={obj.offsetYSobreSuelo:F3})");
        }

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
