# Fase 2: Puerta con Tablones, Hacha y Teléfono — Guía de Setup en Unity

## Scripts creados / modificados

| Archivo | Estado |
|---|---|
| `DoorPuzzleManager.cs` | ✅ Nuevo |
| `TableroDestructible.cs` | ✅ Nuevo |
| `AxeGrabController.cs` | ✅ Nuevo |
| `TelefonoManager.cs` | ✅ Nuevo |
| `ArduinoLuz.cs` | ✅ Modificado (Tilt Switch) |
| `IntroSequenceManager.cs` | ✅ Modificado (conecta DoorPuzzleManager) |

---

## Flujo completo

```
IntroSequenceManager termina la secuencia VR
  → doorPuzzleManager.IniciarPuzzle()
     → Aparece Frame Door + hacha en el mundo MR
     → Jugador agarra el hacha (XRGrabInteractable)
     → Swings válidos (velocidad + inclinación) → TableroDestructible.RecibirImpacto()
        → Tablón desaparece + sonido de madera
        → DoorPuzzleManager.NotificarTablaRota()
     → 5/5 tablones rotos → TelefonoManager.IniciarTelefono()
        → Teléfono suena en bucle
        → Arduino recibe señal "PHONE" del Tilt Switch (auricular inclinado)
           → TelefonoManager para teléfono + reproduce voz
```

---

## Paso 1 — Layer "Tableros"

> [!IMPORTANT]
> Antes de hacer nada en escena, crea una nueva Layer llamada **"Tableros"** en Edit → Project Settings → Tags and Layers. Los tablones deben estar en esta capa para que el AxeGrabController los detecte.

---

## Paso 2 — Jerarquía de GameObjects

Monta esta jerarquía en la escena:

```
Scene
├── DoorPuzzleManager          (vacío) → Script: DoorPuzzleManager
│
├── FrameDoor                  (Frame Door FBX instanciado, inicialmente Inactive)
│   ├── Marco                  (MeshRenderer principal de la puerta)
│   └── Tablones               (GameObject padre vacío)
│       ├── Tablon_01          → Layer: Tableros, BoxCollider, Script: TableroDestructible
│       ├── Tablon_02          → Layer: Tableros, BoxCollider, Script: TableroDestructible
│       ├── Tablon_03          → Layer: Tableros, BoxCollider, Script: TableroDestructible
│       ├── Tablon_04          → Layer: Tableros, BoxCollider, Script: TableroDestructible
│       └── Tablon_05          → Layer: Tableros, BoxCollider, Script: TableroDestructible
│           (asset: Woods)
│
├── Hacha                      (axeLP.fbx instanciado, inicialmente Inactive)
│   ├── [Root]
│   │   ├── XRGrabInteractable
│   │   ├── Rigidbody          (Use Gravity: true, Interpolate: Interpolate)
│   │   ├── Collider           (para físicas, NOT trigger)
│   │   └── Script: AxeGrabController
│   ├── PuntoImpacto           (Transform vacío, posicionarlo en la hoja del hacha)
│   └── EjeHoja                (Transform vacío, su flecha azul FORWARD → de mango a hoja)
│
└── TelefonoManager            (vacío) → Script: TelefonoManager
    ├── AudioSource: TelefonoSonando  (Loop: ✓, clip = sonido teléfono)
    └── AudioSource: VozAudio         (Loop: ✗, clip = "El Hotel Overlook te ha atrapado...")
```

---

## Paso 3 — Configurar DoorPuzzleManager

En el Inspector del GameObject **DoorPuzzleManager**:

| Campo | Valor |
|---|---|
| Puerta Root | FrameDoor (el GameObject raíz de la puerta) |
| Hacha Root | Hacha (el GameObject raíz del hacha) |
| Tablones (array) | Asignar los 5 `TableroDestructible` en orden |
| Telefono Manager | TelefonoManager |
| Sonido Portal Abierto | AudioSource con sonido de puerta abriéndose (opcional) |

---

## Paso 4 — Configurar cada TableroDestructible

En el Inspector de **cada Tablon_0X**:

| Campo | Valor |
|---|---|
| Door Puzzle Manager | DoorPuzzleManager |
| Sonido Rotura | AudioSource en el mismo GameObject (clip = crack de madera) |
| Efecto Rotura | Prefab de partículas (opcional, puede estar vacío) |
| Layer | **Tableros** |
| BoxCollider | Ajustar al tamaño visual del tablón |

---

## Paso 5 — Configurar AxeGrabController

En el Inspector de **Hacha** (root):

| Campo | Valor |
|---|---|
| Punto Impacto | Transform hijo "PuntoImpacto" (en la hoja) |
| Eje Hoja | Transform hijo "EjeHoja" (forward = mango→hoja) |
| Umbral Velocidad | 1.2 (ajustar a gusto, prueba en Quest) |
| Umbral Angulo Hoja | 65° (el hacha debe apuntar ~hacia abajo al golpear) |
| Radio Impacto | 0.18 |
| Cooldown Entre Golpes | 0.55 |
| Layer Tableros | Layer "Tableros" (seleccionar en el dropdown) |

> [!TIP]
> Los **Gizmos** del AxeGrabController se muestran en escena: esfera roja (radio de impacto) y rayo amarillo (dirección del eje). Úsalos para ajustar el PuntoImpacto y EjeHoja visualmente.

---

## Paso 6 — Configurar TelefonoManager

| Campo | Valor |
|---|---|
| Arduino Luz | El GameObject que tiene el script ArduinoLuz |
| Telefono Sonando | AudioSource (Loop: ✓) |
| Voz Audio | AudioSource con el clip de voz |
| Retraso Primer Sonido | 1.2s |
| Fade Salida Telefono | 0.4s |

> [!NOTE]
> En el Editor de Unity puedes pulsar la tecla **T** para simular que el jugador descuelga el teléfono (sin necesitar el Arduino).

---

## Paso 7 — Conectar IntroSequenceManager

En el Inspector de **IntroSequenceManager**, aparece ahora un nuevo campo:

| Campo | Valor |
|---|---|
| Door Puzzle Manager | DoorPuzzleManager |

---

## Paso 8 — Código Arduino (Tilt Switch + Sensor de luz)

El **mismo Arduino** gestiona ahora dos sensores. Añade el Tilt Switch en el pin digital 2:

```arduino
const int PIN_TILT   = 2;   // Tilt Switch en el auricular del teléfono
const int PIN_LUZ    = A0;  // Sensor de luz (LDR)
const int DEBOUNCE   = 150; // ms de debounce para el tilt
unsigned long ultimaSenal = 0;

void setup() {
  Serial.begin(9600);
  pinMode(PIN_TILT, INPUT_PULLUP);  // Pullup interno: closed = LOW
}

void loop() {
  // ── Sensor de luz ──────────────────────────────────────────
  int luz = analogRead(PIN_LUZ);
  Serial.println(luz);  // Unity lee esto como integer

  // ── Tilt Switch ─────────────────────────────────────────────
  // El switch cierra el circuito al inclinar el auricular (descolgarlo).
  // Con INPUT_PULLUP: LOW = cerrado (descolgado).
  if (digitalRead(PIN_TILT) == LOW) {
    unsigned long ahora = millis();
    if (ahora - ultimaSenal > DEBOUNCE) {
      Serial.println("PHONE");  // Unity detecta esta cadena
      ultimaSenal = ahora;
    }
  }

  delay(100);
}
```

> [!IMPORTANT]
> El Tilt Switch debe conectarse entre el **Pin 2** y **GND**. Con `INPUT_PULLUP`, el pin está en HIGH en reposo. Cuando el auricular se inclina y las bolitas cierran el circuito, pasa a LOW y se envía `"PHONE\n"`.

---

## Testing en Editor (sin Arduino)

- **Puzzle de luz**: ya funciona como antes (espera `puzzleCompletado` desde `ArduinoLuz`)
- **Tablones**: durante PlayMode, ve al Inspector → `TableroDestructible` → llama `RecibirImpacto()` desde el menú contextual del script, o añade un botón de debug temporalmente
- **Teléfono**: pulsa **T** en el teclado para simular el auricular descolgado

---

## Ajuste de parámetros del hacha (recomendado)

Hacer primero una build de test:

1. **Umbral Velocidad muy alto** → el hacha nunca impacta. Bajar a 0.8
2. **Umbral Velocidad muy bajo** → el hacha impacta al mover el mando suavemente. Subir a 1.5
3. **Umbral Ángulo Hoja muy bajo** → solo golpea si apuntas perfectamente hacia abajo. Subir a 70°
4. **Radio Impacto muy pequeño** → hay que ser muy preciso. Subir a 0.25

