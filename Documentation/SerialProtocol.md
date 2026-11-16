# Arduino serial protocol

Use 9600 baud, 8 data bits, no parity and one stop bit. Send ASCII lines terminated with LF or CRLF. A note entry sends NOTE,id,distance_cm using a decimal point. Note IDs 0 through 6 map to C5, D5, E5, F5, G5, A5 and B5. Send NOTE,-1,-1.0 on release.

```text
NOTE,0,31.0
NOTE,2,73.0
NOTE,6,158.0
NOTE,-1,-1.0
```

The Unity parser accepts split messages across reads, ignores RAW debug messages and discards malformed or oversized lines. Repeated messages from the same area count as one entry. A release or change of area permits a new entry. An early entry consumes that transition, so holding through the highlight cannot score. The serial worker sends parsed IDs to the main thread through a queue and disposes the port when the component stops or the connection fails.

Press **F2** in the game to open the simulator. Use one of the seven note ID buttons, type a NOTE line, or click **Release**. The simulator and physical serial reader share the same parser, transition logic and central judgement function as keyboard input.
