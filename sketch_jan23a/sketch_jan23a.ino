void setup() {
  Serial.begin(9600);   
  pinMode(9, OUTPUT);    
}

void loop() {
  if (Serial.available() > 0) {
    char dato = Serial.read();
    
    if (dato == '1') {
      digitalWrite(9, HIGH);  
    } 
    else if (dato == '0') {
      digitalWrite(9, LOW);
    }
  }
}