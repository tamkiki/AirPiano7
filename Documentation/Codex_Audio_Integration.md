# Codex — Audio Implementation and Provenance

Target: Unity 2022.3.39f1 on Windows.

1. Run from the Unity project root: `python Tools/download_real_piano.py` (requires FFmpeg and Internet). **Do not claim success until the downloader prints `SUCCESS`** and the manifest exists with `downloaded: true`.
2. Source: Meta Open Sound Meta Piano, 7 real individually recorded upright-piano keys (CC0 1.0), velocity layer 03. The script uses same-source Iowa recordings only if all Meta files cannot be downloaded.
3. Use existing `Assets/AirPiano7/Audio/Piano_C5.wav`, `Piano_D5.wav`, `Piano_E5.wav`, `Piano_F5.wav`, `Piano_G5.wav`, `Piano_A5.wav`, `Piano_B5.wav` as AudioClips. All WAV assets are 44,100Hz / PCM16 / mono.
4. Map note IDs 0..6 to C5, D5, E5, F5, G5, A5, B5. Store clips in editable Inspector references (AudioClip[]), and connect them via a scene-building Editor script if applicable.
5. On a successful hit call `AudioSource.PlayOneShot(clip)`. Do not play on early hit, wrong note, or Miss. Do not add a separate Arduino audio player or overwrite the old 3-note project.
6. If the download fails (no network), the seven **included** WAV files are sample-based temporary SoundFont piano audio; they are usable for UI/game tests, but NOT verified as Meta/Iowa individual recorded key sources. Report this distinction.
7. Inspect `Documentation/Audio_Real_Recordings_Manifest.json` for source and SHA256 after download. Verify all clips assigned, not `None`.
