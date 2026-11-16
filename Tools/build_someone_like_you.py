"""Build the seven-key playable chart and piano backing for the first 93 seconds."""
import argparse
from concurrent.futures import ThreadPoolExecutor
import json
from pathlib import Path
import subprocess
import tempfile
import wave

import numpy as np

from download_real_piano import BASE, fetch, sha
from read_midi import read_midi

RATE = 44100
LIMIT = 93
LANES = (9, 11, 1, 2, 4, 6, 8)
PITCHES = ("C", "Cs", "D", "Ds", "E", "F", "Fs", "G", "Gs", "A", "As", "B")



def write_wav(path, data):
    with wave.open(str(path), "wb") as out:
        out.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        out.writeframes((np.clip(data, -.98, .98) * 32767).astype("<i2").tobytes())


def prepare(midi, root):
    notes, _, _ = read_midi(midi)
    upper = [n for n in notes if n[0] < LIMIT and n[5] == 1]
    lower = [n for n in notes if n[0] < LIMIT and n[5] == 2]
    if not upper or not lower or any(n[2] % 12 not in LANES for n in upper):
        raise ValueError("Input does not match this two-voice A major arrangement")
    manifest = json.loads((root / "Documentation/MetaPiano_SourceManifest.json").read_text())
    names = {entry["file"] for entry in manifest["samples"]}
    if manifest["license"] != "CC0-1.0":
        raise ValueError("Unexpected source license")

    with tempfile.TemporaryDirectory(dir=root) as folder:
        stage = Path(folder)

        def acquire(pitch):
            name = f"INSPiano_Vel03_{PITCHES[pitch % 12]}{pitch // 12 - 1}_01"
            if name + ".wav" not in names:
                raise ValueError(f"Missing Meta Piano source entry for {pitch}")
            url = BASE + manifest["ogg_dir"] + "/" + name + ".ogg"
            ogg = stage / (name + ".ogg")
            fetch(url, ogg)
            if ogg.read_bytes()[:4] != b"OggS":
                raise ValueError(f"Invalid audio download for {pitch}")
            pcm = subprocess.check_output(["ffmpeg", "-v", "error", "-nostdin", "-i", str(ogg),
                                           "-ac", "1", "-ar", str(RATE), "-f", "f32le", "-"])
            signal = np.frombuffer(pcm, dtype="<f4")
            if len(signal) < RATE // 2 or np.max(np.abs(signal)) < .03:
                raise ValueError(f"Invalid sample at pitch {pitch}")
            return pitch, signal, {"midi": pitch, "ogg": url, "sha256": sha(ogg)}

        pitches = sorted({n[2] for n in upper + lower})
        with ThreadPoolExecutor(max_workers=6) as pool:
            recordings = list(pool.map(acquire, pitches))
        samples = {pitch: data for pitch, data, _ in recordings}
        playable = {n[2] for n in upper}
        for pitch in sorted(playable):
            clip = samples[pitch][:round(.40 * RATE)].copy()
            fade = min(len(clip), round(.13 * RATE))
            clip[-fade:] *= np.linspace(1, 0, fade)
            write_wav(stage / f"Pitch_{pitch}.wav", clip * .74)

        mix = np.zeros(RATE * LIMIT, dtype=np.float32)
        for start, end, pitch, velocity, _, _ in lower:
            offset = round(start * RATE)
            length = min(round((end - start + .22) * RATE), len(samples[pitch]), len(mix) - offset)
            if length <= 0:
                continue
            signal = samples[pitch][:length].copy()
            release = min(length, round(.24 * RATE))
            signal[-release:] *= np.linspace(1, 0, release)
            mix[offset:offset + length] += signal * (.45 * velocity / 100)
        mix[-RATE // 5:] *= np.linspace(1, 0, RATE // 5)
        peak = float(np.max(np.abs(mix)))
        if peak > .72:
            mix *= .72 / peak
        write_wav(stage / "Backing.wav", mix)

        chart = {"duration": LIMIT, "pitches": sorted(playable), "notes": [
            {"noteId": LANES.index(pitch % 12), "spawnBeat": round(start + .08, 6),
             "displaySeconds": round(min(end, LIMIT) - start, 6), "midiPitch": pitch}
            for start, end, pitch, _, _, _ in upper]}
        (stage / "SomeoneLikeYouChart.json").write_text(json.dumps(chart, indent=2) + "\n")
        audio = root / "Assets/AirPiano7/Audio/SomeoneLikeYou"
        audio.mkdir(parents=True, exist_ok=True)
        for wav in stage.glob("*.wav"):
            wav.replace(audio / wav.name)
        data = root / "Assets/AirPiano7/Data/SomeoneLikeYouChart.json"
        (stage / data.name).replace(data)
        provenance = {"midi": midi.name, "midi_sha256": sha(midi), "clip_seconds": LIMIT,
                      "playable_notes": len(upper), "backing_notes": len(lower),
                      "sample_license": "CC0-1.0", "samples": [entry for _, _, entry in recordings]}
        (root / "Documentation/SomeoneLikeYouSources.json").write_text(json.dumps(provenance, indent=2) + "\n")
        print(f"Prepared {len(upper)} playable notes, {len(lower)} backing notes and {len(recordings)} source recordings.")


if __name__ == "__main__":
    args = argparse.ArgumentParser(description=__doc__)
    args.add_argument("midi", type=Path)
    prepare(args.parse_args().midi, Path(__file__).resolve().parents[1])

