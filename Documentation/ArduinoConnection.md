# Arduino Mega 2560 and HC-SR04

| HC-SR04 | Mega 2560 |
| --- | --- |
| VCC | 5V |
| GND | GND |
| TRIG | D4 |
| ECHO | D3 |

Upload **Arduino/AirPiano7Notes/AirPiano7Notes.ino** to an Arduino Mega 2560. Confirm the COM port in the Arduino IDE, then close Serial Monitor before Unity opens the port. Set the port on **Air Piano Controllers → Arduino Serial Input**, leave baud at 9600 and enable the component. Console warnings report port-open and read failures. After reconnecting, restart Play mode or toggle the component off and back on once the previous connection closes.

Mount the sensor at one end of the playing surface, aimed horizontally along the line of hand travel. Measure each zone from the face of the sensor along its beam. Keep nearby surfaces out of the beam where possible. Moving a hand nearer to or farther from the sensor selects a pitch.

| Pitch | Initial distance interval |
| --- | --- |
| C5 | 21–42 cm |
| D5 | 42–63 cm |
| E5 | 63–84 cm |
| F5 | 84–105 cm |
| G5 | 105–126 cm |
| A5 | 126–147 cm |
| B5 | 147–168 cm |

These distances are unverified calibration values. The sketch uses lower-inclusive intervals, 2 cm of hysteresis around the current zone, a three-reading median and three consecutive classifications before changing state. A missing echo clears the median history and repeated invalid readings release the current note. Allow time for confirmation when moving across zones. Adjust Edges, HysteresisCm and ConfirmSamples after observing real hand positions and releases. Recognition with one HC-SR04 has yet to be verified on physical hardware.
