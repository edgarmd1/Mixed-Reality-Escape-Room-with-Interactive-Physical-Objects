package com.tfg.usbserial;

import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbDeviceConnection;
import android.hardware.usb.UsbManager;
import android.os.Build;
import android.util.Log;

import com.hoho.android.usbserial.driver.UsbSerialDriver;
import com.hoho.android.usbserial.driver.UsbSerialPort;
import com.hoho.android.usbserial.driver.UsbSerialProber;

import java.io.IOException;
import java.util.List;

/**
 * Plugin de comunicación USB Serial para Unity.
 * Permite la comunicación con dispositivos Arduino vía USB Host Mode.
 */
public class USBSerialPlugin {
    private static final String TAG = "USBSerialPlugin";
    private static final String ACTION_USB_PERMISSION = "com.tfg.usbserial.USB_PERMISSION";

    private Context context;
    private UsbManager usbManager;
    private UsbSerialPort serialPort;
    private UsbDeviceConnection connection;
    private boolean isConnected = false;
    private boolean permissionRequested = false;

    // Receiver para permisos USB
    private final BroadcastReceiver usbPermissionReceiver = new BroadcastReceiver() {
        @Override
        public void onReceive(Context context, Intent intent) {
            String action = intent.getAction();
            if (ACTION_USB_PERMISSION.equals(action)) {
                synchronized (this) {
                    UsbDevice device = intent.getParcelableExtra(UsbManager.EXTRA_DEVICE);
                    if (intent.getBooleanExtra(UsbManager.EXTRA_PERMISSION_GRANTED, false)) {
                        if (device != null) {
                            Log.i(TAG, "Permiso USB concedido para: " + device.getDeviceName());
                        }
                    } else {
                        Log.w(TAG, "Permiso USB denegado para: "
                                + (device != null ? device.getDeviceName() : "dispositivo desconocido"));
                    }
                    permissionRequested = false;
                }
            }
        }
    };

    /**
     * Constructor del plugin.
     * 
     * @param context Contexto Android (Activity de Unity)
     */
    public USBSerialPlugin(Context context) {
        this.context = context;
        this.usbManager = (UsbManager) context.getSystemService(Context.USB_SERVICE);

        // Registrar receiver para permisos USB
        IntentFilter filter = new IntentFilter(ACTION_USB_PERMISSION);
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
            context.registerReceiver(usbPermissionReceiver, filter, Context.RECEIVER_NOT_EXPORTED);
        } else {
            context.registerReceiver(usbPermissionReceiver, filter);
        }

        Log.i(TAG, "USBSerialPlugin inicializado");
    }

    /**
     * Conecta con el primer dispositivo USB Serial disponible.
     * 
     * @param baudRate Velocidad de baudios (normalmente 9600)
     * @return true si la conexión fue exitosa
     */
    public boolean connect(int baudRate) {
        Log.i(TAG, "Intentando conectar a baudRate: " + baudRate);

        // Buscar drivers disponibles
        List<UsbSerialDriver> drivers = UsbSerialProber.getDefaultProber().findAllDrivers(usbManager);

        if (drivers.isEmpty()) {
            Log.w(TAG, "No se encontraron dispositivos USB Serial");
            return false;
        }

        Log.i(TAG, "Encontrados " + drivers.size() + " dispositivo(s) USB Serial");

        // Usar el primer driver disponible
        UsbSerialDriver driver = drivers.get(0);
        UsbDevice device = driver.getDevice();

        Log.i(TAG, "Dispositivo encontrado - Vendor ID: " + device.getVendorId() +
                ", Product ID: " + device.getProductId() +
                ", Nombre: " + device.getDeviceName());

        // Intentar abrir conexión
        connection = usbManager.openDevice(device);

        if (connection == null) {
            // Necesitamos solicitar permiso
            if (!permissionRequested) {
                Log.i(TAG, "Solicitando permiso USB...");
                requestPermission(device);
            } else {
                Log.w(TAG, "Esperando respuesta de permiso USB...");
            }
            return false;
        }

        // Obtener el primer puerto del driver
        serialPort = driver.getPorts().get(0);

        try {
            serialPort.open(connection);
            serialPort.setParameters(
                    baudRate,
                    8, // Data bits
                    UsbSerialPort.STOPBITS_1, // Stop bits
                    UsbSerialPort.PARITY_NONE // Parity
            );

            // Configurar control de flujo
            serialPort.setDTR(true);
            serialPort.setRTS(true);

            isConnected = true;
            Log.i(TAG, "¡Conexión establecida exitosamente!");
            return true;

        } catch (IOException e) {
            Log.e(TAG, "Error al abrir puerto serial: " + e.getMessage());
            closeConnection();
            return false;
        }
    }

    /**
     * Escribe datos al puerto serial.
     * 
     * @param data String a enviar
     */
    public void write(String data) {
        if (serialPort == null || !isConnected) {
            Log.w(TAG, "No se puede escribir: puerto no conectado");
            return;
        }

        try {
            byte[] bytes = data.getBytes();
            serialPort.write(bytes, 1000); // Timeout de 1 segundo
            Log.d(TAG, "Datos enviados: " + data);
        } catch (IOException e) {
            Log.e(TAG, "Error al escribir: " + e.getMessage());
        }
    }

    /**
     * Lee datos del puerto serial.
     * 
     * @param maxBytes Máximo número de bytes a leer
     * @return String con los datos leídos o null si no hay datos
     */
    public String read(int maxBytes) {
        if (serialPort == null || !isConnected) {
            return null;
        }

        try {
            byte[] buffer = new byte[maxBytes];
            int bytesRead = serialPort.read(buffer, 100); // Timeout corto

            if (bytesRead > 0) {
                String result = new String(buffer, 0, bytesRead);
                Log.d(TAG, "Datos recibidos: " + result);
                return result;
            }
        } catch (IOException e) {
            Log.e(TAG, "Error al leer: " + e.getMessage());
        }

        return null;
    }

    /**
     * Desconecta y libera recursos.
     */
    public void disconnect() {
        Log.i(TAG, "Desconectando...");
        closeConnection();

        try {
            context.unregisterReceiver(usbPermissionReceiver);
        } catch (IllegalArgumentException e) {
            // Receiver ya estaba desregistrado
        }
    }

    /**
     * Verifica si hay conexión activa.
     * 
     * @return true si está conectado
     */
    public boolean isConnected() {
        return isConnected && serialPort != null;
    }

    /**
     * Obtiene el número de dispositivos USB Serial disponibles.
     * 
     * @return Número de dispositivos encontrados
     */
    public int getDeviceCount() {
        List<UsbSerialDriver> drivers = UsbSerialProber.getDefaultProber().findAllDrivers(usbManager);
        return drivers.size();
    }

    /**
     * Solicita permiso para acceder a un dispositivo USB.
     */
    private void requestPermission(UsbDevice device) {
        permissionRequested = true;

        int flags = PendingIntent.FLAG_IMMUTABLE;
        PendingIntent permissionIntent = PendingIntent.getBroadcast(
                context,
                0,
                new Intent(ACTION_USB_PERMISSION),
                flags);

        usbManager.requestPermission(device, permissionIntent);
    }

    /**
     * Cierra la conexión y libera recursos.
     */
    private void closeConnection() {
        if (serialPort != null) {
            try {
                serialPort.close();
            } catch (IOException e) {
                Log.e(TAG, "Error al cerrar puerto: " + e.getMessage());
            }
            serialPort = null;
        }

        if (connection != null) {
            connection.close();
            connection = null;
        }

        isConnected = false;
        Log.i(TAG, "Conexión cerrada");
    }
}
