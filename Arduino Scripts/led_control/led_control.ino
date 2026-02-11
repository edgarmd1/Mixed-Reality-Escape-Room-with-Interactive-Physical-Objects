const int ledPin = 9; 

void setup() {
  Serial.begin(9600);
  pinMode(ledPin, OUTPUT);
  
  digitalWrite(ledPin, HIGH);
  delay(500);
  digitalWrite(ledPin, LOW);
}

void loop() {
  if (Serial.available() > 0) {
    char data = Serial.read();
    if (data == '1') {
      digitalWrite(ledPin, HIGH);
    } else if (data == '0') {
      digitalWrite(ledPin, LOW);
    }
  }
}