// ---------------- PIN MAP ----------------
#define DC_POWER 26
#define FWD_POWER 2
#define OVER_TEMP 3        // <-- NOW INPUT
#define OVER_DUTY 4        // <-- NOW INPUT
#define GAIN_CONTROL 5
#define REFL_POWER_LIMIT 6
#define VFWD 7
#define VREFL 8
#define FAULT 9
#define TRG_GT_SELECT 10
#define SHUTDOWN_STATUS 11
#define ADDRESS_SELECTED 12
#define ADDR3 13

#define ENABLE_STATUS 14
#define MISMATCH 15
#define PTT_IN 20
#define SYSLINK 17

#define AUX 18             // <-- INPUT
#define SHUTDOWN 19
#define GATE_IN 16
#define GND 21
#define PSU_ADJUST 22
#define ADDR0 23
#define ADDR1 24
#define ADDR2 25

// ---------------- INTERNAL STATE ----------------
int currentGain = 50;

// ---------------- GUI CONNECTION / HEARTBEAT ----------------
bool guiConnected = false;
unsigned long lastCmdTime = 0;

unsigned long lastHeartbeat = 0;
bool heartbeatState = false;
const int heartbeatOn = 2;   // 2ms ON
const int heartbeatOff = 2;  // 2ms OFF

// ---------------- PULSE ENGINE ----------------
bool pulseEnabled = false;
unsigned long pulseOnMs = 1;
unsigned long pulseOffMs = 4;
unsigned long lastPulseToggle = 0;
bool gateState = false;

// ---------------- SETUP ----------------
void setup() {
  Serial.begin(57600);
 Serial.begin(57600);

  // All input pins with internal pull-up enabled
  pinMode(DC_POWER, INPUT_PULLUP);
  pinMode(FWD_POWER, INPUT_PULLUP);
  pinMode(OVER_TEMP, INPUT_PULLUP);
  pinMode(OVER_DUTY, INPUT_PULLUP);
  pinMode(REFL_POWER_LIMIT, INPUT_PULLUP);
  pinMode(VFWD, INPUT_PULLUP);
  pinMode(VREFL, INPUT_PULLUP);
  pinMode(FAULT, INPUT_PULLUP);
  pinMode(TRG_GT_SELECT, INPUT_PULLUP);
  pinMode(SHUTDOWN_STATUS, INPUT_PULLUP);
  pinMode(ADDRESS_SELECTED, INPUT_PULLUP);
  pinMode(ENABLE_STATUS, INPUT_PULLUP);
  pinMode(MISMATCH, INPUT_PULLUP);
  pinMode(SYSLINK, INPUT_PULLUP);
  pinMode(AUX, INPUT_PULLUP);

  // Output pins remain as before
  pinMode(GAIN_CONTROL, OUTPUT);
  pinMode(ADDR3, OUTPUT);
  pinMode(PTT_IN, OUTPUT);
  pinMode(SHUTDOWN, OUTPUT);
  pinMode(GATE_IN, OUTPUT);
  pinMode(PSU_ADJUST, OUTPUT);
  pinMode(ADDR0, OUTPUT);
  pinMode(ADDR1, OUTPUT);
  pinMode(ADDR2, OUTPUT);

  pinMode(LED_BUILTIN, OUTPUT);

  // DEFAULT VALUES
  analogWrite(GAIN_CONTROL, currentGain);

  digitalWrite(ADDR3, LOW);
  digitalWrite(PTT_IN, LOW);
  digitalWrite(SHUTDOWN, HIGH);   // default HIGH
  digitalWrite(GATE_IN, LOW);     // default LOW
  digitalWrite(PSU_ADJUST, LOW);
  digitalWrite(ADDR0, LOW);
  digitalWrite(ADDR1, LOW);
  digitalWrite(ADDR2, LOW);
}

// ---------------- COMMAND HANDLER ----------------
void handleCommand(String cmd) {
  cmd.trim();

  guiConnected = true;
  lastCmdTime = millis();

  // Digital pin control: SET <pin> <0/1>
  if (cmd.startsWith("SET")) {
    int s1 = cmd.indexOf(' ');
    int s2 = cmd.indexOf(' ', s1 + 1);

    int pin = cmd.substring(s1 + 1, s2).toInt();
    int value = cmd.substring(s2 + 1).toInt();

    digitalWrite(pin, value);
  }

  // Gain control
  if (cmd.startsWith("GAIN")) {
    int spaceIndex = cmd.indexOf(' ');
    int gainValue = cmd.substring(spaceIndex + 1).toInt();

    currentGain = constrain(gainValue, 0, 255);
    analogWrite(GAIN_CONTROL, currentGain);
  }

  // Flash test
  if (cmd == "F") digitalWrite(LED_BUILTIN, HIGH);
  if (cmd == "S") digitalWrite(LED_BUILTIN, LOW);

  // ---------------- PULSE COMMAND ----------------
  if (cmd.startsWith("PULSE")) {

    if (cmd == "PULSE STOP") {
      pulseEnabled = false;
      gateState = false;
      digitalWrite(GATE_IN, LOW);
      return;
    }

    int s1 = cmd.indexOf(' ');
    int s2 = cmd.indexOf(' ', s1 + 1);

    pulseOnMs = cmd.substring(s1 + 1, s2).toInt();
    pulseOffMs = cmd.substring(s2 + 1).toInt();

    pulseEnabled = true;
    gateState = false;
    digitalWrite(GATE_IN, LOW);
    lastPulseToggle = millis();
  }
}

// ---------------- MAIN LOOP ----------------
void loop() {

  if (Serial.available()) {
    String cmd = Serial.readStringUntil('\n');
    handleCommand(cmd);
  }

  // Auto-disconnect
  if (guiConnected && millis() - lastCmdTime > 3000) {
    guiConnected = false;
    pulseEnabled = false;
    gateState = false;
    digitalWrite(GATE_IN, LOW);
  }

  // Heartbeat
  if (!guiConnected) {
    unsigned long now = millis();

    if (!heartbeatState && now - lastHeartbeat >= heartbeatOff) {
      heartbeatState = true;
      digitalWrite(LED_BUILTIN, HIGH);
      lastHeartbeat = now;
    }
    else if (heartbeatState && now - lastHeartbeat >= heartbeatOn) {
      heartbeatState = false;
      digitalWrite(LED_BUILTIN, LOW);
      lastHeartbeat = now;
    }
  }

  // Pulse engine
  if (pulseEnabled) {
    unsigned long now = millis();

    if (!gateState && now - lastPulseToggle >= pulseOffMs) {
      gateState = true;
      digitalWrite(GATE_IN, HIGH);
      lastPulseToggle = now;
    }
    else if (gateState && now - lastPulseToggle >= pulseOnMs) {
      gateState = false;
      digitalWrite(GATE_IN, LOW);
      lastPulseToggle = now;
    }
  }

  // ---------------- SEND INPUT PACKET ----------------
Serial.print("IN:");
Serial.print(digitalRead(DC_POWER)); Serial.print(",");         // 1
Serial.print(digitalRead(FWD_POWER)); Serial.print(",");        // 2
Serial.print(digitalRead(OVER_TEMP)); Serial.print(",");        // 3
Serial.print(digitalRead(OVER_DUTY)); Serial.print(",");        // 4
Serial.print(digitalRead(REFL_POWER_LIMIT)); Serial.print(","); // 6
Serial.print(digitalRead(VFWD)); Serial.print(",");             // 7
Serial.print(digitalRead(VREFL)); Serial.print(",");            // 8
Serial.print(digitalRead(FAULT)); Serial.print(",");            // 9
Serial.print(digitalRead(TRG_GT_SELECT)); Serial.print(",");    // 10
Serial.print(digitalRead(SHUTDOWN_STATUS)); Serial.print(",");  // 11
Serial.print(digitalRead(ADDRESS_SELECTED)); Serial.print(","); // 12
Serial.print(digitalRead(ENABLE_STATUS)); Serial.print(",");    // 14
Serial.print(digitalRead(MISMATCH)); Serial.print(",");         // 15
Serial.print(digitalRead(SYSLINK)); Serial.print(",");          // 17
Serial.print(digitalRead(AUX));                                  // 18
Serial.println();

  // ---------------- SEND OUTPUT PACKET ----------------
  Serial.print("OUT:");


  Serial.print(currentGain); Serial.print(","); //5

  Serial.print(digitalRead(ADDR3)); Serial.print(","); //13
  Serial.print(digitalRead(GATE_IN)); Serial.print(","); //22
  Serial.print(digitalRead(SHUTDOWN)); Serial.print(","); //19
  Serial.print(digitalRead(PTT_IN)); Serial.print(","); //18
  Serial.print(digitalRead(PSU_ADJUST)); Serial.print(","); //23
  Serial.print(digitalRead(ADDR0)); Serial.print(","); //24
  Serial.print(digitalRead(ADDR1)); Serial.print(","); //25
  Serial.print(digitalRead(ADDR2));
  Serial.println();

  delay(100);
}