#include <Keypad.h>

// ─────────────────────────────────────────────────────────────────────────────
//  PINES
// ─────────────────────────────────────────────────────────────────────────────

const int pinLDR  = A0;
const int pinTilt = 2;
const int pinAccelZ = A3;

// ─────────────────────────────────────────────────────────────────────────────
//  KEYPAD 4×4
// ─────────────────────────────────────────────────────────────────────────────

const byte FILAS = 4;
const byte COLS  = 4;

char teclas[FILAS][COLS] = {
  { '1', '2', '3', 'A' },
  { '4', '5', '6', 'B' },
  { '7', '8', '9', 'C' },
  { '*', '0', '#', 'D' }
};

byte pinesFila[FILAS] = {  7,  8,  9, 10 };
byte pinesCol[COLS]   = {  3,  4,  5,  6 };


Keypad keypad = Keypad(makeKeymap(teclas), pinesFila, pinesCol, FILAS, COLS);

const int  MAX_DIGITOS  = 10;
String     entradaActual = "";

// ─────────────────────────────────────────────────────────────────────────────
//  TELÉFONO (TILT)
// ─────────────────────────────────────────────────────────────────────────────

unsigned long ultimaVezPHONE = 0;
const int     tiempoEspera   = 500;

// ─────────────────────────────────────────────────────────────────────────────
//  ACELERÓMETRO / KNOCK
// ─────────────────────────────────────────────────────────────────────────────

int reposoZ = 512;
const int          umbralGolpe  = 50;
const unsigned long debounceKnock = 200;
unsigned long ultimoKnock = 0;

// ─────────────────────────────────────────────────────────────────────────────
//  LDR – temporización sin delay()
// ─────────────────────────────────────────────────────────────────────────────

const unsigned long INTERVALO_LDR = 50;
unsigned long ultimaLectura = 0;

// ─────────────────────────────────────────────────────────────────────────────
//  SETUP
// ─────────────────────────────────────────────────────────────────────────────

void setup() {
  Serial.begin(9600);
  pinMode(pinTilt, INPUT_PULLUP);

  // Calibrar eje Z del acelerómetro en reposo
  long sumaZ = 0;
  const int muestras = 50;
  for (int i = 0; i < muestras; i++) {
    sumaZ += analogRead(pinAccelZ);
    delay(10);
  }
  reposoZ = sumaZ / muestras;
}

// ─────────────────────────────────────────────────────────────────────────────
//  LOOP
// ─────────────────────────────────────────────────────────────────────────────

void loop() {
  leerKeypad();
  leerLDR();
  leerTelefono();
  leerKnock();
}

// ─────────────────────────────────────────────────────────────────────────────
//  FUNCIONES
// ─────────────────────────────────────────────────────────────────────────────

// --- Keypad ---

void leerKeypad() {
  // ── Teclado físico ──────────────────────────────────────────────────────
  char tecla = keypad.getKey();

  if (tecla == NO_KEY && Serial.available() > 0) {
    tecla = (char)Serial.read();
  }

  if (tecla == NO_KEY) return;

  switch (tecla) {

    case '#':
      if (entradaActual.length() > 0) {
        Serial.println("COMBO:" + entradaActual);
      }
      entradaActual = "";
      break;

    case '*':
      if (entradaActual.length() > 0) {
        entradaActual.remove(entradaActual.length() - 1);
        Serial.println("[DEBUG] Entrada: " + entradaActual);
      }
      break;

    default:
      if (tecla >= '0' && tecla <= '9') {
        if ((int)entradaActual.length() < MAX_DIGITOS) {
          entradaActual += tecla;
          Serial.println("[DEBUG] Entrada: " + entradaActual);
        } else {
          entradaActual = "";
          entradaActual += tecla;
          Serial.println("[DEBUG] Reset. Entrada: " + entradaActual);
        }
      }
      break;
  }
}


// --- LDR (con millis() en lugar de delay) ---

void leerLDR() {
  unsigned long ahora = millis();
  if (ahora - ultimaLectura < INTERVALO_LDR) return;
  ultimaLectura = ahora;

  int valorLuz = analogRead(pinLDR);
  Serial.println(valorLuz);
}

// --- Teléfono (tilt) ---

void leerTelefono() {
  if (digitalRead(pinTilt) == LOW) {
    unsigned long tiempoActual = millis();
    if (tiempoActual - ultimaVezPHONE > tiempoEspera) {
      Serial.println("PHONE");
      ultimaVezPHONE = tiempoActual;
    }
  }
}

// --- Knock (acelerómetro eje Z) ---

void leerKnock() {
  int az       = analogRead(pinAccelZ) - reposoZ;
  int magnitud = abs(az);

  if (magnitud > umbralGolpe) {
    unsigned long ahora = millis();
    if (ahora - ultimoKnock > debounceKnock) {
      //Serial.println("KNOCK");   //todo
      ultimoKnock = ahora;
    }
  }
}
