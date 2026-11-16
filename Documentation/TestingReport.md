# Air Piano 7 testing report

**Environment** — Windows, Unity 2022.3.39f1 in batch mode, Python 3.14, FFmpeg and Arduino AVR core 1.8.8. The automated Play Mode run used accelerated timings and simulator inputs. A separate model test used the delivered default timings.

| Group | Status | Executed check and outcome |
| --- | --- | --- |
| A — Source and files | PASS | Matched all seven source filenames to the official Meta Piano CC0-1.0 collection manifest and downloaded each Vel03 OGG preview. Independently re-read all delivered WAV headers, confirmed 44.1 kHz mono 16-bit PCM and exactly 79,380 frames, verified pitch, signal peaks, attack, fade endpoint and SHA-256. The original fallback backups match the ZIP byte for byte. |
| A — Mapping and playback | PASS | Opened the saved scene and checked the seven AudioClip bindings. During the 13-hit Play Mode run, stopped AudioSource before each hit and confirmed that each hit started it again. Early and wrong inputs and a Miss stayed silent. An intentionally missing clip produced the expected Console error. |
| A — Listening review | NOT TESTED | Audible output and fade quality have not been checked by listening. |
| B — Timing and scoring | PASS | Executed 2.49 s ignored, 2.50 and 2.90 s Perfect, 3.20 s Good and 4.50 s Miss. Tested simultaneous notes, earliest eligible same-pitch selection, wrong input, a held early entry, deduplication, release, another entry and combo reset. |
| B — Results and restart | PASS | Unity Play Mode completed the full 13-note chart via the simulator. It showed 1,300 points, 13 Perfect hits and a 13-note combo. Clicking the restart event cleared the results overlay, score and progress. |
| C — Keyboard and simulator | PASS | Unity loaded the scene with serial disabled. Verified serialized A/S/D/F/G/H/J bindings and used simulator NOTE messages for all seven pitches through the central gameplay path. |
| C — Physical keyboard | NOT TESTED | The batch test did not focus a Game view and press the physical keys. |
| C — Parser and missing port | PASS | Fed valid, malformed, oversized, partial, debug and release messages. In Play Mode, attempted to open an absent COM port and confirmed the warning while the game continued. |
| C — Arduino build | PASS | Arduino CLI compiled the sketch for arduino:avr:mega. It used 5,234 bytes of program storage and 251 bytes of dynamic memory. |
| C — Arduino hardware | NOT TESTED | No sensor validation, real serial communication or USB-unplug test was performed. |
| D — Unity compile and generation | PASS | The specified Unity editor compiled and ran the scene builder, generated the scene and prefabs, and reopened the saved scene. Verified the chart, timing settings, references, prefab links, all seven audio bindings and the initial UI state. |
| D — Builder repeat run | PASS | A second batch run reported that it preserved the existing scene. The scene SHA-256 before and after the rerun matched. |
| D — Visual review | NOT TESTED | The headless test did not visually inspect rendered output at either 1920 × 1080 or 1280 × 720. |
| D — Existing project | PASS | Recomputed SHA-256 for the original SampleScene, SongManager.cs, ArduinoInput.cs and PianoAudioPlayer.cs. All four matched the values recorded before development. The original project and its sibling copy were accessed read-only. |

During setup, the first Unity compile failed to resolve System.IO.Ports under the .NET Standard profile. The delivered settings use .NET Framework and the subsequent Unity compile and Play Mode runs passed. The first Arduino CLI invocation could not discover the AVR core from its sandbox data path. The subsequent build used the installed Arduino15 data path and passed.

To reproduce the automated Unity checks, open the project and choose **Air Piano → Verify Seven-Note Demo**. The missing-clip error and missing-port warning are expected test inputs. Check the Console for the **AIR PIANO VERIFY** pass messages.


## Perspective stage update

- **PASS** — Unity 2022.3.39f1 generated and reopened the updated scene and seven StageNote prefabs. Checked note-to-lane alignment and nominal text sizes at 720p.
- **PASS** — Inspected Unity Play Mode captures rendered at 1920 × 1080 and 1280 × 720. The captured frames show distinct rails, readable pitch and key labels, and an unobstructed judgement line during a Perfect effect.
- **PASS** — Replayed the entire 13-note chart with simulator events, including audio, results and restart.
- **NOT TESTED** — This run used an independent copy and simulated input. Physical keyboard and Arduino checks were outside this visual pass.

## Glass-neon stage update

- **PASS** — Rebuilt in Unity 2022.3.39f1 and inspected real Play Mode renders at both 1920 × 1080 and 1280 × 720. Note names, keyboard captions and the judgement line remained readable in the captured hit frame.
- **PASS** — Replayed the complete 13-note chart with the serial simulator. Scene checks, playback, scoring, results and restart passed.

## Real-time 3D stage update

- **PASS** — Ran the revised scene on Unity 2022.3.39f1 with Direct3D 11 and inspected 720p and 1080p gameplay captures. The E5 Perfect frame showed the 3D key and emissive beam, while pitch names, shortcut labels and the judgement line remained visible.
- **PASS** — Completed the 13-note chart using the seven simulator inputs and checked scoring, playback, results and restart.
- **NOT TESTED** — Interactive GPU performance and physical Arduino input.

## Clear 2D stage update

- **PASS** — Unity generated the 2D stage and seven note prefabs. In Play Mode, checked that visible gameplay uses the Canvas without an active rendering Camera or RawImage stage texture.
- **PASS** — Reviewed captured 720p and 1080p frames for the outlined notes, dark labels and uninterrupted judgement line during E5 Perfect.
- **PASS** — Completed the 13-note simulator run and checked audio, scoring, results and restart.

## Vertical piano roll

- **PASS** — Unity generated and reopened the 2D piano-roll scene. Checked the track alignment with each white key, bottom-edge note anchors and minimum text sizes. Reviewed 720p and 1080p Play Mode captures.
- **PASS** — Verified that changing a bar's display duration leaves its judgement timestamp unchanged. The full 13-note simulator run passed, including scoring, audio, results and restart.

## Piano keyboard clarity

- **PASS** — Rebuilt the 2D scene with a pixel-perfect overlay Canvas and solid, sharply separated key surfaces. Reviewed the keyboard and labels in both 720p and 1080p Unity Play Mode captures.
- **PASS** — Completed the 13-note simulator run and checked audio, scoring, results and restart.

## Someone Like You

- **PASS** — Verified the 93-second mono PCM accompaniment and ten distinct playable pitch clips. Checked all 496 upper-voice target times in the rhythm model.
- **PASS** — In Unity Play Mode, selected the song from Afterglow, played the full excerpt against the DSP clock, verified that the backing played, and returned to Afterglow from the results screen. The simulator scored 49,600 with 496 Perfect hits and zero misses.
- The first automation runs used a separate time origin. The final run driven by the game clock passed.

## Someone Like You recording-based easy chart

- **PASS** — The recording plays at its original speed after removing the first 3.92 seconds, leaving 90.693 seconds. The chart has 43 single-tap cues with a 1.646-second minimum gap and an approximately 1.966-second median gap.
- **PASS** — Unity Play Mode ran the full recording, hit 43 cues with zero misses, showed the results after the backing finished, and returned to Afterglow.

## Someone Like You sampled piano arrangement

- **PASS** — Verified 90.693 seconds of sampled acoustic piano accompaniment, four matching cue pitches, and 27 playable notes with a 2.3048-second minimum gap. The final three cues are E5, C-sharp5, and A4 over D–E–A, spaced 3.3 seconds apart. The original room recording supplies analysis data only.
- **PASS** — Unity Play Mode played the new backing and playable note samples, completed all 27 cues with zero misses, displayed results, and returned to Afterglow.

## Someone Like You polished scene

- **PASS** — Duplicated the song scene with its own GUID and seven styling prefabs. Verified that the duplicate retains the original arrangement and hit timing. The original scene's SHA-256 was unchanged.
- **PASS** — Tested backing and keyboard-note playback for the first three cues in the duplicate. Captured the game at 720p and 1080p.

## Breathing flower garden

- **PASS** — Checked the flower border and the unobstructed piano roll in 720p and 1080p Unity gameplay renders.
- **PASS** — Completed all 27 cues in the polished scene. Each successful cue triggered a bloom, including all three ending cues. Results and the return to Afterglow also passed.

## Flower timing and clarity

- **PASS** — The first audio-clock cue opened the flower garden before simulated input. A hit then triggered the stronger local bloom.
- **PASS** — Compared the before/after Play Mode frames in 720p and 1080p. The pastel coverage in the side borders more than doubled, with outlined petal silhouettes.
- **PASS** — The full 27-cue run triggered a bloom for every successful cue and completed with zero misses.

## Floating flower lights

- **PASS** — Tested both flower layers on the full 27-cue run. They opened before input at the first audio cue and counted every successful hit.
- **PASS** — Reviewed the final scattered layout in 720p and 1080p captures and replayed the first three cues after refining the flower positions.

## Flower detail refinement

- **PASS** — Reviewed 720p and 1080p Play Mode captures of the three blossom shapes, petal folds and visible flower centers. The detailed meshes are limited to foreground blossoms.
- **PASS** — Verified the opening beat response, local hit bloom, accompaniment, and first three notes after the visual refinement.

## Rotating blooms

- **PASS** — Verified Play Mode visuals before the beat, just after the hit and midway through the rotating bloom at 720p and 1080p. The opening accompaniment and first three cues passed.

## Garden keyboard clarity

- **PASS** — Restored native horizontal proportions for the keyboard labels while preserving lane alignment. Verified Canvas pixel alignment, actual 720p and 1080p renders, and successful playback of the first three cues.

## Higher-resolution output

- **PASS** — Rendered the garden directly at 2560 × 1440 and 3840 × 2160 in Unity Play Mode. Verified both PNG dimensions and the opening three hits.
- Desktop default display settings target 2560 × 1440. The Windows standalone player was not built or launched for this check.

## Larger blooms and flying flowers

- **PASS** — Reviewed the larger foreground flowers, the flying flowers behind the roll, and the moving sparkles in the 1440p and 4K captures.
- **PASS** — Confirmed the opening accompaniment and first three timed hits in Unity Play Mode.

## Game view display camera

- **PASS** — The polished scene has an enabled screen camera targeting Display 1 and retaining the overlay Canvas. The opening accompaniment and first three cues passed in batch Play Mode. The interactive Game view warning itself was not observed in batch mode.
