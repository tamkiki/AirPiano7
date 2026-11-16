#include <Arduino.h>

constexpr uint8_t Trig = 4;
constexpr uint8_t Echo = 3;
constexpr uint8_t LaneCount = 7;
// Initial calibration only. Distances are measured along the sensor beam.
constexpr float Edges[LaneCount + 1] = {21, 42, 63, 84, 105, 126, 147, 168};
constexpr float HysteresisCm = 2.0f;
constexpr uint8_t ConfirmSamples = 3;
int current = -1;
int candidate = -2;
uint8_t consecutive = 0;
float history[3] = {};
uint8_t used = 0;
uint8_t next = 0;

float measure() {
  digitalWrite(Trig, LOW);
  delayMicroseconds(3);
  digitalWrite(Trig, HIGH);
  delayMicroseconds(10);
  digitalWrite(Trig, LOW);
  const unsigned long echo = pulseIn(Echo, HIGH, 15000);
  if (!echo) return -1;
  const float cm = echo * 0.01715f;
  return cm >= 2 && cm <= 220 ? cm : -1;
}

float filter(float distance) {
  if (distance < 0) {
    used = next = 0;
    return -1;
  }
  history[next] = distance;
  next = (next + 1) % 3;
  if (used < 3) ++used;
  if (used < 3) return distance;
  const float a = history[0], b = history[1], c = history[2];
  return max(min(a, b), min(max(a, b), c));
}

int zone(float cm) {
  if (current >= 0 && cm >= Edges[current] - HysteresisCm &&
      cm < Edges[current + 1] + HysteresisCm) return current;
  for (uint8_t id = 0; id < LaneCount; ++id)
    if (cm >= Edges[id] && cm < Edges[id + 1]) return id;
  return -1;
}

void sendNote(int id, float distance) {
  Serial.print(F("NOTE,"));
  Serial.print(id);
  Serial.print(',');
  Serial.println(id < 0 ? -1.0f : distance, 1);
}

void setup() {
  pinMode(Trig, OUTPUT);
  pinMode(Echo, INPUT);
  Serial.begin(9600);
  sendNote(-1, -1);
}

void loop() {
  const float distance = filter(measure());
  const int observed = zone(distance);
  if (observed != candidate) {
    candidate = observed;
    consecutive = 1;
  } else if (consecutive < ConfirmSamples) {
    ++consecutive;
  }
  if (consecutive >= ConfirmSamples && candidate != current) {
    current = candidate;
    sendNote(current, distance);
  }
  delay(60);
}
