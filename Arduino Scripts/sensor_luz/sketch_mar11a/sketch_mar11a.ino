// --- Configuración de Pines ---
const int pinLDR  = A0;  // Sensor de luz (fotorresistor)
const int pinTilt = 2;   // El cilindro negro (conectado al Pin 2 y a GND)

// --- Acelerómetro ADXL335 ---
const int pinAccelX = A1;
const int pinAccelY = A2;
const int pinAccelZ = A3;

// Valores de reposo (se calibran en setup)
int reposoX = 512;
int reposoY = 512;
int reposoZ = 512;

// Umbral de golpe: desviación total respecto al reposo
const int umbralGolpe = 80;
const unsigned long debounceKnock = 200; // ms entre golpes
unsigned long ultimoKnock = 0;

// --- Variables para el control del tiempo (Debounce) ---
unsigned long ultimaVezPHONE = 0; 
const int tiempoEspera = 500; // Medio segundo de espera para no repetir la señal

void setup() {
  // Iniciamos la comunicación serie a 9600 baudios
  Serial.begin(9600);

  // Configuramos el pin del cilindro con la resistencia interna del Arduino
  // Esto es fundamental para que funcione conectándolo directamente a GND
  pinMode(pinTilt, INPUT_PULLUP); 

  // Calibrar acelerómetro en reposo (promedio de varias lecturas)
  long sumaX = 0, sumaY = 0, sumaZ = 0;
  const int muestras = 50;
  for (int i = 0; i < muestras; i++) {
    sumaX += analogRead(pinAccelX);
    sumaY += analogRead(pinAccelY);
    sumaZ += analogRead(pinAccelZ);
    delay(10);
  }
  reposoX = sumaX / muestras;
  reposoY = sumaY / muestras;
  reposoZ = sumaZ / muestras;
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

  // 3. Lectura del ACELERÓMETRO (golpes en la mesa)
  int ax = analogRead(pinAccelX) - reposoX;
  int ay = analogRead(pinAccelY) - reposoY;
  int az = analogRead(pinAccelZ) - reposoZ;

  // Magnitud de la desviación (sin sqrt para ahorrar cómputo)
  // Usamos la suma de valores absolutos como aproximación rápida
  int magnitud = abs(ax) + abs(ay) + abs(az);

  if (magnitud > umbralGolpe) {
    unsigned long ahora = millis();
    if (ahora - ultimoKnock > debounceKnock) {
      Serial.println("KNOCK");
      ultimoKnock = ahora;
    }
  }

  // Pausa de 100ms para que Unity pueda procesar los datos sin saturarse
  delay(100); 
}