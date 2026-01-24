# Plugin USB Serial para Unity - Meta Quest 3

Este proyecto Android genera un archivo AAR que permite la comunicación USB Serial entre Unity y Arduino.

## Requisitos

- Android Studio (Arctic Fox o superior)
- Java 8 o superior
- Gradle 8.0 o superior

## Compilar el Plugin

### Opción 1: Desde Android Studio

1. Abrir Android Studio
2. File > Open > Seleccionar la carpeta `AndroidPlugin`
3. Esperar a que Gradle sincronice
4. Build > Make Project
5. El archivo AAR estará en: `usbserial/build/outputs/aar/usbserial-release.aar`

### Opción 2: Desde Terminal

```bash
cd AndroidPlugin
./gradlew assembleRelease
```

El archivo AAR estará en: `usbserial/build/outputs/aar/usbserial-release.aar`

## Instalar en Unity

1. Copiar `usbserial-release.aar` a `Assets/Plugins/Android/`
2. También necesitas copiar la dependencia `usb-serial-for-android`:
   - Descargar de: https://jitpack.io/com/github/mik3y/usb-serial-for-android/3.7.0/usb-serial-for-android-3.7.0.aar
   - Copiar a `Assets/Plugins/Android/`

## Uso en Unity

```csharp
// El script ArduinoUSBSerial.cs ya está configurado
// Solo añádelo a un GameObject en tu escena

// Para encender el LED:
GetComponent<ArduinoUSBSerial>().EncenderLED();

// Para apagar el LED:
GetComponent<ArduinoUSBSerial>().ApagarLED();
```

## Solución de Problemas

### No se encuentran dispositivos USB
- Verifica que el cable USB soporta datos (no solo carga)
- Usa un adaptador USB-C OTG de calidad
- Comprueba que el Arduino está encendido

### Error de permisos
- La primera vez que conectes, aparecerá un diálogo de permisos
- Acepta el permiso para que la app pueda comunicarse con el Arduino

### El LED no responde
- Verifica el código del Arduino (debe esperar '1' y '0' por Serial)
- Comprueba el baudRate (debe ser 9600 en ambos lados)
