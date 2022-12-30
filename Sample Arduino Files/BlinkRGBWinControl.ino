//Connections
//  RGB LED
//    R - 430 Ohm resistor - D9 on Arduino - Should have lower value (val = ?) resistor to Red!!
//    G - 430 Ohm resistor - D10 on Arduino
//    B - 430 Ohm resistor - D11 on Arduino
//    On Nano: PWM Pins 3, 9, 10 and 11 have same duty cycle
//             PWM Pins 5, 6 have higher duty cycle because they are involved with millis()
//  Photoresistor
//    pRes - 5V
//    pRes - a0 on Arduino - 1K Ohm resistor - GND

// Define Pins
#define BLUE 11
#define GREEN 10
#define RED 9
#define PHRES A0

// Declare Variables
char s[64];
int serialID;
int incomingSerialValue;
bool incomingSerialValueIsNegative;

int nShort;
int onShort;
int offShort;

int nLong;
int onLong;
int offLong;

int redValue;
int greenValue;
int blueValue;
int value;          // Store value from photoresistor (0-1023)
int rgbVal;

// State Engine to handle incoming Serial data from:
//   http://gammon.com.au/serial
typedef enum { NONE,
               GOT_C,  // Set N Short (number of blinks)
               GOT_D,  // Set On Short (time in ms)
               GOT_E,  // Set Off Short (time in ms)
               GOT_F,  // Set N Long (number of blinks)
               GOT_G,  // Set On Long (time in ms)
               GOT_H,  // Set Off Long (time in ms)
               GOT_L,  // Long Blink (command to run BlinkLong())
               GOT_S,  // Short Blink (command to run BlinkShort())
               GOT_T,  // RGB Red
               GOT_U,  // RGB Green
               GOT_V,  // RGB Blue
               GOT_Z   // Unique Serial ID for this sketch
             } states;
// current state-machine state
states state = NONE;

void setup() {
  pinMode(LED_BUILTIN, OUTPUT);
  pinMode(PHRES, INPUT);
  pinMode(RED, OUTPUT);
  pinMode(GREEN, OUTPUT);
  pinMode(BLUE, OUTPUT);
  Serial.begin(115200);
  Init_Variables();
  analogWrite(RED, LOW);
  analogWrite(GREEN, LOW);
  analogWrite(BLUE, LOW);
}

void loop()
{
  analogWrite(RED, redValue);
  analogWrite(GREEN, greenValue);
  analogWrite(BLUE, blueValue);
  value = analogRead(PHRES);
  rgbVal = 255 - value;
  if (value > 127) rgbVal = 255 - (int)(value * 0.25);
  while (Serial.available() > 0)
  {
    ProcessIncomingByte(Serial.read());
  }
}

void Init_Variables()
{
  serialID = 1003;
  
  nShort = 1;
  onShort = 25;
  offShort = 75;

  nLong = 2;
  onLong = 100;
  offLong = 900;
  
  redValue = 0;
  greenValue = 0;
  blueValue = 0;
  value = 0;
  rgbVal = -1;
}

void Blink_N_Times(int n, int onTime, int offTime)
{
  for (int i = 0; i < n; i++)
  {
    digitalWrite(LED_BUILTIN, HIGH);
    delay(onTime);
    digitalWrite(LED_BUILTIN, LOW);
    if (i < n - 1) delay(offTime);
  }
}
void ProcessIncomingByte(const byte c)
{
  //Blink_N_Times(1, onShort, offShort);
  if (isdigit (c))
  {
    incomingSerialValue *= 10;
    incomingSerialValue += c - '0';
  }
  else
  {
    HandlePreviousState ();
    switch (c)
    {
      case 'C':
        state = GOT_C;
        break;
      case 'D':
        state = GOT_D;
        break;
      case 'E':
        state = GOT_E;
        break;
      case 'F':
        state = GOT_F;
        break;
      case 'G':
        state = GOT_G;
        break;
      case 'H':
        state = GOT_H;
        break;
      case 'L':
        state = GOT_L;
        break;
      case 'S':
        state = GOT_S;
        break;
      case 'T':
        state = GOT_T;
        break;
      case 'U':
        state = GOT_U;
        break;
      case 'V':
        state = GOT_V;
        break;
      case 'Z':
        state = GOT_Z;
        break;
      case '-':
        incomingSerialValueIsNegative = true;
        break;
      default:
        state = NONE;
        break;
    }
  }
}

void HandlePreviousState()
{
  if (incomingSerialValueIsNegative) incomingSerialValue = -incomingSerialValue;
  incomingSerialValueIsNegative = false;
  switch (state)
  {
    case GOT_C:
      SetNShort();
      break;
    case GOT_D:
      SetOnShort();
      break;
    case GOT_E:
      SetOffShort();
      break;
    case GOT_F:
      SetNLong();
      break;
    case GOT_G:
      SetOnLong();
      break;
    case GOT_H:
      SetOffLong();
      break;
    case GOT_L:
      BlinkLong();
      break;
    case GOT_S:
      BlinkShort();
      break;
    case GOT_T:
      SetR();
      break;
    case GOT_U:
      SetG();
      break;
    case GOT_V:
      SetB();
      break;
    case GOT_Z:
      CheckSerialID();
      break;
    default:
      break;
  }
  incomingSerialValue = 0;
}

void CheckSerialID()
{
  snprintf_P(s, sizeof(s), PSTR("%d\n"), serialID);
  Serial.print(s);
}

void SetR()
{
  redValue = incomingSerialValue;
  snprintf_P(s, sizeof(s), PSTR("Red = %d of 255\n"), redValue);
  Serial.print(s);
  ShowPhRes();
}

void SetG()
{
  greenValue = incomingSerialValue;
  snprintf_P(s, sizeof(s), PSTR("Green = %d of 255\n"), greenValue);
  Serial.print(s);
  ShowPhRes();
}

void SetB()
{
  blueValue = incomingSerialValue;
  snprintf_P(s, sizeof(s), PSTR("Blue = %d of 255\n"), blueValue);
  Serial.print(s);
  ShowPhRes();
}

void ShowPhRes()
{
  snprintf_P(s, sizeof(s), PSTR("p:%d\n"), value);
  Serial.print(s);
}

void SetNShort()
{
  nShort = incomingSerialValue;
  snprintf_P(s, sizeof(s), PSTR("N Short = %d times\n"), nShort);
  Serial.print(s);
}

void SetOnShort()
{
  onShort = incomingSerialValue;
  snprintf_P(s, sizeof(s), PSTR("ON time Short = %d milliseconds\n"), onShort);
  Serial.print(s);
}

void SetOffShort()
{
  offShort = incomingSerialValue;
  snprintf_P(s, sizeof(s), PSTR("OFF time Short = %d milliseconds\n"), offShort);
  Serial.print(s);
}

void SetNLong()
{
  nLong = incomingSerialValue;
  snprintf_P(s, sizeof(s), PSTR("N Long = %d times\n"), nLong);
  Serial.print(s);
}

void SetOnLong()
{
  onLong = incomingSerialValue;
  snprintf_P(s, sizeof(s), PSTR("ON time Long = %d milliseconds\n"), onLong);
  Serial.print(s);
}

void SetOffLong()
{
  offLong = incomingSerialValue;
  snprintf_P(s, sizeof(s), PSTR("OFF time Long = %d milliseconds\n"), offLong);
  Serial.print(s);
}

void BlinkShort()
{
  long startTimeU = micros();
  Blink_N_Times(nShort, onShort, offShort);
  long elapsedTimeU = micros() - startTimeU;
  
  snprintf_P(s, sizeof(s), PSTR("m:%ld\n"), elapsedTimeU / 1000);
  Serial.print(s);
  snprintf_P(s, sizeof(s), PSTR("u:%ld\n"), elapsedTimeU);
  Serial.print(s);
}

void BlinkLong()
{
  long startTimeU = micros();
  Blink_N_Times(nLong, onLong, offLong);
  long elapsedTimeU = micros() - startTimeU;
  
  snprintf_P(s, sizeof(s), PSTR("m:%ld\n"), elapsedTimeU / 1000);
  Serial.print(s);
  snprintf_P(s, sizeof(s), PSTR("u:%ld\n"), elapsedTimeU);
  Serial.print(s);
}

//
