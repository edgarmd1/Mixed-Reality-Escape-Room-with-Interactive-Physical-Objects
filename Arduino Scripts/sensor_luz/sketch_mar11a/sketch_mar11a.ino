// --- Configuración de Pines ---
const int pinLDR  = A0;  // Sensor de luz (fotorresistor)
const int pinTilt = 2;   // El cilindro negro (conectado al Pin 2 y a GND)

// --- Acelerómetro ---
const int pinAccelZ = A3;
int reposoZ = 512;
const int umbralGolpe = 50;
const unsigned long debounceKnock = 200; // ms entre golpes
unsigned long ultimoKnock = 0;

// --- Variables para el control del tiempo ---
unsigned long ultimaVezPHONE = 0; 
const int tiempoEspera = 500; // Medio segundo de espera para no repetir la señal

void setup() {
  // Iniciamos la comunicación serie a 9600 baudios
  Serial.begin(9600);

  pinMode(pinTilt, INPUT_PULLUP); 

  // Calibrar eje Z del acelerómetro en reposo (promedio de varias lecturas)
  long sumaZ = 0;
  const int muestras = 50;
  for (int i = 0; i < muestras; i++) {
    sumaZ += analogRead(pinAccelZ);
    delay(10);
  }
  reposoZ = sumaZ / muestras;
}

void loop() {
  // 1. Lectura de la LUZ 
  int valorLuz = analogRead(pinLDR);
  Serial.println(valorLuz); // Envía el número a Unity 
 
  if (digitalRead(pinTilt) == LOW) {
    unsigned long tiempoActual = millis();

    if (tiempoActual - ultimaVezPHONE > tiempoEspera) {
      Serial.println("PHONE"); // Envía la palabra clave a Unity
      ultimaVezPHONE = tiempoActual;
    }
  }

  // 3. Lectura del ACELERÓMETRO (solo eje Z, único soldado)
  int az = analogRead(pinAccelZ) - reposoZ;
  int magnitud = abs(az);


  if (magnitud > umbralGolpe) {
    unsigned long ahora = millis();
    if (ahora - ultimoKnock > debounceKnock) {
      Serial.println("KNOCK");
      ultimoKnock = ahora;
    }
  }

  // Pausa de 50ms para captar mejor los picos de vibración
  delay(50); 
}