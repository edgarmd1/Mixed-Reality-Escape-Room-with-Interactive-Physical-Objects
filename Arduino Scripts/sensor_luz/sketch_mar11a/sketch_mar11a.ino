// --- Configuración de Pines ---
const int pinLDR  = A0;  // Sensor de luz (fotorresistor)
const int pinTilt = 2;   // El cilindro negro (conectado al Pin 2 y a GND)

// --- Variables para el control del tiempo (Debounce) ---
unsigned long ultimaVezPHONE = 0; 
const int tiempoEspera = 500; // Medio segundo de espera para no repetir la señal

void setup() {
  // Iniciamos la comunicación serie a 9600 baudios
  Serial.begin(9600);

  // Configuramos el pin del cilindro con la resistencia interna del Arduino
  // Esto es fundamental para que funcione conectándolo directamente a GND
  pinMode(pinTilt, INPUT_PULLUP); 
}

void loop() {
  // 1. Lectura de la LUZ (Lo que ya tenías)
  int valorLuz = analogRead(pinLDR);
  Serial.println(valorLuz); // Envía el número a Unity (ej: 450)

  // 2. Lectura del TELÉFONO (Cilindro negro)
  // Al usar INPUT_PULLUP, "LOW" significa que las bolitas están haciendo contacto
  if (digitalRead(pinTilt) == LOW) {
    unsigned long tiempoActual = millis();

    // Verificamos si ha pasado suficiente tiempo desde la última vez que avisamos
    if (tiempoActual - ultimaVezPHONE > tiempoEspera) {
      Serial.println("PHONE"); // Envía la palabra clave a Unity
      ultimaVezPHONE = tiempoActual;
    }
  }

  // Pausa de 100ms para que Unity pueda procesar los datos sin saturarse
  delay(100); 
}