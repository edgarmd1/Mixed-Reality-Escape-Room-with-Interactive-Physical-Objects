using UnityEngine;

/// <summary>
/// Controlador de comunicación USB Serial con Arduino para Meta Quest 3.
/// Usa un plugin Android nativo para comunicarse vía USB Host Mode.
/// </summary>
public class ArduinoUSBSerial : MonoBehaviour
{
    [Header("Configuración Serial")]
    [SerializeField] private int baudRate = 9600;
    [SerializeField] private bool autoConnectOnStart = true;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    private AndroidJavaObject usbPlugin;
    private bool isInitialized = false;
    
    /// <summary>
    /// Evento que se dispara cuando cambia el estado de conexión.
    /// </summary>
    public System.Action<bool> OnConnectionChanged;
    
    void Start()
    {
        InitializePlugin();
        
        if (autoConnectOnStart)
        {
            Connect();
        }
    }
    
    /// <summary>
    /// Inicializa el plugin Android nativo.
    /// </summary>
    private void InitializePlugin()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // Obtener contexto de la actividad Unity
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            
            // Crear instancia del plugin
            usbPlugin = new AndroidJavaObject("com.tfg.usbserial.USBSerialPlugin", activity);
            isInitialized = true;
            
            Log("Plugin USB Serial inicializado correctamente");
        }
        catch (System.Exception e)
        {
            LogError($"Error inicializando plugin USB: {e.Message}");
            isInitialized = false;
        }
#else
        LogWarning("ArduinoUSBSerial solo funciona en Android/Quest. En el Editor, las funciones no tendrán efecto.");
#endif
    }
    
    /// <summary>
    /// Intenta conectar con el Arduino vía USB.
    /// </summary>
    /// <returns>True si la conexión fue exitosa.</returns>
    public bool Connect()
    {
        if (!isInitialized)
        {
            LogError("Plugin no inicializado. No se puede conectar.");
            return false;
        }
        
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            bool connected = usbPlugin.Call<bool>("connect", baudRate);
            
            if (connected)
            {
                Log("¡Arduino conectado exitosamente!");
            }
            else
            {
                LogWarning("No se pudo conectar al Arduino. Verifica que esté conectado via USB OTG y que hayas aceptado los permisos.");
            }
            
            OnConnectionChanged?.Invoke(connected);
            return connected;
        }
        catch (System.Exception e)
        {
            LogError($"Error al conectar: {e.Message}");
            return false;
        }
#else
        Log("[Editor] Simulando conexión exitosa");
        return true;
#endif
    }
    
    /// <summary>
    /// Desconecta del Arduino.
    /// </summary>
    public void Disconnect()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (isInitialized)
        {
            try
            {
                usbPlugin.Call("disconnect");
                Log("Arduino desconectado");
                OnConnectionChanged?.Invoke(false);
            }
            catch (System.Exception e)
            {
                LogError($"Error al desconectar: {e.Message}");
            }
        }
#else
        Log("[Editor] Simulando desconexión");
#endif
    }
    
    /// <summary>
    /// Enciende el LED enviando '1' al Arduino.
    /// </summary>
    public void EncenderLED()
    {
        Write("1");
        Log("Comando enviado: Encender LED");
    }
    
    /// <summary>
    /// Apaga el LED enviando '0' al Arduino.
    /// </summary>
    public void ApagarLED()
    {
        Write("0");
        Log("Comando enviado: Apagar LED");
    }
    
    /// <summary>
    /// Envía datos al Arduino vía puerto serial.
    /// </summary>
    /// <param name="data">String a enviar.</param>
    public void Write(string data)
    {
        if (!isInitialized)
        {
            LogError("Plugin no inicializado. No se pueden enviar datos.");
            return;
        }
        
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            usbPlugin.Call("write", data);
        }
        catch (System.Exception e)
        {
            LogError($"Error al enviar datos: {e.Message}");
        }
#else
        Log($"[Editor] Simulando envío: {data}");
#endif
    }
    
    /// <summary>
    /// Verifica si hay conexión activa con el Arduino.
    /// </summary>
    /// <returns>True si está conectado.</returns>
    public bool IsConnected()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!isInitialized) return false;
        
        try
        {
            return usbPlugin.Call<bool>("isConnected");
        }
        catch
        {
            return false;
        }
#else
        return false;
#endif
    }
    
    void OnApplicationQuit()
    {
        Disconnect();
    }
    
    void OnDestroy()
    {
        Disconnect();
    }
    
    #region Logging Helpers
    
    private void Log(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ArduinoUSB] {message}");
        }
    }
    
    private void LogWarning(string message)
    {
        if (enableDebugLogs)
        {
            Debug.LogWarning($"[ArduinoUSB] {message}");
        }
    }
    
    private void LogError(string message)
    {
        Debug.LogError($"[ArduinoUSB] {message}");
    }
    
    #endregion
}
