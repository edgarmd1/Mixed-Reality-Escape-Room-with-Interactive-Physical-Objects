# Escape Room de Realidad Mixta inspirado en El Resplandor
Esta es la versión final Escape from Shining, un prototipo de Escape Room que combina entorno físico y virtual usando Meta Quest, Unity y objetos físicos. El jugador se desplaza entre la sala real y el Hotel Overlook virtual, y las acciones realizadas en un mundo repercuten directamente en el otro: los objetos físicos detectados por sensores Arduino se reflejan y se vuelven usables dentro de la experiencia virtual.

## ⚠️ Requisito imprescindible: hardware Arduino
Este proyecto no es jugable sin el montaje físico correspondiente. No se trata de una build VR autónoma: la experiencia depende de la comunicación en tiempo real entre Unity y los siguientes elementos:

* Fotorresistor, para la detección de luz/oscuridad en uno de los puzzles.
* Teléfono físico (auricular), con su flujo de activación.
* Keypad físico, para la introducción de códigos.
* Sistema DMX (Opcional pero recomendable), que controla la iluminación ambiental de la sala mediante DMXController.cs.

Sin estos componentes conectados y calibrados, varias de las mecánicas centrales del Escape Room no se activarán ni avanzarán.

### 📍 Pensado para la sala XR Lab del CITM - Terrassa.
Esta build está diseñada y calibrada específicamente para jugarse en la sala XR Lab del CITM, con su disposición física, cableado e instalación Arduino/DMX ya preparados. Ejecutarla en otro espacio requerirá adaptar el montaje físico (posiciones de sensores, direccionamiento DMX, distribución del espacio de juego) y virtual para que la correspondencia entre el mundo real y el virtual se mantenga coherente.

## Nota
Este proyecto es un prototipo desarrollado como Trabajo de Fin de Grado (CITM, curso 2025-2026), enfocado en validar la integración entre interacción física y virtual, no un producto comercial pulido.
