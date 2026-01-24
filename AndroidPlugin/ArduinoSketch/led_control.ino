// Código Arduino para control de LED via USB Serial
// Compatible con ArduinoUSBSerial.cs de Unity

// Pin del LED (usar LED_BUILTIN o el pin donde está conectado tu LED rojo)
const int LED_PIN = LED_BUILTIN;  // Cambia a 13 o el pin que uses

void setup() {
    // Inicializar comunicación serial a 9600 baudios
    Serial.begin(9600);
    
    // Configurar pin del LED como salida
    pinMode(LED_PIN, OUTPUT);
    
    // Apagar LED al inicio
    digitalWrite(LED_PIN, LOW);
    
    // Mensaje de inicio (opcional, para debug)
    Serial.println("Arduino listo - Esperando comandos...");
}

void loop() {
    // Si hay datos disponibles en el puerto serial
    if (Serial.available() > 0) {
        // Leer el caracter recibido
        char command = Serial.read();
        
        // Procesar comando
        switch (command) {
            case '1':
                // Encender LED
                digitalWrite(LED_PIN, HIGH);
                Serial.println("LED ON");
                break;
                
            case '0':
                // Apagar LED
                digitalWrite(LED_PIN, LOW);
                Serial.println("LED OFF");
                break;
                
            default:
                // Comando no reconocido
                Serial.print("Comando desconocido: ");
                Serial.println(command);
                break;
        }
    }
}
