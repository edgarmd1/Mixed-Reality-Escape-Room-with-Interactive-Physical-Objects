// Definición del pin
const int pinLDR = A0;

void setup() {
  // Iniciamos la comunicación serie a 9600 baudios
  Serial.begin(9600);
}

void loop() {
  // Leemos el valor analógico (0 a 1023)
  int valorLuz = analogRead(pinLDR);

  // Enviamos el valor por el puerto serie para Unity
  Serial.println(valorLuz);

  // Pequeña pausa para no saturar el buffer
  delay(100); 
}