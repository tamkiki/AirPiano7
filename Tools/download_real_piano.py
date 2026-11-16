#!/usr/bin/env python3
"""Install seven verified Meta Piano Vel03 recordings for Unity."""
import argparse
from array import array
import hashlib
import json
import math
import os
import re
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import urllib.request
import wave

NOTES = ("C5", "D5", "E5", "F5", "G5", "A5", "B5")
MIDI = (72, 74, 76, 77, 79, 81, 83)
BASE = "https://raw.githubusercontent.com/facebookincubator/Meta-open-sound/main/systems/instruments/meta-piano/"
RATE = 44100
FRAMES = 79380


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def fetch(url, path):
    request = urllib.request.Request(url, headers={"User-Agent": "AirPiano7/1.0"})
    with urllib.request.urlopen(request, timeout=30) as response, path.open("wb") as output:
        shutil.copyfileobj(response, output)


def verify(path, midi):
    with wave.open(str(path), "rb") as wav:
        if (wav.getframerate(), wav.getnchannels(), wav.getsampwidth(), wav.getnframes(),
                wav.getcomptype()) != (RATE, 1, 2, FRAMES, "NONE"):
            raise ValueError(f"{path.name}: invalid WAV header")
        samples = array("h", wav.readframes(FRAMES))
    if sys.byteorder != "little":
        samples.byteswap()
    peak = max(map(abs, samples))
    if len(samples) != FRAMES or not 1000 < peak < 32767 or abs(samples[-1]) > 8:
        raise ValueError(f"{path.name}: invalid signal, clipping or fade")
    attack = next(i for i, value in enumerate(samples) if abs(value) > peak * .025)
    if attack > RATE * .055:
        raise ValueError(f"{path.name}: attack starts late")
    # Correlate the waveform near the expected fundamental and adjacent semitones.
    signal = samples[2200:11020:3]
    scores = {}
    for offset in range(-3, 4):
        hz = 440 * 2 ** ((midi + offset - 69) / 12)
        omega = 2 * math.pi * hz * 3 / RATE
        spectrum = sum(value * complex(math.cos(i * omega), math.sin(i * omega))
                       for i, value in enumerate(signal))
        scores[offset] = abs(spectrum)
    if max(scores, key=scores.get) != 0 or scores[0] < 1.8 * max(v for k, v in scores.items() if k):
        raise ValueError(f"{path.name}: pitch verification failed")
    return {"frames": FRAMES, "peak_pcm": peak, "attack_ms": round(1000 * attack / RATE, 2),
            "pitch_midi_verified": midi, "sha256": sha(path)}


def install(project, ffmpeg, source_archive):
    docs = project / "Documentation"
    audio = project / "Assets/AirPiano7/Audio"
    manifest = docs / "Audio_Real_Recordings_Manifest.json"
    with tempfile.TemporaryDirectory(prefix=".audio-stage-", dir=project) as directory:
        stage = Path(directory)
        official = stage / "source.json"
        fetch(BASE + "manifest.json", official)
        collection = json.loads(official.read_text(encoding="utf-8"))
        if collection["id"] != "meta-piano" or collection["license"] != "CC0-1.0":
            raise ValueError("Unexpected source collection or license")
        wanted = {item["file"] for item in collection["samples"]}
        listing = []
        payload = {"verification_status": "verified", "downloaded": True,
                   "license": "CC0-1.0", "license_url": BASE + "LICENSE",
                   "collection_manifest": BASE + "manifest.json", "collection_manifest_sha256": sha(official),
                   "source_format": "OGG Vorbis previews of individual acoustic upright piano recordings",
                   "velocity_layer": "Vel03", "format": {"encoding": "PCM", "rate_hz": RATE,
                   "channels": 1, "bits": 16, "seconds": 1.8}, "files": listing}
        if source_archive:
            import zipfile
            archive = stage / "masters.zip"
            fetch(source_archive, archive)
            payload["source_format"] = "Official lossless acoustic upright piano WAV masters"
        changes = {}
        for note_id, (note, midi) in enumerate(zip(NOTES, MIDI)):
            master = f"INSPiano_Vel03_{note}_01.wav"
            if master not in wanted:
                raise ValueError(f"Source manifest is missing {master}")
            if source_archive:
                url = source_archive
                source = stage / master
                with zipfile.ZipFile(archive) as packed:
                    entry, = [name for name in packed.namelist() if Path(name).name == master]
                    source.write_bytes(packed.read(entry))
                if source.read_bytes()[:4] != b"RIFF":
                    raise ValueError(f"{master}: invalid WAV master")
            else:
                source = stage / (Path(master).stem + ".ogg")
                url = f"{BASE}{collection['ogg_dir']}/{source.name}"
                fetch(url, source)
                if source.read_bytes()[:4] != b"OggS":
                    raise ValueError(f"{source.name}: invalid OGG preview")
            output = stage / f"Piano_{note}.wav"
            subprocess.run([ffmpeg, "-nostdin", "-v", "error", "-i", str(source),
                            "-af", "volume=0.85,apad,atrim=duration=1.8,afade=t=out:st=1.5:d=0.3",
                            "-ar", str(RATE), "-ac", "1", "-c:a", "pcm_s16le", str(output)], check=True)
            listing.append({"note_id": note_id, "note": note, "midi": midi, "source_url": url,
                            "source_filename": source.name, "manifest_filename": master,
                            "source_sha256": sha(source), "output": output.name,
                            **verify(output, midi)})
            changes[audio / output.name] = output
            print(f"Verified {note}: {source.name}", flush=True)

        staged_manifest = stage / "installed.json"
        staged_manifest.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
        changes[docs / "MetaPiano_SourceManifest.json"] = official
        changes[manifest] = staged_manifest
        backup_root = project / "Audio_Backups"
        backup_root.mkdir(exist_ok=True)
        backup = Path(tempfile.mkdtemp(prefix="before-real-piano-", dir=backup_root))
        for target in changes:
            if target.exists():
                shutil.copy2(target, backup / target.name)
        installed = []
        try:
            # Remove the old success marker first. Publish the new marker last.
            if manifest.exists():
                manifest.unlink()
                installed.append(manifest)
            for target, replacement in changes.items():
                os.replace(replacement, target)
                installed.append(target)
        except BaseException:
            for target in reversed(list(dict.fromkeys(installed))):
                saved = backup / target.name
                if saved.exists():
                    shutil.copy2(saved, target)
                else:
                    target.unlink(missing_ok=True)
            raise
        print(f"Installed seven verified recordings. Backup: {backup}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=Path(__file__).resolve().parent.parent)
    parser.add_argument("--source", choices=["meta"], default="meta")
    parser.add_argument("--source-archive", help="Official Meta Piano release ZIP URL for lossless masters")
    args = parser.parse_args()
    if args.source_archive and not re.fullmatch(
        r"https://github\.com/facebookincubator/Meta-open-sound/releases/download/"
        r"[A-Za-z0-9._-]+/meta-open-sound-meta-piano-wav\.zip", args.source_archive
    ):
        parser.error("--source-archive must be an official Meta Piano WAV release asset")
    ffmpeg = shutil.which("ffmpeg")
    if not ffmpeg:
        parser.error("FFmpeg is required on PATH")
    try:
        install(args.project.resolve(), ffmpeg, args.source_archive)
    except Exception as exc:
        parser.exit(1, f"Recording installation failed: {exc}\n")


if __name__ == "__main__":
    main()

