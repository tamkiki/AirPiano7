# Unity setup and editing

1. In Unity Hub, install or select editor **2022.3.39f1**.
2. Choose **Add → Add project from disk** and select the **AirPiano_7Note_Work** folder containing Assets, Packages and ProjectSettings.
3. Once Unity finishes importing, open **Assets/AirPiano7/Scenes/SevenNoteVisualDemo.unity**. If the scene is absent, choose **Air Piano → Build Seven-Note Demo**.
4. Select 1920 × 1080 or 1280 × 720 in the Game view. Enter Play mode, focus the Game view and click **Play**.
5. For automated verification of the delivered chart and scene, leave Play mode and choose **Air Piano → Verify Seven-Note Demo**. The check opens the game scene and uses accelerated timing in Play mode.

The **Air Piano Controllers** object exposes fall duration, spawn interval, highlight lead, Perfect window and the seven lane records. Change track and key positions on the Canvas. Edit note art in **Assets/AirPiano7/Prefabs**. Key hints, judgement text, score pulses and particles animate from the runtime controller. The builder asks before replacing the generated scene and reuses existing song and prefab assets.

**Assets/AirPiano7/Data/Afterglow.asset** stores note IDs and spawn beats. The controller multiplies each beat by Note Interval to get the spawn time in seconds. Assign another song asset to replace the melody. With defaults, the first spawn is at 0 seconds and the last deadline is at 40.5 seconds.

The Seven Note Audio component exposes its seven imported AudioClips in lane order. Unity reports missing assignments in the Console. The scene builder requires all seven input WAVs. AudioSource volume is editable on the same object.

The Windows project uses Mono, .NET Framework compatibility, and the legacy Input Manager. Arduino Serial Input is disabled in the delivered scene. Enable it for hardware use and set the actual COM port.
