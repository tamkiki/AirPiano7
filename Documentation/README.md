# Air Piano 7

Afterglow is a 13-note rhythm game for Unity 2022.3.39f1 on Windows. Open the workspace in Unity Hub, then open **Assets/AirPiano7/Scenes/SevenNoteVisualDemo.unity** and enter Play mode. Click **Play** and use **A S D F G H J** for C5 through B5.

Hit as soon as the key lights up for 100 points. Hit later, before the yellow line, for 60 points. Missing a note resets the combo. **Play Again** restarts the song. **F2** toggles the Arduino simulator.

- [Unity setup and editing](UnitySetup.md)
- [Arduino wiring and calibration](ArduinoConnection.md)
- [Serial protocol](SerialProtocol.md)
- [Audio source and license](Audio.md)
- [Recording provenance and checksums](Audio_Real_Recordings_Manifest.json)
- [Testing report](TestingReport.md)
- [Delivery inventory](DeliveryFiles.md)


## Someone Like You

Open the main scene and choose **SOMEONE LIKE YOU**. You can also open **Assets/AirPiano7/Scenes/SomeoneLikeYou.unity** directly. Click **PLAY** to start the simplified piano arrangement.

| Keyboard | Pitch class |
| --- | --- |
| A | A |
| S | B |
| D | C♯ |
| F | D |
| G | E |
| H | F♯ |
| J | G♯ |

Play 27 single-tap cues, with at least 2.30 seconds between prompts and a 0.78-second hit window. The room recording informed the cue selection. The backing and the notes you play use individual acoustic piano samples. The finale uses three slow cues over D–E–A. Use **AFTERGLOW** to return to the original song.

[Arrangement sample and harmony details](SomeoneLikeYouArrangementSources.json)
