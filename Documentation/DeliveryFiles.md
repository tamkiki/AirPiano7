# Delivery inventory

## Created

- **Assets/AirPiano7/Scenes/SevenNoteVisualDemo.unity** and its Unity metadata.
- **Assets/AirPiano7/Data/Afterglow.asset** with the 13-note chart.
- **Assets/AirPiano7/Prefabs/** with seven colored note prefabs and the hit spark prefab.
- **Assets/AirPiano7/Scripts/** contains RhythmSession.cs, PianoGame.cs, PianoSong.cs, SevenNoteAudio.cs, NoteProtocol.cs, SerialSimulator.cs and ArduinoSerialInput.cs.
- **Assets/AirPiano7/Editor/** contains BuildSevenNoteDemo.cs and VerifySevenNoteDemo.cs.
- **Packages/** and **ProjectSettings/** for the independent Unity project. Unity generated its own asset metadata.
- **Arduino/AirPiano7Notes/AirPiano7Notes.ino** for the Mega 2560.
- **Documentation/** contains the new README.md, UnitySetup.md, ArduinoConnection.md, SerialProtocol.md, Audio.md, Audio_Real_Recordings_Manifest.json, MetaPiano_SourceManifest.json, TestingReport.md and DeliveryFiles.md.
- **Audio_Backups/** with the original temporary WAVs and the source manifest present before conversion.

## Modified

- Seven **Assets/AirPiano7/Audio/Piano_*.wav** files were replaced with verified Meta Piano conversions. The original fallback WAVs are in Audio_Backups and in the untouched source ZIP.
- **Tools/download_real_piano.py** now validates source filenames, audio and hashes before installation and publishes the installed manifest last.
- **Tools/Download_Real_Piano_Windows.bat** now explicitly selects the official Meta source.
- The documentation shipped with the source ZIP remains available for the fallback files.

## Pending verification

- Physical Arduino connection, USB unplug behavior and HC-SR04 recognition and calibration.
- Interactive keyboard dispatch, visual inspection at both target resolutions and audible listening review. See the [test report](TestingReport.md) for the Unity and audio checks that ran.

