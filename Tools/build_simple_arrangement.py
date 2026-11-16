"""Arrange the recording's sparse cues for solo play over clean sampled piano."""
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path
import json
import subprocess
import tempfile
import wave

import numpy as np
from scipy import signal

from build_someone_like_you import write_wav, RATE, LANES, PITCHES
from download_real_piano import BASE, fetch, sha

DURATION = 90.693333
DEADLINE = .34
CHORDS = {
    "A":   (45, 52, 57, 61, 64),
    "E":   (40, 52, 56, 59, 64),
    "F#m": (42, 54, 57, 61, 66),
    "D":   (38, 50, 54, 57, 62),
    "Bm":  (47, 54, 59, 62, 66),
    "C#m": (49, 52, 56, 61, 64),
}


def make_chart(root):
    input_chart = root / "Assets/AirPiano7/Data/SomeoneLikeYouEasyChart.json"
    source = json.loads(input_chart.read_text())["notes"]
    notes = []
    for note in source:
        if note["spawnBeat"] > 78.4:
            break
        if not notes or note["spawnBeat"] - notes[-1]["spawnBeat"] >= 2.22:
            notes.append(dict(note))
    for onset, pitch in [(81.5, 76), (84.8, 73), (88.1, 69)]:
        notes.append({"noteId": LANES.index(pitch % 12), "midiPitch": pitch,
                      "spawnBeat": round(onset + DEADLINE, 4), "displaySeconds": .62})
    return notes


def choose_harmony(reference, notes):
    with wave.open(str(reference), "rb") as source:
        audio = np.frombuffer(source.readframes(source.getnframes()), "<i2").reshape(-1, 2)
    audio = signal.resample_poly(audio.mean(axis=1), 1, 4)
    f, t, z = signal.stft(audio, RATE / 4, nperseg=4096, noverlap=3544, nfft=4096)
    mag = abs(z)
    pitch_class = np.zeros((12, len(t)))
    bass = np.zeros_like(pitch_class)
    for pitch in range(38, 84):
        k = round(440 * 2 ** ((pitch - 69) / 12) / f[1])
        level = mag[max(1, k - 1):k + 2].max(axis=0)
        pitch_class[pitch % 12] += level
        if pitch < 58:
            bass[pitch % 12] += level
    segments = []
    starts = [0] + [max(0, n["spawnBeat"] - DEADLINE) for n in notes[2:-3:2] if n["spawnBeat"] < 78]
    prior = "A"
    progression = {"A": "E", "E": "F#m", "F#m": "D", "D": "A", "Bm": "E", "C#m": "F#m"}
    for start, end in zip(starts, starts[1:] + [81.5]):
        rows = (t >= start) & (t < end)
        if not np.any(rows):
            continue
        chroma = pitch_class[:, rows].mean(axis=1)
        lows = bass[:, rows].mean(axis=1)
        melody = [n["midiPitch"] % 12 for n in notes if start <= n["spawnBeat"] - DEADLINE < end]

        def affinity(item):
            name, voicing = item
            pcs = {p % 12 for p in voicing}
            harmony = chroma[list(pcs)].sum() / max(1e-9, chroma.sum())
            root = lows[voicing[0] % 12] / max(1e-9, lows.sum())
            shared = sum(pitch in pcs for pitch in melody) / max(1, len(melody))
            return harmony + .28 * root + .20 * shared + .14 * (name == progression[prior]) - .12 * (name == prior)

        prior = "A" if start == 0 else max(CHORDS.items(), key=affinity)[0]
        segments.append((start, end, prior))
    return segments + [(81.5, 84.8, "D"), (84.8, 88.1, "E"), (88.1, DURATION, "A")]


def build(root):
    song_dir = root / "Assets/AirPiano7/Audio/SomeoneLikeYou"
    notes = make_chart(root)
    harmony = choose_harmony(song_dir / "RecordingEasy.wav", notes)
    source_manifest = json.loads((root / "Documentation/MetaPiano_SourceManifest.json").read_text())
    official_names = {entry["file"] for entry in source_manifest["samples"]}
    if source_manifest["license"] != "CC0-1.0":
        raise ValueError("Unexpected sample license")
    playable = sorted({n["midiPitch"] for n in notes})
    pitches = sorted(set(playable).union(pitch for _, _, chord in harmony for pitch in CHORDS[chord]))
    with tempfile.TemporaryDirectory(dir=root) as temporary:
        stage = Path(temporary)

        def acquire(pitch):
            name = f"INSPiano_Vel03_{PITCHES[pitch % 12]}{pitch // 12 - 1}_01"
            if name + ".wav" not in official_names:
                raise ValueError(f"Missing documented source for MIDI {pitch}")
            url = BASE + source_manifest["ogg_dir"] + "/" + name + ".ogg"
            original = stage / (name + ".ogg")
            fetch(url, original)
            if original.read_bytes()[:4] != b"OggS":
                raise ValueError("Invalid source recording")
            pcm = subprocess.check_output(["ffmpeg", "-v", "error", "-nostdin", "-i", str(original), "-ac", "1",
                                           "-ar", str(RATE), "-f", "f32le", "-"])
            samples = np.frombuffer(pcm, "<f4")
            if len(samples) < RATE // 2 or max(abs(samples)) < .025:
                raise ValueError(f"Quiet or incomplete source at MIDI {pitch}")
            return pitch, samples, {"midi": pitch, "ogg": url, "sha256": sha(original)}

        with ThreadPoolExecutor(max_workers=6) as pool:
            inputs = list(pool.map(acquire, pitches))
        samples = {pitch: data for pitch, data, _ in inputs}

        def voice(pitch, length, release):
            frames = min(len(samples[pitch]), round(length * RATE))
            tone = samples[pitch][:frames].copy()
            fade = min(frames, round(release * RATE))
            tone[-fade:] *= np.linspace(1, 0, fade)
            return tone

        for pitch in playable:
            write_wav(stage / f"Pitch_{pitch}.wav", voice(pitch, 1.05, .24) * .71)
        backing = np.zeros(round(DURATION * RATE), dtype=np.float32)

        def play(at, pitch, length, strength):
            offset = round(at * RATE)
            tone = voice(pitch, min(length, DURATION - at), .44)
            backing[offset:offset + len(tone)] += tone * strength

        for start, end, name in harmony:
            chord = CHORDS[name]
            if start >= 81.5:
                for i, pitch in enumerate(chord[:4]):
                    play(start + .035 * i, pitch, end - start - .15 * i, .26 if i == 0 else .14)
                continue
            for i, pitch in enumerate(chord[:4]):
                position = start + i * min(.31, (end - start) / 6)
                play(position, pitch, min(end - position + .2, 2.65), .27 if i == 0 else .12)
            if end - start >= 3.7:
                play(start + (end - start) * .57, chord[1], min(1.9, (end - start) * .43), .15)
                play(start + (end - start) * .64, chord[2], min(1.55, (end - start) * .36), .12)

        backing[-round(.7 * RATE):] *= np.linspace(1, 0, round(.7 * RATE))
        write_wav(stage / "Backing.wav", backing * (.58 / max(.58, np.max(np.abs(backing)))))
        destination = song_dir / "Arrangement"
        destination.mkdir(exist_ok=True)
        for audio in stage.glob("*.wav"):
            audio.replace(destination / audio.name)
        (root / "Assets/AirPiano7/Data/SomeoneLikeYouArrangementChart.json").write_text(
            json.dumps({"duration": DURATION, "pitches": playable, "notes": notes}, indent=2) + "\n")
        (root / "Documentation/SomeoneLikeYouArrangementSources.json").write_text(json.dumps({
            "sample_license": "CC0-1.0", "samples": [entry for _, _, entry in inputs],
            "harmony": [{"start": round(start, 4), "end": round(end, 4), "chord": chord}
                        for start, end, chord in harmony]
        }, indent=2) + "\n")
        print(f"{len(notes)} cues, {len(harmony)} harmonic segments, {len(inputs)} acoustic samples.")


if __name__ == "__main__":
    build(Path(__file__).resolve().parents[1])
