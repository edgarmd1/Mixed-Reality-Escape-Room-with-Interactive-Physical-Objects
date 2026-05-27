# Fase 3: Hand Tracking + Hacha con Mando Derecho

## Cambio de paradigma

El EscapeRoom ahora usa **hand tracking** como modo principal de interacción.
El **mando derecho** es el hacha: cuando el jugador lo coge de la mesa (donde hay una prop física del hacha pegada), la hacha virtual sigue automáticamente al mando. Al dejarlo, vuelve a la mesa.

El Meta Quest gestiona el cambio automáticamente:
- Mando en mesa → hand tracking activo
- Mando en mano → controller mode activo → hacha sigue al mando

---

## Scripts modificados

| Archivo | Cambio |
|---|---|
| `AxeGrabController.cs` | ✅ Reescrito — ya no usa `XRGrabInteractable` |

---

## Flujo de la hacha

```
Puzzle de puerta iniciado
  → Hacha visible en la mesa (en puntoReposo)
  → Jugador coge el mando físico
     → Quest detecta mando → controller mode
     → AxeGrabController detecta el controller con isTracked = true
     → Hacha sigue al mando (positionOffset + rotationOffset)
     → Detección de impacto habilitada
  → Golpes válidos (velocidad + ángulo) → TableroDestructible.RecibirImpacto()
  → Jugador deja el mando en la mesa
     → Quest cambia a hand tracking
     → AxeGrabController: isTracked = false
     → Hacha vuelve a puntoReposo
```

---

## Setup en Unity Editor

### Concepto clave

> [!NOTE]
> El mando físico es lo único que hay en la mesa. Al ponerse las gafas, el jugador ve el hacha virtual superpuesta sobre el mando físico.
> El hacha virtual sigue al mando **siempre** — en la mesa y en la mano — sin ningún punto de reposo adicional.

---

### Paso 1 — Hacha GameObject

> [!IMPORTANT]
> Elimina los siguientes componentes del GameObject del hacha, ya **no son necesarios**:
> - `XRGrabInteractable`
> - `Rigidbody` (o ponlo en **Is Kinematic = true**)

El script `AxeGrabController` es el único componente necesario en la raíz del hacha.

---

### Paso 2 — Configurar AxeGrabController

| Campo | Valor recomendado |
|---|---|
| **Position Offset** | `(0, 0, 0)` como punto de partida — ajustar en Quest |
| **Rotation Offset** | `(0, 0, -90)` como punto de partida — ajustar en Quest |
| **Smooth Speed** | `0` (instantáneo) |
| **Punto Impacto** | Transform en la hoja del hacha |
| **Eje Hoja** | Transform con forward = mango→hoja |
| **Umbral Velocidad** | `1.2` |
| **Umbral Angulo Hoja** | `65°` |
| **Radio Impacto** | `0.18` |
| **Cooldown Entre Golpes** | `0.55s` |
| **Layer Tableros** | Layer "Tableros" |
| **Haptic Intensidad** | `0.6` |
| **Haptic Duración** | `0.25s` |

---

### Paso 3 — Ajustar el offset en Quest

El `Position Offset` y `Rotation Offset` sirven para que la empuñadura del hacha virtual coincida visualmente con el mando físico.

Proceso:
1. Hacer build en Quest y ponerse las gafas
2. Mirar el mando en la mesa: ¿el hacha virtual está bien colocada encima?
3. Ajustar los offsets en el Inspector (en Link mode se pueden cambiar en tiempo real)
4. Repetir hasta que la alineación sea correcta tanto en reposo como al blandir

> [!TIP]
> Los **Gizmos** del AxeGrabController muestran:
> - 🔴 Esfera roja → radio de impacto en `PuntoImpacto`
> - 🟡 Rayo amarillo → dirección `EjeHoja.forward`

---

## Compatibilidad con el resto del puzzle

`DoorPuzzleManager`, `TableroDestructible` y el sistema de haptics funcionan igual.
Solo cambia cómo se activa la hacha (por detección de hardware en vez de grab).

---

## Testing en Editor

Sin Quest conectado, el mando no se detectará → el hacha permanece en `puntoReposo`.
Para testear los impactos en el Editor, añade temporalmente en `AxeGrabController.Update()`:

```csharp
#if UNITY_EDITOR
if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
    _mandoActivo = !_mandoActivo; // Toggle manual para testear
#endif
```
