// Sample file demonstrating communications between Arduino and C# solution
// 
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

// State Engine to handle incoming Serial data from:
//   http://gammon.com.au/serial
typedef enum { NONE,
               GOT_C,  // Set N Short
               GOT_D,  // Set On Short
               GOT_E,  // Set Off Short
               GOT_F,  // Set N Long
               GOT_G,  // Set On Long
               GOT_H,  // Set Off Long
               GOT_L,  // Long Blink
               GOT_S,  // Short Blink
               GOT_Z   // Unique Serial ID for this sketch
             } states;
// current state-machine state
states state = NONE;

void setup() {
  pinMode(LED_BUILTIN, OUTPUT);
  Serial.begin(115200);
  Init_Variables();
}

void loop()
{
  while (Serial.available() > 0)
  {
    ProcessIncomingByte(Serial.read());
  }
}

void Init_Variables()
{
  serialID = 1002;
  
  nShort = 1;
  onShort = 25;
  offShort = 75;

  nLong = 2;
  onLong = 100;
  offLong = 900;
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
  Blink_N_Times(1, onShort, offShort);
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
